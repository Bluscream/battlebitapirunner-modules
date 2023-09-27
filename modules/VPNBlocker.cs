using System;
using System.Collections.Generic;
using BBRAPIModules;

using Commands;

namespace Bluscream {
    [RequireModule(typeof(Bluscream.BluscreamLib))]
    [RequireModule(typeof(Bluscream.GeoApi))]
    [RequireModule(typeof(Commands.CommandHandler))]
    [Module("Using the GeoApi to block certain players from joining", "2.0.2")]
    public class VPNBlocker : BattleBitModule {
        public static ModuleInfo ModuleInfo = new() {
            Name = "VPNBlocker",
            Description = "Using the GeoApi to block certain players from joining",
            Version = new Version(2, 0, 2),
            Author = "Bluscream",
            WebsiteUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/"),
            UpdateUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/raw/master/modules/GeoApiExample.cs"),
            SupportUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/issues/new?title=GeoApiExample")
        };

        #region References
        [ModuleReference]
        public Commands.CommandHandler CommandHandler { get; set; }

        [ModuleReference]
#if DEBUG
        public Permissions.PlayerPermissions? PlayerPermissions { get; set; }
#else
        public dynamic? PlayerPermissions { get; set; }
#endif

        [ModuleReference]
        public Bluscream.GeoApi? GeoApi { get; set; }
        #endregion

        #region Enums
        public enum BlockMode {
            Blacklist,
            Whitelist,
        }
        #endregion

        #region Methods
        private static void Log(object _msg, string source = "VPNBlocker") => BluscreamLib.Log(_msg, source);
        public string FormatString(string format, RunnerPlayer player, IpApi.Response geoData) {
            return format.
                Replace("{geoData.Isp}", geoData.Isp).
                Replace("{geoData.Continent}", geoData.Continent).
                Replace("{geoData.Country}", geoData.Country).
                Replace("{player.Name}", player.Name).
                Replace("{player.SteamID}", player.SteamID.ToString()).
                Replace("{player.IP}", player.IP.ToString());
        }
        public bool? CheckStringListEntry(BlockListConfiguration config, string entry) {
            switch (Enum.Parse(typeof(BlockMode), config.BlockMode)) {
                case BlockMode.Blacklist:
                    return config.List.Contains(entry);
                case BlockMode.Whitelist:
                    return !config.List.Contains(entry);
            }
            return null;
        }
        public bool CheckWhitelistRoles(RunnerPlayer player, BlockConfiguration config) {
            //Log($"CheckWhitelistRoles({player.fullstr()}, {config.ToJson()})");
            var whitelistedRoles = config.WhitelistedRoles.ParseRoles();
            if (PlayerPermissions is not null && Extensions.HasNoRoleOf(player, PlayerPermissions, whitelistedRoles)) {
                Log($"Player {player.str()} would have been kicked for {config.Name}, but has a whitelisted role ({whitelistedRoles.ToJson()})");
                return true;
            }
            return false;
        }
        public bool CheckPlayer(RunnerPlayer player, IpApi.Response geoData) {
            //Log($"CheckPlayer({player.fullstr()}, {geoData.ToJson()})");
            if (PlayerPermissions is not null && CheckWhitelistRoles(player, Config.BlockProxies)) return false;
            if (Config.BlockProxies.Enabled && (geoData.Proxy) == true) { player.Kick(FormatString(Config.BlockProxies.KickMessage, player, geoData)); return true; }
            if (PlayerPermissions is not null && CheckWhitelistRoles(player, Config.BlockServers)) return false;
            if (Config.BlockServers.Enabled && (geoData.Hosting) == true) { player.Kick(FormatString(Config.BlockServers.KickMessage, player, geoData)); return true; }
            if (PlayerPermissions is not null && CheckWhitelistRoles(player, Config.BlockMobile)) return false;
            if (Config.BlockMobile.Enabled && (geoData.Mobile) == true) { player.Kick(FormatString(Config.BlockMobile.KickMessage, player, geoData)); return true; }

            if (PlayerPermissions is not null && CheckWhitelistRoles(player, Config.ISPs)) return false;
            if (Config.ISPs.Enabled && geoData.Isp is not null && (CheckStringListEntry(Config.ISPs, geoData.Isp) == true)) { player.Kick(FormatString(Config.ISPs.KickMessage, player, geoData)); return true; }
            if (PlayerPermissions is not null && CheckWhitelistRoles(player, Config.Continents)) return false;
            if (Config.Continents.Enabled && geoData.Continent is not null && (CheckStringListEntry(Config.Continents, geoData.Continent) == true)) { player.Kick(FormatString(Config.Continents.KickMessage, player, geoData)); return true; }
            if (PlayerPermissions is not null && CheckWhitelistRoles(player, Config.Countries)) return false;
            if (Config.Countries.Enabled && geoData.Country is not null && (CheckStringListEntry(Config.Countries, geoData.Country) == true)) { player.Kick(FormatString(Config.Countries.KickMessage, player, geoData)); return true; }

            return false;
        }
        public void ToggleBoolEntry(BBRAPIModules.RunnerPlayer commandSource, BlockConfiguration config) {
            config.Enabled = !config.Enabled;
            commandSource.Message($"{config.Name} is now {config.Enabled}");
            Config.Save();
        }
        public void ToggleStringListEntry(BBRAPIModules.RunnerPlayer commandSource, BlockListConfiguration config, string? entry = null) {
            if (string.IsNullOrWhiteSpace(entry)) {
                commandSource.Message($"{config.List.Count} {config.BlockMode}ed {config.Name}:\n\n" + string.Join(", ", config.List));
                return;
            }

            if (config.List.Contains(entry)) {
                config.List.Remove(entry);
                commandSource.Message($"Removed {entry.Quote()} from the list of {config.BlockMode}ed {config.Name}!");
            } else {
                config.List.Add(entry);
                commandSource.Message($"Added {entry.Quote()} to the list of {config.BlockMode}ed {config.Name}!");
            }
            Config.Save();
        }
        #endregion

