using System;
using System.Collections.Generic;
using Blasphemy.Config;
using Blasphemy.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace Blasphemy.Systems
{
    [Autoload(Side = ModSide.Client)]
    public sealed class PainUISystem : ModSystem
    {
        private const float DefaultPosX = 50f;
        private const float DefaultPosY = 85f;
        private const float MouseDragEpsilon = 0.05f;
        private const float BaseSpriteScale = 1f;
        private const int FrameCount = 4;
        private const int TicksPerFrame = 20;
        private const int FillLeft = 22;
        private const int FillTop = 8;
        private const int FillSourceLeft = 3;
        private const int FillChannelWidth = 12;

        private static Vector2? _dragOffset;
        private static Texture2D _barFillTexture;
        private static Texture2D _barFrameTexture;

        public override void OnModLoad()
        {
            _barFillTexture = ModContent.Request<Texture2D>("Blasphemy/Assets/Textures/UI/PainBarFill", AssetRequestMode.ImmediateLoad).Value;
            _barFrameTexture = ModContent.Request<Texture2D>("Blasphemy/Assets/Textures/UI/PainBarFrame", AssetRequestMode.ImmediateLoad).Value;
        }

        public override void Unload()
        {
            _dragOffset = null;
            _barFillTexture = _barFrameTexture = null;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex + 1, new LegacyGameInterfaceLayer(
                    "Blasphemy: Pain Bar",
                    () => { Draw(Main.spriteBatch, Main.LocalPlayer); return true; },
                    InterfaceScaleType.UI)
                );
            }
        }

        public static void Draw(SpriteBatch spriteBatch, Player player)
        {
            var config = BlasphemyConfig.Instance;
            var bp = player.GetModPlayer<BlasphemyPlayer>();

            Vector2 screenRatio = new Vector2(config.PainBarPosX, config.PainBarPosY);
            if (screenRatio.X < 0f || screenRatio.X > 100f) screenRatio.X = DefaultPosX;
            if (screenRatio.Y < 0f || screenRatio.Y > 100f) screenRatio.Y = DefaultPosY;
            
            float totalScale = Main.UIScale * config.PainBarScale * BaseSpriteScale;
            
            Vector2 barSize = new Vector2(_barFrameTexture.Width, _barFrameTexture.Height / FrameCount) * totalScale;
            Vector2 screenPos = new Vector2(
                (int)(screenRatio.X * 0.01f * Main.screenWidth),
                (int)(screenRatio.Y * 0.01f * Main.screenHeight - barSize.Y)
            );
            screenPos.X = MathHelper.Clamp(screenPos.X, 0f, Math.Max(0f, Main.screenWidth - barSize.X));
            screenPos.Y = MathHelper.Clamp(screenPos.Y, 0f, Math.Max(0f, Main.screenHeight - barSize.Y));

            bool showPainBar = config.ShowPainBar &&
                (bp.PainStat > 0 || player.HeldItem.ModItem is BlasphemySystem.IPainWeapon);

            if (showPainBar)
            {
                Rectangle barRect = new Rectangle((int)screenPos.X, (int)screenPos.Y, (int)barSize.X, (int)barSize.Y);
                bool isHovering = barRect.Contains(Main.MouseScreen.ToPoint());

                DrawPainBar(spriteBatch, bp, screenPos, totalScale, isHovering);
            }
            else
            {
                if (config.PainBarPosX != screenRatio.X || config.PainBarPosY != screenRatio.Y)
                {
                    config.PainBarPosX = screenRatio.X;
                    config.PainBarPosY = screenRatio.Y;
                }
            }
            
            if (showPainBar)
            {
                Rectangle mouseHitbox = new Rectangle((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 8, 8);
                Rectangle barRect = new Rectangle((int)screenPos.X, (int)screenPos.Y, (int)barSize.X, (int)barSize.Y);
                MouseState ms = Mouse.GetState();

                if (barRect.Intersects(mouseHitbox))
                {
                    if (!config.LockPainBarPosition)
                        Main.LocalPlayer.mouseInterface = true;

                    Vector2 newScreenRatio = screenRatio;
                    if (!config.LockPainBarPosition && ms.LeftButton == ButtonState.Pressed)
                    {
                        if (!_dragOffset.HasValue)
                            _dragOffset = Main.MouseScreen - screenPos;

                        Vector2 newCorner = Main.MouseScreen - _dragOffset.Value;
                        newScreenRatio.X = (100f * newCorner.X) / Main.screenWidth;
                        newScreenRatio.Y = (100f * (newCorner.Y + barSize.Y)) / Main.screenHeight;
                    }

                    Vector2 delta = newScreenRatio - screenRatio;
                    if (Math.Abs(delta.X) >= MouseDragEpsilon || Math.Abs(delta.Y) >= MouseDragEpsilon)
                    {
                        config.PainBarPosX = newScreenRatio.X;
                        config.PainBarPosY = newScreenRatio.Y;
                    }

                    if (_dragOffset.HasValue && ms.LeftButton == ButtonState.Released)
                    {
                        _dragOffset = null;
                    }
                }
                else if (_dragOffset.HasValue && ms.LeftButton == ButtonState.Released)
                {
                    _dragOffset = null;
                }
            }
        }

        private static void DrawPainBar(SpriteBatch spriteBatch, BlasphemyPlayer bp, Vector2 screenPos, float totalScale, bool isHovering)
        {

            int frameHeight = _barFrameTexture.Height / FrameCount;
            int frameIndex = (int)(Main.GameUpdateCount / TicksPerFrame % FrameCount);
            Rectangle frame = new Rectangle(0, frameIndex * frameHeight, _barFrameTexture.Width, frameHeight);
            spriteBatch.Draw(_barFrameTexture, screenPos, frame, Color.White, 0f, Vector2.Zero, totalScale, SpriteEffects.None, 0f);

            float completionRatio = bp.MaxPain <= 0 ? 0f : MathHelper.Clamp(bp.PainStat / (float)bp.MaxPain, 0f, 1f);
            int fillHeight = (int)Math.Ceiling(_barFillTexture.Height * completionRatio);
            if (fillHeight > 0)
            {
                Rectangle fill = new Rectangle(FillSourceLeft, _barFillTexture.Height - fillHeight, FillChannelWidth, fillHeight);
                Vector2 fillPosition = screenPos + new Vector2(FillLeft, FillTop + _barFillTexture.Height - fillHeight) * totalScale;
                spriteBatch.Draw(_barFillTexture, fillPosition, fill, Color.White, 0f, Vector2.Zero, totalScale, SpriteEffects.None, 0f);
            }
            
            if (isHovering)
            {
                string text = $"{bp.PainStat} / {bp.MaxPain}";
                Vector2 textSize = FontAssets.ItemStack.Value.MeasureString(text);
                Vector2 textPos = screenPos + new Vector2(
                    _barFrameTexture.Width * totalScale + 8f,
                    frameHeight * totalScale / 2f - textSize.Y / 2f);

                Utils.DrawBorderStringFourWay(
                    spriteBatch,
                    FontAssets.ItemStack.Value,
                    text,
                    textPos.X,
                    textPos.Y,
                    Color.White,
                    Color.Black,
                    Vector2.Zero
                );
            }
        }
    }
}
