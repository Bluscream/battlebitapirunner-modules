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
    [RequireModule(typeof(Permissions.PlayerPermissions))]
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
        public CommandsConfiguration MyCommandsConfiguration { get; set; }

        [ModuleReference]
        public Commands.CommandHandler CommandHandler { get; set; }
        [ModuleReference]
#if DEBUG
        public Permissions.PlayerPermissions? PlayerPermissions { get; set; } = null!;
#else
        public dynamic? PlayerPermissions { get; set; } = null!;
#endif

        public override void OnModulesLoaded() {
            this.CommandHandler.Register(this);
        }
        public string GetCurrentMapInfoString() => $"Current Map:\n\nName: {this.Server.Map.ToMap()?.DisplayName} ({this.Server.Map})\nMode: {this.Server.Gamemode.ToGameMode()?.DisplayName} ({this.Server.Gamemode})\nSize: {this.Server.MapSize}";
        #region commands
        [Commands.CommandCallback("map", Description = "Changes the map")]
        public void SetMap(RunnerPlayer commandSource, string? mapName = null, string? gameMode = null, string? dayNight = null)
        {
            if (mapName is null) {
                commandSource.Message(GetCurrentMapInfoString()); return;
            }
            var cmdName = $"\"{Commands.CommandHandler.CommandConfiguration.CommandPrefix}map\""; var cmdConfig = MyCommandsConfiguration.map;
            if (!cmdConfig.Enabled) { commandSource.Message($"Command {cmdName} is not enabled on this server!"); return; }
            if (PlayerPermissions is not null && !Extensions.HasAnyRoleOf(commandSource, PlayerPermissions, Extensions.ParseRoles(cmdConfig.AllowedRoles))) { commandSource.Message($"You do not have permissions to run {cmdName} on this server!"); return; }
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

            [Commands.CommandCallback("gamemode", Description = "Changes the gamemode")]
            public void SetGameMode(RunnerPlayer commandSource, string gameMode, string? dayNight = null) {
                if (gameMode is null) {
                    commandSource.Message(GetCurrentMapInfoString()); return;
            }
            var cmdName = $"\"{Commands.CommandHandler.CommandConfiguration.CommandPrefix}gamemode\""; var cmdConfig = MyCommandsConfiguration.gamemode;
            if (!cmdConfig.Enabled) { commandSource.Message($"Command {cmdName} is not enabled on this server!"); return; }
            if (PlayerPermissions is not null && !Extensions.HasAnyRoleOf(commandSource, PlayerPermissions, Extensions.ParseRoles(cmdConfig.AllowedRoles))) { commandSource.Message($"You do not have permissions to run {cmdName} on this server!"); return; }
            SetMap(commandSource, this.Server.Map, gameMode, dayNight);
            }

            [Commands.CommandCallback("time", Description = "Changes the map time")]
            public void SetMapTime(RunnerPlayer commandSource, string dayNight) {
                if (dayNight is null) {
                    commandSource.Message(GetCurrentMapInfoString()); return;
            }
            var cmdName = $"\"{Commands.CommandHandler.CommandConfiguration.CommandPrefix}time\""; var cmdConfig = MyCommandsConfiguration.time;
            if (!cmdConfig.Enabled) { commandSource.Message($"Command {cmdName} is not enabled on this server!"); return; }
            if (PlayerPermissions is not null && !Extensions.HasAnyRoleOf(commandSource, PlayerPermissions, Extensions.ParseRoles(cmdConfig.AllowedRoles))) { commandSource.Message($"You do not have permissions to run {cmdName} on this server!"); return; }
            SetGameMode(commandSource, this.Server.Gamemode, dayNight);
            }

        [Commands.CommandCallback("maprestart", Description = "Restarts the current map")]
        public void RestartMap(RunnerPlayer commandSource) {
            var cmdName = $"\"{Commands.CommandHandler.CommandConfiguration.CommandPrefix}maprestart\""; var cmdConfig = MyCommandsConfiguration.maprestart;
            if (!cmdConfig.Enabled) { commandSource.Message($"Command {cmdName} is not enabled on this server!"); return; }
            if (PlayerPermissions is not null && !Extensions.HasAnyRoleOf(commandSource, PlayerPermissions, Extensions.ParseRoles(cmdConfig.AllowedRoles))) { commandSource.Message($"You do not have permissions to run {cmdName} on this server!"); return; }
            SetMapTime(commandSource, this.Server.DayNight.ToString());
        }

        [Commands.CommandCallback("allowvotetime", Description = "Changes the allowed map times for votes")]
        public void SetMapVoteTime(RunnerPlayer commandSource, string dayNightAll) {
        var cmdName = $"\"{Commands.CommandHandler.CommandConfiguration.CommandPrefix}allowvotetime\""; var cmdConfig = MyCommandsConfiguration.allowvotetime;
        if (!cmdConfig.Enabled) { commandSource.Message($"Command {cmdName} is not enabled on this server!"); return; }
        if (PlayerPermissions is not null && !Extensions.HasAnyRoleOf(commandSource, PlayerPermissions, Extensions.ParseRoles(cmdConfig.AllowedRoles))) { commandSource.Message($"You do not have permissions to run {cmdName} on this server!"); return; }
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

        [Commands.CommandCallback("listmaps", Description = "Lists all maps")]
        public void ListMaps(RunnerPlayer commandSource) {
            var cmdName = $"\"{Commands.CommandHandler.CommandConfiguration.CommandPrefix}listmaps\""; var cmdConfig = MyCommandsConfiguration.listmaps;
            if (!cmdConfig.Enabled) { commandSource.Message($"Command {cmdName} is not enabled on this server!"); return; }
            if (PlayerPermissions is not null && !Extensions.HasAnyRoleOf(commandSource, PlayerPermissions, Extensions.ParseRoles(cmdConfig.AllowedRoles))) { commandSource.Message($"You do not have permissions to run {cmdName} on this server!"); return; }
            commandSource.Message("<b>Available Maps:</b>\n\n" + string.Join(", ", BluscreamLib.Maps.Select(m => m.DisplayName)));
        }
        [Commands.CommandCallback("listmodes", Description = "Lists all gamemodes")]
        public void ListGameMods(RunnerPlayer commandSource) {
        var cmdName = $"\"{Commands.CommandHandler.CommandConfiguration.CommandPrefix}listmodes\""; var cmdConfig = MyCommandsConfiguration.listmodes;
        if (!cmdConfig.Enabled) { commandSource.Message($"Command {cmdName} is not enabled on this server!"); return; }
        if (PlayerPermissions is not null && !Extensions.HasAnyRoleOf(commandSource, PlayerPermissions, Extensions.ParseRoles(cmdConfig.AllowedRoles))) { commandSource.Message($"You do not have permissions to run {cmdName} on this server!"); return; }
        commandSource.Message("<b>Available Game Modes:</b>\n\n" + string.Join("\n", BluscreamLib.GameModes.Select(m => $"{m.Name}: {m.DisplayName}")));
        }
        [Commands.CommandCallback("listsizes", Description = "Lists all game sizes")]
        public void ListGameSizes(RunnerPlayer commandSource) {
        var cmdName = $"\"{Commands.CommandHandler.CommandConfiguration.CommandPrefix}listsizes\""; var cmdConfig = MyCommandsConfiguration.listsizes;
        if (!cmdConfig.Enabled) { commandSource.Message($"Command {cmdName} is not enabled on this server!"); return; }
        if (PlayerPermissions is not null && !Extensions.HasAnyRoleOf(commandSource, PlayerPermissions, Extensions.ParseRoles(cmdConfig.AllowedRoles))) { commandSource.Message($"You do not have permissions to run {cmdName} on this server!"); return; }
        commandSource.Message("<b>Available Sizes:</b>\n\n" + string.Join("\n", Enum.GetValues(typeof(MapSize))));
        }

        [Commands.CommandCallback("start", Description = "Force starts the round")]
        public void ForceStartRound(RunnerPlayer commandSource) {
        var cmdName = $"\"{Commands.CommandHandler.CommandConfiguration.CommandPrefix}start\""; var cmdConfig = MyCommandsConfiguration.start;
        if (!cmdConfig.Enabled) { commandSource.Message($"Command {cmdName} is not enabled on this server!"); return; }
        if (PlayerPermissions is not null && !Extensions.HasAnyRoleOf(commandSource, PlayerPermissions, Extensions.ParseRoles(cmdConfig.AllowedRoles))) { commandSource.Message($"You do not have permissions to run {cmdName} on this server!"); return; }
        commandSource.Message("Forcing round to start...");
            this.Server.ForceStartGame();
        }
        [Commands.CommandCallback("end", Description = "Force ends the round")]
        public void ForceEndRound(RunnerPlayer commandSource) {
        var cmdName = $"\"{Commands.CommandHandler.CommandConfiguration.CommandPrefix}end\""; var cmdConfig = MyCommandsConfiguration.end;
        if (!cmdConfig.Enabled) { commandSource.Message($"Command {cmdName} is not enabled on this server!"); return; }
        if (PlayerPermissions is not null && !Extensions.HasAnyRoleOf(commandSource, PlayerPermissions, Extensions.ParseRoles(cmdConfig.AllowedRoles))) { commandSource.Message($"You do not have permissions to run {cmdName} on this server!"); return; }
        commandSource.Message("Forcing round to end...");
            this.Server.ForceEndGame();
        }
        [Commands.CommandCallback("exec", Description = "Executes a command on the server")]
        public void ExecServerCommand(RunnerPlayer commandSource, string command) {
        var cmdName = $"\"{Commands.CommandHandler.CommandConfiguration.CommandPrefix}exec\""; var cmdConfig = MyCommandsConfiguration.exec;
        if (!cmdConfig.Enabled) { commandSource.Message($"Command {cmdName} is not enabled on this server!"); return; }
        if (PlayerPermissions is not null && !Extensions.HasAnyRoleOf(commandSource, PlayerPermissions, Extensions.ParseRoles(cmdConfig.AllowedRoles))) { commandSource.Message($"You do not have permissions to run {cmdName} on this server!"); return; }
        this.Server.ExecuteCommand(command);
            commandSource.Message($"Executed {command}");
        }
        [Commands.CommandCallback("bots", Description = "Spawns bots")]
        public void SpawnBotCommand(RunnerPlayer commandSource, int amount = 1) {
        var cmdName = $"\"{Commands.CommandHandler.CommandConfiguration.CommandPrefix}bots\""; var cmdConfig = MyCommandsConfiguration.bots;
        if (!cmdConfig.Enabled) { commandSource.Message($"Command {cmdName} is not enabled on this server!"); return; }
        if (PlayerPermissions is not null && !Extensions.HasAnyRoleOf(commandSource, PlayerPermissions, Extensions.ParseRoles(cmdConfig.AllowedRoles))) { commandSource.Message($"You do not have permissions to run {cmdName} on this server!"); return; }
        this.Server.ExecuteCommand($"join bot {amount}");
            commandSource.Message($"Spawned {amount} bots, use !nobots to remove them");
        }
        [Commands.CommandCallback("nobots", Description = "Kicks all bots")]
        public void KickBotsCommand(RunnerPlayer commandSource, int amount = 999) {
        var cmdName = $"\"{Commands.CommandHandler.CommandConfiguration.CommandPrefix}nobots\""; var cmdConfig = MyCommandsConfiguration.nobots;
        if (!cmdConfig.Enabled) { commandSource.Message($"Command {cmdName} is not enabled on this server!"); return; }
        if (PlayerPermissions is not null && !Extensions.HasAnyRoleOf(commandSource, PlayerPermissions, Extensions.ParseRoles(cmdConfig.AllowedRoles))) { commandSource.Message($"You do not have permissions to run {cmdName} on this server!"); return; }
        this.Server.ExecuteCommand($"remove bot {amount}");
            commandSource.Message($"Kicked {amount} bots");
        }
        [Commands.CommandCallback("fire", Description = "Toggles bots firing")]
        public void BotsFireCommand(RunnerPlayer commandSource) {
        var cmdName = $"\"{Commands.CommandHandler.CommandConfiguration.CommandPrefix}fire\""; var cmdConfig = MyCommandsConfiguration.fire;
        if (!cmdConfig.Enabled) { commandSource.Message($"Command {cmdName} is not enabled on this server!"); return; }
        if (PlayerPermissions is not null && !Extensions.HasAnyRoleOf(commandSource, PlayerPermissions, Extensions.ParseRoles(cmdConfig.AllowedRoles))) { commandSource.Message($"You do not have permissions to run {cmdName} on this server!"); return; }
        this.Server.ExecuteCommand($"bot fire");
            commandSource.Message($"Toggled bots firing");
        }

        [Commands.CommandCallback("listmodules", Description = "Lists all loaded modules")]
        public void ListModules(RunnerPlayer commandSource) {
            var cmdName = $"\"{Commands.CommandHandler.CommandConfiguration.CommandPrefix}modules\""; var cmdConfig = MyCommandsConfiguration.listmodules;
            if (!cmdConfig.Enabled) { commandSource.Message($"Command {cmdName} is not enabled on this server!"); return; }
            if (PlayerPermissions is not null && !Extensions.HasAnyRoleOf(commandSource, PlayerPermissions, Extensions.ParseRoles(cmdConfig.AllowedRoles))) { commandSource.Message($"You do not have permissions to run {cmdName} on this server!"); return; }

            var moduleType = Assembly.GetEntryAssembly().GetType("BattleBitAPIRunner.Module");
            var moduleListField = moduleType.GetField("Modules", BindingFlags.Static | BindingFlags.Public);
            if (moduleListField is null) return;

            IReadOnlyList<BattleBitAPIRunner.Module> modules = (IReadOnlyList<Module>)moduleListField.GetValue(null);
            commandSource.Message(string.Join(", ", modules.Select(m => m.Name)));
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

            [Commands.CommandCallback("pos", Description = "Current position (logs to file)")]
            public void PosCommand(RunnerPlayer commandSource) {
                var cmdName = $"\"{Commands.CommandHandler.CommandConfiguration.CommandPrefix}pos\""; var cmdConfig = MyCommandsConfiguration.pos;
                if (!cmdConfig.Enabled) { commandSource.Message($"Command {cmdName} is not enabled on this server!"); return; }
                if (PlayerPermissions is not null && !Extensions.HasAnyRoleOf(commandSource, PlayerPermissions, Extensions.ParseRoles(cmdConfig.AllowedRoles))) { commandSource.Message($"You do not have permissions to run {cmdName} on this server!"); return; }
                commandSource.Message($"Position: {commandSource.Position}", 5);
                File.AppendAllLines(Configuration.SavedPositionsFile, new[] { $"{this.Server.Map},{this.Server.MapSize},{commandSource.Position.X}|{commandSource.Position.Y}|{commandSource.Position.Z}" });
            }
        #endregion
        public class CommandsConfiguration : ModuleConfiguration {
            public CommandConfiguration map { get; set; } = new CommandConfiguration() { AllowedRoles = Extensions.ToRoleStringList(MoreRoles.Staff) };
            public CommandConfiguration gamemode { get; set; } = new CommandConfiguration() { AllowedRoles = Extensions.ToRoleStringList(MoreRoles.Staff) };
            public CommandConfiguration time { get; set; } = new CommandConfiguration() { AllowedRoles = Extensions.ToRoleStringList(MoreRoles.Staff) };
            public CommandConfiguration maprestart { get; set; } = new CommandConfiguration() { AllowedRoles = Extensions.ToRoleStringList(MoreRoles.Staff) };
            public CommandConfiguration allowvotetime { get; set; } = new CommandConfiguration() { AllowedRoles = Extensions.ToRoleStringList(Roles.Admin) };
            public CommandConfiguration listmaps { get; set; } = new CommandConfiguration() { AllowedRoles = Extensions.ToRoleStringList(MoreRoles.All) };
            public CommandConfiguration listmodes { get; set; } = new CommandConfiguration() { AllowedRoles = Extensions.ToRoleStringList(MoreRoles.All) };
            public CommandConfiguration listsizes { get; set; } = new CommandConfiguration() { AllowedRoles = Extensions.ToRoleStringList(MoreRoles.All) };
            public CommandConfiguration listmodules { get; set; } = new CommandConfiguration() { AllowedRoles = Extensions.ToRoleStringList(MoreRoles.All) };
            public CommandConfiguration start { get; set; } = new CommandConfiguration() { AllowedRoles = Extensions.ToRoleStringList(MoreRoles.Staff) };
            public CommandConfiguration end { get; set; } = new CommandConfiguration() { AllowedRoles = Extensions.ToRoleStringList(MoreRoles.Staff) };
            public CommandConfiguration exec { get; set; } = new CommandConfiguration() { AllowedRoles = Extensions.ToRoleStringList(Roles.Admin) };
            public CommandConfiguration bots { get; set; } = new CommandConfiguration() { AllowedRoles = Extensions.ToRoleStringList(Roles.Admin) };
            public CommandConfiguration nobots { get; set; } = new CommandConfiguration() { AllowedRoles = Extensions.ToRoleStringList(Roles.Admin) };
            public CommandConfiguration fire { get; set; } = new CommandConfiguration() { AllowedRoles = Extensions.ToRoleStringList(Roles.Admin) };
            public CommandConfiguration pos { get; set; } = new CommandConfiguration() { AllowedRoles = Extensions.ToRoleStringList(Roles.Admin) };
        }
        public class MoreCommandsConfiguration : ModuleConfiguration {
            public string SavedPositionsFile { get; set; } = ".data/SavedPositions.txt";
        }
    }
}