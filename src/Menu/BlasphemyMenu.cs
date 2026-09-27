using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace Blasphemy.Menu;

public sealed class BlasphemyMenu : ModMenu
{
    private const string TexturePath = "Blasphemy/Assets/Textures/Menu/";
    private readonly List<Cinder> _cinders = new();

    public override string DisplayName => "Blasphemy";
    public override Asset<Texture2D> Logo => ModContent.Request<Texture2D>(TexturePath + "Logo_DT");
    public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Music/IntoTheDepths");
    public override ModSurfaceBackgroundStyle MenuBackgroundStyle => ModContent.GetInstance<NullSurfaceBackground>();

    public override void OnDeselected() => _cinders.Clear();

    public override void Update(bool isOnTitleScreen)
    {
        if (_cinders.Count < 80 && Main.rand.NextBool(8))
        {
            _cinders.Add(new Cinder
            {
                Position = new Vector2(Main.rand.NextFloat(Main.screenWidth), Main.screenHeight + 40f),
                Velocity = new Vector2(Main.rand.NextFloat(-0.35f, 0.35f), Main.rand.NextFloat(-1.6f, -0.55f)),
                Lifetime = Main.rand.Next(180, 420),
                Size = Main.rand.NextFloat(0.18f, 0.5f)
            });
        }

        for (int i = _cinders.Count - 1; i >= 0; i--)
        {
            Cinder cinder = _cinders[i];
            cinder.Age++;
            cinder.Position += cinder.Velocity;
            cinder.Position.X += (float)Math.Sin(cinder.Age * 0.035f) * 0.2f;
            if (cinder.Age >= cinder.Lifetime || cinder.Position.Y < -50f)
                _cinders.RemoveAt(i);
        }
    }

    public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter,
        ref float logoRotation, ref float logoScale, ref Color drawColor)
    {
        Texture2D background = ModContent.Request<Texture2D>(NullSurfaceBackground.BackgroundPath).Value;
        Texture2D bloom = ModContent.Request<Texture2D>(TexturePath + "BloomCircle").Value;
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

        float scale = Math.Max((float)Main.screenWidth / background.Width,
            (float)Main.screenHeight / background.Height);
        int width = (int)Math.Ceiling(background.Width * scale);
        int height = (int)Math.Ceiling(background.Height * scale);
        spriteBatch.Draw(background,
            new Rectangle((Main.screenWidth - width) / 2, (Main.screenHeight - height) / 2, width, height),
            Color.White);

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
        foreach (Cinder cinder in _cinders)
        {
            float fadeIn = MathHelper.Clamp(cinder.Age / 40f, 0f, 1f);
            float fadeOut = MathHelper.Clamp((cinder.Lifetime - cinder.Age) / 60f, 0f, 1f);
            spriteBatch.Draw(bloom, cinder.Position, null,
                Color.SteelBlue * (0.3f * fadeIn * fadeOut), 0f,
                bloom.Size() * 0.5f, cinder.Size, SpriteEffects.None, 0f);
        }

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

        logoRotation = 0f;
        logoScale = 1.3f;
        logoDrawCenter = new Vector2(Main.screenWidth * 0.5f, 100f);
        drawColor = Color.White;
        return true;
    }

    private sealed class Cinder
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public int Age;
        public int Lifetime;
        public float Size;
    }
}
