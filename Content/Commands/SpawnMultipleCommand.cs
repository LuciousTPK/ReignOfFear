using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ReignOfFear.Content.Commands
{
    /// <summary>
    /// Dev command for spawning multiple enemies on the same frame
    /// Mostly used for testing the combat tracker, but is also handy for testing
    /// phobia effects, external NPC tracking logic, or possible IL edits in the future
    /// </summary>

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

            for (int i = 0; i < count; i++)
            {
                int offsetX = i * 100;
                NPC.NewNPC(new EntitySource_DebugCommand("SpawnMultiple"),
                    (int)player.Center.X + offsetX,
                    (int)player.Center.Y - 200,
                    npcType);
            }

            caller.Reply($"Spawned {count} of NPC type {npcType}", Color.Green);
        }
    }
}