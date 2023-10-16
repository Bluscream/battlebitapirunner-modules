using System;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;

using BBRAPIModules;
using BattleBitAPI.Common;
using System.Linq;
using BattleBitAPI.Server;
using System.Text.Json.Serialization;
using Discord.Webhook;

namespace Bluscream {
    #region Requires
    [RequireModule(typeof(Bluscream.GeoApi))]
    [RequireModule(typeof(Bluscream.SteamApi))]
    [RequireModule(typeof(Bluscream.BluscreamLib))]
    [RequireModule(typeof(Permissions.GranularPermissions))]
    [RequireModule(typeof(Commands.CommandHandler))]
    #endregion
    [Module("Logger", "2.0.1")]
    public class Logger : BattleBitModule {
        public static ModuleInfo ModuleInfo = new() {
            Name = "Logger",
            Description = "Extensive customizable logging for the BattleBit Modular API",
            Version = new Version(2,0,1),
            Author = "Bluscream",
            WebsiteUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/"),
            UpdateUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/raw/master/modules/Logger.cs"),
            SupportUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/issues/new?title=Logger")
        };

        #region References
        [ModuleReference]
        public Commands.CommandHandler CommandHandler { get; set; } = null!;

        [ModuleReference]
        public BluscreamLib BluscreamLib { get; set; } = null!;

        [ModuleReference]
//#if DEBUG
        public GeoApi GeoApi { get; set; } = null!;
//#else
//        public dynamic? GeoApi { get; set; }
//#endif

        [ModuleReference]
//#if DEBUG
        public SteamApi SteamApi { get; set; } = null!;
//#else
//        public dynamic SteamApi { get; set; }
//#endif

        [ModuleReference]
#if DEBUG
        public Permissions.GranularPermissions? GranularPermissions { get; set; } = null!;
#else
        public dynamic? GranularPermissions { get; set; }
#endif
        #endregion

        #region Fields
        internal HttpClient httpClient = new HttpClient();
        internal Random random = Random.Shared;
        #endregion

