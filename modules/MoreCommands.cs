using BattleBitAPI.Common;
using BBRAPIModules;
using BBRModules;
using Commands;
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
        public void SetMapCommand(Context ctx, string? mapName = null, string? dayNight = null, string? gameMode = null, string? mapSize = null) {
            if (mapName is null) {
                ctx.Reply(GetCurrentMapInfoString()); return;
            }
            MapInfo? map = null;
            if (mapName is not null) {
                var maps = mapName.ParseMap();
                if (maps.Count < 1) {
                    ctx.Reply($"Map {mapName} could not be found"); return;
                } else if (maps.Count > 1) {
                    ctx.Reply($"{maps.Count} Maps are matching {mapName}:\n\n{maps.Select(m => m.ToString()).Join("\n")}\n\nPlease specify one!"); return;
                } else map = maps.FirstOrDefault();
            }
            GameModeInfo? mode = null;
            if (gameMode is not null) {
                var modes = gameMode.ParseGameMode();
                if (modes.Count < 1) {
                    ctx.Reply($"GameMode {gameMode} could not be found"); return;
                } else if (modes.Count > 1) {
                    ctx.Reply($"{modes.Count} GameModes are matching {gameMode}:\n\n{modes.Select(m => m.ToString()).Join("\n")}\n\nPlease specify one!"); return;
                } else mode = modes.FirstOrDefault();
            }
            MapSize size = BluscreamLib.GetMapSizeFromString(mapSize);
            this.Server.ChangeMap(map, mode, dayNight, size);
        }

        [Commands.CommandCallback("gamemode", Description = "Changes the gamemode", ConsoleCommand = true, Permissions = new[] { "commands.gamemode" })]
        public void SetGameModeCommand(Context ctx, string gameMode, string? dayNight = null, string? mapSize = null) {
            if (gameMode is null) {
                ctx.Reply(GetCurrentMapInfoString()); return;
            }
            SetMapCommand(ctx, this.Server.Map, dayNight, gameMode, mapSize);
        }

        [Commands.CommandCallback("time", Description = "Changes the map time", ConsoleCommand = true, Permissions = new[] { "commands.time" })]
        public void SetMapTimeCommand(Context ctx, string? dayNight = null, string? gameMode = null, string? mapSize = null) {
            if (dayNight is null) {
                ctx.Reply(GetCurrentMapInfoString()); return;
            }
            SetMapCommand(ctx, this.Server.Map, dayNight, gameMode, mapSize: mapSize);
        }

        [Commands.CommandCallback("maprestart", Description = "Restarts the current map", ConsoleCommand = true, Permissions = new[] { "commands.maprestart" })]
        public void RestartMapCommand(Context ctx) {
            SetMapTimeCommand(ctx, this.Server.DayNight.ToString());
        }

        [Commands.CommandCallback("allowvotetime", Description = "Changes the allowed map times for votes", ConsoleCommand = true, Permissions = new[] { "commands.allowvotetime" })]
        public void SetMapVoteTimeCommand(Context ctx, string dayNightAll) {
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
            ctx.Reply(msg);
        }

        [Commands.CommandCallback("list maps", Description = "Lists all maps", ConsoleCommand = true, Permissions = new[] { "commands.listmaps" })]
        public void ListMapsCommand(Context ctx) {
            ctx.Reply("<b>Available Maps:</b>\n\n" + string.Join(", ", BluscreamLib.MapDisplayNames));
        }

        [Commands.CommandCallback("list modes", Description = "Lists all gamemodes", ConsoleCommand = true, Permissions = new[] { "commands.listmodes" })]
        public void ListGameModsCommand(Context ctx) {
            ctx.Reply("<b>Available Game Modes:</b>\n\n" + string.Join(", ", BluscreamLib.GameModes.Select(m => m.ToString())));
        }

        [Commands.CommandCallback("list sizes", Description = "Lists all game sizes", ConsoleCommand = true, Permissions = new[] { "commands.listsizes" })]
        public void ListGameSizesCommand(Context ctx) {
            ctx.Reply("<b>Available Sizes:</b>\n\n" + string.Join("\n", BluscreamLib.MapSizeNames));
        }

        [Commands.CommandCallback("list squads", Description = "Lists all available squad names", ConsoleCommand = true, Permissions = new[] { "commands.listsquads" })]
        public void ListGameSquadsCommand(Context ctx, int pageNum = 1) {
            List<string> helpLines = Enum.GetNames(typeof(Squads)).ToList();
            var perPage = 30;
            int pages = (int)Math.Ceiling((double)helpLines.Count / perPage);
            if (pageNum < 1 || pageNum > pages) {
                ctx.Reply($"<color=\"red\">Invalid page number. Must be between 1 and {pages}.");
                return;
            }
            ctx.Reply($"<#FFA500>Available squads<br><color=\"white\">{Environment.NewLine}{string.Join(", ", helpLines.Skip((pageNum - 1) * perPage).Take(perPage))}{(pages > 1 ? $"{Environment.NewLine}Page {pageNum} of {pages}{(pageNum < pages ? $" - type !list squads {pageNum + 1} for next page" : "")}" : "")}");
            //string message = new PlaceholderLib(Configuration.TeamFullMessage, "maxPlayers", teamCount + extraPlayers).Run();
            //ctx.Reply(string.Join("\n", Enum.GetNames(typeof(Squads)).Chunk(26).Select(c=>string.Join(",",c))));
        }

        [Commands.CommandCallback("start", Description = "Force starts the round", ConsoleCommand = true, Permissions = new[] { "commands.start" })]
        public void ForceStartRoundCommand(Context ctx) {
            ctx.Reply("Forcing round to start...");
            this.Server.ForceStartGame();
        }

        [Commands.CommandCallback("end", Description = "Force ends the round", ConsoleCommand = true, Permissions = new[] { "commands.end" })]
        public void ForceEndRoundCommand(Context ctx) {
            ctx.Reply("Forcing round to end...");
            this.Server.ForceEndGame();
        }

        [Commands.CommandCallback("exec", Description = "Executes a command on the server", ConsoleCommand = true, Permissions = new[] { "commands.exec" })]
        public void ExecServerCommandCommand(Context ctx, string command) {
            this.Server.ExecuteCommand(command);
            ctx.Reply($"Executed {command}");
        }

        [Commands.CommandCallback("bots", Description = "Spawns bots", ConsoleCommand = true, Permissions = new[] { "commands.bots" })]
        public void SpawnBotCommandCommand(Context ctx, int amount = 1) {
            this.Server.ExecuteCommand($"join bot {amount}");
            ctx.Reply($"Spawned {amount} bots, use !nobots to remove them");
        }

        [Commands.CommandCallback("nobots", Description = "Kicks all bots", ConsoleCommand = true, Permissions = new[] { "commands.nobots" })]
        public void KickBotsCommandCommand(Context ctx, int amount = 999) {
            this.Server.ExecuteCommand($"remove bot {amount}");
            ctx.Reply($"Kicked {amount} bots");
        }

        [Commands.CommandCallback("fire", Description = "Toggles bots firing", ConsoleCommand = true, Permissions = new[] { "commands.fire" })]
        public void BotsFireCommandCommand(Context ctx) {
            this.Server.ExecuteCommand($"bot fire");
            ctx.Reply($"Toggled bots firing");
        }

        [Commands.CommandCallback("pw get", Description = "Gets or sets current server password", ConsoleCommand = true, Permissions = new[] { "commands.pw.get" })]
        public void GetPasswordCommandCommand(Context ctx) {
            ctx.Reply(this.Server.IsPasswordProtected ? "Server has a password!" : "Server has no password set!");
        }

        [Commands.CommandCallback("pw set", Description = "Gets or sets current server password", ConsoleCommand = true, Permissions = new[] { "commands.pw.set" })]
        public void SetPasswordCommandCommand(Context ctx, string newPass = "") {
            this.Server.SetNewPassword(newPass.Trim());
            ctx.Reply(string.IsNullOrEmpty(newPass) ? "Server password removed!" : $"Set server password to {newPass.Trim().Quote()}!");
        }

        [Commands.CommandCallback("server list", Description = "Lists running game server process", ConsoleCommand = true, Permissions = new[] { "commands.server.list" })]
        public void ListServersCommand(Context ctx) {
            var servers = Runner.GetRunningGameServersByName(this.Server.ServerName);
            ctx.Reply(servers.Select(s => (s.Key.MainWindowTitle)).Join("\n"));
        }

        [Commands.CommandCallback("server stop", Description = "Stops the game server process", ConsoleCommand = true, Permissions = new[] { "commands.server.stop" })]
        public void StopServerCommand(Context ctx) {
            ctx.Reply("Restarting game server ...");
            Task.Delay(1000).Wait();
            var servers = Runner.GetRunningGameServersByName(this.Server.ServerName);
            if (servers.Count < 1) { ctx.Reply($"Could not find any running game servers with the name \"{this.Server.ServerName}\""); return; } else if (servers.Count > 1) { ctx.Reply($"Found {servers.Count} running game servers with the name \"{this.Server.ServerName}\", aborting!"); return; }
            servers.First().Key.Exit();
        }

        [Commands.CommandCallback("server restart", Description = "Restarts the game server process", ConsoleCommand = true, Permissions = new[] { "commands.server.restart" })]
        public void RestartServerCommand(Context ctx) {
            ctx.Reply("Restarting game server ...");
            Task.Delay(1000).Wait();
            var servers = Runner.GetRunningGameServersByName(this.Server.ServerName);
            if (servers.Count < 1) { ctx.Reply($"Could not find any running game servers with the name \"{this.Server.ServerName}\""); return; } else if (servers.Count > 1) { ctx.Reply($"Found {servers.Count} running game servers with the name \"{this.Server.ServerName}\", aborting!"); return; }
            servers.First().Key.Restart();
        }

        [Commands.CommandCallback("api restart", Description = "Restarts the API Runner", ConsoleCommand = true, Permissions = new[] { "commands.api.restart" })]
        public void RestartApiCommand(Context ctx) {
            ctx.Reply("Restarting API Runner ...");
            Task.Delay(1000).Wait();
            Runner.Restart();
        }

        [Commands.CommandCallback("api stop", Description = "Stops the API Runner", ConsoleCommand = true, Permissions = new[] { "commands.api.stop" })]
        public void StopApiCommand(Context ctx) {
            ctx.Reply("Stopping API Runner ...");
            Task.Delay(1000).Wait();
            Runner.Exit();
        }

        [Commands.CommandCallback("api info", Description = "Shows Information about the API Runner", ConsoleCommand = true, Permissions = new[] { "commands.api.info" })]
        public void ApiInfoCommand(Context ctx) {
            ctx.Reply($"<b>{Runner.Name}<b>\nv<b>{Runner.Version}<b>\nBy <b>@rainorigami</b>\nRunning <b>{Runner.Modules.Count}</b> Modules");
        }

        [Commands.CommandCallback("api list", Description = "Lists all API Runner modules", ConsoleCommand = true, Permissions = new[] { "commands.api.modules" })]
        public void ApiModuleListCommand(Context ctx) {
            string modulesText;
            if (Runner.Modules.Count < 10) modulesText = string.Join("\n", Runner.Modules.Select(m => $"\"{m.Name}\" v{m.Version}"));
            else if (Runner.Modules.Count < 20) modulesText = string.Join(", ", Runner.Modules.Select(m => $"\"{m.Name}\" v{m.Version}"));
            else if (Runner.Modules.Count < 25) modulesText = string.Join(", ", Runner.Modules.Select(m => $"{m.Name} v{m.Version}"));
            else if (Runner.Modules.Count < 30) modulesText = string.Join(", ", Runner.Modules.Select(m => $"{m.Name} {m.Version}"));
            else modulesText = string.Join(", ", Runner.Modules.Select(m => m.Name));
            ctx.Reply($"<size=175%>{Runner.Modules.Count} BattleBitAPIRunner modules loaded</size>\n\n{modulesText}");
        }

        [Commands.CommandCallback("api module", Description = "Displays information about a specific module", ConsoleCommand = true, Permissions = new[] { "commands.api.module" })]
        public void ApiModuleInfoCommand(Context ctx, string moduleName) {
            var name = moduleName.ToLowerInvariant();
            var module = Runner.Modules.Where(m => m.Name.ToLowerInvariant() == name);
            if (!module.Any()) module = Runner.Modules.Where(m => m.Name.ToLowerInvariant().Contains(name));
            if (!module.Any()) { ctx.Reply($"Could not find module with the name \"{name}\""); return; }
            ctx.Reply($"<size=175%>{module.First().Name} v{module.First().Version}</size>\n\n<b>{module.First().Description}");
        }

        //[Commands.CommandCallback("tps", Description = "Information about server usage")]
        //public void PosCommandCommand(Context ctx) {
        //    var cmdName = $"\"{Commands.CommandHandler.CommandConfiguration.CommandPrefix}tps\""; var cmdConfig = MyCommandsConfiguration.tps;
        //    if (!cmdConfig.Enabled) { ctx.Reply($"Command {cmdName} is not enabled on this server!"); return; }
        //    if (PlayerPermissions is not null && !Extensions.HasAnyRoleOf(ctx.Source, PlayerPermissions, Extensions.ParseRoles(cmdConfig.AllowedRoles))) { ctx.Reply($"You do not have permissions to run {cmdName} on this server!"); return; }
        //    var sb = new StringBuilder();
        //    sb.AppendLine($"CPU: {cpu_ghz_used} / {cpu_ghz_total}");
        //    sb.AppendLine($"Ram: {ram_used} / {ram_total}");
        //    sb.AppendLine($"Upload: {bandwith_used_mbytes_per_second_upload}");
        //    sb.AppendLine($"Download: {bandwith_used_mbytes_per_second_download}");
        //    sb.AppendLine($"Ping (To Google): {ping_to_8_8_8_8}");
        //    System.Net.IPAddress playerIp = ctx.Source.IP;
        //    sb.AppendLine($"Ping (To You): {ping_to_playerIP}");
        //    ctx.Reply(sb.ToString(), 5);
        //}

        [Commands.CommandCallback("pos", Description = "Current position (logs to file)", ConsoleCommand = false, Permissions = new[] { "commands.pos" })]
        public void PosCommandCommand(Context ctx) {
            var player = (ctx.Source as ChatSource).Invoker;
            ctx.Reply($"Position: {player.Position}");
            File.AppendAllLines(Configuration.SavedPositionsFile, new[] { $"{this.Server.Map},{this.Server.MapSize},{player.Position.X}|{player.Position.Y}|{player.Position.Z}|{player.SteamID}" });
        }

        #endregion commands

        public class MoreCommandsConfiguration : ModuleConfiguration {
            public string SavedPositionsFile { get; set; } = ".data/SavedPositions.txt";
        }
    }
}