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
        private const float BaseSpriteScale = 2.5f; // TODO: Change to 1f when sprite will be noramlized

        private static Vector2? _dragOffset = null;
        private static Texture2D _barBgTexture;
        private static Texture2D _barFillTexture;
        private static Texture2D _barFrameTexture;

        public override void OnModLoad()
        {
            _barBgTexture = ModContent.Request<Texture2D>("Blasphemy/Assets/Textures/UI/PainBar", AssetRequestMode.ImmediateLoad).Value;
            _barFillTexture = ModContent.Request<Texture2D>("Blasphemy/Assets/Textures/UI/PainBarFill", AssetRequestMode.ImmediateLoad).Value;
            _barFrameTexture = ModContent.Request<Texture2D>("Blasphemy/Assets/Textures/UI/PainBarFrame", AssetRequestMode.ImmediateLoad).Value;
        }

        public override void Unload()
        {
            _dragOffset = null;
            _barBgTexture = _barFillTexture = _barFrameTexture = null;
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
            
            Vector2 screenPos = new Vector2(
                (int)(screenRatio.X * 0.01f * Main.screenWidth),
                (int)(screenRatio.Y * 0.01f * Main.screenHeight)
            );

            Vector2 barSize = _barBgTexture.Size() * totalScale;

            if (config.ShowPainBar && bp.PainStat > 0)
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
                    config.SaveChanges();
                }
            }
            
            if (config.ShowPainBar && bp.PainStat > 0)
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
                        newScreenRatio.Y = (100f * newCorner.Y) / Main.screenHeight;
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
                        config.SaveChanges();
                    }
                }
                else if (_dragOffset.HasValue && ms.LeftButton == ButtonState.Released)
                {
                    _dragOffset = null;
                    config.SaveChanges();
                }
            }
        }

        private static void DrawPainBar(SpriteBatch spriteBatch, BlasphemyPlayer bp, Vector2 screenPos, float totalScale, bool isHovering)
        {

            spriteBatch.Draw(_barBgTexture, screenPos, null, Color.White, 0f, Vector2.Zero, totalScale, SpriteEffects.None, 0f);
            
            float completionRatio = bp.MaxPain <= 0f ? 0f : bp.PainStat / (float)bp.MaxPain;
            int fillWidth = (int)(_barFillTexture.Width * completionRatio);
            Rectangle fillRect = new Rectangle(0, 0, fillWidth, _barFillTexture.Height);

            spriteBatch.Draw(_barFillTexture, screenPos, fillRect, Color.White, 0f, Vector2.Zero, totalScale, SpriteEffects.None, 0f);
            
            spriteBatch.Draw(_barFrameTexture, screenPos, null, Color.White, 0f, Vector2.Zero, totalScale, SpriteEffects.None, 0f);
            
            if (isHovering)
            {
                string text = $"{(int)bp.PainStat} / {bp.MaxPain}";
                Vector2 textSize = FontAssets.ItemStack.Value.MeasureString(text);
                Vector2 textPos = screenPos + new Vector2(
                    (_barBgTexture.Width * totalScale) / 2f - textSize.X / 2f,
                    (_barBgTexture.Height * totalScale) / 2f - textSize.Y / 2f
                );

                Utils.DrawBorderStringFourWay(
                    spriteBatch,
                    FontAssets.ItemStack.Value,
                    text,
                    textPos.X,
                    textPos.Y,
                    Color.White,
                    Color.Black,
                    Vector2.Zero,
                    1f
                );
            }
        }
    }
}