        #region Events
        public override void OnModulesLoaded() {
            if (GeoApi is null) {
                Log($"GeoApi could not be found! Is it installed?");
            } else {
                this.CommandHandler.Register(this);
                GeoApi.OnPlayerDataReceived += GeoApi_OnPlayerDataReceived;
            }
        }

        private void GeoApi_OnPlayerDataReceived(RunnerPlayer player, IpApi.Response geoData) {
            CheckPlayer(player, geoData);
        }
        #endregion

        #region Commands
        [CommandCallback("blockplayer", Description = "Toggles blocking for a specific player's item")]
        public void ToggleBlockPlayerCommand(BBRAPIModules.RunnerPlayer commandSource, RunnerPlayer target, string list = "") {
            var cmdName = $"\"{CommandHandler.CommandConfiguration.CommandPrefix}blockplayer\""; var cmdConfig = CommandsConfigurationInstance.blockplayer;
            if (!cmdConfig.Enabled) { commandSource.Message($"Command {cmdName} is not enabled on this server!"); return; }
            if (PlayerPermissions is not null && Extensions.HasNoRoleOf(commandSource, PlayerPermissions, cmdConfig.AllowedRoles.ParseRoles())) { commandSource.Message($"You do not have permissions to run {cmdName} on this server!"); return; }
            var geoData = GeoApi?.GetData(target)?.Result;
            if (geoData is null) { commandSource.Message($"Could not fetch geoData for {target.str()}"); return; }
            switch (list.ToLowerInvariant()) {
                case "isp":
                    ToggleStringListEntry(commandSource, Config.ISPs, geoData.Isp); break;
                case "continent":
                    ToggleStringListEntry(commandSource, Config.Continents, geoData.Continent); break;
                case "country":
                    ToggleStringListEntry(commandSource, Config.Countries, geoData.Country); break;
                default:
                    commandSource.Message("Available options:\n\nisp, continent, country"); break;
            }
        }
        [CommandCallback("block", Description = "Toggles blocking for a specific item")]
        public void ToggleBlockCommand(BBRAPIModules.RunnerPlayer commandSource, string? list = null, string? entry = null) {
            var cmdName = $"\"{Commands.CommandHandler.CommandConfiguration.CommandPrefix}block\""; var cmdConfig = CommandsConfigurationInstance.block;
            if (!cmdConfig.Enabled) { commandSource.Message($"Command {cmdName} is not enabled on this server!"); return; }
            if (PlayerPermissions is not null && Extensions.HasNoRoleOf(commandSource, PlayerPermissions, cmdConfig.AllowedRoles.ParseRoles())) { commandSource.Message($"You do not have permissions to run {cmdName} on this server!"); return; }
            if (list is null) {
                commandSource.Message($"VPNBlocker Config:\n" +
                    $"\nBlockProxies: {Config.BlockProxies.Enabled.ToEnabledDisabled()}" +
                    $"\nBlockServers: {Config.BlockServers.Enabled.ToEnabledDisabled()}" +
                    $"\nBlockMobile: {Config.BlockMobile.Enabled.ToEnabledDisabled()}" +
                    $"\nBlockFailed: {Config.BlockFailed.Enabled.ToEnabledDisabled()} (Timeout: {Config.FailTimeout})" +
                    $"\nISPs: {Config.ISPs.Enabled.ToEnabledDisabled()} ({Config.ISPs.List.Count})" +
                    $"\nContinents: {Config.Continents.Enabled.ToEnabledDisabled()} ({Config.Continents.List.Count})" +
                    $"\nCountries: {Config.Countries.Enabled.ToEnabledDisabled()} ({Config.Countries.List.Count})"
                );
                return;
            }
            switch (list.ToLowerInvariant()) {
                case "proxy":
                    ToggleBoolEntry(commandSource, Config.BlockProxies); break;
                case "server":
                    ToggleBoolEntry(commandSource, Config.BlockServers); break;
                case "mobile":
                    ToggleBoolEntry(commandSource, Config.BlockMobile); break;
                case "failed":
                    ToggleBoolEntry(commandSource, Config.BlockFailed); break;
                case "isp":
                    ToggleStringListEntry(commandSource, Config.ISPs, entry); break;
                case "continent":
                    ToggleStringListEntry(commandSource, Config.Continents, entry); break;
                case "country":
                    ToggleStringListEntry(commandSource, Config.Countries, entry); break;
                default:
                    commandSource.Message("Available options:\n\nproxy, server, mobile, failed, isp, continent, country"); break;
            }
        }
        #endregion


