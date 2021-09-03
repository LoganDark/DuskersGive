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
			command = new CommandDefinition("give", "gives a drone any upgrade", "give 1 teleport", ((int)ConsoleCommandTarget.Drone).ToString(), "false", "false", "false", "Drone", "false", "false", "false", "false");
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

					if (command.Arguments.Count == 1) {
						string upgrade = command.Arguments[0];
						DroneUpgradeDefinition definition = DroneUpgradeFactory.UpgradeDefinitions.Find((DroneUpgradeDefinition def) => def.Name.ToLower() == upgrade);

						if (definition != null) {
							if (__instance.NumberOfUpgradesInstalled() == 3) {
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
					} else {
						ConsoleWindow3.SendConsoleResponse("invalid parameter count (expecting one).\n'help give' for usage.", ConsoleMessageType.Info);
					}
				}
			}
		}
	}
}
