using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using Microsoft.Xna.Framework;

namespace Blasphemy.WorldGeneration.FossilArena
{
    public class DesertCaveSystem : ModSystem
    {
        public static LocalizedText DesertCavePassMessage { get; private set; }

        public override void SetStaticDefaults()
        {
            DesertCavePassMessage = Language.GetOrRegister(Mod.GetLocalizationKey($"WorldGen.{nameof(DesertCavePassMessage)}"));
        }

        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            int microBiomesIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Micro Biomes"));
            
            if (microBiomesIndex != -1)
            {
                tasks.Insert(microBiomesIndex + 1, new DesertCaveGenPass("Desert Cave Generation", 100f));
            }
            else
            {
                int desertIndex = tasks.FindIndex(genpass => 
                    genpass.Name.Contains("Desert") || 
                    genpass.Name.Contains("Pots") ||
                    genpass.Name.Equals("Shinies"));
                    
                if (desertIndex != -1)
                {
                    tasks.Insert(desertIndex + 1, new DesertCaveGenPass("Desert Cave Generation", 100f));
                }
                else
                {
                    tasks.Add(new DesertCaveGenPass("Desert Cave Generation", 100f));
                }
            }
        }
    }

    public class DesertCaveGenPass(string name, float loadWeight) : GenPass(name, loadWeight)
    {
        protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
        {
            progress.Message = DesertCaveSystem.DesertCavePassMessage?.Value ?? "Generating Desert Caves...";

            int caveCount = Main.maxTilesX switch
            {
                <= 4200 => 1,
                <= 6400 => 1,
                _ => 2
            };

            for (int i = 0; i < caveCount; i++)
            {
                progress.Set(i / (float)caveCount);
                GenerateSingleDesertCave();
            }
        }

        private void GenerateSingleDesertCave()
        {
            Rectangle desertRect = GenVars.UndergroundDesertLocation;
            
            if (desertRect.Width < 100 || desertRect.Height < 50)
            {
                desertRect = FindDesertBiome();
                if (desertRect.Width < 100 || desertRect.Height < 50) return;
            }
            
            int attempts = 0;
            const int maxAttempts = 3000;
            
            int marginX = 80;
            int marginY = 60;
            
            int minDesertX = desertRect.X + marginX;
            int maxDesertX = desertRect.X + desertRect.Width - marginX;
            int minDesertY = desertRect.Y + marginY;
            int maxDesertY = desertRect.Y + desertRect.Height - marginY;
            
            if (maxDesertX <= minDesertX || maxDesertY <= minDesertY)
            {
                minDesertX = desertRect.X + 30;
                maxDesertX = desertRect.X + desertRect.Width - 30;
                minDesertY = desertRect.Y + 30;
                maxDesertY = desertRect.Y + desertRect.Height - 30;
            }
            
            while (attempts < maxAttempts)
            {
                attempts++;
                
                int x = WorldGen.genRand.Next(minDesertX, maxDesertX);
                int y = WorldGen.genRand.Next(minDesertY, maxDesertY);
                
                int width = WorldGen.genRand.Next(120, 180);
                int height = WorldGen.genRand.Next(80, 120);

                if (IsValidLocation(x, y, width, height))
                {
                    GenerateDesertCave(x, y, width, height);
                    return;
                }
            }
        }

        private Rectangle FindDesertBiome()
        {
            int sandCount = 0;
            int startX = Main.maxTilesX / 4;
            int endX = Main.maxTilesX * 3 / 4;
            int startY = (int)Main.worldSurface;
            int endY = Main.maxTilesY - 200;

            for (int i = startX; i < endX; i += 10)
            {
                for (int j = startY; j < endY; j += 10)
                {
                    if (!WorldGen.InWorld(i, j))
                        continue;
                        
                    Tile tile = Main.tile[i, j];
                    if (tile.HasTile && (tile.TileType == TileID.Sand || 
                        tile.TileType == TileID.Sandstone || 
                        tile.TileType == TileID.HardenedSand))
                    {
                        sandCount++;
                    }
                }
            }

            if (sandCount > 100)
            {
                return new Rectangle(startX + 100, startY + 100, 600, 400);
            }

            return Rectangle.Empty;
        }

        private bool IsValidLocation(int centerX, int centerY, int width, int height)
        {
            if (!WorldGen.InWorld(centerX, centerY, 100))
                return false;
            
            Rectangle caveRect = new Rectangle(
                centerX - width / 2 - 20,
                centerY - height / 2 - 20,
                width + 40,
                height + 40
            );
            
            if (!GenVars.structures.CanPlace(caveRect))
                return false;

            const int searchRadius = 40;
            int startX = Math.Max(0, centerX - searchRadius);
            int endX = Math.Min(Main.maxTilesX - 1, centerX + searchRadius);
            int startY = Math.Max(0, centerY - searchRadius);
            int endY = Math.Min(Main.maxTilesY - 1, centerY + searchRadius);

            int structureCount = 0;

            for (int i = startX; i <= endX; i += 2)
            {
                for (int j = startY; j <= endY; j += 2)
                {
                    if (!WorldGen.InWorld(i, j))
                        continue;
                        
                    Tile tile = Main.tile[i, j];

                    if (tile.HasTile && IsCriticalStructureBlock(tile.TileType))
                    {
                        structureCount += 50;
                        if (structureCount > 100)
                            return false;
                    }

                    if (tile.WallType != WallID.None && !IsNaturalWall(tile.WallType))
                    {
                        structureCount += 5;
                    }
                }
            }

            return structureCount <= 100;
        }

        private void GenerateDesertCave(int centerX, int centerY, int width, int height)
        {
            int shellThickness = WorldGen.genRand.Next(6, 10);
            float floorFlatness = WorldGen.genRand.NextFloat(0.6f, 0.9f);
            int spacing = height / 4;
            int caveTop = centerY - height / 2;
            int caveBottom = centerY + height / 2;
            
            int minX = Math.Max(0, centerX - width / 2 - shellThickness - 20);
            int maxX = Math.Min(Main.maxTilesX - 1, centerX + width / 2 + shellThickness + 20);
            int minY = Math.Max(0, centerY - height / 2 - shellThickness - 20);
            int maxY = Math.Min(Main.maxTilesY - 1, centerY + height / 2 + shellThickness + 20);

            float halfWidth = width / 2f;
            float halfHeight = height / 2f;
            float halfWidthOuter = halfWidth + shellThickness;
            float halfHeightOuter = halfHeight + shellThickness;
            
            for (int i = minX; i <= maxX; i++)
            {
                for (int j = minY; j <= maxY; j++)
                {
                    if (!WorldGen.InWorld(i, j))
                        continue;
                        
                    Tile tile = Main.tile[i, j];
                    
                    if (tile.WallType != WallID.None && !IsNaturalWall(tile.WallType))
                        continue;
                    
                    if (tile.HasTile && IsCriticalStructureBlock(tile.TileType))
                        continue;

                    float dx = i - centerX;
                    float dy = j - centerY;
                    
                    float xNormOuterSq = (dx * dx) / (halfWidthOuter * halfWidthOuter);
                    float yNormOuterSq = (dy * dy) / (halfHeightOuter * halfHeightOuter);
                    float xNormInnerSq = (dx * dx) / (halfWidth * halfWidth);
                    float yNormInnerSq = (dy * dy) / (halfHeight * halfHeight);

                    bool isInsideOuter;
                    if (dy > 0)
                    {
                        float maxYSq = Math.Max(0, 1 - xNormOuterSq);
                        float ellipseMaxY = (float)Math.Sqrt(maxYSq) * (1 - floorFlatness);
                        isInsideOuter = (dy / halfHeightOuter) <= ellipseMaxY;
                    }
                    else
                    {
                        isInsideOuter = (xNormOuterSq + yNormOuterSq) <= 1f;
                    }

                    bool isInsideInner;
                    if (dy > 0)
                    {
                        float maxYSq = Math.Max(0, 1 - xNormInnerSq);
                        float ellipseMaxY = (float)Math.Sqrt(maxYSq) * (1 - floorFlatness);
                        isInsideInner = (dy / halfHeight) <= ellipseMaxY;
                    }
                    else
                    {
                        isInsideInner = (xNormInnerSq + yNormInnerSq) <= 1f;
                    }

                    if (isInsideOuter && !isInsideInner)
                    {
                        if (tile.WallType == WallID.None || !IsNaturalWall(tile.WallType))
                        {
                            tile.WallType = WallID.Sandstone;
                        }

                        if (!tile.HasTile || tile.TileType != TileID.DesertFossil)
                        {
                            WorldGen.PlaceTile(i, j, TileID.DesertFossil, mute: true, forced: true, style: 0);
                        }
                    }
                }
            }

            for (int i = minX; i <= maxX; i++)
            {
                for (int j = minY; j <= maxY; j++)
                {
                    if (!WorldGen.InWorld(i, j))
                        continue;
                        
                    Tile tile = Main.tile[i, j];
                    
                    if (tile.WallType != WallID.None && !IsNaturalWall(tile.WallType))
                        continue;

                    if (tile.HasTile && IsCriticalStructureBlock(tile.TileType))
                        continue;

                    float dx = i - centerX;
                    float dy = j - centerY;
                    
                    float xNormInnerSq = (dx * dx) / (halfWidth * halfWidth);
                    float yNormInnerSq = (dy * dy) / (halfHeight * halfHeight);

                    bool isInsideInner;
                    if (dy > 0)
                    {
                        float maxYSq = Math.Max(0, 1 - xNormInnerSq);
                        float ellipseMaxY = (float)Math.Sqrt(maxYSq) * (1 - floorFlatness);
                        isInsideInner = (dy / halfHeight) <= ellipseMaxY;
                    }
                    else
                    {
                        isInsideInner = (xNormInnerSq + yNormInnerSq) <= 1f;
                    }

                    // ИНТЕРЬЕР - очищаем и ставим стену пустыни
                    if (isInsideInner)
                    {
                        if (tile.HasTile && !IsCriticalStructureBlock(tile.TileType))
                        {
                            WorldGen.KillTile(i, j, noItem: true);
                        }
                        if (tile.WallType == WallID.None || !IsNaturalWall(tile.WallType))
                        {
                            tile.WallType = WallID.Sandstone;
                        }
                    }
                }
            }
            
            for (int stripY = caveTop + spacing; stripY <= caveBottom - spacing; stripY += spacing)
            {
                for (int i = minX; i <= maxX; i++)
                {
                    for (int j = stripY - 1; j <= stripY + 1; j++)
                    {
                        if (!WorldGen.InWorld(i, j) || j < minY || j > maxY) 
                            continue;
                        
                        Tile tile = Main.tile[i, j];
                        if (tile.WallType != WallID.None && !IsNaturalWall(tile.WallType) && tile.WallType != WallID.Sandstone)
                            continue;

                        float dx = i - centerX;
                        float dy = j - centerY;
                        float xNormSq = (dx * dx) / (halfWidth * halfWidth);
                        float yNormSq = (dy * dy) / (halfHeight * halfHeight);

                        bool isInside;
                        if (dy > 0)
                        {
                            float maxYSq = Math.Max(0, 1 - xNormSq);
                            float ellipseMaxY = (float)Math.Sqrt(maxYSq) * (1 - floorFlatness);
                            isInside = (dy / halfHeight) <= ellipseMaxY;
                        }
                        else
                        {
                            isInside = (xNormSq + yNormSq) <= 1f;
                        }

                        if (isInside)
                        {
                            tile.WallType = WallID.DesertFossil;
                        }
                    }
                }
            }

            // ФАЗА 4: Декорации внутри пещеры
            int decorationCount = WorldGen.genRand.Next(10, 20);
            for (int d = 0; d < decorationCount; d++)
            {
                int i = centerX + WorldGen.genRand.Next(-width / 2 + 10, width / 2 - 10);
                int j = centerY + WorldGen.genRand.Next(-height / 2 + 10, height / 2 - 10);

                if (!WorldGen.InWorld(i, j) || !WorldGen.InWorld(i, j + 1))
                    continue;

                Tile tile = Main.tile[i, j];
                Tile tileBelow = Main.tile[i, j + 1];

                if (!tile.HasTile && tileBelow.HasTile && WorldGen.genRand.NextBool(3))
                {
                    WorldGen.PlaceTile(i, j, TileID.DesertFossil, mute: true, forced: false, style: WorldGen.genRand.Next(4));
                }
            }
            
            Rectangle finalCaveRect = new Rectangle(
                centerX - width / 2 - 10,
                centerY - height / 2 - 10,
                width + 20,
                height + 20
            );
            GenVars.structures.AddStructure(finalCaveRect, 4);
        }

        private bool IsNaturalWall(int wallType)
        {
            return wallType == WallID.Sandstone ||
                   wallType == WallID.HardenedSand ||
                   wallType == WallID.CorruptSandstone ||
                   wallType == WallID.CrimsonSandstone ||
                   wallType == WallID.HallowSandstone ||
                   wallType == WallID.DesertFossil;
        }

        private bool IsCriticalStructureBlock(int tileType)
        {
            return tileType is TileID.SandstoneBrick or TileID.Containers2 ||
                   tileType == TileID.Containers ||
                   tileType == TileID.Hive ||
                   tileType == TileID.CrispyHoneyBlock ||
                   tileType == TileID.Larva ||
                   tileType == TileID.Traps ||
                   tileType == TileID.Boulder ||
                   tileType == TileID.Switches;
        }
    }
}