        #region Methods
        internal void LogToConsole(string msg) {
            if (string.IsNullOrWhiteSpace(msg)) return;
            Console.WriteLine(msg);
        }
        private async Task SendToWebhook(string webhookUrl, string msg) {
            if (string.IsNullOrWhiteSpace(webhookUrl) || string.IsNullOrWhiteSpace(msg)) return;
            var success = Uri.TryCreate(webhookUrl, new UriCreationOptions(), out var url);
            if (!success || url is null) return;
            //var pathTokens = url.AbsolutePath.Split("/").ToList();
            //pathTokens.RemoveAll(s => string.IsNullOrEmpty(s));
            //var idToken = url.AbsolutePath.Substring(url.AbsolutePath.LastIndexOf('/') + 1).Split('/');
            //ulong webhookId = ulong.Parse(pathTokens[0]);
            //string webhookToken = pathTokens[1];
            using (var client = new DiscordWebhookClient(url.AbsoluteUri)) {
                success = false;
                while (!success) {
                    try {
                        await client.SendMessageAsync(text: msg);
                        success = true;
                    } catch (Exception ex) {
                        // Console.WriteLine($"Failed to POST webhook: {ex.Message}");
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                }
            }
        }
        internal void SayToAll(string msg) {
            if (this.Server is null || string.IsNullOrWhiteSpace(msg)) return;
            if (string.IsNullOrWhiteSpace(msg)) return;
            this.Server.SayToAllChat(msg);
        }
        internal void SayToPlayer(string msg, RunnerPlayer player) {
            if (this.Server is null || string.IsNullOrWhiteSpace(msg)) return;
            if (string.IsNullOrWhiteSpace(msg)) return;
            Server.SayToChat(msg, player);
        }
        internal void ModalMessage(string msg, RunnerPlayer player) {
            if (this.Server is null || string.IsNullOrWhiteSpace(msg)) return;
            if (string.IsNullOrWhiteSpace(msg)) return;
            player.Message(msg);
        }
        internal void UILogOnServer(string msg, Duration duration) {
            if (this.Server is null || string.IsNullOrWhiteSpace(msg)) return;
            var durationS = 1;
            switch (duration) {
                case Duration.Short:
                    durationS = 3; break;
                case Duration.Long:
                    durationS = 10; break;
            }
            this.Server.UILogOnServer(msg, durationS);
        }
        internal void Announce(string msg, Duration duration) {
            if (this.Server is null || !this.Server.IsConnected || string.IsNullOrWhiteSpace(msg)) return;
            try {
                switch (duration) {
                    case Duration.Short:
                        this.Server.AnnounceShort(msg); return;
                    case Duration.Long:
                        this.Server.AnnounceLong(msg); return;
                }
            } catch (Exception ex) {
                Console.WriteLine($"Got exception {ex.Message} while trying to announce to players");
            }
        }

        internal string FormatString(string input, RunnerServer? server = null, RunnerPlayer? player = null, RunnerPlayer? target = null, IpApi.Response? geoData = null,
            SteamWebApi.Response? steamData = null, ReportReason? reportReason = null, ChatChannel? chatChannel = null, string? msg = null, long? oldSessionId = null, long? newSessionId = null,
            GameState? oldState = null, GameState? newState = null, PlayerJoiningArguments? playerJoiningArguments = null, ulong? steamId64 = null, PlayerStats? playerStats = null) {
            var now = string.IsNullOrWhiteSpace(Config.TimeStampFormat) ? "" : new DateTimeWithZone(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById(Config.TimeZone)).LocalTime.ToString(Config.TimeStampFormat);
            input = input.Replace("{now}", now);
            if (steamId64 is not null) input = input.ReplaceDiscord("player.SteamID", steamId64);
            if (player is not null) {
                input = input.ReplaceDiscord("player.Name", player?.Name);
                input = input.ReplaceDiscord("player.SteamID", player?.SteamID);
                input = input.ReplaceDiscord("player.str()", player?.str());
                input = input.ReplaceDiscord("player.fullstr()", player?.fullstr());
                input = input.ReplaceDiscord("player.IP", player?.IP);
            }
            if (target is not null) {
                input = input.ReplaceDiscord("target.Name", target.Name);
                input = input.ReplaceDiscord("target.SteamID", target.SteamID);
                input = input.ReplaceDiscord("target.str()", target.str());
                input = input.ReplaceDiscord("target.fullstr()", target.fullstr());
                input = input.ReplaceDiscord("target.IP", target.IP);
            }
            if (server is not null) {
                try { input = input.ReplaceDiscord("server.Name", server?.ServerName); } catch { }
                try { input = input.ReplaceDiscord("server.AllPlayers.Count()", server?.AllPlayers.Count()); } catch { }
                try { input = input.ReplaceDiscord("server.str()", server?.str()); } catch { }
            }
            if (geoData is not null) {
                input = input.ReplaceDiscord("geoData.City", geoData.City);
                input = input.ReplaceDiscord("geoData.RegionName", geoData.RegionName);
                input = input.ReplaceDiscord("geoData.Country", geoData.Country);
                input = input.ReplaceDiscord("geoData.CountryCode", geoData.CountryCode?.ToLowerInvariant());
                input = input.ReplaceDiscord("geoData.Continent", geoData.Continent);
                input = input.ReplaceDiscord("geoData.Isp", geoData.Isp);
                input = input.ReplaceDiscord("geoData.Timezone", geoData.Timezone);
                input = input.ReplaceDiscord("geoData.Reverse", geoData.Reverse);
                input = input.ReplaceDiscord("geoData.ToJson()", geoData.ToJson());
                input = input.ReplaceDiscord("geoData.ToJson(true)", geoData.ToJson(true));
            }
            if (steamData is not null) {
                input = input.ReplaceDiscord("steamData.CountryCode", steamData.Summary?.CountryCode?.ToLowerInvariant());
                input = input.ReplaceDiscord("steamData.ToJson()", steamData.ToJson());
                input = input.ReplaceDiscord("steamData.ToJson(true)", steamData.ToJson(true));
            }
            if (playerJoiningArguments is not null && steamId64 is not null) {
                var steam = SteamApi?._GetData((ulong)steamId64).Result;
                if (steam is not null) {
                    if (geoData?.CountryCode is null) input = input.ReplaceDiscord("geoData.CountryCode", steam.Summary?.CountryCode?.ToLowerInvariant());
                    if (player?.Name is null) {
                        input = input.ReplaceDiscord("player.Name", steam.Summary?.PersonaName);
                        input = input.ReplaceDiscord("player.str()", $"\"{steam.Summary?.PersonaName}\" ({steam.Summary?.SteamId64})");
                    }
                }
                input = input.ReplaceDiscord("BannedOrP", playerJoiningArguments.Stats.IsBanned ? "Banned p" : "P");
                input = input.ReplaceDiscord("playerJoiningArguments.Stats.IsBanned.ToYesNo()", playerJoiningArguments.Stats.IsBanned.ToYesNo());
                input = input.ReplaceDiscord("playerJoiningArguments.Stats.Progress.Rank", playerJoiningArguments.Stats.Progress.Rank);
                input = input.ReplaceDiscord("playerJoiningArguments.Progress.Progress.Prestige", playerJoiningArguments.Stats.Progress.Prestige);
                input = input.ReplaceDiscord("playerJoiningArguments.Stats.Roles.ToRoleString()", playerJoiningArguments.Stats.Roles.ToRoleString());
                input = input.ReplaceDiscord("playerJoiningArguments.Progress.Progress.Rank", playerJoiningArguments.Stats.Progress.Rank);
            }
            input = input.ReplaceDiscord("reason", reportReason);
            input = input.ReplaceDiscord("msg", msg);
            input = input.ReplaceDiscord("oldState", oldState);
            input = input.ReplaceDiscord("newState", newState);
            input = input.ReplaceDiscord("oldSessionId", oldSessionId);
            input = input.ReplaceDiscord("newSessionId", newSessionId);
            switch (chatChannel) {
                case ChatChannel.SquadChat: input = input.ReplaceDiscord("chatChannel", $"{player?.Team.ToCountryCode()}-{player?.SquadName} > "); break;
                case ChatChannel.TeamChat: input = input.ReplaceDiscord("chatChannel", $"{player?.Team.ToCountryCode()} > "); break;
                default: input = input.ReplaceDiscord("chatChannel", string.Empty); break;
            }
            
            foreach (var replacement in Config.randomReplacements) {
                input = input.Replace($"{{random.{replacement.Key}}}", replacement.Value[random.Next(replacement.Value.Count)]);
            }
            return input; // Smart.Format(input, now=now, parms);
        }

        internal async void HandleEvent(LogConfigurationEntry config, RunnerPlayer? player = null, RunnerPlayer? target = null, IpApi.Response? geoData = null, SteamWebApi.Response? steamData = null,
            ReportReason? reportReason = null, ChatChannel? chatChannel = null, string? _msg = null, long? oldSessionId = null, long? newSessionId = null, GameState? oldState = null,
            GameState? newState = null, PlayerJoiningArguments? playerJoiningArguments = null, ulong? steamId64 = null, PlayerStats? playerStats = null) {
            if (player is not null && geoData is null) geoData = await GeoApi.GetData(player);
            if (config.Console is not null && config.Console.Enabled && !string.IsNullOrWhiteSpace(config.Console.Message)) {
                LogToConsole(FormatString(config.Console.Message, server: this.Server, player: player, target: target, geoData: geoData, steamData: steamData, reportReason: reportReason,
                    chatChannel: chatChannel, msg: _msg, oldSessionId: oldSessionId, newSessionId: newSessionId, oldState: oldState, newState: newState, playerJoiningArguments: playerJoiningArguments,
                    steamId64: steamId64)) ;
            }
            if (config.Discord is not null && config.Discord.Enabled && !string.IsNullOrWhiteSpace(config.Discord.WebhookUrl) && !string.IsNullOrWhiteSpace(config.Discord.Message)) {
                var msg = FormatString(config.Discord.Message, server: this.Server, player: player, target: target, geoData: geoData, steamData: steamData, reportReason: reportReason,
                    chatChannel: chatChannel, msg: _msg, oldSessionId: oldSessionId, newSessionId: newSessionId, oldState: oldState, newState: newState, playerJoiningArguments: playerJoiningArguments,
                    steamId64: steamId64);
                await SendToWebhook(config.Discord.WebhookUrl, msg);
            }
            try { var a = this.Server.IsConnected; } catch { return; }
            if (this.Server is null || !this.Server.IsConnected) return;
            if (config.Chat is not null && config.Chat.Enabled && !string.IsNullOrWhiteSpace(config.Chat.Message)) {
                var msg = FormatString(config.Chat.Message, server: this.Server, player: player, target: target, geoData: geoData, steamData: steamData, reportReason: reportReason,
                    chatChannel: chatChannel, msg: _msg, oldSessionId: oldSessionId, newSessionId: newSessionId, oldState: oldState, newState: newState, playerJoiningArguments: playerJoiningArguments
                    , steamId64: steamId64);
                if (this.GranularPermissions is not null && config.Chat.Permissions.Count > 0) {
                    try {
                        foreach (var _player in this.Server.AllPlayers) {
                            if (!Extensions.HasAnyPermissionOf(_player, this.GranularPermissions, config.Chat.Permissions)) SayToPlayer(msg, player: _player);
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"Got exception {ex.Message} while trying to send message to players");
                    }
                } else SayToAll(msg);
            }
            if (config.Modal is not null && config.Modal.Enabled && !string.IsNullOrWhiteSpace(config.Modal.Message)) {
                var msg = FormatString(config.Modal.Message, server: this.Server, player: player, target: target, geoData: geoData, steamData: steamData, reportReason: reportReason,
                    chatChannel: chatChannel, msg: _msg, oldSessionId: oldSessionId, newSessionId: newSessionId, oldState: oldState, newState: newState, playerJoiningArguments: playerJoiningArguments
                    , steamId64: steamId64);
                foreach (var _player in this.Server.AllPlayers) {
                    if(this.GranularPermissions is not null && config.Modal.Permissions.Count > 0) {
                        if (!Extensions.HasAnyPermissionOf(_player, this.GranularPermissions, config.Modal.Permissions)) continue;
                    }
                    ModalMessage(msg, player: _player);
                }
            }
            if (config.UILog is not null && config.UILog.Enabled && !string.IsNullOrWhiteSpace(config.UILog.Message)) {
                var msg = FormatString(config.UILog.Message, server: this.Server, player: player, target: target, geoData: geoData, steamData: steamData, reportReason: reportReason,
                    chatChannel: chatChannel, msg: _msg, oldSessionId: oldSessionId, newSessionId: newSessionId, oldState: oldState, newState: newState, playerJoiningArguments: playerJoiningArguments
                    , steamId64: steamId64);
                UILogOnServer(msg, config.UILog.Duration);
            }
            if (config.Announce is not null && config.Announce.Enabled && !string.IsNullOrWhiteSpace(config.Announce.Message)) {
                var msg = FormatString(config.Announce.Message, server: this.Server, player: player, target: target, geoData: geoData, steamData: steamData, reportReason: reportReason,
                    chatChannel: chatChannel, msg: _msg, oldSessionId: oldSessionId, newSessionId: newSessionId, oldState: oldState, newState: newState, playerJoiningArguments: playerJoiningArguments
                    , steamId64: steamId64);
                Announce(msg, config.Announce.Duration);
            }
        }
        #endregion

        #region Events
        public override void OnModulesLoaded() {
            Extensions.OnPlayerKicked += OnPlayerKicked;
            GeoApi.OnPlayerDataReceived += GeoApi_OnPlayerDataReceived;
            SteamApi.OnPlayerDataReceived += SteamApi_OnPlayerDataReceived;
            HandleEvent(Config.OnApiModulesLoaded);
        }

        private void SteamApi_OnPlayerDataReceived(RunnerPlayer player, SteamWebApi.Response steamData) {
            HandleEvent(Config.OnSteamDataReceived, player: player, steamData: steamData);
        }

        private void GeoApi_OnPlayerDataReceived(RunnerPlayer player, IpApi.Response geoData) {
            HandleEvent(Config.OnGeoDataReceived, player: player, geoData: geoData);
        }

        public override Task OnConnected() {
            HandleEvent(Config.OnApiConnected);
            return Task.CompletedTask;
        }

        public override Task OnPlayerJoiningToServer(ulong steamID, PlayerJoiningArguments args) {
            HandleEvent(Config.OnPlayerJoiningToServer, steamId64: steamID, playerJoiningArguments: args);
            return Task.CompletedTask;
        }
        public override async Task OnPlayerConnected(RunnerPlayer player) {
            Task.Delay(TimeSpan.FromSeconds(1)).Wait();
            HandleEvent(Config.OnPlayerConnected, player: player);
        }
        public override Task OnSavePlayerStats(ulong steamID, PlayerStats stats) {
            HandleEvent(Config.OnSavePlayerStats, steamId64: steamID, playerStats: stats);
            return Task.CompletedTask;
        }

        public override async Task<bool> OnPlayerRequestingToChangeRole(RunnerPlayer player, GameRole requestedRole) {
            HandleEvent(Config.OnPlayerRequestingToChangeRole, player: player, _msg: requestedRole.ToString());
            return true;
        }
        public override async Task OnPlayerChangedRole(RunnerPlayer player, GameRole role) {
            HandleEvent(Config.OnPlayerChangedRole, player: player, _msg: role.ToString());
        }

        public override async Task<bool> OnPlayerRequestingToChangeTeam(RunnerPlayer player, Team requestedTeam) {
            HandleEvent(Config.OnPlayerRequestingToChangeTeam, player: player, _msg: requestedTeam.ToString());
            return true;
        }
        public override async Task OnPlayerChangeTeam(RunnerPlayer player, Team team) {
            HandleEvent(Config.OnPlayerChangeTeam, player: player, _msg: team.ToString());
        }

        public override async Task OnPlayerJoinedSquad(RunnerPlayer player, Squad<RunnerPlayer> squad) {
            HandleEvent(Config.OnPlayerJoinedSquad, player: player, _msg: squad.Name.ToString());
        }
        public override async Task OnPlayerLeftSquad(RunnerPlayer player, Squad<RunnerPlayer> squad) {
            HandleEvent(Config.OnPlayerLeftSquad, player: player, _msg: squad.Name.ToString());
        }

        public override async Task OnSquadLeaderChanged(Squad<RunnerPlayer> squad, RunnerPlayer newLeader) {
            HandleEvent(Config.OnSquadLeaderChanged, player: newLeader, _msg: squad.Name.ToString());
        }

        public override async Task<OnPlayerSpawnArguments?> OnPlayerSpawning(RunnerPlayer player, OnPlayerSpawnArguments request) {
            HandleEvent(Config.OnPlayerSpawning, player: player, _msg: request.RequestedPoint.ToString());
            return request;
        }

        public override async Task OnPlayerSpawned(RunnerPlayer player) {
            HandleEvent(Config.OnPlayerSpawned, player: player);
        }

        public override async Task OnPlayerDied(RunnerPlayer player) {
            HandleEvent(Config.OnPlayerDied, player: player);
        }

        public override async Task OnPlayerGivenUp(RunnerPlayer player) {
            HandleEvent(Config.OnPlayerGivenUp, player: player);
        }

        public override async Task OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<RunnerPlayer> args) {
            HandleEvent(Config.OnAPlayerDownedAnotherPlayer, player: args.Killer, target: args.Victim);
        }

