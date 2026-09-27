using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;

namespace Blasphemy.WorldGeneration.FossilArena
{
    public class DesertCaveSystem : ModSystem
    {
        public static LocalizedText DesertCavePassMessage { get; private set; }

        // The first generated cave is the canonical fossil boss arena.
        public static Rectangle FossilArenaBounds { get; private set; } = Rectangle.Empty;
        public static bool HasFossilArena => FossilArenaBounds.Width > 0 && FossilArenaBounds.Height > 0;
        public static Vector2 FossilArenaCenter => HasFossilArena
            ? new Vector2(FossilArenaBounds.Center.X, FossilArenaBounds.Center.Y)
            : Vector2.Zero;

        public static void RegisterFossilArena(Rectangle bounds)
        {
            if (!HasFossilArena)
                FossilArenaBounds = bounds;
        }

        public override void ClearWorld()
        {
            FossilArenaBounds = Rectangle.Empty;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (!HasFossilArena)
                return;

            tag["FossilArenaX"] = FossilArenaBounds.X;
            tag["FossilArenaY"] = FossilArenaBounds.Y;
            tag["FossilArenaWidth"] = FossilArenaBounds.Width;
            tag["FossilArenaHeight"] = FossilArenaBounds.Height;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            if (!tag.ContainsKey("FossilArenaX"))
            {
                FossilArenaBounds = Rectangle.Empty;
                return;
            }

            FossilArenaBounds = new Rectangle(
                tag.GetInt("FossilArenaX"),
                tag.GetInt("FossilArenaY"),
                tag.GetInt("FossilArenaWidth"),
                tag.GetInt("FossilArenaHeight")
            );
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(FossilArenaBounds.X);
            writer.Write(FossilArenaBounds.Y);
            writer.Write(FossilArenaBounds.Width);
            writer.Write(FossilArenaBounds.Height);
        }

        public override void NetReceive(BinaryReader reader)
        {
            FossilArenaBounds = new Rectangle(
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32()
            );
        }

        public override void SetStaticDefaults()
        {
            DesertCavePassMessage =
                Language.GetOrRegister(Mod.GetLocalizationKey($"WorldGen.{nameof(DesertCavePassMessage)}"));
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
                desertRect = GenVars.UndergroundDesertHiveLocation;
                if (desertRect.Width < 100 || desertRect.Height < 50)
                    desertRect = FindDesertBiome();
            }

            const int maxAttempts = 500;
            int bestPenalty = int.MaxValue;
            int bestX = 0;
            int bestY = 0;
            int bestWidth = 0;
            int bestHeight = 0;

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

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int x = maxDesertX > minDesertX
                    ? WorldGen.genRand.Next(minDesertX, maxDesertX)
                    : desertRect.Center.X;
                int y = maxDesertY > minDesertY
                    ? WorldGen.genRand.Next(minDesertY, maxDesertY)
                    : desertRect.Center.Y;

                int width = WorldGen.genRand.Next(120, 180);
                int height = WorldGen.genRand.Next(80, 120);

                int penalty = GetLocationPenalty(x, y, width, height);
                if (penalty == 0)
                {
                    GenerateDesertCave(x, y, width, height);
                    return;
                }

                if (penalty < bestPenalty)
                {
                    bestPenalty = penalty;
                    bestX = x;
                    bestY = y;
                    bestWidth = width;
                    bestHeight = height;
                }
            }

            // A cave is required even when the desert contains too many protected tiles
            // for a completely clean placement. Use the least obstructed candidate.
            if (bestPenalty != int.MaxValue)
            {
                GenerateDesertCave(bestX, bestY, bestWidth, bestHeight);
                return;
            }

