using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Net;
using System.Net.Http;
using BattleBitAPI.Common;
using BBRAPIModules;

using Commands;

namespace Bluscream {
    [RequireModule(typeof(CommandHandler))]
    [Module("IP and geolocation data provider API for other modules", "2.0.0")]
    public class GeoApi : BattleBitModule {
        public static ModuleInfo ModuleInfo = new() {
            Name = "GeoApi",
            Description = "IP and geolocation data provider API for other modules",
            Version = new Version(2, 0, 0),
            Author = "Bluscream",
            WebsiteUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/"),
            UpdateUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/raw/master/modules/GeoApi.cs"),
            SupportUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/issues/new?title=GeoApi")
        };
        [ModuleReference]
        public CommandHandler CommandHandler { get; set; }

        public IpApiConfiguration Configuration { get; set; }
        public GeoApiCommandsConfiguration CommandsConfiguration { get; set; }
        internal static HttpClient httpClient = new HttpClient();
        private bool GettingGeoData = false;

        public IReadOnlyDictionary<RunnerPlayer, IpApi.Response> Players { get { return _Players; } }
        private Dictionary<RunnerPlayer, IpApi.Response> _Players { get; set; } = new Dictionary<RunnerPlayer, IpApi.Response>();
        #region Methods
        public static void Log(object _msg, string source = "GeoApi") {
            var msg = _msg.ToString();
            if (string.IsNullOrWhiteSpace(msg)) return;
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {source} > {msg.Trim()}");
        }
        private async Task AddAllGeoData(RunnerServer? server = null) {
            server = server ?? this.Server;
            foreach (var player in server.AllPlayers) {
                await AddGeoData(player);
            }
        }
        private async Task RemoveAllGeoData(RunnerServer? server = null, TimeSpan? delay = null) {
            server = server ?? this.Server;
            if (delay is not null && delay != TimeSpan.Zero) Task.Delay(delay.Value); // Todo: Make configurable
            foreach (var player in server.AllPlayers) {
                RemoveGeoData(player);
            }
        }
        private async Task AddGeoData(RunnerPlayer player) {
            if (Players.ContainsKey(player)) return;
            IpApi.Response? geoData = await _GetGeoData(player);
            if (geoData is null) return;
            _Players.Add(player, geoData);
        }
        private void RemoveGeoData(RunnerPlayer player, TimeSpan? delay = null) {
            if (delay is not null && delay != TimeSpan.Zero) Task.Delay(delay.Value); // Todo: Make configurable
            if (_Players.ContainsKey(player))
                _Players.Remove(player);
        }
        #endregion
        #region Api
        public async Task<IpApi.Response>? GetGeoData(RunnerPlayer player) {
            if (!Players.ContainsKey(player)) {
                Log($"For some reason we dont have GeoData for {player.str()}, getting it now...");
                await AddGeoData(player);
            }
            return Players[player];
        }
        public async Task<IpApi.Response?> _GetGeoData(RunnerPlayer player) => await _GetGeoData(player.IP);
        public async Task<IpApi.Response?> _GetGeoData(IPAddress ip) {
            // if (GettingGeoData) return null;
            GettingGeoData = true;
            var url = Configuration.IpApiUrl.Replace("{ip}", ip.ToString());
            HttpResponseMessage httpResponse;
            try { httpResponse = await GeoApi.httpClient.GetAsync(url); } catch (Exception ex) {
                BluscreamLib.Log($"Failed to get geo data for {ip}: {ex.Message}");
                return null;
            }
            var json = await httpResponse.Content.ReadAsStringAsync();
            var response = IpApi.Response.FromJson(json);
            GettingGeoData = false;
            return response;
        }
        #endregion
        #region Events
        public override void OnModulesLoaded() {
            this.CommandHandler.Register(this);
        }
        public override Task OnConnected() {
            AddAllGeoData(this.Server).Wait();
            return Task.CompletedTask;
        }
        public override Task OnDisconnected() {
            RemoveAllGeoData(this.Server, Configuration.RemoveDelay).Wait();
            return Task.CompletedTask;
        }
        public override Task OnPlayerConnected(RunnerPlayer player) {
            AddGeoData(player).Wait();
            return Task.CompletedTask;
        }
        public override Task OnPlayerDisconnected(RunnerPlayer player) {
            RemoveGeoData(player, Configuration.RemoveDelay);
            return Task.CompletedTask;
        }
        #endregion
        #region Commands
        [CommandCallback("playerinfo", Description = "Displays info about a player", AllowedRoles = Roles.Admin )]
        public void GetPlayerInfo(RunnerPlayer commandSource, RunnerPlayer? player = null) {
            player = player ?? commandSource;
            var geoResponse = GetGeoData(player)?.Result;
            if (geoResponse is null) { commandSource.Message($"Failed to get Geo Data for player {player.str()}"); return; }
            var response = new StringBuilder();
            if (!string.IsNullOrEmpty(player.Name)) response.AppendLine($"Name: {player.str()} ({player.Name.Length} chars)");
            if (!string.IsNullOrEmpty(player.IP.ToString())) response.Append($"IP: {player.IP}");
            if (geoResponse is not null) {
                if (geoResponse.Proxy == true) response.Append($" (Proxy/VPN)");
                if (geoResponse.Hosting == true) response.Append($" (Server)");
                if (!string.IsNullOrEmpty(geoResponse.Isp)) response.AppendLine($"\nISP: {geoResponse.Isp}");
                if (!string.IsNullOrEmpty(geoResponse.Country)) response.AppendLine($"Country: {geoResponse.Country}");
                if (!string.IsNullOrEmpty(geoResponse.RegionName)) response.AppendLine($"Region: {geoResponse.RegionName}");
                if (!string.IsNullOrEmpty(geoResponse.City)) response.AppendLine($"City: {geoResponse.City} ({geoResponse.Zip})");
                if (!string.IsNullOrEmpty(geoResponse.Timezone)) response.AppendLine($"Time: {TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, geoResponse.Timezone).ToString("HH:mm")} ({geoResponse.Timezone})");
            }
            commandSource.Message(response.ToString());
        }
        [CommandCallback("playerlist", Description = "Lists players and their respective countries")]
        public void ListPlayers(RunnerPlayer commandSource) {
            var response = new StringBuilder($"{Players.Count} Players:\n\n");
            foreach (var (player, geoData) in Players) {
                response.AppendLine($"{player.str()} from {geoData.Country}, {geoData.Continent}");
            }
            commandSource.Message(response.ToString());
        }
        #endregion
    }
    public class GeoApiCommandsConfiguration : ModuleConfiguration {
        public CommandConfiguration playerinfo { get; set; } = new CommandConfiguration() { AllowedRoles = new() { "Admin" } };
        public CommandConfiguration players { get; set; } = new CommandConfiguration() { AllowedRoles = new () { "All" } };
    }
    public class IpApiConfiguration : ModuleConfiguration {
        public string IpApiUrl { get; set; } = "http://ip-api.com/json/{ip}?fields=status,message,continent,continentCode,country,countryCode,region,regionName,city,district,zip,lat,lon,timezone,offset,currency,isp,org,as,asname,reverse,mobile,proxy,hosting,query";
        public TimeSpan RemoveDelay { get; set; } = TimeSpan.FromMinutes(1);
    }
}
#region json
namespace IpApi {
    public partial class Response {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("continent")]
        public string? Continent { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("continentCode")]
        public string? ContinentCode { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("countryCode")]
        public string? CountryCode { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("regionName")]
        public string? RegionName { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("district")]
        public string? District { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("zip")]
        public string? Zip { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("lat")]
        public double? Lat { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("lon")]
        public double? Lon { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("offset")]
        public long? Offset { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("isp")]
        public string? Isp { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("org")]
        public string? Org { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("as")]
        public string? As { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("asname")]
        public string? Asname { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("reverse")]
        public string? Reverse { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("mobile")]
        public bool? Mobile { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("proxy")]
        public bool? Proxy { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("hosting")]
        public bool? Hosting { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("query")]
        public string? Query { get; set; }
    }

    public partial class Response {
        public static Response FromJson(string json) => JsonSerializer.Deserialize<Response>(json, Converter.Settings);
    }

    public static class Serialize {
        public static string ToJson(this Response self) => JsonSerializer.Serialize(self, Converter.Settings);
    }
}
#endregion