        public override async Task OnAPlayerRevivedAnotherPlayer(RunnerPlayer from, RunnerPlayer to) {
            HandleEvent(Config.OnAPlayerRevivedAnotherPlayer, player: from, target: to);
        }

        public override Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg) {
            if (this.CommandHandler.IsCommand(msg)) {
                HandleEvent(Config.OnPlayerChatCommand, player: player, chatChannel: channel, _msg: msg);
            } else {
                HandleEvent(Config.OnPlayerChatMessage, player: player, chatChannel: channel, _msg: msg);
            }
            return Task.FromResult(true);
        }
        public override void OnConsoleCommand(string command) {
            if (this.CommandHandler.IsCommand(command)) {
                HandleEvent(Config.OnConsoleCommand, _msg: command);
            } else {
                HandleEvent(Config.OnConsoleChat, _msg: command);
            }
        }
        public override Task OnPlayerReported(RunnerPlayer from, RunnerPlayer to, ReportReason reason, string additional) {
            HandleEvent(Config.OnPlayerReported, player: from, target: to, reportReason: reason, _msg: additional);
            return Task.CompletedTask;
        }

        private void OnPlayerKicked(RunnerPlayer player, string? reason) {
            HandleEvent(Config.OnPlayerKicked, player: player, _msg: reason!);
        }
        public override Task OnPlayerDisconnected(RunnerPlayer player) {
            HandleEvent(Config.OnPlayerDisconnected, player: player);
            return Task.CompletedTask;
        }

