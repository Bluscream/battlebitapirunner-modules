// Version 2.0
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BattleBitAPI.Common;
using BBRAPIModules;
using Commands;
using Bluscream;
using System.Collections.Generic;

namespace Bluscream {
    [RequireModule(typeof(BluscreamLib))]
    [RequireModule(typeof(CommandHandler))]
    [Module("More Commands", "2.0.0")]
    public class MoreCommands : BattleBitModule {
        public static ModuleInfo ModuleInfo = new() {
            Name = "More Commands",
            Description = "GenMore commands for the Battlebit Modular API",
            Version = new Version(2, 0, 0),
            Author = "Bluscream",
            WebsiteUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/"),
            UpdateUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/raw/master/modules/MoreCommands.cs"),
            SupportUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/issues/new?title=MoreCommands")
        };
        public static MoreCommandsConfiguration Configuration { get; set; } = null!;

        [ModuleReference]
        public CommandHandler CommandHandler { get; set; }

        public override void OnModulesLoaded() {
            this.CommandHandler.Register(this);
        }
        public string GetCurrentMapInfoString() => $"Current Map:\n\nName: {this.Server.Map.ToMap()?.DisplayName} ({this.Server.Map})\nMode: {this.Server.Gamemode.ToGameMode()?.DisplayName} ({this.Server.Gamemode})\nSize: {this.Server.MapSize}";
        #region commands
        [CommandCallback("map", Description = "Changes the map")]
        public void SetMap(RunnerPlayer commandSource, string? mapName = null, string? gameMode = null, string? dayNight = null)
        {
            if (mapName is null) {
                commandSource.Message(GetCurrentMapInfoString()); return;
            }
            var map = mapName.ParseMap();
            if (map is null) {
                commandSource.Message($"Map {mapName} could not be found"); return;
            }
            GameModeInfo? mode = null;
            if (gameMode is not null) {
                if (gameMode != null) {
                    mode = gameMode.ParseGameMode();
                    if (mode is null) {
                        commandSource.Message($"GameMode {gameMode} could not be found"); return;
                    }
                }
            }
            this.Server.ChangeMap(map, mode, dayNight.ParseDayNight());
        }

            [CommandCallback("gamemode", Description = "Changes the gamemode")]
            public void SetGameMode(RunnerPlayer commandSource, string gameMode, string? dayNight = null) {
                if (gameMode is null) {
                    commandSource.Message(GetCurrentMapInfoString()); return;
                }
                SetMap(commandSource, this.Server.Map, gameMode, dayNight);
            }

            [CommandCallback("time", Description = "Changes the map time")]
            public void SetMapTime(RunnerPlayer commandSource, string dayNight) {
                if (dayNight is null) {
                    commandSource.Message(GetCurrentMapInfoString()); return;
                }
                SetGameMode(commandSource, this.Server.Gamemode, dayNight);
            }

            [CommandCallback("maprestart", Description = "Restarts the current map")]
            public void RestartMap(RunnerPlayer commandSource) => SetMapTime(commandSource, this.Server.DayNight.ToString());

            [CommandCallback("allowvotetime", Description = "Changes the allowed map times for votes")]
            public void SetMapVoteTime(RunnerPlayer commandSource, string dayNightAll) {
                var DayNight = dayNightAll.ParseDayNight();
                var msg = $"Players can now vote for ";
                switch (DayNight) {
                    case MapDayNight.Day:
                        this.Server.ServerSettings.CanVoteDay = true;
                        this.Server.ServerSettings.CanVoteNight = false;
                        msg += "Day";
                        break;
                    case MapDayNight.Night:
                        this.Server.ServerSettings.CanVoteDay = false;
                        this.Server.ServerSettings.CanVoteNight = true;
                        msg += "Night";
                        break;
                    default:
                        this.Server.ServerSettings.CanVoteDay = true;
                        this.Server.ServerSettings.CanVoteNight = true;
                        msg += "All";
                        break;
                }
                commandSource.Message(msg);
            }

            [CommandCallback("listmaps", Description = "Lists all maps")]
            public void ListMaps(RunnerPlayer commandSource) {
                commandSource.Message("<b>Available Maps:</b>\n\n" + string.Join("\n", BluscreamLib.Maps.Select(m => $"{m.Name}: {m.DisplayName}")));
            }
            [CommandCallback("listmodes", Description = "Lists all gamemodes")]
            public void ListGameMods(RunnerPlayer commandSource) {
                commandSource.Message("<b>Available Game Modes:</b>\n\n" + string.Join("\n", BluscreamLib.GameModes.Select(m => $"{m.Name}: {m.DisplayName}")));
            }
            [CommandCallback("listsizes", Description = "Lists all game sizes")]
            public void ListGameSizes(RunnerPlayer commandSource) {
                commandSource.Message("<b>Available Sizes:</b>\n\n" + string.Join("\n", Enum.GetValues(typeof(MapSize))));
            }
        [CommandCallback("listmodules", Description = "Lists all loaded modules")]
        public void ListModules(RunnerPlayer commandSource) {

            var moduleType = Assembly.GetEntryAssembly().GetType("BattleBitAPIRunner.Module");
            var moduleListField = moduleType.GetField("Modules", BindingFlags.Static | BindingFlags.Public);
            if (moduleListField is null) return;

            IReadOnlyList<Module> modules = (IReadOnlyList<Module>)moduleListField.GetValue(null);
            commandSource.Message(string.Join(", ", modules.Select(m => m.Name)));
        }

