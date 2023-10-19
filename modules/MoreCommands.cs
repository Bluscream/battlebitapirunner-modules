using BattleBitAPI.Common;
using BBRAPIModules;
using BBRModules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Bluscream {

    [RequireModule(typeof(PaginatorLib))]
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
        public void SetMapCommand(RunnerPlayer? commandSource, string? mapName = null, string? dayNight = null, string? gameMode = null, string? mapSize = null) {
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

        [Commands.CommandCallback("list squads", Description = "Lists all available squad names", ConsoleCommand = true, Permissions = new[] { "commands.listsquads" })]
        public void ListGameSquadsCommand(RunnerPlayer commandSource, int pageNum = 1) {
            List<string> helpLines = Enum.GetNames(typeof(Squads)).ToList();
            var perPage = 30;
            int pages = (int)Math.Ceiling((double)helpLines.Count / perPage);
            if (pageNum < 1 || pageNum > pages) {
                this.Reply($"<color=\"red\">Invalid page number. Must be between 1 and {pages}.", commandSource);
                return;
            }
            commandSource.Message($"<#FFA500>Available squads<br><color=\"white\">{Environment.NewLine}{string.Join(", ", helpLines.Skip((pageNum - 1) * perPage).Take(perPage))}{(pages > 1 ? $"{Environment.NewLine}Page {pageNum} of {pages}{(pageNum < pages ? $" - type !list squads {pageNum + 1} for next page" : "")}" : "")}");
            //string message = new PlaceholderLib(Configuration.TeamFullMessage, "maxPlayers", teamCount + extraPlayers).Run();
            //this.Reply(string.Join("\n", Enum.GetNames(typeof(Squads)).Chunk(26).Select(c=>string.Join(",",c))), commandSource);
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

        [Commands.CommandCallback("pw get", Description = "Gets or sets current server password", ConsoleCommand = true, Permissions = new[] { "commands.pw.get" })]
        public void GetPasswordCommandCommand(RunnerPlayer commandSource) {
            this.Reply(this.Server.IsPasswordProtected ? "Server has a password!" : "Server has no password set!", commandSource);
        }

        [Commands.CommandCallback("pw set", Description = "Gets or sets current server password", ConsoleCommand = true, Permissions = new[] { "commands.pw.set" })]
        public void SetPasswordCommandCommand(RunnerPlayer commandSource, string newPass = "") {
            this.Server.SetNewPassword(newPass.Trim());
            this.Reply(string.IsNullOrEmpty(newPass) ? "Server password removed!" : $"Set server password to {newPass.Trim().Quote()}!", commandSource);
        }

        [Commands.CommandCallback("server list", Description = "Lists running game server process", ConsoleCommand = true, Permissions = new[] { "commands.server.list" })]
        public void ListServersCommand(RunnerPlayer commandSource) {
            var servers = Runner.GetRunningGameServersByName(this.Server.ServerName);
            this.Reply(servers.Select(s => (s.Key.MainWindowTitle)).Join("\n"), commandSource);
        }

        [Commands.CommandCallback("server stop", Description = "Stops the game server process", ConsoleCommand = true, Permissions = new[] { "commands.server.stop" })]
        public void StopServerCommand(RunnerPlayer commandSource) {
            this.Reply("Restarting game server ...", commandSource);
            Task.Delay(1000).Wait();
            var servers = Runner.GetRunningGameServersByName(this.Server.ServerName);
            if (servers.Count < 1) { this.Reply($"Could not find any running game servers with the name \"{this.Server.ServerName}\"", commandSource); return; } else if (servers.Count > 1) { this.Reply($"Found {servers.Count} running game servers with the name \"{this.Server.ServerName}\", aborting!", commandSource); return; }
            servers.First().Key.Exit();
        }

        [Commands.CommandCallback("server restart", Description = "Restarts the game server process", ConsoleCommand = true, Permissions = new[] { "commands.server.restart" })]
        public void RestartServerCommand(RunnerPlayer commandSource) {
            this.Reply("Restarting game server ...", commandSource);
            Task.Delay(1000).Wait();
            var servers = Runner.GetRunningGameServersByName(this.Server.ServerName);
            if (servers.Count < 1) { this.Reply($"Could not find any running game servers with the name \"{this.Server.ServerName}\"", commandSource); return; } else if (servers.Count > 1) { this.Reply($"Found {servers.Count} running game servers with the name \"{this.Server.ServerName}\", aborting!", commandSource); return; }
            servers.First().Key.Restart();
        }

        [Commands.CommandCallback("api restart", Description = "Restarts the API Runner", ConsoleCommand = true, Permissions = new[] { "commands.api.restart" })]
        public void RestartApiCommand(RunnerPlayer commandSource) {
            this.Reply("Restarting API Runner ...", commandSource);
            Task.Delay(1000).Wait();
            Runner.Restart();
        }

        [Commands.CommandCallback("api stop", Description = "Stops the API Runner", ConsoleCommand = true, Permissions = new[] { "commands.api.stop" })]
        public void StopApiCommand(RunnerPlayer commandSource) {
            this.Reply("Stopping API Runner ...", commandSource);
            Task.Delay(1000).Wait();
            Runner.Exit();
        }

        [Commands.CommandCallback("api info", Description = "Shows Information about the API Runner", ConsoleCommand = true, Permissions = new[] { "commands.api.info" })]
        public void ApiInfoCommand(RunnerPlayer commandSource) {
            this.Reply($"<b>{Runner.Name}<b>\nv<b>{Runner.Version}<b>\nBy <b>@rainorigami</b>\nRunning <b>{Runner.Modules.Count}</b> Modules", commandSource);
        }

        [Commands.CommandCallback("api list", Description = "Lists all API Runner modules", ConsoleCommand = true, Permissions = new[] { "commands.api.modules" })]
        public void ApiModuleListCommand(RunnerPlayer commandSource) {
            string modulesText;
            if (Runner.Modules.Count < 10) modulesText = string.Join("\n", Runner.Modules.Select(m => $"\"{m.Name}\" v{m.Version}"));
            else if (Runner.Modules.Count < 20) modulesText = string.Join(", ", Runner.Modules.Select(m => $"\"{m.Name}\" v{m.Version}"));
            else if (Runner.Modules.Count < 25) modulesText = string.Join(", ", Runner.Modules.Select(m => $"{m.Name} v{m.Version}"));
            else if (Runner.Modules.Count < 30) modulesText = string.Join(", ", Runner.Modules.Select(m => $"{m.Name} {m.Version}"));
            else modulesText = string.Join(", ", Runner.Modules.Select(m => m.Name));
            commandSource.Message($"<size=175%>{Runner.Modules.Count} BattleBitAPIRunner modules loaded</size>\n\n{modulesText}");
        }

        [Commands.CommandCallback("api module", Description = "Displays information about a specific module", ConsoleCommand = true, Permissions = new[] { "commands.api.module" })]
        public void ApiModuleInfoCommand(RunnerPlayer commandSource, string moduleName) {
            var name = moduleName.ToLowerInvariant();
            var module = Runner.Modules.Where(m => m.Name.ToLowerInvariant() == name);
            if (!module.Any()) module = Runner.Modules.Where(m => m.Name.ToLowerInvariant().Contains(name));
            if (!module.Any()) { commandSource.SayToChat($"Could not find module with the name \"{name}\""); return; }
            commandSource.Message($"<size=175%>{module.First().Name} v{module.First().Version}</size>\n\n<b>{module.First().Description}");
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

        #endregion commands

        public class MoreCommandsConfiguration : ModuleConfiguration {
            public string SavedPositionsFile { get; set; } = ".data/SavedPositions.txt";
        }
    }
}