        public override async Task OnGameStateChanged(GameState oldState, GameState newState) {
            HandleEvent(Config.OnGameStateChanged, oldState: oldState, newState: newState);
        }

        public override async Task OnRoundStarted() {
            HandleEvent(Config.OnRoundStarted);
        }

        public override async Task OnRoundEnded() {
            HandleEvent(Config.OnRoundEnded);
        }

        public override async Task OnSessionChanged(long oldSessionID, long newSessionID) {
            HandleEvent(Config.OnSessionChanged, oldSessionId: oldSessionID, newSessionId: newSessionID);
        }

        public override Task OnDisconnected() {
            HandleEvent(Config.OnApiDisconnected);
            return Task.CompletedTask;
        }

        public override void OnModuleUnloading() {
            HandleEvent(Config.OnModuleUnloading);
        }
        #endregion

        #region Enums
        public enum Duration {
            None,
            Short,
            Medium,
            Long,
            Infinite
        }
        #endregion

        #region Config
        public class LogConfigurationEntrySettings {
            public bool Enabled { get; set; } = false;
            public string Message { get; set; } = string.Empty;
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public List<string>? Permissions { get; set; } = new();
            public Duration Duration { get; set; } = Duration.None;
        }
        public class DiscordWebhookLogConfigurationEntrySettings : LogConfigurationEntrySettings {
            public string WebhookUrl { get; set; } = string.Empty;
        }
        public class LogConfigurationEntry {
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public LogConfigurationEntrySettings Chat { get; set; } = null!;
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public LogConfigurationEntrySettings Console { get; set; } = null!;
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public LogConfigurationEntrySettings UILog { get; set; } = null!;
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public LogConfigurationEntrySettings Announce { get; set; } = null!;
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public LogConfigurationEntrySettings Modal { get; set; } = null!;
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public LogConfigurationEntrySettings File { get; set; } = null!;
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public DiscordWebhookLogConfigurationEntrySettings Discord { get; set; } = null!;
        }
        public Configuration Config { get; set; } = null!;
        public class Configuration : ModuleConfiguration {
            public string TimeStampFormat { get; set; } = "HH:mm:ss";
            [Obsolete]
            public string TimeZone { get; set; } = System.TimeZone.CurrentTimeZone.StandardName;
            public Dictionary<string, List<string>> randomReplacements = new() {
                { "joined", new() { "joined", "connected", "hailed" } },
            };
            public LogConfigurationEntry OnApiModulesLoaded { get; set; } = new() {
                Chat = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] API Modules loaded", Permissions = { "logger.OnApiModulesLoaded" } },
                Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] API Modules loaded" },
                UILog = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] API Modules loaded" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] API Modules loaded" },
            };
            public LogConfigurationEntry OnApiConnected { get; set; } = new() {
                Chat = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Server connected to API", Permissions = { "logger.OnApiConnected" } },
                Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] {server.str()} connected to API" },
                UILog = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Server connected to API" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] {server.str()} connected to API" },
            };
            public LogConfigurationEntry OnApiDisconnected { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Server disconnected from API", Permissions = { "logger.OnApiDisconnected" } },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Server disconnected from API" },
            };
            public LogConfigurationEntry OnPlayerJoiningToServer { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] {BannedOrP}layer [{playerJoiningArguments.Stats.Roles.ToRoleString()}] {player.str()} is connecting to the server from {geoData.CountryCode} (Prestige: {playerJoiningArguments.Progress.Progress.Prestige} | Rank: {playerJoiningArguments.Stats.Progress.Rank})" },
                UILog = new LogConfigurationEntrySettings() { Enabled = true, Message = "{player.Name} [~]" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] {BannedOrP}layer [{playerJoiningArguments.Stats.Roles.ToRoleString()}] {player.str()} is connecting to the server from :flag_{geoData.CountryCode}: (Prestige: {playerJoiningArguments.Progress.Progress.Prestige} | Rank: {playerJoiningArguments.Stats.Progress.Rank})" }
            };
            public LogConfigurationEntry OnPlayerConnected { get; set; } = new() {
                Chat = new LogConfigurationEntrySettings() { Enabled = true, Message = "[+] {player.Name} {random.joined} from {geoData.Country}", Permissions = { "logger.OnPlayerConnected" } },
                Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] [+] {player.Name} ({player.SteamID})) [{player.IP},{geoData.Country},{geoData.Continent}]" },
                UILog = new LogConfigurationEntrySettings() { Enabled = true, Message = "{player.Name} [+]" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] `{player.str()}`connected from {geoData.Country}, {geoData.Continent} :flag_{geoData.CountryCode}:" },
            };
            public LogConfigurationEntry OnPlayerDisconnected { get; set; } = new() {
                Chat = new LogConfigurationEntrySettings() { Enabled = true, Message = "[-] {player.Name} from {geoData.Country} left", Permissions = { "logger.OnPlayerDisconnected" } },
                Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] [-] {player.Name} ({player.SteamID})) [{player.IP} {geoData.Country}]" },
                UILog = new LogConfigurationEntrySettings() { Enabled = true, Message = "{player.Name} [-]" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] `{player.str()}`from {geoData.Country} :flag_{geoData.CountryCode}: disconnected :arrow_left:" },
            };
            public LogConfigurationEntry OnPlayerKicked { get; set; } = new() {
                Chat = new LogConfigurationEntrySettings() { Enabled = true, Message = "[-] {player.Name} was kicked for {msg}", Permissions = { "logger.OnPlayerKicked" } },
                Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] [-] {player.Name} ({player.SteamID})) [{player.IP}] kicked for {msg}" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] `{player.str()}` kicked :leg:" },
            };
            public LogConfigurationEntry OnPlayerChatMessage { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] {player.str()} says \"{msg}\" in {chatChannel}" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] `{player.Name}` says \"{msg}\" in {chatChannel} :speech_balloon:" },
            };
            public LogConfigurationEntry OnPlayerChatCommand { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] {player.str()} issued command \"{msg}\" in {chatChannel}" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] {chatChannel}`{player.str()}` issued command \"{msg}\" in {chatChannel}" },
            };
            public LogConfigurationEntry OnConsoleCommand { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Console issued command \"{msg}\"" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Console issued command \"{msg}\"" },
            };
            public LogConfigurationEntry OnConsoleChat { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Console wrote \"{msg}\"" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Console wrote \"{msg}\"" },
            };
            public LogConfigurationEntry OnPlayerReported { get; set; } = new() {
                Chat = new LogConfigurationEntrySettings() { Enabled = true, Message = "{target.Name} was reported for {reason}" },
                Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] {player.str()} reported {target.str()} for {reason}: \"{msg}\"" },
                UILog = new LogConfigurationEntrySettings() { Enabled = true, Message = "{target.Name} was reported ({reason})" },
                Modal = new LogConfigurationEntrySettings() { Enabled = true, Message = "{target.fullstr()}\nwas reported by\n{player.fullstr()}\n\nReason: {reason}\n\n\"{msg}\"", Permissions = { "logger.OnPlayerReported" } },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] {target.Name} was reported for {reason} :warning:" },
            };
            public LogConfigurationEntry OnSavePlayerStats { get; set; } = new();
            public LogConfigurationEntry OnPlayerRequestingToChangeRole { get; set; } = new();
            public LogConfigurationEntry OnPlayerChangedRole { get; set; } = new();
            public LogConfigurationEntry OnPlayerRequestingToChangeTeam { get; set; } = new();
            public LogConfigurationEntry OnPlayerChangeTeam { get; set; } = new();
            public LogConfigurationEntry OnPlayerJoinedSquad { get; set; } = new();
            public LogConfigurationEntry OnPlayerLeftSquad { get; set; } = new();
            public LogConfigurationEntry OnSquadLeaderChanged { get; set; } = new();
            public LogConfigurationEntry OnPlayerSpawning { get; set; } = new();
            public LogConfigurationEntry OnPlayerSpawned { get; set; } = new();
            public LogConfigurationEntry OnPlayerDied { get; set; } = new();
            public LogConfigurationEntry OnPlayerGivenUp { get; set; } = new();
            public LogConfigurationEntry OnAPlayerDownedAnotherPlayer { get; set; } = new();
            public LogConfigurationEntry OnAPlayerRevivedAnotherPlayer { get; set; } = new();
            public LogConfigurationEntry OnGameStateChanged { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Game State changed from {oldState} to {newState}" },
                UILog = new LogConfigurationEntrySettings() { Enabled = true, Message = "Game State changed to {newState}" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Game State changed from {oldState} to {newState}" },
            };
            public LogConfigurationEntry OnRoundEnded { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Round ended" },
                UILog = new LogConfigurationEntrySettings() { Enabled = true, Message = "Round ended" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Round ended" },
            };
            public LogConfigurationEntry OnRoundStarted { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Round started" },
                UILog = new LogConfigurationEntrySettings() { Enabled = true, Message = "Round started" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Round started" },
            };
            public LogConfigurationEntry OnSessionChanged { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Game Session changed from {oldSessionId} to {newSessionId}" },
                UILog = new LogConfigurationEntrySettings() { Enabled = true, Message = "Game State changed to {newSessionId}" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Game Session changed from {oldSessionId} to {newSessionId}" },
            };
            public LogConfigurationEntry OnModuleUnloading { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Logger module unloaded" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Logger module unloaded" },
            };
            public LogConfigurationEntry OnSteamDataReceived { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] OnSteamDataReceived: {steamData.ToJson()}" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = false, Message = "[{now}] OnSteamDataReceived: {steamData.ToJson()}" },
            };
            public LogConfigurationEntry OnGeoDataReceived { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] OnGeoDataRecieved: {geoData.ToJson()}" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = false, Message = "[{now}] OnGeoDataRecieved: {geoData.ToJson(true)}" },
            };
        }
        #endregion
    }
}