        #region Configuration
        public CommandsConfiguration CommandsConfigurationInstance { get; set; }
        public class CommandsConfiguration : ModuleConfiguration {
            public CommandConfiguration block { get; set; } = new CommandConfiguration() { AllowedRoles = Extensions.ToRoleStringList(BattleBitAPI.Common.Roles.Admin) };
            public CommandConfiguration blockplayer { get; set; } = new CommandConfiguration() { AllowedRoles = Extensions.ToRoleStringList(BattleBitAPI.Common.Roles.Admin) };
        }
        public class BlockListConfiguration : BlockConfiguration {
            public string BlockMode { get; set; } = "Blacklist";
            public List<string> List { get; set; } = new List<string>();
        }
        public class BlockConfiguration {
            public string Name = string.Empty;
            public bool Enabled { get; set; } = false;
            public string KickMessage { get; set; } = "Kicked by VPNBlocker!";
            public List<string> WhitelistedRoles { get; set; } = new() { "Admin", "Moderator", "Special", "Vip" };
        }
        public Configuration Config { get; set; }
        public class Configuration : ModuleConfiguration {
            public TimeSpan FailTimeout { get; set; } = TimeSpan.FromMinutes(1);
            public BlockConfiguration BlockProxies { get; set; } = new BlockConfiguration() { Name = "BlockProxies", Enabled = true, KickMessage = "Proxies and VPNs are not allowed on this server, disable your VPN and try again!" };
            public BlockConfiguration BlockServers { get; set; } = new BlockConfiguration() { Name = "BlockServers", Enabled = true, KickMessage = "Non-Residential IPs are not allowed on this server!" };
            public BlockConfiguration BlockMobile { get; set; } = new BlockConfiguration() { Name = "BlockMobile", Enabled = false, KickMessage = "Mobile Networks are not allowed on this server!" };
            public BlockConfiguration BlockFailed { get; set; } = new BlockConfiguration() { Name = "BlockFailed", Enabled = true, KickMessage = "Your IP {player.IP} failed to be validated, try again" };
            public BlockListConfiguration ISPs { get; set; } = new BlockListConfiguration() { Name = "ISPs", Enabled = true, KickMessage = "Sorry, your ISP \"{geoData.Isp}\" is not allowed on this server!", List = new() { "Cloudflare, Inc." } };
            public BlockListConfiguration Continents { get; set; } = new BlockListConfiguration() { Name = "Continents", Enabled = true, KickMessage = "Sorry, your continent \"{geoData.Continent}\" is not allowed on this server!" };
            public BlockListConfiguration Countries { get; set; } = new BlockListConfiguration() { Name = "Countries", Enabled = true, KickMessage = "Sorry, your country \"{geoData.Country}\" is not allowed on this server!" };
        }
        #endregion
    }
}