#nullable enable
#pragma warning disable CS8618
#pragma warning disable CS8601
#pragma warning disable CS8603
using System;
using System.Linq;
using System.Net;
using System.Globalization;
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
using JsonExtensions;

/// <summary>
/// Author: Bluscream
/// Version: 1.0.0
/// </summary>

namespace LoggerBattlebitModule {

    internal static partial class Extensions {
        internal static string str(this RunnerPlayer player) => $"\"{player.Name}\"";
        internal static string fullstr(this RunnerPlayer player) => $"{player.str()} ({player.SteamID})";
        internal static string ToYesNoString(this bool input) => input ? "Yes" : "No";
        internal static string ToEnabledDisabledString(this bool input) => input ? "Enabled" : "Disabled";
    }

    [RequireModule(typeof(CommandHandler))]
    public class Logger : BattleBitModule {
        [ModuleReference]
        public CommandHandler CommandHandler { get; set; }
        [ModuleReference]
        public BattleBitModule? PlayerPermissions { get; set; }

        public ChatLoggerConfiguration Configuration { get; set; }
        internal HttpClient httpClient = new HttpClient();
        internal Random random = Random.Shared;

        internal async Task<IpApi.Response> GetGeoData(IPAddress ip) {
            var url = $"http://ip-api.com/json/{ip}";
            var httpResponse = await this.httpClient.GetAsync(url);
            var json = await httpResponse.Content.ReadAsStringAsync();
            var response = IpApi.Response.FromJson(json);
            return response;
        }
        internal async Task<SteamWebApi.BanResponse> GetSteamBans(ulong steamId64) {
            if (string.IsNullOrWhiteSpace(Configuration.SteamWebApiKey)) {
                Console.WriteLine("Steam Web API Key is not set up in config, can't continue!");
                return null;
            }
            var url = $"http://api.steampowered.com/ISteamUser/GetPlayerBans/v1/?steamids={steamId64}&key={Configuration.SteamWebApiKey}";
            var httpResponse = await this.httpClient.GetAsync(url);
            var json = await httpResponse.Content.ReadAsStringAsync();
            var response = SteamWebApi.BanResponse.FromJson(json);
            return response;
        }
        internal async Task<long> GetBanCount(ulong steamId64) {
            var bans = await GetSteamBans(steamId64);
            if (bans is null) return -1;
            var player = bans.Players.First();
            var banCount = player.NumberOfVacBans + player.NumberOfGameBans;
            if (player.CommunityBanned) banCount++;
            if (player.EconomyBan != "none") banCount++;
            return banCount;
        }

        [CommandCallback("playerbans", Description = "Lists bans of a player", AllowedRoles = (BattleBitAPI.Common.Roles)Roles.AdminMod)]
        public async void GetPlayerBans(RunnerPlayer commandSource, RunnerPlayer _player) {
            var response = new StringBuilder();
            if (!string.IsNullOrEmpty(_player.Name)) response.AppendLine($"Name: {_player.str()} ({_player.Name.Length} chars)");
            if (!string.IsNullOrEmpty(_player.SteamID.ToString())) {
                var bans = await GetSteamBans(_player.SteamID);
                if (bans is null) {
                    commandSource.Message("Steam bans request failed, check connection and config!");
                    return;
                }
                var player = bans.Players.First();
                response.AppendLine($"VAC Banned: {player.VacBanned.ToYesNoString()} ({player.NumberOfVacBans} times)");
                if (player.VacBanned) response.AppendLine($"Last VAC Ban: {player.DaysSinceLastBan} days ago");
                response.AppendLine($"Community Banned: {player.CommunityBanned.ToYesNoString()}");
                response.AppendLine($"Trade Banned: {(player.EconomyBan != "none").ToYesNoString()}");
                response.AppendLine($"Game Banned: {(player.NumberOfGameBans > 0).ToYesNoString()} ({player.NumberOfGameBans} times)");
            }
            commandSource.Message(response.ToString());
        }

