using BattleBitAPI.Common;
using BBRAPIModules;
using Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Globalization;
using System.Net;
using System.Reflection.Metadata;
using System.Text;

namespace ChatLoggerBattleBitModule {

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

        public ChatLoggerConfiguration Configuration { get; set; }
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

    public class ChatLoggerConfiguration : ModuleConfiguration {
        public string SteamWebApiKey { get; set; } = string.Empty;
    }

}

namespace IpApi {

    public partial class Response {
        [JsonProperty("query")]
        public virtual string Query { get; set; }

        [JsonProperty("status")]
        public virtual string Status { get; set; }

        [JsonProperty("country")]
        public virtual string Country { get; set; }

        [JsonProperty("countryCode")]
        public virtual string CountryCode { get; set; }

        [JsonProperty("region")]
        public virtual string Region { get; set; }

        [JsonProperty("regionName")]
        public virtual string RegionName { get; set; }

        [JsonProperty("city")]
        public virtual string City { get; set; }

        [JsonProperty("zip")]
        public virtual string Zip { get; set; }

        [JsonProperty("lat")]
        public virtual double? Lat { get; set; }

        [JsonProperty("lon")]
        public virtual double? Lon { get; set; }

        [JsonProperty("timezone")]
        public virtual string Timezone { get; set; }

        [JsonProperty("isp")]
        public virtual string Isp { get; set; }

        [JsonProperty("org")]
        public virtual string Org { get; set; }

        [JsonProperty("as")]
        public virtual string As { get; set; }
    }

    public partial class Response {
        public static Response FromJson(string json) => JsonConvert.DeserializeObject<Response>(json, IpApi.Converter.Settings);
    }

    public static class Serialize {
        public static string ToJson(this Response self) => JsonConvert.SerializeObject(self, IpApi.Converter.Settings);
    }

    internal static class Converter {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}

namespace SteamWebApi {
    public partial class BanResponse {
        [JsonProperty("players")]
        public Player[] Players { get; set; }
    }

    public partial class Player {
        [JsonProperty("SteamId")]
        public string SteamId { get; set; }

        [JsonProperty("CommunityBanned")]
        public bool CommunityBanned { get; set; }

        [JsonProperty("VACBanned")]
        public bool VacBanned { get; set; }

        [JsonProperty("NumberOfVACBans")]
        public long NumberOfVacBans { get; set; }

        [JsonProperty("DaysSinceLastBan")]
        public long DaysSinceLastBan { get; set; }

        [JsonProperty("NumberOfGameBans")]
        public long NumberOfGameBans { get; set; }

        [JsonProperty("EconomyBan")]
        public string EconomyBan { get; set; }
    }

    public partial class BanResponse {
        public static BanResponse FromJson(string json) => JsonConvert.DeserializeObject<BanResponse>(json, SteamWebApi.Converter.Settings);
    }

    public static class Serialize {
        public static string ToJson(this BanResponse self) => JsonConvert.SerializeObject(self, SteamWebApi.Converter.Settings);
    }

    internal static class Converter {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}
