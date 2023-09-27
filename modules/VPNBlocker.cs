using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Reflection.Metadata.Ecma335;
using BBRAPIModules;

using Commands;
using static Bluscream.GeoApi;
using static Bluscream.VPNBlocker;

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
        public bool CheckPlayer(RunnerPlayer player, IpApi.Response geoData) {
            if (Config.BlockProxies.Enabled && (geoData.Proxy) == true) { player.Kick(FormatString(Config.BlockProxies.KickMessage, player, geoData)); return true; }
            if (Config.BlockServers.Enabled && (geoData.Hosting) == true) { player.Kick(FormatString(Config.BlockServers.KickMessage, player, geoData)); return true; }
            if (Config.BlockMobile.Enabled && (geoData.Mobile) == true) { player.Kick(FormatString(Config.BlockMobile.KickMessage, player, geoData)); return true; }

            if (Config.ISPs.Enabled && geoData.Isp is not null && Config.ISPs.List.Contains(geoData.Isp)) { player.Kick(FormatString(Config.ISPs.KickMessage, player, geoData)); return true; }
            if (Config.Continents.Enabled && geoData.Continent is not null && Config.Continents.List.Contains(geoData.Continent)) { player.Kick(FormatString(Config.Continents.KickMessage, player, geoData)); return true; }
            if (Config.Countries.Enabled && geoData.Country is not null && Config.Countries.List.Contains(geoData.Country)) { player.Kick(FormatString(Config.Countries.KickMessage, player, geoData)); return true; }

            return false;
        }
        public void ToggleBoolEntry(BBRAPIModules.RunnerPlayer commandSource, BlockConfiguration config) {
            config.Enabled = !config.Enabled;
            commandSource.Message($"{nameof(config)} is now {config.Enabled}");
            Config.Save();
        }
        public void ToggleStringListEntry(BBRAPIModules.RunnerPlayer commandSource, BlockListConfiguration config, string? entry = null) {
            var listName = nameof(config);
            if (string.IsNullOrWhiteSpace(entry)) {
                commandSource.Message($"{config.List.Count} {config.BlockMode}ed {listName}:\n\n" + string.Join(", ", config.List));
                return;
            }

            if (config.List.Contains(entry)) {
                config.List.Remove(entry);
                commandSource.Message($"Removed {entry.Quote()} from the list of {config.BlockMode}ed {listName}!");
            } else {
                config.List.Add(entry);
                commandSource.Message($"Added {entry.Quote()} to the list of {config.BlockMode}ed {listName}!");
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
            Log($"Recieved geoData for \"{this.Server.ServerName}\" in {geoData?.Country ?? "Unknown Country"}");
        }
        #endregion

        #region Commands
        [CommandCallback("block", Description = "Toggles blocking for a specific item (proxies, servers, mobile, failed, isp, continent, country")]
        public void ToggleBlockCommand(BBRAPIModules.RunnerPlayer commandSource, string? list = null, string? entry = null) {
            var cmdName = $"\"{Commands.CommandHandler.CommandConfiguration.CommandPrefix}playerinfo\""; var cmdConfig = CommandsConfigurationInstance.block_country;
            if (!cmdConfig.Enabled) { commandSource.Message($"Command {cmdName} is not enabled on this server!"); return; }
            if (PlayerPermissions is not null && !Extensions.HasAnyRoleOf(commandSource, PlayerPermissions, Extensions.ParseRoles(cmdConfig.AllowedRoles))) { commandSource.Message($"You do not have permissions to run {cmdName} on this server!"); return; }
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
                    ToggleStringListEntry(commandSource, Config.ISPs, entry); break;
                case "country":
                    ToggleStringListEntry(commandSource, Config.ISPs, entry); break;
                default:
                    commandSource.Message("Available options:\n\nproxy, server, mobile, failed, isp, continent, country"); break;
            }
            
        }
        #endregion


        #region Configuration
        public CommandsConfiguration CommandsConfigurationInstance { get; set; }
        public class CommandsConfiguration : ModuleConfiguration {
            public CommandConfiguration block_country { get; set; } = new CommandConfiguration() { AllowedRoles = Extensions.ToRoleStringList(BattleBitAPI.Common.Roles.Admin) };
        }
        public class BlockListConfiguration : BlockConfiguration {
            public BlockMode BlockMode { get; set; } = BlockMode.Blacklist;
            public List<string> List { get; set; } = new List<string>();
        }
        public class BlockConfiguration {
            public bool Enabled { get; set; } = false;
            public string KickMessage { get; set; } = "Kicked by VPNBlocker!";
        }
        public Configuration Config { get; set; }
        public class Configuration : ModuleConfiguration {
            public TimeSpan FailTimeout { get; set; } = TimeSpan.FromMinutes(1);
            public BlockConfiguration BlockProxies { get; set; } = new BlockConfiguration() { Enabled = true, KickMessage = "Proxies and VPNs are not allowed on this server, disable your VPN and try again!" };
            public BlockConfiguration BlockServers { get; set; } = new BlockConfiguration() { Enabled = true, KickMessage = "Non-Residential IPs are not allowed on this server!" };
            public BlockConfiguration BlockMobile { get; set; } = new BlockConfiguration() { Enabled = true, KickMessage = "Mobile Networks are not allowed on this server!" };
            public BlockConfiguration BlockFailed { get; set; } = new BlockConfiguration() { Enabled = true, KickMessage = "Your IP failed to be validated, try again" };
            public BlockListConfiguration ISPs { get; set; } = new BlockListConfiguration() { Enabled = true, KickMessage = "Sorry, your ISP \"{geoData.Isp}\" is not allowed on this server!", BlockMode = BlockMode.Blacklist };
            public BlockListConfiguration Continents { get; set; } = new BlockListConfiguration() { Enabled = true, KickMessage = "Sorry, your continent \"{geoData.Continent}\" is not allowed on this server!", BlockMode = BlockMode.Blacklist };
            public BlockListConfiguration Countries { get; set; } = new BlockListConfiguration() { Enabled = true, KickMessage = "Sorry, your country \"{geoData.Country}\" is not allowed on this server!", BlockMode = BlockMode.Blacklist };
        }
        #endregion
    }

    #region Extensions
    public static partial class Extensions {
        public static string ToWhitelistBlacklistString(this BlockMode mode) => mode == BlockMode.Whitelist ? "Whitelist" : "Blacklist";
    }
    #endregion
}