        [CommandCallback("playerinfo", Description = "Displays info about a player", AllowedRoles = (BattleBitAPI.Common.Roles)Roles.Admin)]
        public async void GetPlayerInfo(RunnerPlayer commandSource, RunnerPlayer player) {
            var geoResponse = await GetGeoData(player.IP);
            var response = new StringBuilder();
            if (!string.IsNullOrEmpty(player.Name)) response.AppendLine($"Name: {player.str()} ({player.Name.Length} chars)");
            if (!string.IsNullOrEmpty(player.SteamID.ToString())) {
                var banCount = await GetBanCount(player.SteamID);
                response.AppendLine($"SteamId64: {player.SteamID} ({banCount} bans)");
            }
            if (!string.IsNullOrEmpty(player.IP.ToString())) response.AppendLine($"IP: {player.IP}");
            if (!string.IsNullOrEmpty(geoResponse.Isp)) response.AppendLine($"ISP: {geoResponse.Isp}");
            if (!string.IsNullOrEmpty(geoResponse.Country)) response.AppendLine($"Country: {geoResponse.Country}");
            if (!string.IsNullOrEmpty(geoResponse.RegionName)) response.AppendLine($"Region: {geoResponse.RegionName}");
            if (!string.IsNullOrEmpty(geoResponse.City)) response.AppendLine($"City: {geoResponse.City} ({geoResponse.Zip})");
            if (!string.IsNullOrEmpty(geoResponse.Timezone)) response.AppendLine($"Time: {TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, geoResponse.Timezone).ToString("HH:mm")} ({geoResponse.Timezone})");
            commandSource.Message(response.ToString());
        }

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
                var response = await this.httpClient.PostAsync(webhookUrl, content);
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
            if (input.Contains("{geoResponse.Country}")) input = input.Replace("{geoResponse.Country}", geoResponse.Country);
            if (input.Contains("{geoResponse.CountryCode}")) input = input.Replace("{geoResponse.CountryCode}", geoResponse.CountryCode.ToLowerInvariant());
            if (input.Contains("{geoResponse.ToJson()}")) input = input.Replace("{geoResponse.ToJson()}", geoResponse.ToJson());
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
            try { var _ = this.Server.IsConnected; } catch (Exception ex) {
                Console.WriteLine($"Got exception {ex.Message} while trying this.Server.IsConnected");
                return;
            }
            if (this.Server is null || !this.Server.IsConnected) return;
            if (config.Chat is not null && config.Chat.Enabled && !string.IsNullOrWhiteSpace(config.Chat.Message)) {
                var msg = FormatString(config.Chat.Message, player, target, geoResponse, reportReason, chatChannel, _msg);
                if (this.PlayerPermissions is not null && config.Chat.Roles != Roles.None) {
                    try {
                        foreach (var _player in this.Server.AllPlayers) {
                            if ((this.PlayerPermissions.Call<Roles>("GetPlayerRoles", _player.SteamID) & config.Chat.Roles) == 0) continue;
                            SayToPlayer(msg, player: player);
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"Got exception {ex.Message} while trying to send message to players");
                    }
                } else SayToAll(msg);
            }
            if (config.Modal is not null && config.Modal.Enabled && !string.IsNullOrWhiteSpace(config.Modal.Message)) {
                var msg = FormatString(config.Modal.Message, player, target, geoResponse, reportReason, chatChannel, _msg);
                foreach (var _player in this.Server.AllPlayers) {
                    if (this.PlayerPermissions is not null && (this.PlayerPermissions.Call<Roles>("GetPlayerRoles", _player.SteamID) & config.Modal.Roles) == 0) continue;
                    ModalMessage(msg, player: player);
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
    }

    public enum Roles : ulong {
        None = BattleBitAPI.Common.Roles.None,
        Admin = BattleBitAPI.Common.Roles.Admin,
        Moderator = BattleBitAPI.Common.Roles.Moderator,
        Special = BattleBitAPI.Common.Roles.Special,
        Vip = BattleBitAPI.Common.Roles.Vip,
        AdminMod = Admin | Moderator,
        Member = Admin | Moderator | Vip | Special,
        All = Admin | Moderator | Vip | Special | None
    }
    public enum Duration {
        None,
        Short,
        Medium,
        Long,
        Infinite
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
        public LogConfigurationEntrySettings Chat { get; set; }
        public LogConfigurationEntrySettings Console { get; set; }
        public LogConfigurationEntrySettings UILog { get; set; }
        public LogConfigurationEntrySettings Announce { get; set; }
        public LogConfigurationEntrySettings Modal { get; set; }
        public DiscordWebhookLogConfigurationEntrySettings Discord { get; set; }
    }
    public class ChatLoggerConfiguration : ModuleConfiguration {
        public string SteamWebApiKey { get; set; } = string.Empty;
        public string TimeStampFormat { get; set; } = "HH:mm:ss";
        public Dictionary<string, string[]> randomReplacements = new Dictionary<string, string[]>() {
            { "joined", new string[] { "joined", "connected", "hailed" } },
        };
        public LogConfigurationEntry OnApiModulesLoaded { get; set; } = new LogConfigurationEntry() {
            Chat = new LogConfigurationEntrySettings() { Enabled = false, Message = "[{now}] API Modules loaded", Roles = Roles.Member },
            Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] API Modules loaded" },
            UILog = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] API Modules loaded" },
            Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] API Modules loaded" },
        };
        public LogConfigurationEntry OnApiConnected { get; set; } = new LogConfigurationEntry() {
            Chat = new LogConfigurationEntrySettings() { Enabled = false, Message = "[{now}] Server connected to API", Roles = Roles.Member },
            Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Server connected to API" },
            UILog = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Server connected to API" },
            Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Server connected to API" },
        };
        public LogConfigurationEntry OnApiDisconnected { get; set; } = new LogConfigurationEntry() {
            Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Server disconnected from API" },
            Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] Server disconnected from API" },
        };
        public LogConfigurationEntry OnPlayerConnected { get; set; } = new LogConfigurationEntry() {
            Chat = new LogConfigurationEntrySettings() { Enabled = false, Message = "[+] {player.Name} {random.joined} from {geoResponse.Country}", Roles = Roles.All },
            Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] [+] {player.Name} ({player.SteamID})) | {geoResponse.ToJson()}" },
            UILog = new LogConfigurationEntrySettings() { Enabled = true, Message = "{player.Name} [+]" },
            Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] `{player.str()}` connected from {geoResponse.Country} :flag_{geoResponse.CountryCode}:" },
        };
        public LogConfigurationEntry OnPlayerDisconnected { get; set; } = new LogConfigurationEntry() {
            Chat = new LogConfigurationEntrySettings() { Enabled = false, Message = "[-] {player.Name} left", Roles = Roles.All },
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
            Chat = new LogConfigurationEntrySettings() { Enabled = false, Message = "{to.Name} was reported for {reason}", Roles = Roles.All },
            Console = new LogConfigurationEntrySettings() { Enabled = true, Message = "[{now}] {player.str()} reported {to.str()} for {reason}: \"{msg}\"" },
            UILog = new LogConfigurationEntrySettings() { Enabled = true, Message = "{to.Name} was reported ({reason})" },
            Modal = new LogConfigurationEntrySettings() { Enabled = false, Message = "{to.fullstr()}\nwas reported by\n{player.fullstr()}\n\nReason: {reason}\n\n\"{msg}\"", Roles = Roles.AdminMod },
            Discord = new DiscordWebhookLogConfigurationEntrySettings() { Enabled = false, Message = "[{now}] {to.Name} was reported for {reason} :warning:" },
        };
    }
}

