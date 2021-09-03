using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

using UnityModManagerNet;
using HarmonyLib;

namespace DuskersGive {
	static class Main {
		static List<CommandDefinition> commands;
		static CommandDefinition command;
		static Harmony harmony;
		static bool active;

		[MethodImpl(MethodImplOptions.NoInlining)]
		static bool Load(UnityModManager.ModEntry entry) {
			commands = CommandHelper.GetCommands("Drone");
			command = new CommandDefinition("give", "gives a drone any upgrade", "give 1 teleport", ((int)ConsoleCommandTarget.Drone).ToString(), "false", "false", "false", "Drone", "true", "false", "false", "false");
			command.DetailedDescription.Add(new ConsoleMessage("\t\tAllows you to give your drone any upgrade.", ConsoleMessageType.Info, ConsoleMessageFormat.SmallFont));
			command.DetailedDescription.Add(new ConsoleMessage("\t\t", ConsoleMessageType.Info, ConsoleMessageFormat.SmallFont));
			command.DetailedDescription.Add(new ConsoleMessage("\t\t'give'               Lists all giveable upgrades", ConsoleMessageType.Info, ConsoleMessageFormat.SmallFont));
			command.DetailedDescription.Add(new ConsoleMessage("\t\t'give 1 2 teleport'  Gives drones 1 and 2 a Teleport upgrade", ConsoleMessageType.Info, ConsoleMessageFormat.SmallFont));
			command.DetailedDescription.Add(new ConsoleMessage("\t\t'give speed boost i' Gives your current drone a Speed Boost I upgrade", ConsoleMessageType.Info, ConsoleMessageFormat.SmallFont));
			command.DetailedDescription.Add(new ConsoleMessage("\t\t", ConsoleMessageType.Info, ConsoleMessageFormat.SmallFont));
			command.DetailedDescription.Add(new ConsoleMessage("\t\tGive mod by <color=#00c2ff>Logan</color><color=#f04040>Dark</color>.", ConsoleMessageType.Info, ConsoleMessageFormat.SmallFont));
			harmony = new Harmony(entry.Info.Id);

			entry.OnToggle = OnToggle;

			return true;
		}

		static bool OnToggle(UnityModManager.ModEntry entry, bool active) {
			Main.active = active;

			if (active) {
				harmony.PatchAll(Assembly.GetExecutingAssembly());
				commands.Add(command);
			} else {
				commands.Remove(command);
				harmony.UnpatchAll(entry.Info.Id);
			}

			return true;
		}

		[HarmonyPatch(typeof(Drone))]
		[HarmonyPatch("ExecuteCommand")]
		class OnExecuteCommand {
			static void Postfix(Drone __instance, ExecutedCommand command) {
				if (active && command.Command.CommandName == "give") { // just in case unpatch fails
					command.Handled = true;

					if (command.Arguments.Count == 0) {
						string text = "Full list of drone upgrades:";

						foreach (DroneUpgradeDefinition definition in DroneUpgradeFactory.UpgradeDefinitions) {
							text += string.Format("\n\t   {0}", definition.Name);
						}

						ConsoleWindow3.SendConsoleResponse(text, ConsoleMessageType.Info);
					} else {
						string upgrade = command.Arguments.Join(null, " ");
						DroneUpgradeDefinition definition = DroneUpgradeFactory.UpgradeDefinitions.Find((DroneUpgradeDefinition def) => def.Name.ToLowerInvariant() == upgrade.ToLowerInvariant());

						if (definition != null) {
							if (__instance.NumberOfUpgradesInstalled() >= __instance.NumberOfUpgradeSlots) {
								ConsoleWindow3.SendConsoleResponse(string.Format("drone {0} has no free upgrade slots", __instance.DroneNumber), ConsoleMessageType.Error);
							} else if (!__instance.CanAddDroneUpgrade(definition)) {
								ConsoleWindow3.SendConsoleResponse(string.Format("drone {0} cannot take that upgrade", __instance.DroneNumber), ConsoleMessageType.Error);
							} else {
								__instance.AddDroneUpgrade(DroneUpgradeFactory.CreateUpgradeInstance(definition.Type));
								ConsoleWindow3.SendConsoleResponse(string.Format("drone {0} was given {1}", __instance.DroneNumber, definition.Name), ConsoleMessageType.Info);
							}
						} else {
							ConsoleWindow3.SendConsoleResponse(string.Format("could not locate upgrade {0}.\n'help give' for usage.", upgrade), ConsoleMessageType.Info);
						}
					}
				}
			}
		}
	}
}