        [CommandCallback("start", Description = "Force starts the round")]
            public void ForceStartRound(RunnerPlayer commandSource) {
                commandSource.Message("Forcing round to start...");
                this.Server.ForceStartGame();
            }
            [CommandCallback("end", Description = "Force ends the round")]
            public void ForceEndRound(RunnerPlayer commandSource) {
                commandSource.Message("Forcing round to end...");
                this.Server.ForceEndGame();
            }
            [CommandCallback("exec", Description = "Executes a command on the server")]
            public void ExecServerCommand(RunnerPlayer commandSource, string command) {
                this.Server.ExecuteCommand(command);
                commandSource.Message($"Executed {command}");
            }
            [CommandCallback("bots", Description = "Spawns bots")]
            public void SpawnBotCommand(RunnerPlayer commandSource, int amount = 1) {
                this.Server.ExecuteCommand($"join bot {amount}");
                commandSource.Message($"Spawned {amount} bots, use !nobots to remove them");
            }
            [CommandCallback("nobots", Description = "Kicks all bots")]
            public void KickBotsCommand(RunnerPlayer commandSource, int amount = 999) {
                this.Server.ExecuteCommand($"remove bot {amount}");
                commandSource.Message($"Kicked {amount} bots");
            }
            [CommandCallback("fire", Description = "Toggles bots firing")]
            public void BotsFireCommand(RunnerPlayer commandSource) {
                this.Server.ExecuteCommand($"bot fire");
                commandSource.Message($"Toggled bots firing");
            }

            [CommandCallback("pos", Description = "Current position (logs to file)")]
            public void PosCommand(RunnerPlayer commandSource) {
                commandSource.Message($"Position: {commandSource.Position}", 5);
                File.AppendAllLines(Configuration.SavedPositionsFile, new[] { $"{this.Server.Map},{this.Server.MapSize},{commandSource.Position.X}|{commandSource.Position.Y}|{commandSource.Position.Z}" });
            }
        #endregion
        public class CommandsConfiguration : ModuleConfiguration {
            public CommandConfiguration map { get; set; } = new CommandConfiguration() { Enabled = true, AllowedRoles = Extensions.ToRoleStringList(MoreRoles.Staff) };
            public CommandConfiguration gamemode { get; set; } = new CommandConfiguration() { Enabled = true, AllowedRoles = Extensions.ToRoleStringList(MoreRoles.Staff) };
            public CommandConfiguration time { get; set; } = new CommandConfiguration() { Enabled = true, AllowedRoles = Extensions.ToRoleStringList(MoreRoles.Staff) };
            public CommandConfiguration maprestart { get; set; } = new CommandConfiguration() { Enabled = true, AllowedRoles = Extensions.ToRoleStringList(MoreRoles.Staff) };
            public CommandConfiguration allowvotetime { get; set; } = new CommandConfiguration() { Enabled = true, AllowedRoles = Extensions.ToRoleStringList(Roles.Admin) };
            public CommandConfiguration listmaps { get; set; } = new CommandConfiguration() { Enabled = true, AllowedRoles = Extensions.ToRoleStringList(MoreRoles.All) };
            public CommandConfiguration listmodes { get; set; } = new CommandConfiguration() { Enabled = true, AllowedRoles = Extensions.ToRoleStringList(MoreRoles.All) };
            public CommandConfiguration listsizes { get; set; } = new CommandConfiguration() { Enabled = true, AllowedRoles = Extensions.ToRoleStringList(MoreRoles.All) };
            public CommandConfiguration listmodules { get; set; } = new CommandConfiguration() { Enabled = true, AllowedRoles = Extensions.ToRoleStringList(MoreRoles.All) };
            public CommandConfiguration start { get; set; } = new CommandConfiguration() { Enabled = true, AllowedRoles = Extensions.ToRoleStringList(MoreRoles.Staff) };
            public CommandConfiguration end { get; set; } = new CommandConfiguration() { Enabled = true, AllowedRoles = Extensions.ToRoleStringList(MoreRoles.Staff) };
            public CommandConfiguration exec { get; set; } = new CommandConfiguration() { Enabled = true, AllowedRoles = Extensions.ToRoleStringList(Roles.Admin) };
            public CommandConfiguration bots { get; set; } = new CommandConfiguration() { Enabled = true, AllowedRoles = Extensions.ToRoleStringList(Roles.Admin) };
            public CommandConfiguration nobots { get; set; } = new CommandConfiguration() { Enabled = true, AllowedRoles = Extensions.ToRoleStringList(Roles.Admin) };
            public CommandConfiguration fire { get; set; } = new CommandConfiguration() { Enabled = true, AllowedRoles = Extensions.ToRoleStringList(Roles.Admin) };
            public CommandConfiguration pos { get; set; } = new CommandConfiguration() { Enabled = true, AllowedRoles = Extensions.ToRoleStringList(Roles.Admin) };
        }
        public class MoreCommandsConfiguration : ModuleConfiguration {
            public string SavedPositionsFile { get; set; } = ".data/SavedPositions.txt";
        }
    }
}