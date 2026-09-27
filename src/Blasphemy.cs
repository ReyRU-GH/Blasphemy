using System.IO;
using Blasphemy.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Blasphemy;

internal enum BlasphemyMessageType : byte
{
    FossilSummonItemRequest,
    FossilQuestSummonRequest
}

public class Blasphemy : Mod
{
    public static Blasphemy Instance { get; private set; }

    public Blasphemy()
    {
        Instance = this;
    }

    public override void Unload()
    {
        Instance = null;
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        BlasphemyMessageType messageType = (BlasphemyMessageType)reader.ReadByte();

        switch (messageType)
        {
            case BlasphemyMessageType.FossilSummonItemRequest:
            {
                int playerIndex = reader.ReadByte();
                if (Main.netMode == NetmodeID.Server && playerIndex == whoAmI && IsValidPlayer(playerIndex))
                    FossilEncounterSystem.TryStartFromSummonItem(Main.player[playerIndex]);
                break;
            }

            case BlasphemyMessageType.FossilQuestSummonRequest:
            {
                int playerIndex = reader.ReadByte();
                if (Main.netMode == NetmodeID.Server && playerIndex == whoAmI && IsValidPlayer(playerIndex))
                    FossilEncounterSystem.TryStartFromArchaeologistQuest(Main.player[playerIndex]);
                break;
            }
        }
    }

    private static bool IsValidPlayer(int playerIndex)
    {
        return playerIndex >= 0 && playerIndex < Main.maxPlayers && Main.player[playerIndex].active;
    }
}