            int fallbackX = Math.Clamp(desertRect.Center.X, 100, Main.maxTilesX - 100);
            int fallbackY = Math.Clamp(desertRect.Center.Y, 100, Main.maxTilesY - 100);
            GenerateDesertCave(fallbackX, fallbackY, 120, 80);
        }

        private Rectangle FindDesertBiome()
        {
            int bestScore = -1;
            int bestX = Main.maxTilesX / 2;
            int bestY = (int)Main.worldSurface + 200;

            for (int x = Main.maxTilesX / 10; x < Main.maxTilesX * 9 / 10; x += 80)
            {
                for (int y = (int)Main.worldSurface + 80; y < Main.maxTilesY - 180; y += 80)
                {
                    int score = 0;
                    for (int dx = -40; dx <= 40; dx += 20)
                    {
                        for (int dy = -40; dy <= 40; dy += 20)
                        {
                            Tile tile = Main.tile[x + dx, y + dy];
                            if (tile.HasTile && tile.TileType is
                                TileID.Sand or TileID.Sandstone or TileID.HardenedSand or TileID.DesertFossil)
                                score++;
                        }
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestX = x;
                        bestY = y;
                    }
                }
            }

            return new Rectangle(
                Math.Clamp(bestX - 200, 100, Main.maxTilesX - 500),
                Math.Clamp(bestY - 140, 100, Main.maxTilesY - 380),
                400, 280);
        }

        private int GetLocationPenalty(int centerX, int centerY, int width, int height)
        {
            if (!WorldGen.InWorld(centerX, centerY, 100))
                return int.MaxValue;

            // Include the thickest possible fossil shell and a buffer around structures.
            const int placementMargin = 25;
            Rectangle caveRect = new Rectangle(
                centerX - width / 2 - placementMargin,
                centerY - height / 2 - placementMargin,
                width + placementMargin * 2,
                height + placementMargin * 2
            );

            if (caveRect.Left < 10 || caveRect.Top < 10 ||
                caveRect.Right >= Main.maxTilesX - 10 || caveRect.Bottom >= Main.maxTilesY - 10)
                return int.MaxValue;

            int penalty = GenVars.structures.CanPlace(caveRect, 8) ? 0 : 1000;

            int startX = Math.Max(0, caveRect.Left);
            int endX = Math.Min(Main.maxTilesX - 1, caveRect.Right - 1);
            int startY = Math.Max(0, caveRect.Top);
            int endY = Math.Min(Main.maxTilesY - 1, caveRect.Bottom - 1);

            for (int i = startX; i <= endX; i += 2)
            {
                for (int j = startY; j <= endY; j += 2)
                {
                    if (!WorldGen.InWorld(i, j))
                        continue;

                    Tile tile = Main.tile[i, j];

                    if (tile.HasTile && IsCriticalStructureBlock(tile.TileType))
                        penalty += 200;

                    if (tile.WallType != WallID.None && !IsNaturalWall(tile.WallType))
                        penalty++;
                }
            }

            return penalty;
        }

    private void GenerateDesertCave(int centerX, int centerY, int width, int height)
    {
        int shellThickness = WorldGen.genRand.Next(9, 14);
        int spacing = height / 4;
        int caveTop = centerY - height / 2;
        int caveBottom = centerY + height / 2;

        int minX = Math.Max(0, centerX - width / 2 - shellThickness - 20);
        int maxX = Math.Min(Main.maxTilesX - 1, centerX + width / 2 + shellThickness + 20);
        int minY = Math.Max(0, centerY - height / 2 - shellThickness - 20);
        int maxY = Math.Min(Main.maxTilesY - 1, centerY + height / 2 + shellThickness + 20);

        int totalColumns = maxX - minX + 1;
        int totalRows = maxY - minY + 1;
        bool[,] interior = new bool[totalColumns, totalRows];
        bool[,] shell = new bool[totalColumns, totalRows];
        int left = centerX - width / 2;
        int right = centerX + width / 2;
        float halfHeight = height / 2f;
        float peak = WorldGen.genRand.NextFloat(0.38f, 0.62f);
        float leftEdge = centerY + halfHeight * WorldGen.genRand.NextFloat(0.05f, 0.2f);
        float rightEdge = centerY + halfHeight * WorldGen.genRand.NextFloat(0.05f, 0.2f);
        int noiseStep = WorldGen.genRand.Next(10, 16);
        int noiseCount = width / noiseStep + 2;
        float[] topNoise = new float[noiseCount];
        float[] bottomNoise = new float[noiseCount];
        for (int n = 0; n < noiseCount; n++)
        {
            topNoise[n] = WorldGen.genRand.NextFloat(-0.12f, 0.12f) * halfHeight;
            bottomNoise[n] = WorldGen.genRand.NextFloat(-0.065f, 0.065f) * halfHeight;
        }

        // A high, off-center crown and a broad, almost flat floor form one cave silhouette.
        // Independent random control points break up both edges without periodic waves.
        for (int i = left; i <= right; i++)
        {
            int localX = i - left;
            float t = localX / (float)width;
            float edgeY = MathHelper.Lerp(leftEdge, rightEdge, t);
            float rise = t < peak ? t / peak : (1f - t) / (1f - peak);
            rise = (float)Math.Pow(MathHelper.Clamp(rise, 0f, 1f), 0.72);
            float noiseFade = Math.Min(1f, rise * 2f);
            float topY = edgeY - halfHeight * rise;
            float bottomY = edgeY + halfHeight * 0.78f * Math.Min(1f, rise * 3f);
            topY += InterpolateCaveNoise(topNoise, localX, noiseStep) * noiseFade;
            bottomY += InterpolateCaveNoise(bottomNoise, localX, noiseStep) * noiseFade;

            int top = Math.Max(minY, (int)Math.Ceiling(topY));
            int bottom = Math.Min(maxY, Math.Max(top, (int)Math.Floor(bottomY)));
            int column = i - minX;
            for (int j = top; j <= bottom; j++)
                interior[column, j - minY] = true;

            // Circular dilation gives the same shell thickness at the roof, floor and sides.
            for (int dx = -shellThickness; dx <= shellThickness; dx++)
            {
                int outerColumn = column + dx;
                if (outerColumn < 0 || outerColumn >= totalColumns)
                    continue;
                int verticalReach = (int)Math.Sqrt(shellThickness * shellThickness - dx * dx);
                for (int j = Math.Max(minY, top - verticalReach);
                     j <= Math.Min(maxY, bottom + verticalReach); j++)
                    shell[outerColumn, j - minY] = true;
            }
        }

        // PHASE 1: Turn natural terrain and gaps around the silhouette into a fossil shell.
        for (int i = minX; i <= maxX; i++)
        {
            for (int j = minY; j <= maxY; j++)
            {
                if (!shell[i - minX, j - minY] || interior[i - minX, j - minY] ||
                    !WorldGen.InWorld(i, j))
                    continue;

                Tile tile = Main.tile[i, j];

                if (tile.WallType != WallID.None && !IsNaturalWall(tile.WallType))
                    continue;

                if (tile.HasTile && IsCriticalStructureBlock(tile.TileType))
                    continue;

                tile.HasTile = true;
                tile.TileType = TileID.DesertFossil;
                tile.Slope = SlopeType.Solid;
                tile.IsHalfBlock = false;
                tile.TileFrameX = 0;
                tile.TileFrameY = 0;
                tile.LiquidAmount = 0;
                tile.IsActuated = false;
                tile.WallType = WallID.Sandstone;
            }
        }

        // PHASE 2: Clear the cave interior.
        for (int i = minX; i <= maxX; i++)
        {
            for (int j = minY; j <= maxY; j++)
            {
                if (!interior[i - minX, j - minY] || !WorldGen.InWorld(i, j))
                    continue;

                Tile tile = Main.tile[i, j];

                if (tile.WallType != WallID.None && !IsNaturalWall(tile.WallType))
                    continue;

                if (tile.HasTile && IsCriticalStructureBlock(tile.TileType))
                    continue;

                if (tile.HasTile)
                {
                    WorldGen.KillTile(i, j, noItem: true);
                }

                tile.LiquidAmount = 0;
                tile.WallType = WallID.Sandstone;
            }
        }

        // PHASE 3: Add wavy wall bands that follow the deformed cave shape.
        for (int stripY = caveTop + spacing; stripY <= caveBottom - spacing; stripY += spacing)
        {
            for (int i = minX; i <= maxX; i++)
            {
                // Smooth, visible vertical wave offset for each band.
                int yOffset = (int)(Math.Sin(i * 0.15f) * 2.5f + Math.Sin(i * 0.3f) * 1f);

                // Main band: five tiles thick.
                for (int j = stripY - 2 + yOffset; j <= stripY + 2 + yOffset; j++)
                {
                    if (!WorldGen.InWorld(i, j) || j < minY || j > maxY)
                        continue;

                    Tile tile = Main.tile[i, j];
                    if (tile.WallType != WallID.None && !IsNaturalWall(tile.WallType) &&
                        tile.WallType != WallID.Sandstone)
                        continue;

                    if (interior[i - minX, j - minY])
                    {
                        tile.WallType = WallID.DesertFossil;
                    }
                }

                // Scatter extra fossil wall tiles around the main band.
                for (int scatterOffset = 3; scatterOffset <= 5; scatterOffset++)
                {
                    int upperY = stripY - scatterOffset + yOffset;
                    if (WorldGen.InWorld(i, upperY) && upperY >= minY && upperY <= maxY)
                    {
                        if (WorldGen.genRand.NextBool(4))
                        {
                            Tile tile = Main.tile[i, upperY];
                            if (tile.WallType == WallID.None || IsNaturalWall(tile.WallType))
                            {
                                if (interior[i - minX, upperY - minY])
                                {
                                    tile.WallType = WallID.DesertFossil;
                                }
                            }
                        }
                    }

                    int lowerY = stripY + scatterOffset + yOffset;
                    if (WorldGen.InWorld(i, lowerY) && lowerY >= minY && lowerY <= maxY)
                    {
                        if (WorldGen.genRand.NextBool(4))
                        {
                            Tile tile = Main.tile[i, lowerY];
                            if (tile.WallType == WallID.None || IsNaturalWall(tile.WallType))
                            {
                                if (interior[i - minX, lowerY - minY])
                                {
                                    tile.WallType = WallID.DesertFossil;
                                }
                            }
                        }
                    }
                }
            }
        }

        // PHASE 4: Add decorations inside valid cave areas.
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
                WorldGen.PlaceTile(i, j, TileID.DesertFossil, mute: true, forced: false,
                    style: WorldGen.genRand.Next(4));
            }
        }

        // Reframe the fossil blocks after the neighboring interior has been cleared.
        for (int i = minX; i <= maxX; i++)
        {
            for (int j = minY; j <= maxY; j++)
            {
                if (shell[i - minX, j - minY] && !interior[i - minX, j - minY] &&
                    WorldGen.InWorld(i, j) && Main.tile[i, j].HasTile &&
                    Main.tile[i, j].TileType == TileID.DesertFossil)
                    WorldGen.SquareTileFrame(i, j);
            }
        }

        Rectangle finalCaveRect = new Rectangle(
            centerX - width / 2 - 10,
            centerY - height / 2 - 10,
            width + 20,
            height + 20
        );
        Rectangle protectedShellRect = new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        GenVars.structures.AddProtectedStructure(protectedShellRect, 4);
        // Generation rectangles use tile coordinates; combat code works in world pixels.
        Rectangle arenaWorldBounds = new Rectangle(
            finalCaveRect.X * 16,
            finalCaveRect.Y * 16,
            finalCaveRect.Width * 16,
            finalCaveRect.Height * 16
        );
        DesertCaveSystem.RegisterFossilArena(arenaWorldBounds);
    }

        private static float InterpolateCaveNoise(float[] nodes, int position, int step)
        {
            int index = Math.Min(position / step, nodes.Length - 2);
            float t = (position - index * step) / (float)step;
            t = t * t * (3f - 2f * t);
            return MathHelper.Lerp(nodes[index], nodes[index + 1], t);
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