namespace JsonExtensions {
    internal static class Converter {
        public static readonly JsonSerializerOptions Settings = new(JsonSerializerDefaults.General) {
            Converters =
            {
                new DateOnlyConverter(),
                new TimeOnlyConverter(),
                IsoDateTimeOffsetConverter.Singleton
            },
        };
    }

    internal class ParseStringConverter : JsonConverter<long> {
        public override bool CanConvert(Type t) => t == typeof(long);

        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var value = reader.GetString();
            long l;
            if (Int64.TryParse(value, out l)) {
                return l;
            }
            throw new Exception("Cannot unmarshal type long");
        }

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options) {
            JsonSerializer.Serialize(writer, value.ToString(), options);
            return;
        }

        public static readonly ParseStringConverter Singleton = new ParseStringConverter();
    }

    public class DateOnlyConverter : JsonConverter<DateOnly> {
        private readonly string serializationFormat;
        public DateOnlyConverter() : this(null) { }

        public DateOnlyConverter(string? serializationFormat) {
            this.serializationFormat = serializationFormat ?? "yyyy-MM-dd";
        }

        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var value = reader.GetString();
            return DateOnly.Parse(value!);
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(serializationFormat));
    }

    public class TimeOnlyConverter : JsonConverter<TimeOnly> {
        private readonly string serializationFormat;

        public TimeOnlyConverter() : this(null) { }

        public TimeOnlyConverter(string? serializationFormat) {
            this.serializationFormat = serializationFormat ?? "HH:mm:ss.fff";
        }

        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var value = reader.GetString();
            return TimeOnly.Parse(value!);
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(serializationFormat));
    }

    internal class IsoDateTimeOffsetConverter : JsonConverter<DateTimeOffset> {
        public override bool CanConvert(Type t) => t == typeof(DateTimeOffset);

        private const string DefaultDateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";

        private DateTimeStyles _dateTimeStyles = DateTimeStyles.RoundtripKind;
        private string? _dateTimeFormat;
        private CultureInfo? _culture;

        public DateTimeStyles DateTimeStyles {
            get => _dateTimeStyles;
            set => _dateTimeStyles = value;
        }

        public string? DateTimeFormat {
            get => _dateTimeFormat ?? string.Empty;
            set => _dateTimeFormat = (string.IsNullOrEmpty(value)) ? null : value;
        }

        public CultureInfo Culture {
            get => _culture ?? CultureInfo.CurrentCulture;
            set => _culture = value;
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) {
            string text;


            if ((_dateTimeStyles & DateTimeStyles.AdjustToUniversal) == DateTimeStyles.AdjustToUniversal
                || (_dateTimeStyles & DateTimeStyles.AssumeUniversal) == DateTimeStyles.AssumeUniversal) {
                value = value.ToUniversalTime();
            }

            text = value.ToString(_dateTimeFormat ?? DefaultDateTimeFormat, Culture);

            writer.WriteStringValue(text);
        }

        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            string? dateText = reader.GetString();

            if (string.IsNullOrEmpty(dateText) == false) {
                if (!string.IsNullOrEmpty(_dateTimeFormat)) {
                    return DateTimeOffset.ParseExact(dateText, _dateTimeFormat, Culture, _dateTimeStyles);
                } else {
                    return DateTimeOffset.Parse(dateText, Culture, _dateTimeStyles);
                }
            } else {
                return default(DateTimeOffset);
            }
        }

        public static readonly IsoDateTimeOffsetConverter Singleton = new IsoDateTimeOffsetConverter();
    }
}

