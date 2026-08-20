using System;
using Blasphemy.Items;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Blasphemy.Systems;

namespace Blasphemy.Players
{
    public class BlasphemyPlayer : ModPlayer
    {
        public int RecoveryStat;
        private int _recoveryTimer = 0; 
        private const int RecoveryInterval = 60;

        public const int DefaultMaxPain = 100;
        public int PainStat;
        public int MaxPain = DefaultMaxPain;
        public bool IsAgonized; 
        
        public float MaxPainMultiplier = 1f;
        public float PainGainMultiplier = 1f;
        private const float PainDecayDelay = 20f;      // 20 секунд до начала снижения
        private const float PainDecayRampTime = 10f;   // 10 секунд на разгон
        private const float PainDecayStartRate = 1f;   // 1 очко в секунду в начале
        private const float PainDecayMaxRate = 20f;    // 20 очков в секунду на пике

        public float agonyImmunityTimer;
        public float lifeCostReductionTimer;
        
        private float painDecayTimer;         
        private float decayAccumulator; 
        
        public Item LastWeaponUsed;

        public override void Initialize()
        {
            RecoveryStat = 0;
            _recoveryTimer = 0;
            PainStat = 0;
            MaxPain = DefaultMaxPain;
            IsAgonized = false;
            MaxPainMultiplier = 1f;
            PainGainMultiplier = 1f;
            agonyImmunityTimer = 0f;
            lifeCostReductionTimer = 0f;
            painDecayTimer = 0f;
            decayAccumulator = 0f;
        }

        public override void ResetEffects()
        {
            MaxPainMultiplier = 1f;
            PainGainMultiplier = 1f;
        }

        public override void PostUpdate()
        {
            if (Player.dead) return; 
            
            if (Player.statLife < Player.statLifeMax2 && RecoveryStat > 0)
            {
                _recoveryTimer++;
                if (_recoveryTimer >= RecoveryInterval)
                {
                    _recoveryTimer = 0;
                    int healAmount = (int)Math.Ceiling(RecoveryStat * 0.3f);
                    if (healAmount > 0)
                    {
                        int actualHeal = Math.Min(healAmount, Player.statLifeMax2 - Player.statLife);
                        Player.statLife += actualHeal;
                        RecoveryStat -= healAmount;
                        if (RecoveryStat < 0) RecoveryStat = 0;
                        
                        CombatText.NewText(Player.getRect(), new Color(100, 255, 100), $"+{actualHeal}", dramatic: false);
                        if (Main.netMode == NetmodeID.MultiplayerClient)
                            NetMessage.SendData(MessageID.PlayerHeal, -1, -1, null, Player.whoAmI);
                    }
                }
            }
            
            int newMaxPain = (int)(DefaultMaxPain * MaxPainMultiplier);
            if (newMaxPain != MaxPain)
            {
                MaxPain = newMaxPain;
                if (PainStat > MaxPain) PainStat = MaxPain;
            }
            
            if (!IsAgonized && PainStat >= MaxPain)
            {
                IsAgonized = true;
                CombatText.NewText(Player.getRect(), new Color(255, 50, 50), "AGONY READY!", dramatic: true);
            }
            
            float dt = 1f / 60f;
            if (agonyImmunityTimer > 0f) agonyImmunityTimer -= dt;
            if (lifeCostReductionTimer > 0f) lifeCostReductionTimer -= dt;
            if (PainStat > 0 && agonyImmunityTimer <= 0f)
            {
                painDecayTimer += dt;

                if (painDecayTimer >= PainDecayDelay)
                {
                    float timeSinceRampStart = painDecayTimer - PainDecayDelay;
                    float rampProgress = MathHelper.Clamp(timeSinceRampStart / PainDecayRampTime, 0f, 1f);
                    
                    float currentRatePerSecond = MathHelper.Lerp(PainDecayStartRate, PainDecayMaxRate, rampProgress);
                    
                    decayAccumulator += currentRatePerSecond * dt;
                    
                    if (decayAccumulator >= 1f)
                    {
                        int decayAmount = (int)decayAccumulator;
                        PainStat -= decayAmount;
                        decayAccumulator -= decayAmount;
                        
                        if (PainStat < 0) PainStat = 0;
                    }
                }
            }
            else
            {
                painDecayTimer = 0f;
                decayAccumulator = 0f;
            }
        }

        public void AddRecovery(int amount)
        {
            if (amount <= 0) return;
            RecoveryStat += amount;
        }

        public void AddPain(int amount)
        {
            if (amount <= 0 || agonyImmunityTimer > 0f) return;
            
            int modifiedAmount = (int)(amount * PainGainMultiplier);
            if (modifiedAmount <= 0) return;

            PainStat += modifiedAmount;
            if (PainStat > MaxPain) PainStat = MaxPain;
        }

        public int GetEffectiveLifeCost(int baseCost)
        {
            float cost = baseCost;
            // +1% за каждые 10 боли
            cost *= 1f + (PainStat / 10f) * 0.01f;
            // -15% после агонии
            if (lifeCostReductionTimer > 0f) cost *= 0.85f;

            return Math.Max(1, (int)cost);
        }


        private void HandleHit(Item item)
        {

            if (item.ModItem is BlasphemySystem.IConditionalActivation)
            {
                if (item.ModItem is BlasphemySystem.ILifeCostItem lc)
                {
                    int effCost = GetEffectiveLifeCost(lc.LifeCost);
                    Player.statLife -= effCost;
                    if (Player.statLife < 0) Player.statLife = 0;
                    CombatText.NewText(Player.getRect(), Color.Red, $"-{effCost}", dramatic: true);

                    if (lc.RecoveryPercent > 0)
                    {
                        float recGain = (float)Math.Floor(effCost * lc.RecoveryPercent / 100f);
                        AddRecovery((int)recGain);
                    }
                }

                if (item.ModItem is BlasphemySystem.IPainWeapon pw)
                {
                    AddPain(pw.PainGain);
                }
            }
            
            if (IsAgonized)
            {
                IsAgonized = false; 
                PainStat = 0;
                agonyImmunityTimer = 10f;
                lifeCostReductionTimer = 10f;
                CombatText.NewText(Player.getRect(), new Color(255, 215, 0), "AGONY STRIKE!", dramatic: true);
            }
        }
        
        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            LastWeaponUsed = item;
            HandleHit(item);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (proj.owner == Player.whoAmI && LastWeaponUsed != null)
            {
                HandleHit(LastWeaponUsed);
            }
        }
        
        public override void ModifyWeaponDamage(Item item, ref StatModifier damage)
        {
            if (IsAgonized)
            {
                // X% бонуса, где X = MaxPain (при 100 MaxPain = +100% урона, т.е. x2)
                float bonus = MaxPain / 100f;
                damage *= (1f + bonus);
            }
        }
    }
}