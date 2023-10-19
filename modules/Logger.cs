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
using System.Net;

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
        internal class LoggerBase {
            public string ParamName { get; set; }
            public IpApi.Response? GeoData { get; set; }
            public string ReplaceDiscord(string input) {
                if (GeoData is not null) {
                    input = input
                        .ReplaceDiscord($"{ParamName}.geoData.City", GeoData?.City)
                        .ReplaceDiscord($"{ParamName}.geoData.RegionName", GeoData?.RegionName)
                        .ReplaceDiscord($"{ParamName}.geoData.Country", GeoData?.Country)
                        .ReplaceDiscord($"{ParamName}.geoData.CountryCode", GeoData?.CountryCode)
                        .ReplaceDiscord($"{ParamName}.geoData.CountryFlagEmoji", GeoData?.CountryFlagEmoji)
                        .ReplaceDiscord($"{ParamName}.geoData.Continent", GeoData?.Continent)
                        .ReplaceDiscord($"{ParamName}.geoData.Isp", GeoData?.Isp)
                        .ReplaceDiscord($"{ParamName}.geoData.Timezone", GeoData?.Timezone)
                        .ReplaceDiscord($"{ParamName}.geoData.Reverse", GeoData?.Reverse)
                        .ReplaceDiscord($"{ParamName}.geoData.ToJson()", GeoData?.ToJson())
                        .ReplaceDiscord($"{ParamName}.geoData.ToJson(true)", GeoData?.ToJson(true));
                }
                return input;
            }
        }
        internal class LoggerServer : LoggerBase {
            public RunnerServer Server { get; set; }
            public LoggerServer(RunnerServer server, string paramName) {
                ParamName = paramName; Server = server;
                GeoData = server.GetGeoData()?.Result;
                // SteamData = SteamApi?._GetData((ulong)SteamId64).Result;
            }
            public string ReplaceDiscord(string input) {
                base.ReplaceDiscord(input);
                if (Server is not null) {
                    input = input
                        .ReplaceDiscord($"{ParamName}.Name", Server?.ServerName)
                        .ReplaceDiscord($"{ParamName}.AllPlayers.Count()", Server?.AllPlayers.Count())
                        .ReplaceDiscord($"{ParamName}.str()", Server?.str());
                }
                return input;
            }
        }
        internal class LoggerPlayer : LoggerBase {
            public RunnerPlayer? Player { get; set; }
            public ulong? SteamId64 { get; set; }
            public string CountryCode => GeoData?.CountryCode?.ToLowerInvariant() ?? SteamData?.Summary?.CountryCode?.ToLowerInvariant();
            public string CountryFlagEmoji => GeoData?.CountryFlagEmoji ?? SteamData?.Summary?.CountryFlagEmoji ?? "🌎";
            public SteamWebApi.Response? SteamData { get; set; }
            public LoggerPlayer(string paramName, RunnerPlayer player = null) {
                ParamName = paramName; Player = player; SteamId64 = player.SteamID;
                GeoData = player.GetGeoData()?.Result;
                // SteamData = SteamApi?._GetData((ulong)SteamId64).Result;
            }
            public string ReplaceDiscord(string input) {
                base.ReplaceDiscord(input);
                if (Player is not null) {
                    input = input
                        .ReplaceDiscord($"{ParamName}.SteamID", SteamId64)
                        .ReplaceDiscord($"{ParamName}.Name", Player?.Name)
                        .ReplaceDiscord($"{ParamName}.SteamID", Player?.SteamID)
                        .ReplaceDiscord($"{ParamName}.str()", Player?.str())
                        .ReplaceDiscord($"{ParamName}.fullstr()", Player?.fullstr())
                        .ReplaceDiscord($"{ParamName}.IP", Player?.IP);
                }
                if (SteamData is not null) {
                    input = input
                        .ReplaceDiscord($"{ParamName}.steamData.CountryCode", SteamData.Summary?.CountryCode?.ToLowerInvariant())
                        .ReplaceDiscord($"{ParamName}.steamData.ToJson()", SteamData.ToJson())
                        .ReplaceDiscord($"{ParamName}.steamData.ToJson(true)", SteamData.ToJson(true));
                }
                input = input
                    .ReplaceDiscord($"{ParamName}.CountryCode", CountryCode)
                    .ReplaceDiscord($"{ParamName}.CountryFlagEmoji", CountryFlagEmoji);
                return input;
            }
        }

        internal string FormatString(string input, LoggerServer? server = null, LoggerPlayer? player = null, LoggerPlayer? target = null, ReportReason? reportReason = null, ChatChannel? chatChannel = null,
            string? msg = null, long? oldSessionId = null, long? newSessionId = null, GameState? oldState = null, GameState? newState = null, PlayerJoiningArguments? playerJoinArgs = null,
            PlayerStats? playerStats = null) {
            var now = string.IsNullOrWhiteSpace(Config.TimeStampFormat) ? "" : new DateTimeWithZone(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById(Config.TimeZone)).LocalTime.ToString(Config.TimeStampFormat);
            input = input.Replace("{now}", now);

            if (server is not null) {
                input = server.ReplaceDiscord(input);
            }

            if (player is not null) {
                input = player.ReplaceDiscord(input);
                if (playerJoinArgs is not null && player.SteamId64 is not null) {
                    var bannedStr = playerJoinArgs.Stats.IsBanned ? "Banned" : "Player";
                    input = input.ReplaceDiscord("playerJoinArgs.Stats.IsBanned", bannedStr);
                    input = input.ReplaceDiscord("playerJoinArgs.Stats.IsBanned.ToYesNo()", playerJoinArgs.Stats.IsBanned.ToYesNo());
                    input = input.ReplaceDiscord("playerJoinArgs.Stats.Progress.Rank", playerJoinArgs.Stats.Progress.Rank);
                    input = input.ReplaceDiscord("playerJoinArgs.Stats.Progress.Prestige", playerJoinArgs.Stats.Progress.Prestige);
                    input = input.ReplaceDiscord("playerJoinArgs.Stats.Roles", playerJoinArgs.Stats.Roles == Roles.None ? bannedStr : playerJoinArgs.Stats.Roles.ToRoleString());
                }
            }

            if (target is not null) {
                input = target.ReplaceDiscord(input);
            }

            input = input.ReplaceDiscord("reason", reportReason);
            input = input.ReplaceDiscord("msg", msg);
            input = input.ReplaceDiscord("oldState", oldState);
            input = input.ReplaceDiscord("newState", newState);
            input = input.ReplaceDiscord("oldSessionId", oldSessionId);
            input = input.ReplaceDiscord("newSessionId", newSessionId);
            switch (chatChannel) {
                case ChatChannel.SquadChat: input = input.ReplaceDiscord("chatChannel", $"{player?.Player?.Team.ToCountryCode()}-{player?.Player?.SquadName} > "); break;
                case ChatChannel.TeamChat: input = input.ReplaceDiscord("chatChannel", $"{player?.Player?.Team.ToCountryCode()} > "); break;
                default: input = input.ReplaceDiscord("chatChannel", string.Empty); break;
            }
            foreach (var replacement in Config.randomReplacements) {
                input = input.Replace($"{{random.{replacement.Key}}}", replacement.Value[random.Next(replacement.Value.Count)]);
            }
            return input; // Smart.Format(input, now=now, parms);
        }

        internal async void HandleEvent(LogConfigurationEntry config, RunnerPlayer? _player = null, RunnerPlayer? _target = null, IpApi.Response? geoData = null, SteamWebApi.Response? steamData = null,
            ReportReason? reportReason = null, ChatChannel? chatChannel = null, string? _msg = null, long? oldSessionId = null, long? newSessionId = null, GameState? oldState = null,
            GameState? newState = null, PlayerJoiningArguments? playerJoiningArguments = null, ulong? steamId64 = null, PlayerStats? playerStats = null) {
            var server = new LoggerServer(this.Server, "server");
            LoggerPlayer player = new LoggerPlayer("player", _player);
            LoggerPlayer target = new LoggerPlayer("target", _target);
            if (config.Console is not null && config.Console.Enabled && !string.IsNullOrWhiteSpace(config.Console.Message)) {
                LogToConsole(FormatString(config.Console.Message, server: server, player: player, target: target, reportReason: reportReason,
                    chatChannel: chatChannel, msg: _msg, oldSessionId: oldSessionId, newSessionId: newSessionId, oldState: oldState, newState: newState, playerJoinArgs: playerJoiningArguments));
            }
            if (config.Discord is not null && config.Discord.Enabled && !string.IsNullOrWhiteSpace(config.Discord.WebhookUrl) && !string.IsNullOrWhiteSpace(config.Discord.Message)) {
                var msg = FormatString(config.Discord.Message, server: server, player: player, target: target, reportReason: reportReason,
                    chatChannel: chatChannel, msg: _msg, oldSessionId: oldSessionId, newSessionId: newSessionId, oldState: oldState, newState: newState, playerJoinArgs: playerJoiningArguments);
                await SendToWebhook(config.Discord.WebhookUrl, msg);
            }
            try { var a = this.Server.IsConnected; } catch { return; }
            if (this.Server is null || !this.Server.IsConnected) return;
            if (config.Chat is not null && config.Chat.Enabled && !string.IsNullOrWhiteSpace(config.Chat.Message)) {
                var msg = FormatString(config.Chat.Message, server: server, player: player, target: target, reportReason: reportReason,
                    chatChannel: chatChannel, msg: _msg, oldSessionId: oldSessionId, newSessionId: newSessionId, oldState: oldState, newState: newState, playerJoinArgs: playerJoiningArguments);
                if (this.GranularPermissions is not null && config.Chat.Permissions.Count > 0) {
                    //try {
                        foreach (var __player in this.Server.AllPlayers) {
                            if (!Extensions.HasAnyPermissionOf(__player, this.GranularPermissions, config.Chat.Permissions)) SayToPlayer(msg, player: __player);
                        }
                    //} catch (Exception ex) {
                    //    Console.WriteLine($"Got exception {ex.Message} while trying to send message to players");
                    //}
                } else SayToAll(msg);
            }
            if (config.Modal is not null && config.Modal.Enabled && !string.IsNullOrWhiteSpace(config.Modal.Message)) {
                var msg = FormatString(config.Modal.Message, server: server, player: player, target: target, reportReason: reportReason,
                    chatChannel: chatChannel, msg: _msg, oldSessionId: oldSessionId, newSessionId: newSessionId, oldState: oldState, newState: newState, playerJoinArgs: playerJoiningArguments);
                foreach (var __player in server.Server.AllPlayers) {
                    if(this.GranularPermissions is not null && config.Modal.Permissions.Count > 0) {
                        if (!Extensions.HasAnyPermissionOf(__player, this.GranularPermissions, config.Modal.Permissions)) continue;
                    }
                    ModalMessage(msg, player: __player);
                }
            }
            if (config.UILog is not null && config.UILog.Enabled && !string.IsNullOrWhiteSpace(config.UILog.Message)) {
                var msg = FormatString(config.UILog.Message, server: server, player: player, target: target, reportReason: reportReason,
                    chatChannel: chatChannel, msg: _msg, oldSessionId: oldSessionId, newSessionId: newSessionId, oldState: oldState, newState: newState, playerJoinArgs: playerJoiningArguments);
                UILogOnServer(msg, config.UILog.Duration);
            }
            if (config.Announce is not null && config.Announce.Enabled && !string.IsNullOrWhiteSpace(config.Announce.Message)) {
                var msg = FormatString(config.Announce.Message, server: server, player: player, target: target, reportReason: reportReason,
                    chatChannel: chatChannel, msg: _msg, oldSessionId: oldSessionId, newSessionId: newSessionId, oldState: oldState, newState: newState, playerJoinArgs: playerJoiningArguments);
                Announce(msg, config.Announce.Duration);
            }
        }
        #endregion

        #region Events
        public override void OnModulesLoaded() {
            Extensions.OnPlayerKicked += OnPlayerKicked;
            GeoApi.OnDataReceived += GeoApi_OnDataReceived;
            SteamApi.OnPlayerDataReceived += SteamApi_OnPlayerDataReceived;
            HandleEvent(Config.OnApiModulesLoaded);
        }

        private void SteamApi_OnPlayerDataReceived(RunnerPlayer player, SteamWebApi.Response steamData) {
            HandleEvent(Config.OnSteamDataReceived, _player: player, steamData: steamData);
        }

        private void GeoApi_OnDataReceived(IPAddress ip, IpApi.Response geoData) {
            HandleEvent(Config.OnGeoDataReceived, _player: this.Server.GetPlayersByIp(ip).First(), geoData: geoData);
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
            HandleEvent(Config.OnPlayerConnected, _player: player);
        }
        public override Task OnSavePlayerStats(ulong steamID, PlayerStats stats) {
            HandleEvent(Config.OnSavePlayerStats, steamId64: steamID, playerStats: stats);
            return Task.CompletedTask;
        }

        public override async Task<bool> OnPlayerRequestingToChangeRole(RunnerPlayer player, GameRole requestedRole) {
            HandleEvent(Config.OnPlayerRequestingToChangeRole, _player: player, _msg: requestedRole.ToString());
            return true;
        }
        public override async Task OnPlayerChangedRole(RunnerPlayer player, GameRole role) {
            HandleEvent(Config.OnPlayerChangedRole, _player: player, _msg: role.ToString());
        }

        public override async Task<bool> OnPlayerRequestingToChangeTeam(RunnerPlayer player, Team requestedTeam) {
            HandleEvent(Config.OnPlayerRequestingToChangeTeam, _player: player, _msg: requestedTeam.ToString());
            return true;
        }
        public override async Task OnPlayerChangeTeam(RunnerPlayer player, Team team) {
            HandleEvent(Config.OnPlayerChangeTeam, _player: player, _msg: team.ToString());
        }

        public override async Task OnPlayerJoinedSquad(RunnerPlayer player, Squad<RunnerPlayer> squad) {
            HandleEvent(Config.OnPlayerJoinedSquad, _player: player, _msg: squad.Name.ToString());
        }
        public override async Task OnPlayerLeftSquad(RunnerPlayer player, Squad<RunnerPlayer> squad) {
            HandleEvent(Config.OnPlayerLeftSquad, _player: player, _msg: squad.Name.ToString());
        }

        public override async Task OnSquadLeaderChanged(Squad<RunnerPlayer> squad, RunnerPlayer newLeader) {
            HandleEvent(Config.OnSquadLeaderChanged, _player: newLeader, _msg: squad.Name.ToString());
        }

        public override async Task<OnPlayerSpawnArguments?> OnPlayerSpawning(RunnerPlayer player, OnPlayerSpawnArguments request) {
            HandleEvent(Config.OnPlayerSpawning, _player: player, _msg: request.RequestedPoint.ToString());
            return request;
        }

        public override async Task OnPlayerSpawned(RunnerPlayer player) {
            HandleEvent(Config.OnPlayerSpawned, _player: player);
        }

        public override async Task OnPlayerDied(RunnerPlayer player) {
            HandleEvent(Config.OnPlayerDied, _player: player);
        }

        public override async Task OnPlayerGivenUp(RunnerPlayer player) {
            HandleEvent(Config.OnPlayerGivenUp, _player: player);
        }

        public override async Task OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<RunnerPlayer> args) {
            HandleEvent(Config.OnAPlayerDownedAnotherPlayer, _player: args.Killer, _target: args.Victim);
        }

        public override async Task OnAPlayerRevivedAnotherPlayer(RunnerPlayer from, RunnerPlayer to) {
            HandleEvent(Config.OnAPlayerRevivedAnotherPlayer, _player: from, _target: to);
        }

        public override Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg) {
            if (this.CommandHandler.IsCommand(msg)) {
                HandleEvent(Config.OnPlayerChatCommand, _player: player, chatChannel: channel, _msg: msg);
            } else {
                HandleEvent(Config.OnPlayerChatMessage, _player: player, chatChannel: channel, _msg: msg);
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
            HandleEvent(Config.OnPlayerReported, _player: from, _target: to, reportReason: reason, _msg: additional);
            return Task.CompletedTask;
        }

        public void OnPlayerKicked(object targetPlayer, string? reason) {
            HandleEvent(Config.OnPlayerKicked, _player: targetPlayer as RunnerPlayer ?? null, steamId64: targetPlayer as ulong? ?? null, _msg: reason!);
        }
        public override Task OnPlayerDisconnected(RunnerPlayer player) {
            HandleEvent(Config.OnPlayerDisconnected, _player: player);
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
            public bool Enabled { get; set; } = true;
            public string Message { get; set; } = string.Empty;
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public List<string>? Permissions { get; set; } = new();
            public Duration Duration { get; set; } = Duration.None;
        }
        public class FileLogConfigurationEntrySettings : LogConfigurationEntrySettings {
            public string Path { get; set; } = string.Empty;
            public string Mode { get; set; } = "a";
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
            public FileLogConfigurationEntrySettings File { get; set; } = null!;
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
                Chat = new LogConfigurationEntrySettings() { Message = "[{now}] API Modules loaded", Permissions = { "logger.OnApiModulesLoaded" } },
                Console = new LogConfigurationEntrySettings() { Message = "[{now}] API Modules loaded" },
                UILog = new LogConfigurationEntrySettings() { Message = "[{now}] API Modules loaded" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Message = "[{now}] API Modules loaded" },
            };
            public LogConfigurationEntry OnApiConnected { get; set; } = new() {
                Chat = new LogConfigurationEntrySettings() { Message = "[{now}] Server connected to API", Permissions = { "logger.OnApiConnected" } },
                Console = new LogConfigurationEntrySettings() { Message = "[{now}] {server.str()} connected to API" },
                UILog = new LogConfigurationEntrySettings() { Message = "[{now}] Server connected to API" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Message = "[{now}] {server.str()} connected to API" },
            };
            public LogConfigurationEntry OnApiDisconnected { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Message = "[{now}] Server disconnected from API", Permissions = { "logger.OnApiDisconnected" } },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Message = "[{now}] Server disconnected from API" },
            };
            public LogConfigurationEntry OnPlayerJoiningToServer { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Message = "[{now}] {BannedOrP}layer [{playerJoiningArguments.Stats.Roles.ToRoleString()}] {player.str()} is connecting to the server from {geoData.CountryCode} (Prestige: {playerJoiningArguments.Progress.Progress.Prestige} | Rank: {playerJoiningArguments.Stats.Progress.Rank})" },
                UILog = new LogConfigurationEntrySettings() { Message = "{player.Name} [~]" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Message = "[{now}] {BannedOrP}layer [{playerJoiningArguments.Stats.Roles.ToRoleString()}] {player.str()} is connecting to the server from {CountryFlagEmoji} (Prestige: {playerJoiningArguments.Progress.Progress.Prestige} | Rank: {playerJoiningArguments.Stats.Progress.Rank})" }
            };
            public LogConfigurationEntry OnPlayerConnected { get; set; } = new() {
                Chat = new LogConfigurationEntrySettings() { Message = "[+] {player.Name} {random.joined}", Permissions = { "logger.OnPlayerConnected" } },
                Console = new LogConfigurationEntrySettings() { Message = "[{now}] [+] {player.Name} ({player.SteamID})) [{player.IP},{player.geoData.Country},{player.geoData.Continent}]" },
                UILog = new LogConfigurationEntrySettings() { Message = "{player.Name} [+]" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Message = "[{now}] `{player.str()}`connected from {player.geoData.Country}, {player.geoData.Continent} {player.CountryFlagEmoji}" },
            };
            public LogConfigurationEntry OnPlayerDisconnected { get; set; } = new() {
                Chat = new LogConfigurationEntrySettings() { Message = "[-] {player.Name} left", Permissions = { "logger.OnPlayerDisconnected" } },
                Console = new LogConfigurationEntrySettings() { Message = "[{now}] [-] {player.Name} ({player.SteamID})) [{player.IP} {player.geoData.Country}]" },
                UILog = new LogConfigurationEntrySettings() { Message = "{player.Name} [-]" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Message = "[{now}] {player.CountryFlagEmoji} `{player.str()}` disconnected :arrow_left:" },
            };
            public LogConfigurationEntry OnPlayerKicked { get; set; } = new() {
                Chat = new LogConfigurationEntrySettings() { Message = "[-] {player.Name} was kicked for {msg}", Permissions = { "logger.OnPlayerKicked" } },
                Console = new LogConfigurationEntrySettings() { Message = "[{now}] [-] {player.Name} ({player.SteamID})) [{player.IP}] kicked for {msg}" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Message = "[{now}] `{player.str()}` kicked :leg:" },
            };
            public LogConfigurationEntry OnPlayerChatMessage { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Message = "[{now}] {player.str()} says \"{msg}\" in {chatChannel}" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Message = "[{now}] `{player.Name}` says \"{msg}\" in {chatChannel} :speech_balloon:" },
            };
            public LogConfigurationEntry OnPlayerChatCommand { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Message = "[{now}] {player.str()} issued command \"{msg}\" in {chatChannel}" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Message = "[{now}] {chatChannel}`{player.str()}` issued command \"{msg}\" in {chatChannel}" },
            };
            public LogConfigurationEntry OnConsoleCommand { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Message = "[{now}] Console issued command \"{msg}\"" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Message = "[{now}] Console issued command \"{msg}\"" },
            };
            public LogConfigurationEntry OnConsoleChat { get; set; } = new() {
                Chat = new LogConfigurationEntrySettings() { Message = "[{now}] Console : {msg}" },
                Console = new LogConfigurationEntrySettings() { Message = "[{now}] Console wrote \"{msg}\"" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Message = "[{now}] Console wrote \"{msg}\"" },
            };
            public LogConfigurationEntry OnPlayerReported { get; set; } = new() {
                Chat = new LogConfigurationEntrySettings() { Message = "{target.Name} was reported for {reason}" },
                Console = new LogConfigurationEntrySettings() { Message = "[{now}] {player.str()} reported {target.str()} for {reason}: \"{msg}\"" },
                UILog = new LogConfigurationEntrySettings() { Message = "{target.Name} was reported ({reason})" },
                Modal = new LogConfigurationEntrySettings() { Message = "{target.fullstr()}\nwas reported by\n{player.fullstr()}\n\nReason: {reason}\n\n\"{msg}\"", Permissions = { "logger.OnPlayerReported" } },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Message = "[{now}] {target.Name} was reported for {reason} :warning:" },
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
                Console = new LogConfigurationEntrySettings() { Message = "[{now}] Game State changed from {oldState} to {newState}" },
                UILog = new LogConfigurationEntrySettings() { Message = "Game State changed to {newState}" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Message = "[{now}] Game State changed from {oldState} to {newState}" },
            };
            public LogConfigurationEntry OnRoundEnded { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Message = "[{now}] Round ended" },
                UILog = new LogConfigurationEntrySettings() { Message = "Round ended" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Message = "[{now}] Round ended" },
            };
            public LogConfigurationEntry OnRoundStarted { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Message = "[{now}] Round started" },
                UILog = new LogConfigurationEntrySettings() { Message = "Round started" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Message = "[{now}] Round started" },
            };
            public LogConfigurationEntry OnSessionChanged { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Message = "[{now}] Game Session changed from {oldSessionId} to {newSessionId}" },
                UILog = new LogConfigurationEntrySettings() { Message = "Game State changed to {newSessionId}" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Message = "[{now}] Game Session changed from {oldSessionId} to {newSessionId}" },
            };
            public LogConfigurationEntry OnModuleUnloading { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Message = "[{now}] Logger module unloaded" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Message = "[{now}] Logger module unloaded" },
            };
            public LogConfigurationEntry OnSteamDataReceived { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Message = "[{now}] OnSteamDataReceived: {steamData.ToJson()}" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = false, Message = "[{now}] OnSteamDataReceived: {steamData.ToJson()}" },
            };
            public LogConfigurationEntry OnGeoDataReceived { get; set; } = new() {
                Console = new LogConfigurationEntrySettings() { Message = "[{now}] OnGeoDataRecieved: {geoData.ToJson()}" },
                Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = false, Message = "[{now}] OnGeoDataRecieved: {geoData.ToJson(true)}" },
            };
        }
        #endregion
    }
}