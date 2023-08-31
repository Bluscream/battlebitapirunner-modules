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

namespace ChatLoggerNewBattleBitModule {

    internal static partial class Extensions {
        internal static string str(this RunnerPlayer player) => $"\"{player.Name}\"";
        internal static string fullstr(this RunnerPlayer player) => $"{player.str()} ({player.SteamID})";
        internal static string ToYesNoString(this bool input) => input ? "Yes" : "No";
        internal static string ToEnabledDisabledString(this bool input) => input ? "Enabled" : "Disabled";
    }

    [RequireModule(typeof(CommandHandler))]
    public class ChatLogger : BattleBitModule {
        [ModuleReference]
        public CommandHandler CommandHandler { get; set; }

        public static ChatLoggerConfiguration Configuration { get; set; }
        internal HttpClient httpClient = new HttpClient();
        internal Random random = new Random();
        internal static readonly string[] joined = { "joined", "connected", "hailed"  };

        internal async Task<IpApi.Response> GetGeoData(IPAddress ip) {
            var url = $"http://ip-api.com/json/{ip}";
            var httpResponse = await this.httpClient.GetAsync(url);
            var json = await httpResponse.Content.ReadAsStringAsync();
            var response = IpApi.Response.FromJson(json);
            return response;
            //using (var wc = new System.Net.WebClient()) {
            //    var json = wc.DownloadString(url);
            //    var response = IpApi.Response.FromJson(json);
            //    return response;
            //}
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

        [CommandCallback("playerbans", Description = "Lists bans of a player", AllowedRoles = Roles.Admin)]
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

        [CommandCallback("playerinfo", Description = "Displays info about a player", AllowedRoles = Roles.Admin)]
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

        internal void SayToAll(string msg) {
            if (!string.IsNullOrWhiteSpace(msg))
                this.Server.SayToAllChat(msg);
            //foreach (RunnerPlayer player in this.Server.AllPlayers) {
            //    player.Message(msg, this.msg.MessageToPlayerTimeout);
            //}
        }

        public override void OnModulesLoaded() {
            this.CommandHandler.Register(this);
            try { SayToAll("[API] Modules loaded"); } catch { }
        }

        public override Task OnConnected() {
            try { SayToAll("[API] Server connected"); } catch { }
            return Task.CompletedTask;
        }

        public override Task OnDisconnected() {
            SayToAll("[API] Server disconnected");
            return Task.CompletedTask;
        }

        public override async Task OnPlayerConnected(RunnerPlayer player) {
            var geoResponse = await GetGeoData(player.IP);
            SayToAll($"[+] {player.Name} {joined[random.Next(joined.Length)]} from {geoResponse.Country}");
        }

        public override Task OnPlayerDisconnected(RunnerPlayer player) {
            SayToAll($"[-] {player.Name} disconnected");
            return Task.CompletedTask;
        }

        public override Task OnPlayerReported(RunnerPlayer from, RunnerPlayer to, ReportReason reason, string additional) {
            SayToAll($"[API] {to.fullstr()} was reported for {reason}");
            return Task.CompletedTask;
        }
    }

    public static class ChatLoggerConfiguration : ModuleConfiguration {
        public static string SteamWebApiKey { get; set; } = string.Empty;
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
