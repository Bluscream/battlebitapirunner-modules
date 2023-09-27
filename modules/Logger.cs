using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using BattleBitAPI.Common;
using BBRAPIModules;
using Commands;
using static Bluscream.BluscreamLib;
using static Bluscream.Extensions;
using Bluscream;
#if DEBUG
using Permissions;
#endif

namespace Bluscream {
    [RequireModule(typeof(Bluscream.BluscreamLib))]
    [RequireModule(typeof(Permissions.PlayerPermissions))]
    [RequireModule(typeof(Commands.CommandHandler))]
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

        [ModuleReference]
        public CommandHandler CommandHandler { get; set; } = null!;
        [ModuleReference]
#if DEBUG
        public Permissions.PlayerPermissions? PlayerPermissions { get; set; }
#else
        public dynamic? PlayerPermissions { get; set; }
#endif
        public LoggerCommandsConfiguration CommandsConfiguration { get; set; }

        public ChatLoggerConfiguration Configuration { get; set; } = null!;
        internal HttpClient httpClient = new HttpClient();
        internal Random random = Random.Shared;
        #region Methods
        internal void LogToConsole(string msg) {
            if (string.IsNullOrWhiteSpace(msg)) return;
            Console.WriteLine(msg);
        }
        private async Task SendToWebhook(string webhookUrl, string msg) {
            if (string.IsNullOrWhiteSpace(msg)) return;
            bool success = false;
            while (!success) {
                var payload = new {
                    content = msg.Replace("@", "\\@")
                };
                var payloadJson = JsonSerializer.Serialize(payload);
                var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                HttpResponseMessage response;
                try { response = await this.httpClient.PostAsync(webhookUrl, content); } catch (Exception ex) {
                    // BluscreamLib.Log($"Failed to POST webhook: {ex.Message}");
                    return;
                }
                if (!response.IsSuccessStatusCode) {
                    Console.WriteLine($"Error sending webhook message. Status Code: {response.StatusCode}");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
                success = response.IsSuccessStatusCode;
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

        internal string FormatString(string input, RunnerPlayer? player = null, RunnerPlayer? target = null, IpApi.Response? geoResponse = null, ReportReason? reportReason = null, ChatChannel? chatChannel = null, string msg = null) {
            var now = string.IsNullOrWhiteSpace(Configuration.TimeStampFormat) ? "" : DateTime.Now.ToString(Configuration.TimeStampFormat);
            input = input.Replace("{now}", now);
            if (input.Contains("{player.Name}")) input = input.Replace("{player.Name}", player.Name);
            if (input.Contains("{to.Name}")) input = input.Replace("{to.Name}", target.Name);
            if (input.Contains("{player.SteamID}")) input = input.Replace("{player.SteamID}", player.SteamID.ToString());
            if (input.Contains("{to.SteamID}")) input = input.Replace("{to.SteamID}", target.SteamID.ToString());
            if (input.Contains("{player.str()}")) input = input.Replace("{player.str()}", player.str());
            if (input.Contains("{to.str()}")) input = input.Replace("{to.str()}", target.str());
            if (input.Contains("{player.fullstr()}")) input = input.Replace("{player.fullstr()}", player.fullstr());
            if (input.Contains("{to.fullstr()}")) input = input.Replace("{to.fullstr()}", target.fullstr());
            if (input.Contains("{player.IP}")) input = input.Replace("{player.IP}", player.IP.ToString());
            if (input.Contains("{to.IP}")) input = input.Replace("{to.IP}", target.IP.ToString());
            if (geoResponse is not null) {
                if (input.Contains("{geoResponse.Country}")) input = input.Replace("{geoResponse.Country}", geoResponse.Country);
                if (input.Contains("{geoResponse.CountryCode}")) input = input.Replace("{geoResponse.CountryCode}", geoResponse.CountryCode.ToLowerInvariant());
                if (input.Contains("{geoResponse.ToJson()}")) input = input.Replace("{geoResponse.ToJson()}", IpApi.Serialize.ToJson(geoResponse));
            }
            if (input.Contains("{reason}")) input = input.Replace("{reason}", reportReason.ToString());
            if (input.Contains("{msg}")) input = input.Replace("{msg}", msg);
            if (input.Contains("{chatChannel}")) input = input.Replace("{chatChannel}", chatChannel.ToString());
            foreach (var replacement in Configuration.randomReplacements) {
                input = input.Replace("{random." + replacement.Key + "}", replacement.Value[random.Next(replacement.Value.Length)]);
            }
            return input; // Smart.Format(input, now=now, parms);
        }

        internal async void HandleEvent(LogConfigurationEntry config, RunnerPlayer? player = null, RunnerPlayer? target = null, IpApi.Response? geoResponse = null, ReportReason? reportReason = null, ChatChannel? chatChannel = null, string _msg = null) {
            if (config.Console is not null && config.Console.Enabled && !string.IsNullOrWhiteSpace(config.Console.Message)) {
                LogToConsole(FormatString(config.Console.Message, player, target, geoResponse, reportReason, chatChannel, _msg));
            }
            if (config.Discord is not null && config.Discord.Enabled && !string.IsNullOrWhiteSpace(config.Discord.WebhookUrl) && !string.IsNullOrWhiteSpace(config.Discord.Message)) {
                var msg = FormatString(config.Discord.Message, player, target, geoResponse, reportReason, chatChannel, _msg);
                await SendToWebhook(config.Discord.WebhookUrl, msg);
            }
            try { var _ = this.Server.IsConnected; } catch (Exception ex) { return; }
            if (this.Server is null || !this.Server.IsConnected) return;
            if (config.Chat is not null && config.Chat.Enabled && !string.IsNullOrWhiteSpace(config.Chat.Message)) {
                var msg = FormatString(config.Chat.Message, player, target, geoResponse, reportReason, chatChannel, _msg);
                if (this.PlayerPermissions is not null && config.Chat.Roles != Roles.None) {
                    try {
                        foreach (var _player in this.Server.AllPlayers) {
                            var playerRoles = this.PlayerPermissions.GetPlayerRoles(_player.SteamID);
                            if ((playerRoles & config.Chat.Roles) == 0) continue;
                            SayToPlayer(msg, player: _player);
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"Got exception {ex.Message} while trying to send message to players");
                    }
                } else SayToAll(msg);
            }
            if (config.Modal is not null && config.Modal.Enabled && !string.IsNullOrWhiteSpace(config.Modal.Message)) {
                var msg = FormatString(config.Modal.Message, player, target, geoResponse, reportReason, chatChannel, _msg);
                foreach (var _player in this.Server.AllPlayers) {
                    if (this.PlayerPermissions is not null) {
                        var playerRoles = this.PlayerPermissions.GetPlayerRoles(_player.SteamID);
                        if ((playerRoles & config.Modal.Roles) == 0) continue;
                    }
                    ModalMessage(msg, player: _player);
                }
            }
            if (config.UILog is not null && config.UILog.Enabled && !string.IsNullOrWhiteSpace(config.UILog.Message)) {
                var msg = FormatString(config.UILog.Message, player, target, geoResponse, reportReason, chatChannel, _msg);
                UILogOnServer(msg, config.UILog.Duration);
            }
            if (config.Announce is not null && config.Announce.Enabled && !string.IsNullOrWhiteSpace(config.Announce.Message)) {
                var msg = FormatString(config.Announce.Message, player, target, geoResponse, reportReason, chatChannel, _msg);
                Announce(msg, config.Announce.Duration);
            }
        }
        #endregion
        #region Events
        public override void OnModulesLoaded() {
            this.CommandHandler.Register(this);
            HandleEvent(Configuration.OnApiModulesLoaded);
        }
        public override Task OnConnected() {
            HandleEvent(Configuration.OnApiConnected);
            return Task.CompletedTask;
        }
        public override async Task OnPlayerConnected(RunnerPlayer player) {
            var geoResponse = await GetGeoData(player.IP);
            HandleEvent(Configuration.OnPlayerConnected, player: player, geoResponse: geoResponse);
        }
        public override Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg) {
            if (msg.StartsWith(CommandHandler.CommandConfiguration.CommandPrefix)) {
                HandleEvent(Configuration.OnPlayerChatCommand, player: player, chatChannel: channel, _msg: msg);
            } else {
                HandleEvent(Configuration.OnPlayerChatMessage, player: player, chatChannel: channel, _msg: msg);
            }
            return Task.FromResult(true);
        }
        public override Task OnPlayerDisconnected(RunnerPlayer player) {
            HandleEvent(Configuration.OnPlayerDisconnected, player: player);
            return Task.CompletedTask;
        }
        public override Task OnPlayerReported(RunnerPlayer from, RunnerPlayer to, ReportReason reason, string additional) {
            HandleEvent(Configuration.OnPlayerReported, player: from, target: to, reportReason: reason, _msg: additional);
            return Task.CompletedTask;
        }
        public override Task OnDisconnected() {
            HandleEvent(Configuration.OnApiDisconnected);
            return Task.CompletedTask;
        }
        #endregion
    }
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
    public class LoggerCommandsConfiguration : ModuleConfiguration {
        public CommandConfiguration playerbans { get; set; } = new CommandConfiguration() { AllowedRoles = Extensions.ToRoleStringList(MoreRoles.Staff) };
    }
    public class LogConfigurationEntrySettings {
        public bool Enabled { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public Roles Roles { get; set; } = Roles.None;
        public Duration Duration { get; set; } = Duration.None;
    }
    public class DiscordWebhookLogConfigurationEntrySettings : LogConfigurationEntrySettings {
        public string WebhookUrl { get; set; } = string.Empty;
    }
    public class LogConfigurationEntry {
        public LogConfigurationEntrySettings Chat { get; set; } = null!;
        public LogConfigurationEntrySettings Console { get; set; } = null!;
        public LogConfigurationEntrySettings UILog { get; set; } = null!;
        public LogConfigurationEntrySettings Announce { get; set; } = null!;
        public LogConfigurationEntrySettings Modal { get; set; } = null!;
        public DiscordWebhookLogConfigurationEntrySettings Discord { get; set; } = null!;
    }
    public class ChatLoggerConfiguration : ModuleConfiguration {
        public string TimeStampFormat { get; set; } = "HH:mm:ss";
        public Dictionary<string, string[]> randomReplacements = new Dictionary<string, string[]>() {
            { "joined", new string[] { "joined", "connected", "hailed" } },
        };
        public LogConfigurationEntry OnApiModulesLoaded { get; set; } = new LogConfigurationEntry() {
            Chat = new LogConfigurationEntrySettings() { Enabled = false, Message = "[{now}] API Modules loaded", Roles = Roles.Admin },
            Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] API Modules loaded" },
            UILog = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] API Modules loaded" },
            Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] API Modules loaded" },
        };
        public LogConfigurationEntry OnApiConnected { get; set; } = new LogConfigurationEntry() {
            Chat = new LogConfigurationEntrySettings() { Enabled = false, Message = "[{now}] Server connected to API", Roles = Roles.Admin },
            Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Server connected to API" },
            UILog = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Server connected to API" },
            Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Server connected to API" },
        };
        public LogConfigurationEntry OnApiDisconnected { get; set; } = new LogConfigurationEntry() {
            Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Server disconnected from API", Roles = Roles.Admin },
            Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Server disconnected from API" },
        };
        public LogConfigurationEntry OnPlayerConnected { get; set; } = new LogConfigurationEntry() {
            Chat = new LogConfigurationEntrySettings() { Enabled = false, Message = "[+] {player.Name} {random.joined} from {geoResponse.Country}", Roles = MoreRoles.All },
            Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] [+] {player.Name} ({player.SteamID})) | {geoResponse.ToJson()}" },
            UILog = new LogConfigurationEntrySettings() { Enabled = true, Message = "{player.Name} [+]" },
            Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] `{player.str()}` connected from {geoResponse.Country} :flag_{geoResponse.CountryCode}:" },
        };
        public LogConfigurationEntry OnPlayerDisconnected { get; set; } = new LogConfigurationEntry() {
            Chat = new LogConfigurationEntrySettings() { Enabled = false, Message = "[-] {player.Name} left", Roles = MoreRoles.All },
            Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] [-] {player.Name} ({player.SteamID})) [{player.IP}]" },
            UILog = new LogConfigurationEntrySettings() { Enabled = true, Message = "{player.Name} [-]" },
            Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] `{player.str()}` disconnected :arrow_left:" },
        };
        public LogConfigurationEntry OnPlayerChatMessage { get; set; } = new LogConfigurationEntry() {
            Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] {player.str()} says \"{msg}\" in {chatChannel}" },
            Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] `{player.Name}` says \"{msg}\" in {chatChannel} :speech_balloon:" },
        };
        public LogConfigurationEntry OnPlayerChatCommand { get; set; } = new LogConfigurationEntry() {
            Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] {player.str()} issued command \"{msg}\" in {chatChannel}" },
            Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] `{player.Name}` issued command \"{msg}\" in {chatChannel}" },
        };
        public LogConfigurationEntry OnPlayerReported { get; set; } = new LogConfigurationEntry() {
            Chat = new LogConfigurationEntrySettings() { Enabled = false, Message = "{to.Name} was reported for {reason}", Roles = MoreRoles.All },
            Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] {player.str()} reported {to.str()} for {reason}: \"{msg}\"" },
            UILog = new LogConfigurationEntrySettings() { Enabled = true, Message = "{to.Name} was reported ({reason})" },
            Modal = new LogConfigurationEntrySettings() { Enabled = false, Message = "{to.fullstr()}\nwas reported by\n{player.fullstr()}\n\nReason: {reason}\n\n\"{msg}\"", Roles = MoreRoles.Staff },
            Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = false, Message = "[{now}] {to.Name} was reported for {reason} :warning:" },
        };
    }
    #endregion
}