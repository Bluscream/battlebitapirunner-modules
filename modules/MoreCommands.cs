using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using BattleBitAPI.Common;
using BBRAPIModules;
using System.Text;

namespace Bluscream {
    [RequireModule(typeof(BluscreamLib))]
    [RequireModule(typeof(Commands.CommandHandler))]
    [Module("More Commands", "2.0.0.1")]
    public class MoreCommands : BattleBitModule {
        public static ModuleInfo ModuleInfo = new() {
            Name = "More Commands",
            Description = "More commands for the Battlebit Modular API",
            Version = new Version(2, 0, 0, 1),
            Author = "Bluscream",
            WebsiteUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/"),
            UpdateUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/raw/master/modules/MoreCommands.cs"),
            SupportUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/issues/new?title=MoreCommands")
        };
        public static MoreCommandsConfiguration Configuration { get; set; } = null!;

        [ModuleReference]
        public Commands.CommandHandler CommandHandler { get; set; } = null!;

        public override void OnModulesLoaded() {
            this.CommandHandler.Register(this);
        }
        public string GetCurrentMapInfoString() => $"Current Map:\n\nName: {this.Server.Map.ToMap()?.DisplayName} ({this.Server.Map})\nMode: {this.Server.Gamemode.ToGameMode()?.DisplayName} ({this.Server.Gamemode})\nSize: {this.Server.MapSize}";
        #region commands
        [Commands.CommandCallback("map", Description = "Changes the map", Permissions = new[] { "commands.map" })]
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

            [Commands.CommandCallback("gamemode", Description = "Changes the gamemode", Permissions = new[] { "commands.gamemode" })]
            public void SetGameMode(RunnerPlayer commandSource, string gameMode, string? dayNight = null) {
                if (gameMode is null) {
                    commandSource.Message(GetCurrentMapInfoString()); return;
                }
                SetMap(commandSource, this.Server.Map, gameMode, dayNight);
            }

            [Commands.CommandCallback("time", Description = "Changes the map time", Permissions = new[] { "commands.time" })]
            public void SetMapTime(RunnerPlayer commandSource, string dayNight) {
                if (dayNight is null) {
                        commandSource.Message(GetCurrentMapInfoString()); return;
                }
                SetGameMode(commandSource, this.Server.Gamemode, dayNight);
            }

        [Commands.CommandCallback("maprestart", Description = "Restarts the current map", Permissions = new[] { "commands.maprestart" })]
        public void RestartMap(RunnerPlayer commandSource) {
            SetMapTime(commandSource, this.Server.DayNight.ToString());
        }

        [Commands.CommandCallback("allowvotetime", Description = "Changes the allowed map times for votes", Permissions = new[] { "commands.allowvotetime" })]
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

        [Commands.CommandCallback("list maps", Description = "Lists all maps", Permissions = new[] { "commands.listmaps" })]
        public void ListMaps(RunnerPlayer commandSource) {
            commandSource.Message("<b>Available Maps:</b>\n\n" + string.Join(", ", BluscreamLib.MapDisplayNames));
        }
        [Commands.CommandCallback("list modes", Description = "Lists all gamemodes", Permissions = new[] { "commands.listmodes" })]
        public void ListGameMods(RunnerPlayer commandSource) {
        commandSource.Message("<b>Available Game Modes:</b>\n\n" + string.Join("\n", BluscreamLib.GameModes.Select(m => $"{m.Name}: {m.DisplayName}")));
        }
        [Commands.CommandCallback("list sizes", Description = "Lists all game sizes", Permissions = new[] { "commands.listsizes" })]
        public void ListGameSizes(RunnerPlayer commandSource) {
        commandSource.Message("<b>Available Sizes:</b>\n\n" + string.Join("\n", Enum.GetValues(typeof(MapSize))));
        }

