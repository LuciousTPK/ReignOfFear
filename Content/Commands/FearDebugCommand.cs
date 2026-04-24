using Microsoft.Xna.Framework;
using ReignOfFear.Content.Systems.FearSystem;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace ReignOfFear.Content.Commands
{
    /// <summary>
    /// Dev commands that allow for the manipulation of the Fear System for testing purposes
    /// Currently it allows you to do a multitude of things, such as adding/removing phobias,
    /// adding/removing fear, adding/removing courage, and more. Useful for ongoing tests with
    /// the Fear System as a whole and is integral to the development process of this mod
    /// </summary>

    public class FearDebugCommand : ModCommand
    {
        public override string Command => "fear";

        public override CommandType Type => CommandType.Chat;

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            var player = caller.Player.GetModPlayer<FearSystemPlayer>();

            if (args.Length > 1 && args[0] == "show" && args[1] == "phobias")
            {
                int phobiaCount = 0;
                foreach (PhobiaID phobiaName in Enum.GetValues<PhobiaID>())
                {
                    if (player.HasPhobia(phobiaName))
                    {
                        caller.Reply(phobiaName.ToString(), Color.Yellow);
                        phobiaCount++;
                    }
                }
                if (phobiaCount <= 0)
                {
                    caller.Reply($"{caller.Player.name} currently has no phobias.");
                }

                return;
            }

            else if (args.Length > 1 && Enum.TryParse<PhobiaID>(args[0], true, out PhobiaID phobia))
            {
                switch (args[1])
                {
                    case "add":
                        {
                            if (args.Length > 3 && args[2] == "fear" && float.TryParse(args[3], out float addFearValue))
                            {
                                if (player.GetPhobiaState(phobia).hasPhobia)
                                {
                                    player.AddFearPoints(phobia, (int)Math.Floor(addFearValue));
                                    PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);
                                    caller.Reply("Adding " + addFearValue + " fear to " + phobia.ToString() + "!", Color.Yellow);
                                    caller.Reply(phobia.ToString() + "'s Fear points: " + player.GetPhobiaState(phobia).fearPoints.ToString() + "/" + definition.postAcquisitionMax, Color.Yellow);
                                    caller.Reply(phobia.ToString() + "'s Courage points: " + player.GetPhobiaState(phobia).couragePoints.ToString() + "/" + definition.courageMax, Color.Yellow);
                                    break;
                                }

                                else
                                {
                                    player.AddFearPoints(phobia, (int)Math.Floor(addFearValue));
                                    PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);
                                    caller.Reply("Adding " + addFearValue + " fear to " + phobia.ToString() + "!", Color.Yellow);
                                    caller.Reply(phobia.ToString() + "'s Fear points: " + player.GetPhobiaState(phobia).fearPoints.ToString() + "/" + definition.preAcquisitionMax, Color.Yellow);
                                    caller.Reply(phobia.ToString() + "'s Courage points: " + player.GetPhobiaState(phobia).couragePoints.ToString() + "/" + definition.courageMax, Color.Yellow);
                                    break;
                                }

                            }

                            else if (args.Length > 3 && args[2] == "courage" && float.TryParse(args[3], out float addCourageValue))
                            {
                                player.AddCouragePoints(phobia, (int)Math.Floor(addCourageValue));
                                PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);
                                caller.Reply("Adding " + addCourageValue + " courage to " + phobia.ToString() + "!", Color.Yellow);
                                caller.Reply(phobia.ToString() + "'s Fear points: " + player.GetPhobiaState(phobia).fearPoints.ToString() + "/" + definition.postAcquisitionMax, Color.Yellow);
                                caller.Reply(phobia.ToString() + "'s Courage points: " + player.GetPhobiaState(phobia).couragePoints.ToString() + "/" + definition.courageMax, Color.Yellow);
                                break;
                            }

                            else if (args.Length > 2 && args[2] == "phobia")
                            {
                                player.GetPhobiaState(phobia).fearPoints = 0;
                                player.GetPhobiaState(phobia).hasPhobia = true;
                                player.RecalculateSetRank(PhobiaData.Definitions[phobia].set);
                                caller.Reply("Giving " + phobia.ToString() + " to player!", Color.Yellow);
                                break;
                            }

                            else
                            {
                                caller.Reply("Invalid 'Add' command!", Color.Red);
                                break;
                            }
                        }

                    case "remove":
                        {
                            if (args.Length > 3 && args[2] == "fear" && float.TryParse(args[3], out float removeFearValue))
                            {
                                if (player.GetPhobiaState(phobia).hasPhobia)
                                {
                                    player.RemoveFearPoints(phobia, (int)Math.Floor(removeFearValue));
                                    PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);
                                    caller.Reply("Removing " + removeFearValue + " fear from " + phobia.ToString() + "!", Color.Yellow);
                                    caller.Reply(phobia.ToString() + "'s Fear points: " + player.GetPhobiaState(phobia).fearPoints.ToString() + "/" + definition.postAcquisitionMax, Color.Yellow);
                                    break;
                                }

                                else
                                {
                                    player.RemoveFearPoints(phobia, (int)Math.Floor(removeFearValue));
                                    PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);
                                    caller.Reply("Removing " + removeFearValue + " fear from " + phobia.ToString() + "!", Color.Yellow);
                                    caller.Reply(phobia.ToString() + "'s Fear points: " + player.GetPhobiaState(phobia).fearPoints.ToString() + "/" + definition.preAcquisitionMax, Color.Yellow);
                                    break;
                                }
                            }

                            else if (args.Length > 3 && args[2] == "courage" && float.TryParse(args[3], out float removeCourageValue))
                            {
                                player.RemoveCouragePoints(phobia, (int)Math.Floor(removeCourageValue));
                                PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);
                                caller.Reply("Removing " + removeCourageValue + " courage from " + phobia.ToString() + "!", Color.Yellow);
                                caller.Reply(phobia.ToString() + "'s Fear points: " + player.GetPhobiaState(phobia).fearPoints.ToString() + "/" + definition.postAcquisitionMax, Color.Yellow);
                                caller.Reply(phobia.ToString() + "'s Courage points: " + player.GetPhobiaState(phobia).couragePoints.ToString() + "/" + definition.courageMax, Color.Yellow);
                                break;
                            }

                            else if (args.Length > 2 && args[2] == "phobia")
                            {
                                player.GetPhobiaState(phobia).fearPoints = 0;
                                player.GetPhobiaState(phobia).couragePoints = 0;
                                player.GetPhobiaState(phobia).currentRank = 1;
                                player.GetPhobiaState(phobia).isBurden = false;
                                player.GetPhobiaState(phobia).hasPhobia = false;
                                player.RecalculateSetRank(PhobiaData.Definitions[phobia].set);
                                caller.Reply("Removing " + phobia.ToString() + " from player!", Color.Yellow);
                                break;
                            }

                            else
                            {
                                caller.Reply("Invalid 'Remove' command!", Color.Red);
                                break;
                            }
                        }

                    case "clear":
                        {
                            if (args.Length > 2 && args[2] == "fear")
                            {
                                player.GetPhobiaState(phobia).fearPoints = 0;
                                PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);
                                caller.Reply("Removing all fear from " + phobia.ToString() + "!", Color.Yellow);
                                caller.Reply(phobia.ToString() + "'s Fear points: " + player.GetPhobiaState(phobia).fearPoints.ToString() + "/" + definition.postAcquisitionMax, Color.Yellow);
                                caller.Reply(phobia.ToString() + "'s Courage points: " + player.GetPhobiaState(phobia).couragePoints.ToString() + "/" + definition.courageMax, Color.Yellow);
                                break;
                            }

                            else
                            {
                                caller.Reply("Invalid 'Clear' command!", Color.Red);
                                break;
                            }
                        }

                    case "max":
                        {
                            if (args.Length > 2 && args[2] == "fear")
                            {
                                PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);
                                player.GetPhobiaState(phobia).hasPhobia = true;
                                player.GetPhobiaState(phobia).couragePoints = 0;
                                player.SetFearPoints(phobia, definition.postAcquisitionMax);
                                caller.Reply("Maxing out fear for " + phobia.ToString() + "!", Color.Yellow);
                                caller.Reply(phobia.ToString() + "'s Fear points: " + player.GetPhobiaState(phobia).fearPoints.ToString() + "/" + definition.postAcquisitionMax, Color.Yellow);
                                caller.Reply(phobia.ToString() + "'s Courage points: " + player.GetPhobiaState(phobia).couragePoints.ToString() + "/" + definition.courageMax, Color.Yellow);
                                break;
                            }

                            else if (args.Length > 2 && args[2] == "courage")
                            {
                                PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);
                                player.GetPhobiaState(phobia).hasPhobia = true;
                                player.GetPhobiaState(phobia).fearPoints = 0;
                                player.SetCouragePoints(phobia, definition.courageMax);
                                caller.Reply("Maxing out courage for " + phobia.ToString() + "!", Color.Yellow);
                                caller.Reply(phobia.ToString() + "'s Fear points: " + player.GetPhobiaState(phobia).fearPoints.ToString() + "/" + definition.postAcquisitionMax, Color.Yellow);
                                caller.Reply(phobia.ToString() + "'s Courage points: " + player.GetPhobiaState(phobia).couragePoints.ToString() + "/" + definition.courageMax, Color.Yellow);
                                break;
                            }

                            else
                            {
                                caller.Reply("Invalid 'Max' command!", Color.Red);
                                break;
                            }
                        }

                    case "show":
                        {
                            if (args.Length > 2 && args[2] == "fear")
                            {
                                if (player.GetPhobiaState(phobia).hasPhobia)
                                {
                                    PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);
                                    caller.Reply(phobia.ToString() + "'s Fear points: " + player.GetPhobiaState(phobia).fearPoints.ToString() + "/" + definition.postAcquisitionMax, Color.Yellow);
                                    break;
                                }

                                else
                                {
                                    PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);
                                    caller.Reply(phobia.ToString() + "'s Fear points: " + player.GetPhobiaState(phobia).fearPoints.ToString() + "/" + definition.preAcquisitionMax, Color.Yellow);
                                    break;
                                }
                            }

                            else if (args.Length > 2 && args[2] == "courage")
                            {
                                PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);
                                caller.Reply(phobia.ToString() + "'s Courage points: " + player.GetPhobiaState(phobia).couragePoints.ToString() + "/" + definition.courageMax, Color.Yellow);
                                break;
                            }

                            else
                            {
                                caller.Reply("Invalid 'show' command!", Color.Red);
                                break;
                            }
                        }

                    default:
                        {
                            caller.Reply("Invalid command!", Color.Red);
                            break;
                        }
                }

                return;
            }

            else if (!Enum.TryParse<PhobiaID>(args[0], true, out PhobiaID phobiaID))
            {
                caller.Reply("Invalid phobia name!", Color.Red);
                return;
            }

            else
            {
                caller.Reply("Invalid command!", Color.Red);
                return;
            }
        }
    }
}
