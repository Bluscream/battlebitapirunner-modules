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
        public string GetCurrentMapInfoString() {
            var sb = new StringBuilder("Current Map:\n\n");
            sb.AppendLine($"Name: {this.Server.GetCurrentMap()?.DisplayName} ({this.Server.Map})");
            sb.AppendLine($"Mode: {this.Server.GetCurrentGameMode()?.DisplayName} ({this.Server.Gamemode})");
            sb.AppendLine($"Size: {this.Server.MapSize}");
            return sb.ToString();
        }
        #region commands
        [Commands.CommandCallback("map", Description = "Changes the map", ConsoleCommand = true, Permissions = new[] { "commands.map" })]
        public void SetMapCommand(RunnerPlayer? commandSource, string? mapName = null, string? dayNight = null, string? gameMode = null, string? mapSize = null)
        {
            if (mapName is null) {
                this.Reply(GetCurrentMapInfoString(), commandSource); return;
            }
            MapInfo? map = null;
            if (mapName is not null) {
                var maps = mapName.ParseMap();
                if (maps.Count < 1) {
                    this.Reply($"Map {mapName} could not be found", commandSource); return;
                } else if (maps.Count > 1) {
                    this.Reply($"{maps.Count} Maps are matching {mapName}:\n\n{maps.Select(m => m.ToString()).Join("\n")}\n\nPlease specify one!", commandSource); return;
                } else map = maps.FirstOrDefault();
            }
            GameModeInfo? mode = null;
            if (gameMode is not null) {
                var modes = gameMode.ParseGameMode();
                if (modes.Count < 1) {
                    this.Reply($"GameMode {gameMode} could not be found", commandSource); return;
                } else if (modes.Count > 1) {
                    this.Reply($"{modes.Count} GameModes are matching {gameMode}:\n\n{modes.Select(m => m.ToString()).Join("\n")}\n\nPlease specify one!", commandSource); return;
                } else mode = modes.FirstOrDefault();
            }
            MapSize size = BluscreamLib.GetMapSizeFromString(mapSize);
            this.Server.ChangeMap(map, mode, dayNight, size);
        }

        [Commands.CommandCallback("gamemode", Description = "Changes the gamemode", ConsoleCommand = true, Permissions = new[] { "commands.gamemode" })]
        public void SetGameModeCommand(RunnerPlayer commandSource, string gameMode, string? dayNight = null, string? mapSize = null) {
            if (gameMode is null) {
                this.Reply(GetCurrentMapInfoString(), commandSource); return;
            }
            SetMapCommand(commandSource, this.Server.Map, dayNight, gameMode, mapSize);
        }

        [Commands.CommandCallback("time", Description = "Changes the map time", ConsoleCommand = true, Permissions = new[] { "commands.time" })]
        public void SetMapTimeCommand(RunnerPlayer commandSource, string? dayNight = null, string? gameMode = null, string? mapSize = null) {
            if (dayNight is null) {
                    this.Reply(GetCurrentMapInfoString(), commandSource); return;
            }
            SetMapCommand(commandSource, this.Server.Map, dayNight, gameMode, mapSize: mapSize);
        }

        [Commands.CommandCallback("maprestart", Description = "Restarts the current map", ConsoleCommand = true, Permissions = new[] { "commands.maprestart" })]
        public void RestartMapCommand(RunnerPlayer commandSource) {
            SetMapTimeCommand(commandSource, this.Server.DayNight.ToString());
        }

        [Commands.CommandCallback("allowvotetime", Description = "Changes the allowed map times for votes", ConsoleCommand = true, Permissions = new[] { "commands.allowvotetime" })]
        public void SetMapVoteTimeCommand(RunnerPlayer commandSource, string dayNightAll) {
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
            this.Reply(msg, commandSource);
        }

        [Commands.CommandCallback("list maps", Description = "Lists all maps", ConsoleCommand = true, Permissions = new[] { "commands.listmaps" })]
        public void ListMapsCommand(RunnerPlayer commandSource) {
            this.Reply("<b>Available Maps:</b>\n\n" + string.Join(", ", BluscreamLib.MapDisplayNames), commandSource);
        }
        [Commands.CommandCallback("list modes", Description = "Lists all gamemodes", ConsoleCommand = true, Permissions = new[] { "commands.listmodes" })]
        public void ListGameModsCommand(RunnerPlayer commandSource) {
        this.Reply("<b>Available Game Modes:</b>\n\n" + string.Join(", ", BluscreamLib.GameModes.Select(m => m.ToString())), commandSource);
        }
        [Commands.CommandCallback("list sizes", Description = "Lists all game sizes", ConsoleCommand = true, Permissions = new[] { "commands.listsizes" })]
        public void ListGameSizesCommand(RunnerPlayer commandSource) {
        this.Reply("<b>Available Sizes:</b>\n\n" + string.Join("\n", BluscreamLib.MapSizeNames), commandSource);
        }

        [Commands.CommandCallback("start", Description = "Force starts the round", ConsoleCommand = true, Permissions = new[] { "commands.start" })]
        public void ForceStartRoundCommand(RunnerPlayer commandSource) {
        this.Reply("Forcing round to start...", commandSource);
            this.Server.ForceStartGame();
        }
        [Commands.CommandCallback("end", Description = "Force ends the round", ConsoleCommand = true, Permissions = new[] { "commands.end" })]
        public void ForceEndRoundCommand(RunnerPlayer commandSource) {
        this.Reply("Forcing round to end...", commandSource);
            this.Server.ForceEndGame();
        }
        [Commands.CommandCallback("exec", Description = "Executes a command on the server", ConsoleCommand = true, Permissions = new[] { "commands.exec" })]
        public void ExecServerCommandCommand(RunnerPlayer commandSource, string command) {
        this.Server.ExecuteCommand(command);
            this.Reply($"Executed {command}", commandSource);
        }
        [Commands.CommandCallback("bots", Description = "Spawns bots", ConsoleCommand = true, Permissions = new[] { "commands.bots" })]
        public void SpawnBotCommandCommand(RunnerPlayer commandSource, int amount = 1) {
        this.Server.ExecuteCommand($"join bot {amount}");
            this.Reply($"Spawned {amount} bots, use !nobots to remove them", commandSource);
        }
        [Commands.CommandCallback("nobots", Description = "Kicks all bots", ConsoleCommand = true, Permissions = new[] { "commands.nobots" })]
        public void KickBotsCommandCommand(RunnerPlayer commandSource, int amount = 999) {
        this.Server.ExecuteCommand($"remove bot {amount}");
            this.Reply($"Kicked {amount} bots", commandSource);
        }
        [Commands.CommandCallback("fire", Description = "Toggles bots firing", ConsoleCommand = true, Permissions = new[] { "commands.fire" })]
        public void BotsFireCommandCommand(RunnerPlayer commandSource) {
        this.Server.ExecuteCommand($"bot fire");
            this.Reply($"Toggled bots firing", commandSource);
        }
        [Commands.CommandCallback("pw", Description = "Gets or sets current server password", ConsoleCommand = true, Permissions = new[] { "commands.pw" })]
        public void SetPasswordCommandCommand(RunnerPlayer commandSource, string? newPass = null) {
            if (newPass is null) {
                this.Reply(this.Server.IsPasswordProtected ? "Server has a password!" : "Server has no password set!", commandSource);
                return;
            }
            this.Server.SetNewPassword(newPass);
            this.Reply(string.IsNullOrEmpty(newPass) ? "Server password removed!" : $"Set server password to {newPass.Quote()}!", commandSource);
        }

        //[Commands.CommandCallback("tps", Description = "Information about server usage")]
        //public void PosCommandCommand(RunnerPlayer commandSource) {
        //    var cmdName = $"\"{Commands.CommandHandler.CommandConfiguration.CommandPrefix}tps\""; var cmdConfig = MyCommandsConfiguration.tps;
        //    if (!cmdConfig.Enabled) { this.Reply($"Command {cmdName} is not enabled on this server!"); return; }
        //    if (PlayerPermissions is not null && !Extensions.HasAnyRoleOf(commandSource, PlayerPermissions, Extensions.ParseRoles(cmdConfig.AllowedRoles))) { this.Reply($"You do not have permissions to run {cmdName} on this server!"); return; }
        //    var sb = new StringBuilder();
        //    sb.AppendLine($"CPU: {cpu_ghz_used} / {cpu_ghz_total}");
        //    sb.AppendLine($"Ram: {ram_used} / {ram_total}");
        //    sb.AppendLine($"Upload: {bandwith_used_mbytes_per_second_upload}");
        //    sb.AppendLine($"Download: {bandwith_used_mbytes_per_second_download}");
        //    sb.AppendLine($"Ping (To Google): {ping_to_8_8_8_8}");
        //    System.Net.IPAddress playerIp = commandSource.IP;
        //    sb.AppendLine($"Ping (To You): {ping_to_playerIP}");
        //    this.Reply(sb.ToString(), 5);
        //}

        [Commands.CommandCallback("pos", Description = "Current position (logs to file)", ConsoleCommand = true, Permissions = new[] { "commands.pos" })]
            public void PosCommandCommand(RunnerPlayer commandSource) {
                this.Reply($"Position: {commandSource.Position}", commandSource);
                File.AppendAllLines(Configuration.SavedPositionsFile, new[] { $"{this.Server.Map},{this.Server.MapSize},{commandSource.Position.X}|{commandSource.Position.Y}|{commandSource.Position.Z}" });
            }
        #endregion
        public class MoreCommandsConfiguration : ModuleConfiguration {
            public string SavedPositionsFile { get; set; } = ".data/SavedPositions.txt";
        }
    }
}