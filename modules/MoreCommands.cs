// Version 2.0
using BattleBitAPI.Common;
using BBRAPIModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Commands;
using Bluscream;
using static Bluscream.BluscreamLib;

namespace Bluscream {
    [RequireModule(typeof(BluscreamLibModule))]
    [RequireModule(typeof(CommandHandler))]
    [Module("More Commands", "2.0.0")]
    public class MoreCommands : BattleBitModule {
        public static class ModuleInfo {
            public const string Name = "More Commands";
            public const string Description = "More commands for the Battlebit Modular API";
            public static readonly Version Version = new Version(2, 0, 0);
            public const string UpdateUrl = "https://github.com/Bluscream/battlebitapirunner-modules/raw/master/modules/MoreCommands.cs";
            public const string Author = "Bluscream";
        }

        [ModuleReference]
        public CommandHandler CommandHandler { get; set; }

        public override void OnModulesLoaded() {
            this.CommandHandler.Register(this);
        }

        public string GetCurrentMapInfoString() => $"Current Map:\n\nName: {ResolveGameModeMapNameMatch(this.Server.Map, Maps)?.DisplayName} ({this.Server.Map})\nMode: {ResolveGameModeMapNameMatch(this.Server.Gamemode, GameModes)} ({this.Server.Gamemode})\nSize: {this.Server.MapSize}";

        [CommandCallback("map", Description = "Changes the map", AllowedRoles = Roles.Admin | Roles.Moderator)]
        public void SetMap(RunnerPlayer commandSource, string? mapName = null, string? gameMode = null, string? dayNight = null)
        {
            if (mapName is null) {
                commandSource.Message(GetCurrentMapInfoString()); return;
            }
            var map = ResolveGameModeMapNameMatch(mapName, Maps);
            if (map is null) {
                commandSource.Message($"Map {mapName} could not be found"); return;
            }
            GameModeInfo? mode = null;
            if (gameMode is not null) {
                if (gameMode != null) {
                    mode = ResolveGameModeMapNameMatch(gameMode, GameModes);
                    if (mode is null) {
                        commandSource.Message($"GameMode {gameMode} could not be found"); return;
                    }
                }
            }
            var DayNight = GetDayNightFromString(dayNight);
            this.Server.ChangeMap(map, mode, DayNight);
        }

            [CommandCallback("gamemode", Description = "Changes the gamemode", AllowedRoles = Roles.Admin | Roles.Moderator)]
            public void SetGameMode(RunnerPlayer commandSource, string gameMode, string? dayNight = null) {
                if (gameMode is null) {
                    commandSource.Message(GetCurrentMapInfoString()); return;
                }
                SetMap(commandSource, this.Server.Map, gameMode, dayNight);
            }

            [CommandCallback("time", Description = "Changes the map time", AllowedRoles = Roles.Admin | Roles.Moderator)]
            public void SetMapTime(RunnerPlayer commandSource, string dayNight) {
                if (dayNight is null) {
                    commandSource.Message(GetCurrentMapInfoString()); return;
                }
                SetGameMode(commandSource, this.Server.Gamemode, dayNight);
            }

            [CommandCallback("maprestart", Description = "Restarts the current map", AllowedRoles = Roles.Admin | Roles.Moderator)]
            public void RestartMap(RunnerPlayer commandSource) => SetMapTime(commandSource, this.Server.DayNight.ToString());

            [CommandCallback("votetime", Description = "Changes the allowed map times for votes", AllowedRoles = Roles.Admin | Roles.Moderator)]
            public void SetMapVoteTime(RunnerPlayer commandSource, string dayNightAll) {
                var DayNight = GetDayNightFromString(dayNightAll);
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

            [CommandCallback("start", Description = "Force starts the round", AllowedRoles = Roles.Admin | Roles.Moderator)]
            public void ForceStartRound(RunnerPlayer commandSource) {
                commandSource.Message("Forcing round to start...");
                this.Server.ForceStartGame();
            }
            [CommandCallback("end", Description = "Force ends the round", AllowedRoles = Roles.Admin | Roles.Moderator)]
            public void ForceEndRound(RunnerPlayer commandSource) {
                commandSource.Message("Forcing round to end...");
                this.Server.ForceEndGame();
            }
            [CommandCallback("exec", Description = "Executes a command on the server", AllowedRoles = Roles.Admin)]
            public void ExecServerCommand(RunnerPlayer commandSource, string command) {
                this.Server.ExecuteCommand(command);
                commandSource.Message($"Executed {command}");
            }
            [CommandCallback("bots", Description = "Spawns bots", AllowedRoles = Roles.Admin)]
            public void SpawnBotCommand(RunnerPlayer commandSource, int amount = 1) {
                this.Server.ExecuteCommand($"join bot {amount}");
                commandSource.Message($"Spawned {amount} bots, use !nobots to remove them");
            }
            [CommandCallback("nobots", Description = "Kicks all bots", AllowedRoles = Roles.Admin)]
            public void KickBotsCommand(RunnerPlayer commandSource, int amount = 999) {
                this.Server.ExecuteCommand($"remove bot {amount}");
                commandSource.Message($"Kicked {amount} bots");
            }
            [CommandCallback("fire", Description = "Toggles bots firing", AllowedRoles = Roles.Admin)]
            public void BotsFireCommand(RunnerPlayer commandSource) {
                this.Server.ExecuteCommand($"bot fire");
                commandSource.Message($"Toggled bots firing");
            }

            [CommandCallback("pos", Description = "Current position (logs to file)", AllowedRoles = Roles.Admin)] // YOINK!
            public void PosCommand(RunnerPlayer commandSource) {
                commandSource.Message($"Position: {commandSource.Position}", 5);
                System.IO.File.AppendAllLines(".data/SavedPositions.txt", new[] { $"{this.Server.Map},{this.Server.MapSize},{commandSource.Position.X}|{commandSource.Position.Y}|{commandSource.Position.Z}" });
            }
        }
    }