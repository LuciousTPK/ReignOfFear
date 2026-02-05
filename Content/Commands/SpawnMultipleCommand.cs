using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ReignOfFear.Content.Commands
{
    public class SpawnMultipleCommand : ModCommand
    {
        public override string Command => "spawnmultiple";
        public override CommandType Type => CommandType.Chat;

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length < 2)
            {
                caller.Reply("Usage: /spawnmultiple [npcType] [count]", Color.Red);
                return;
            }

            if (!int.TryParse(args[0], out int npcType))
            {
                caller.Reply("Invalid NPC type!", Color.Red);
                return;
            }

            if (!int.TryParse(args[1], out int count))
            {
                caller.Reply("Invalid count!", Color.Red);
                return;
            }

            Player player = caller.Player;

            // Spawn all NPCs on the same frame
            for (int i = 0; i < count; i++)
            {
                int offsetX = i * 100; // Space them out horizontally
                NPC.NewNPC(new EntitySource_DebugCommand("SpawnMultiple"),
                    (int)player.Center.X + offsetX,
                    (int)player.Center.Y - 200,
                    npcType);
            }

            caller.Reply($"Spawned {count} of NPC type {npcType}", Color.Green);
        }
    }
}