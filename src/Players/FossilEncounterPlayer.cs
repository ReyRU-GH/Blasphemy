using Blasphemy.NPCs.Bosses.FossilBoss;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Blasphemy.Players;

public sealed class FossilEncounterPlayer : ModPlayer
{
    private const int HypnosisTellTime = 4 * 60;
    private const int HypnosisDuration = 10 * 60;

    private int _hypnosisNpc = -1;
    private int _hypnosisRemaining;
    private int _offscreenTellTicks;
    private bool _sawLizardDuringTell;
    private bool _escapedCurrentHypnosis;
    private Vector2 _hypnosisCameraPosition;
    private bool _hypnosisCameraActive;

    private bool _wasCarried;

    public bool IsHypnotized => _hypnosisRemaining > 0;

    public override void PreUpdate()
    {
        UpdateHypnosis();
    }

    public override void SetControls()
    {
        if (!TryGetCarryingSnake(out _))
            return;

        Player.controlLeft = false;
        Player.controlRight = false;
        Player.controlUp = false;
        Player.controlDown = false;
        Player.controlJump = false;
        Player.controlHook = false;
        Player.controlMount = false;
    }

    public override bool PreItemCheck()
    {
        return !TryGetCarryingSnake(out _);
    }

    public override void PreUpdateMovement()
    {
        if (TryGetCarryingSnake(out FossilSnakeHead snake))
        {
            _wasCarried = true;

            Player.velocity = Vector2.Zero;
            Player.Center = snake.GetCarryAnchor();
            Player.fallStart = (int)(Player.position.Y / 16f);
            return;
        }

        if (_wasCarried)
        {
            Player.velocity *= 0.25f;

            _wasCarried = false;
        }
    }

    public override void FrameEffects()
    {
        if (!TryGetCarryingSnake(out _))
            return;

        Player.bodyFrame.Y = Player.bodyFrame.Height * 5;
        Player.legFrame.Y = Player.legFrame.Height * 5;
        Player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, -MathHelper.PiOver2);
        Player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, -MathHelper.PiOver2);
    }

    public override void ModifyScreenPosition()
    {
        if (!IsHypnotized || !TryGetHypnosisLizard(out NPC lizard))
        {
            if (_hypnosisCameraActive)
            {
                Vector2 playerCamera = Main.screenPosition;
                _hypnosisCameraPosition = Vector2.Lerp(_hypnosisCameraPosition, playerCamera, 0.12f);
                Main.screenPosition = _hypnosisCameraPosition;
                if (Vector2.DistanceSquared(_hypnosisCameraPosition, playerCamera) < 1f)
                    _hypnosisCameraActive = false;
            }
            return;
        }

        Vector2 focus = ((FossilLizard)lizard.ModNPC).FlareWorldPosition;
        Vector2 desired = focus - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
        if (!_hypnosisCameraActive)
        {
            _hypnosisCameraPosition = Main.screenPosition;
            _hypnosisCameraActive = true;
        }

        _hypnosisCameraPosition = Vector2.Lerp(_hypnosisCameraPosition, desired, 0.12f);
        if (Vector2.DistanceSquared(_hypnosisCameraPosition, desired) < 1f)
            _hypnosisCameraPosition = desired;
        Main.screenPosition = _hypnosisCameraPosition;
    }

    private void UpdateHypnosis()
    {
        if (Main.dedServ || Player.whoAmI != Main.myPlayer)
            return;

        NPC lizardNpc = FindHypnosisLizard();
        if (lizardNpc is null || lizardNpc.ModNPC is not FossilLizard lizard || !lizard.IsHypnosisState)
        {
            ResetHypnosis();
            return;
        }

        if (_hypnosisNpc != lizardNpc.whoAmI)
        {
            _hypnosisNpc = lizardNpc.whoAmI;
            _hypnosisRemaining = 0;
            _offscreenTellTicks = 0;
            _sawLizardDuringTell = false;
            _escapedCurrentHypnosis = false;
        }

        if (_hypnosisRemaining > 0)
        {
            _hypnosisRemaining--;
            return;
        }

        if (_escapedCurrentHypnosis)
            return;

        if (lizardNpc.ai[1] < HypnosisTellTime)
        {
            Rectangle view = new((int)Main.screenPosition.X, (int)Main.screenPosition.Y,
                Main.screenWidth, Main.screenHeight);
            if (view.Intersects(lizardNpc.Hitbox))
            {
                _sawLizardDuringTell = true;
                _offscreenTellTicks = 0;
            }
            else if (_sawLizardDuringTell && ++_offscreenTellTicks >= 45)
            {
                _escapedCurrentHypnosis = true;
            }
            return;
        }
        
        _hypnosisRemaining = HypnosisDuration;
    }

    private NPC FindHypnosisLizard()
    {
        int type = ModContent.NPCType<FossilLizard>();
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (npc.active && npc.type == type && npc.ModNPC is FossilLizard lizard && lizard.IsHypnosisState)
                return npc;
        }

        return null;
    }

    private bool TryGetHypnosisLizard(out NPC lizard)
    {
        lizard = null;
        if (_hypnosisNpc < 0 || _hypnosisNpc >= Main.maxNPCs)
            return false;

        NPC npc = Main.npc[_hypnosisNpc];
        if (!npc.active || npc.ModNPC is not FossilLizard fossilLizard || !fossilLizard.IsHypnosisState)
            return false;

        lizard = npc;
        return true;
    }

    private bool TryGetCarryingSnake(out FossilSnakeHead snake)
    {
        snake = null;
        int type = ModContent.NPCType<FossilSnakeHead>();

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (!npc.active || npc.type != type || npc.target != Player.whoAmI)
                continue;

            if (npc.ModNPC is FossilSnakeHead head && head.IsCarryingTarget)
            {
                snake = head;
                return true;
            }
        }

        return false;
    }

    private void ResetHypnosis()
    {
        _hypnosisNpc = -1;
        _hypnosisRemaining = 0;
        _offscreenTellTicks = 0;
        _sawLizardDuringTell = false;
        _escapedCurrentHypnosis = false;
    }
}
