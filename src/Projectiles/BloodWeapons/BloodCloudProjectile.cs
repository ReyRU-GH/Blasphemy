using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace Blasphemy.Projectiles.BloodWeapons;

public sealed class BloodCloudProjectile : BloodWeaponProjectile
{
    public override string Texture => "Blasphemy/Assets/Textures/Projectiles/BloodWeapons/BloodCloudProjectile";
    protected override Color Tint => new(110, 10, 20, 75);
    private int _age;
    private bool _stopped;
    private int _stopAge;
    public override void SetDefaults()
    {
        Standard(24, 24, 660);
        Projectile.penetrate = -1;
        Projectile.tileCollide = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 30;
    }
    public override void AI()
    {
        _age++;
        bool large = Projectile.ai[0] == 1f;
        if (large)
        {
            Projectile.Center = Main.player[Projectile.owner].Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            if (_age >= 540) BeginFade();
        }
        else if (!_stopped)
        {
            Projectile.velocity *= 0.96f;
            if (_age >= 26) BeginFade(stopOnly: true);
        }
        else if (_age - _stopAge >= 180) BeginFade();
        if (_stopped && _stopAge < 0 && _age >= -_stopAge + 60) Projectile.Kill();
        if (Main.rand.NextBool(2))
            Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(large ? 100f : 42f, large ? 100f : 42f),
                DustID.Blood, Vector2.Zero, 100, default, 1.2f);
    }
    private void BeginFade(bool stopOnly = false)
    {
        if (!_stopped)
        {
            _stopped = true;
            _stopAge = _age;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
        }
        if (!stopOnly && _stopAge >= 0) _stopAge = -_age;
    }
    public override bool OnTileCollide(Vector2 oldVelocity) { BeginFade(stopOnly: true); return false; }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float radius = Projectile.ai[0] == 1f ? 112f : 52f;
        Vector2 closest = Vector2.Clamp(Projectile.Center,
            new Vector2(targetHitbox.Left, targetHitbox.Top), new Vector2(targetHitbox.Right, targetHitbox.Bottom));
        return Vector2.DistanceSquared(closest, Projectile.Center) <= radius * radius;
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (_stopAge < 0)
            modifiers.SourceDamage *= MathHelper.Clamp(1f - (_age + _stopAge) / 60f, 0.1f, 1f);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        float radius = Projectile.ai[0] == 1f ? 112f : 52f;
        float fade = _stopAge < 0 ? MathHelper.Clamp(1f - (_age + _stopAge) / 60f, 0f, 1f) : 1f;
        DrawSprite(radius * 2f / 74f, _age * 0.004f, Color.White * (fade * 0.55f));
        return false;
    }
}