namespace IpApi {
    public partial class Response {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("countryCode")]
        public string CountryCode { get; set; }

        [JsonPropertyName("region")]
        public string Region { get; set; }

        [JsonPropertyName("regionName")]
        public string RegionName { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("zip")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Zip { get; set; }

        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lon")]
        public double Lon { get; set; }

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; }

        [JsonPropertyName("isp")]
        public string Isp { get; set; }

        [JsonPropertyName("org")]
        public string Org { get; set; }

        [JsonPropertyName("as")]
        public string As { get; set; }

        [JsonPropertyName("query")]
        public string Query { get; set; }
    }

    public partial class Response {
        public static Response FromJson(string json) => JsonSerializer.Deserialize<Response>(json, JsonExtensions.Converter.Settings);
    }

    public static class Serialize {
        public static string ToJson(this Response self) => JsonSerializer.Serialize(self, JsonExtensions.Converter.Settings);
    }
}

namespace SteamWebApi {
    public partial class BanResponse {
        [JsonPropertyName("players")]
        public List<Player> Players { get; set; }
    }

    public partial class Player {
        [JsonPropertyName("SteamId")]
        public string SteamId { get; set; }

        [JsonPropertyName("CommunityBanned")]
        public bool CommunityBanned { get; set; }

        [JsonPropertyName("VACBanned")]
        public bool VacBanned { get; set; }

        [JsonPropertyName("NumberOfVACBans")]
        public long NumberOfVacBans { get; set; }

        [JsonPropertyName("DaysSinceLastBan")]
        public long DaysSinceLastBan { get; set; }

        [JsonPropertyName("NumberOfGameBans")]
        public long NumberOfGameBans { get; set; }

        [JsonPropertyName("EconomyBan")]
        public string EconomyBan { get; set; }
    }

    public partial class BanResponse {
        public static BanResponse FromJson(string json) => JsonSerializer.Deserialize<BanResponse>(json, JsonExtensions.Converter.Settings);
    }

    public static class Serialize {
        public static string ToJson(this BanResponse self) => JsonSerializer.Serialize(self, JsonExtensions.Converter.Settings);
    }
}
