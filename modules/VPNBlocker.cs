using System;
using System.Collections.Generic;
using BBRAPIModules;
using Commands;
using Permissions;

namespace Bluscream {
    [RequireModule(typeof(Bluscream.BluscreamLib))]
    [RequireModule(typeof(Bluscream.GeoApi))]
    [RequireModule(typeof(Commands.CommandHandler))]
    [RequireModule(typeof(Permissions.GranularPermissions))]
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
        public Commands.CommandHandler CommandHandler { get; set; } = null!;

        [ModuleReference]
        public Permissions.GranularPermissions GranularPermissions { get; set; } = null!;

        [ModuleReference]
        public Bluscream.GeoApi GeoApi { get; set; } = null!;
        #endregion

        #region Enums
        public enum BlockMode {
            Blacklist,
            Whitelist,
        }
        #endregion

        #region Methods
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
        public bool CheckWhitelistPermissions(RunnerPlayer player, BlockConfiguration config) {
            //Log($"CheckWhitelistRoles({player.fullstr()}, {config.ToJson()})");
            if (GranularPermissions is not null && player.HasAnyPermissionOf(GranularPermissions, config.WhitelistedPermissions)) {
                this.Logger.Info($"Player {player.str()} would have been kicked for {config.Name}, but has a whitelisted permission ({config.WhitelistedPermissions.ToJson()})");
                return true;
            }
            return false;
        }
        public bool CheckPlayer(RunnerPlayer player, IpApi.Response geoData) {
            //Log($"CheckPlayer({player.fullstr()}, {geoData.ToJson()})");
            if (CheckWhitelistPermissions(player, Config.BlockProxies)) return false;
            if (Config.BlockProxies.Enabled && (geoData.Proxy) == true) { player.Kick(FormatString(Config.BlockProxies.KickMessage, player, geoData)); return true; }
            if (CheckWhitelistPermissions(player, Config.BlockServers)) return false;
            if (Config.BlockServers.Enabled && (geoData.Hosting) == true) { player.Kick(FormatString(Config.BlockServers.KickMessage, player, geoData)); return true; }
            if (CheckWhitelistPermissions(player, Config.BlockMobile)) return false;
            if (Config.BlockMobile.Enabled && (geoData.Mobile) == true) { player.Kick(FormatString(Config.BlockMobile.KickMessage, player, geoData)); return true; }

            if (CheckWhitelistPermissions(player, Config.ISPs)) return false;
            if (Config.ISPs.Enabled && geoData.Isp is not null && (CheckStringListEntry(Config.ISPs, geoData.Isp) == true)) { player.Kick(FormatString(Config.ISPs.KickMessage, player, geoData)); return true; }
            if (CheckWhitelistPermissions(player, Config.Continents)) return false;
            if (Config.Continents.Enabled && geoData.Continent is not null && (CheckStringListEntry(Config.Continents, geoData.Continent) == true)) { player.Kick(FormatString(Config.Continents.KickMessage, player, geoData)); return true; }
            if (CheckWhitelistPermissions(player, Config.Countries)) return false;
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
                this.Logger.Info($"GeoApi could not be found! Is it installed?");
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
        [CommandCallback("blockplayer", Description = "Toggles blocking for a specific player's item", ConsoleCommand = true, Permissions = new[] { "commands.blockplayer" })]
        public void ToggleBlockPlayerCommand(BBRAPIModules.RunnerPlayer commandSource, RunnerPlayer target, string list = "") {
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
        [CommandCallback("block", Description = "Toggles blocking for a specific item", ConsoleCommand = true, Permissions = new[] { "commands.block" })]
        public void ToggleBlockCommand(BBRAPIModules.RunnerPlayer commandSource, string? list = null, string? entry = null) {
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
        public class BlockListConfiguration : BlockConfiguration {
            public string BlockMode { get; set; } = "Blacklist";
            public List<string> List { get; set; } = new List<string>();
        }
        public class BlockConfiguration {
            public string Name = string.Empty;
            public bool Enabled { get; set; } = false;
            public string KickMessage { get; set; } = "Kicked by VPNBlocker!";
            public List<string> WhitelistedPermissions { get; set; } = new() { "vpnblocker.bypass" };
        }
        public Configuration Config { get; set; } = null!;
        public class Configuration : ModuleConfiguration {
            public TimeSpan FailTimeout { get; set; } = TimeSpan.FromMinutes(1);
            public BlockConfiguration BlockProxies { get; set; } = new BlockConfiguration() { Name = "BlockProxies", Enabled = true, WhitelistedPermissions = new() { "vpnblocker.bypass", "vpnblocker.bypass.proxy" }, KickMessage = "Proxies and VPNs are not allowed on this server, disable your VPN and try again!" };
            public BlockConfiguration BlockServers { get; set; } = new BlockConfiguration() { Name = "BlockServers", Enabled = true, WhitelistedPermissions = new() { "vpnblocker.bypass", "vpnblocker.bypass.server" }, KickMessage = "Non-Residential IPs are not allowed on this server!" };
            public BlockConfiguration BlockMobile { get; set; } = new BlockConfiguration() { Name = "BlockMobile", Enabled = false, WhitelistedPermissions = new() { "vpnblocker.bypass", "vpnblocker.bypass.mobile" }, KickMessage = "Mobile Networks are not allowed on this server!" };
            public BlockConfiguration BlockFailed { get; set; } = new BlockConfiguration() { Name = "BlockFailed", Enabled = true, WhitelistedPermissions = new() { "vpnblocker.bypass", "vpnblocker.bypass.failed" }, KickMessage = "Your IP {player.IP} failed to be validated, try again" };
            public BlockListConfiguration ISPs { get; set; } = new BlockListConfiguration() { Name = "ISPs", Enabled = true, WhitelistedPermissions = new() { "vpnblocker.bypass", "vpnblocker.bypass.isp" }, KickMessage = "Sorry, your ISP \"{geoData.Isp}\" is not allowed on this server!", List = new() { "Cloudflare, Inc." } };
            public BlockListConfiguration Continents { get; set; } = new BlockListConfiguration() { Name = "Continents", Enabled = true, WhitelistedPermissions = new() { "vpnblocker.bypass", "vpnblocker.bypass.continent" }, KickMessage = "Sorry, your continent \"{geoData.Continent}\" is not allowed on this server!" };
            public BlockListConfiguration Countries { get; set; } = new BlockListConfiguration() { Name = "Countries", Enabled = true, WhitelistedPermissions = new() { "vpnblocker.bypass", "vpnblocker.bypass.country" }, KickMessage = "Sorry, your country \"{geoData.Country}\" is not allowed on this server!" };
        }
        #endregion
    }
}