using System;
using Blasphemy.Items;
using Blasphemy.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Blasphemy.Players
{
    public class BlasphemyPlayer : ModPlayer
    {
        public int RecoveryStat;
        private int _recoveryTimer; 
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

        public float AgonyImmunityTimer;
        public float LifeCostReductionTimer;
        
        private float _painDecayTimer;         
        private float _decayAccumulator; 
        
        public Item LastWeaponUsed;
        public bool CurrentAttackAgonized;
        public int CurrentAttackItemType;
        public int LastLifeSpent;
        private ulong _lastConditionalHitTick;
        private int _lastConditionalItemType;

        public override void Initialize()
        {
            RecoveryStat = 0;
            _recoveryTimer = 0;
            PainStat = 0;
            MaxPain = DefaultMaxPain;
            IsAgonized = false;
            MaxPainMultiplier = 1f;
            PainGainMultiplier = 1f;
            AgonyImmunityTimer = 0f;
            LifeCostReductionTimer = 0f;
            _painDecayTimer = 0f;
            _decayAccumulator = 0f;
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
            if (AgonyImmunityTimer > 0f) AgonyImmunityTimer -= dt;
            if (LifeCostReductionTimer > 0f) LifeCostReductionTimer -= dt;
            if (PainStat > 0 && AgonyImmunityTimer <= 0f)
            {
                _painDecayTimer += dt;

                if (_painDecayTimer >= PainDecayDelay)
                {
                    float timeSinceRampStart = _painDecayTimer - PainDecayDelay;
                    float rampProgress = MathHelper.Clamp(timeSinceRampStart / PainDecayRampTime, 0f, 1f);
                    
                    float currentRatePerSecond = MathHelper.Lerp(PainDecayStartRate, PainDecayMaxRate, rampProgress);
                    
                    _decayAccumulator += currentRatePerSecond * dt;
                    
                    if (_decayAccumulator >= 1f)
                    {
                        int decayAmount = (int)_decayAccumulator;
                        PainStat -= decayAmount;
                        _decayAccumulator -= decayAmount;
                        
                        if (PainStat < 0) PainStat = 0;
                    }
                }
            }
            else
            {
                _painDecayTimer = 0f;
                _decayAccumulator = 0f;
            }
        }

        public void AddRecovery(int amount)
        {
            if (amount <= 0) return;
            RecoveryStat += amount;
        }

        public void AddPain(int amount)
        {
            if (amount <= 0 || AgonyImmunityTimer > 0f) return;
            
            int modifiedAmount = (int)(amount * PainGainMultiplier);
            if (modifiedAmount <= 0) return;
            
            _painDecayTimer = 0;
            PainStat += modifiedAmount;
            if (PainStat > MaxPain) PainStat = MaxPain;
        }

        public int GetEffectiveLifeCost(int baseCost)
        {
            float cost = baseCost;
            // +1% за каждые 10 боли
            cost *= 1f + (PainStat / 10f) * 0.01f;
            // -15% после агонии
            if (LifeCostReductionTimer > 0f) cost *= 0.85f;

            return Math.Max(1, (int)cost);
        }

        public void BeginAgonizedAttack(Item item)
        {
            CurrentAttackAgonized = IsAgonized;
            CurrentAttackItemType = item.type;
            if (IsAgonized)
            {
                IsAgonized = false;
                PainStat = 0;
                AgonyImmunityTimer = 10f;
                LifeCostReductionTimer = 10f;
                CombatText.NewText(Player.getRect(), new Color(255, 215, 0), "AGONY STRIKE!", dramatic: true);
            }
        }

        public bool IsCurrentAttackAgonized(Item item) =>
            CurrentAttackItemType == item.type && CurrentAttackAgonized;

        public bool TryMarkConditionalHit(Item item)
        {
            ulong tick = Main.GameUpdateCount;
            if (_lastConditionalItemType == item.type && tick - _lastConditionalHitTick < (ulong)Math.Max(1, item.useAnimation))
                return false;

            _lastConditionalItemType = item.type;
            _lastConditionalHitTick = tick;
            return true;
        }


        private void HandleHit(Item item)
        {
            if (IsAgonized && LastWeaponUsed.ModItem is not BlasphemySystem.IAgonizedWeapon)
            {
                IsAgonized = false; 
                PainStat = 0;
                AgonyImmunityTimer = 10f;
                LifeCostReductionTimer = 10f;
                CombatText.NewText(Player.getRect(), new Color(255, 215, 0), "AGONY STRIKE!", dramatic: true);
            }
        }
        
        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            LastWeaponUsed = item;
            HandleHit(LastWeaponUsed);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (proj.owner == Player.whoAmI && proj.TryGetGlobalProjectile<GlobalProjectileTracker>(out var tracker) && tracker.ParentItem != null) {
                LastWeaponUsed = tracker.ParentItem;
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
