using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;

using BBRAPIModules;
using Bluscream;

namespace Bluscream {
    [RequireModule(typeof(BluscreamLib))]
    [Module("IP and geolocation data provider for other modules", "2.0.2")]
    public class GeoApi : BattleBitModule {
        public static ModuleInfo ModuleInfo = new() {
            Name = "GeoApi",
            Description = "IP and geolocation data provider for other modules",
            Version = new Version(2, 0, 2),
            Author = "Bluscream",
            WebsiteUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/"),
            UpdateUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/raw/master/modules/GeoApi.cs"),
            SupportUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/issues/new?title=GeoApi")
        };

        public Configuration Config { get; set; }

        internal static HttpClient httpClient = new HttpClient();
        public delegate void DataReceivedHandler(RunnerPlayer player, IpApi.Response geoData);
        public event DataReceivedHandler OnPlayerDataReceived;

        public IReadOnlyDictionary<RunnerPlayer, IpApi.Response> Players { get { return _Players; } }
        private Dictionary<RunnerPlayer, IpApi.Response> _Players { get; set; } = new Dictionary<RunnerPlayer, IpApi.Response>();
        #region Methods
        private static void Log(object _msg, string source = "SteamApi") => BluscreamLib.Log(_msg, source);
        private async Task RemoveAllData(RunnerServer? server = null, TimeSpan? delay = null) {
            server = server ?? this.Server;
            if (delay is not null && delay != TimeSpan.Zero) await Task.Delay(delay.Value);
            foreach (var player in server.AllPlayers) {
                await RemoveData(player);
            }
        }
        private async Task AddAllData(RunnerServer? server = null) {
            server = server ?? this.Server;
            foreach (var player in server.AllPlayers) {
                await AddData(player);
            }
        }
        private async Task AddData(RunnerPlayer player) {
            if (Players.ContainsKey(player)) return;
            IpApi.Response? geoData = await _GetData(player);
            if (geoData is null || Players.ContainsKey(player)) return;
            _Players.Add(player, geoData);
            OnPlayerDataReceived?.Invoke(player, geoData);
        }
        private async Task RemoveData(RunnerPlayer player, TimeSpan? delay = null) {
            if (delay is not null && delay != TimeSpan.Zero) await Task.Delay(delay.Value);
            if (_Players.ContainsKey(player))
                _Players.Remove(player);
        }
        #endregion
        #region Api
        public async Task<IpApi.Response>? GetData(RunnerPlayer player) {
            if (!Players.ContainsKey(player)) {
                Log($"For some reason we dont have Data for \"{player.Name}\", getting it now...");
                await AddData(player);
            }
            return Players[player];
        }
        public async Task<IpApi.Response?> _GetData(RunnerPlayer player) => await _GetData(player.IP);
        public async Task<IpApi.Response?> _GetData(IPAddress ip) {
            var url = Config.IpApiUrl.Replace("{ip}", ip.ToString());
            HttpResponseMessage httpResponse;
            try { httpResponse = await GeoApi.httpClient.GetAsync(url); } catch (Exception ex) {
                Log($"Failed to get geo data for {ip}: {ex.Message}");
                return null;
            }
            var json = await httpResponse.Content.ReadAsStringAsync();
            var response = IpApi.Response.FromJson(json);
            return response;
        }
        #endregion
        #region Events
        public override Task OnConnected() {
            Task.Run(() => {
                AddAllData(this.Server).Wait();
            });
            return Task.CompletedTask;
        }
        public override Task OnDisconnected() {
            Task.Run(() => {
                RemoveAllData(this.Server, Config.RemoveDelay).Wait();
            });
            return Task.CompletedTask;
        }
        public override Task OnPlayerConnected(RunnerPlayer player) {
            Task.Run(() => {
                AddData(player).Wait();
            });
            return Task.CompletedTask;
        }
        public override Task OnPlayerDisconnected(RunnerPlayer player) {
            Task.Run(() => {
                RemoveData(player, Config.RemoveDelay).Wait();
            });
            return Task.CompletedTask;
        }
        #endregion
        #region Configuration
        public class Configuration : ModuleConfiguration {
            public string IpApiUrl { get; set; } = "http://ip-api.com/json/{ip}?fields=status,message,continent,continentCode,country,countryCode,region,regionName,city,district,zip,lat,lon,timezone,offset,currency,isp,org,as,asname,reverse,mobile,proxy,hosting,query";
            public TimeSpan RemoveDelay { get; set; } = TimeSpan.FromMinutes(1);
        }
        #endregion
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
        public string CountryFlagEmoji => string.IsNullOrWhiteSpace(CountryCode) ? "🌎" : $":flag_{CountryCode?.ToLowerInvariant()}:";

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

        public static Response FromJson(string json) => JsonUtils.FromJson<Response>(json);
    }
    public static class Serialize {
        public static string ToJson(this Response self) => JsonUtils.ToJson(self);
    }
}
#endregion
#region MaxMindDB
namespace MaxMindDB {
    public class DataBase {
        
    }
}
#endregion