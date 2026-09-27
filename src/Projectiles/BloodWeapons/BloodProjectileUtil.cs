using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace Blasphemy.Projectiles.BloodWeapons;

internal static class BloodProjectileUtil
{
    public static NPC Nearest(Vector2 point, float range)
    {
        NPC found = null;
        float best = range * range;
        foreach (NPC npc in Main.npc)
        {
            if (!npc.CanBeChasedBy()) continue;
            float distance = Vector2.DistanceSquared(point, npc.Center);
            if (distance >= best) continue;
            best = distance;
            found = npc;
        }
        return found;
    }

    public static bool HitsLine(Rectangle hitbox, Vector2 from, Vector2 to, float thickness)
    {
        float point = 0f;
        return Collision.CheckAABBvLineCollision(new Vector2(hitbox.X, hitbox.Y),
            new Vector2(hitbox.Width, hitbox.Height), from, to, thickness, ref point);
    }

    public static void DrawPixel(SpriteBatch batch, Vector2 center, Vector2 size, Color color)
    {
        batch.Draw(TextureAssets.MagicPixel.Value, center, null, color, 0f,
            new Vector2(0.5f), size, SpriteEffects.None, 0f);
    }
}