        [Commands.CommandCallback("start", Description = "Force starts the round", Permissions = new[] { "commands.start" })]
        public void ForceStartRound(RunnerPlayer commandSource) {
        commandSource.Message("Forcing round to start...");
            this.Server.ForceStartGame();
        }
        [Commands.CommandCallback("end", Description = "Force ends the round", Permissions = new[] { "commands.end" })]
        public void ForceEndRound(RunnerPlayer commandSource) {
        commandSource.Message("Forcing round to end...");
            this.Server.ForceEndGame();
        }
        [Commands.CommandCallback("exec", Description = "Executes a command on the server", Permissions = new[] { "commands.exec" })]
        public void ExecServerCommand(RunnerPlayer commandSource, string command) {
        this.Server.ExecuteCommand(command);
            commandSource.Message($"Executed {command}");
        }
        [Commands.CommandCallback("bots", Description = "Spawns bots", Permissions = new[] { "commands.bots" })]
        public void SpawnBotCommand(RunnerPlayer commandSource, int amount = 1) {
        this.Server.ExecuteCommand($"join bot {amount}");
            commandSource.Message($"Spawned {amount} bots, use !nobots to remove them");
        }
        [Commands.CommandCallback("nobots", Description = "Kicks all bots", Permissions = new[] { "commands.nobots" })]
        public void KickBotsCommand(RunnerPlayer commandSource, int amount = 999) {
        this.Server.ExecuteCommand($"remove bot {amount}");
            commandSource.Message($"Kicked {amount} bots");
        }
        [Commands.CommandCallback("fire", Description = "Toggles bots firing", Permissions = new[] { "commands.fire" })]
        public void BotsFireCommand(RunnerPlayer commandSource) {
        this.Server.ExecuteCommand($"bot fire");
            commandSource.Message($"Toggled bots firing");
        }

            //[Commands.CommandCallback("tps", Description = "Information about server usage")]
            //public void PosCommand(RunnerPlayer commandSource) {
            //    var cmdName = $"\"{Commands.CommandHandler.CommandConfiguration.CommandPrefix}tps\""; var cmdConfig = MyCommandsConfiguration.tps;
            //    if (!cmdConfig.Enabled) { commandSource.Message($"Command {cmdName} is not enabled on this server!"); return; }
            //    if (PlayerPermissions is not null && !Extensions.HasAnyRoleOf(commandSource, PlayerPermissions, Extensions.ParseRoles(cmdConfig.AllowedRoles))) { commandSource.Message($"You do not have permissions to run {cmdName} on this server!"); return; }
            //    var sb = new StringBuilder();
            //    sb.AppendLine($"CPU: {cpu_ghz_used} / {cpu_ghz_total}");
            //    sb.AppendLine($"Ram: {ram_used} / {ram_total}");
            //    sb.AppendLine($"Upload: {bandwith_used_mbytes_per_second_upload}");
            //    sb.AppendLine($"Download: {bandwith_used_mbytes_per_second_download}");
            //    sb.AppendLine($"Ping (To Google): {ping_to_8_8_8_8}");
            //    System.Net.IPAddress playerIp = commandSource.IP;
            //    sb.AppendLine($"Ping (To You): {ping_to_playerIP}");
            //    commandSource.Message(sb.ToString(), 5);
            //}

            [Commands.CommandCallback("pos", Description = "Current position (logs to file)", Permissions = new[] { "commands.pos" })]
            public void PosCommand(RunnerPlayer commandSource) {
                commandSource.Message($"Position: {commandSource.Position}", 5);
                File.AppendAllLines(Configuration.SavedPositionsFile, new[] { $"{this.Server.Map},{this.Server.MapSize},{commandSource.Position.X}|{commandSource.Position.Y}|{commandSource.Position.Z}" });
            }
        #endregion
        public class MoreCommandsConfiguration : ModuleConfiguration {
            public string SavedPositionsFile { get; set; } = ".data/SavedPositions.txt";
        }
    }
}