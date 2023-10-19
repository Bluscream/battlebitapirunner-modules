using BBRAPIModules;
using Bluscream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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

        public const string IpApiUrl = "http://ip-api.com";
        public const string IpApiProUrl = "https://pro.ip-api.com";
        public const string IpApiFields = "status,message,continent,continentCode,country,countryCode,region,regionName,city,district,zip,lat,lon,timezone,offset,currency,isp,org,as,asname,reverse,mobile,proxy,hosting,query";
        public const string IpApiSingleUrl = "/{ip}?";
        public const string IpApiBatchUrl = "/batch?";

        public static Configuration Config { get; set; }

        internal static HttpClient httpClient = new HttpClient();
        public delegate void DataReceivedHandler(IPAddress ip, IpApi.Response geoData);
        public static event DataReceivedHandler OnDataReceived;

        public static IReadOnlyDictionary<IPAddress, IpApi.Response> Cache { get { return _Cache; } }
        private static Dictionary<IPAddress, IpApi.Response> _Cache { get; set; } = new Dictionary<IPAddress, IpApi.Response>();
        #region Methods
        private static void Log(object _msg, string source = "GeoApi") => BluscreamLib.Log(_msg, source);
        #endregion
        #region Api
        public static async Task<IpApi.Response>? GetData(RunnerServer server) => await GetData(server.GameIP);
        public static async Task<IpApi.Response>? GetData(RunnerPlayer player) => await GetData(player.IP);
        public static async Task<IpApi.Response>? GetData(string ip) => await GetData(IPAddress.Parse(ip));
        public static async Task<IpApi.Response>? GetData(IPAddress ip) {
            if (!Cache.ContainsKey(ip)) {
                Log($"For some reason we dont have Data for \"{ip}\", getting it now...");
                await AddData(ip);
            }
            return Cache[ip];
        }

        private static async Task AddData(RunnerServer server) => await AddData((List<IPAddress>)new List<IPAddress>() { server.GameIP }.Concat(server.AllPlayers.Select(p => p.IP)));
        private static async Task AddData(RunnerPlayer player) => await AddData(player.IP);
        private static async Task AddData(IPAddress ip) => await AddData(new List<IPAddress>() { ip });
        private static async Task AddData(List<IPAddress> ips) {
            if (Cache.Keys.ContainsAll(ips)) return;
            if (ips.Count > 1) {
                var geoDataResponse = await _GetBatchData(ips);
                if (geoDataResponse is null) return;
                foreach (var geoData in geoDataResponse) {
                    _Cache[geoData.Query] = geoData;
                    OnDataReceived?.Invoke(geoData.Query, geoData);
                }
            } else if (ips.Count == 1) {
                var ip = ips.First();
                var geoDataResponse = await _GetData(ips.First());
                if (geoDataResponse is null) return;
                _Cache.Add(ip, geoDataResponse);
                OnDataReceived?.Invoke(ip, geoDataResponse);
            }
        }
        public static async Task<IpApi.Response> RemoveData(RunnerPlayer player, TimeSpan? delay = null) => await RemoveData(player.IP, delay);
        public static async Task<IpApi.Response> RemoveData(IPAddress ip, TimeSpan? delay = null) {
            if (delay is not null && delay != TimeSpan.Zero) await Task.Delay(delay.Value);
            var geoData = Cache[ip];
            _Cache.Remove(ip);
            return geoData;
        }
        public static async Task<List<IpApi.Response>> PurgeCache(bool force = false, bool refresh = false, TimeSpan? delay = null) {
            if (delay is not null && delay != TimeSpan.Zero) await Task.Delay(delay.Value);
            var oldEntries = new List<IpApi.Response>();
            //lock(_Cache) {
            var oldTime = DateTime.Now - Config.RemoveEntriesFromCacheDelay;
            foreach (var entry in _Cache) {
                if (force || entry.Value.CacheTime < oldTime) {
                    oldEntries.Add(RemoveData(entry.Key).Result);
                }
            }
            //}
            if (refresh) await AddData(oldEntries.Select(e => e.Query).ToList());
            Log($"Found and removed {oldEntries.Count} entries older than {oldTime} from cache.");
            return oldEntries;
        }

        private static Uri GetApiUrl(bool pro = false, bool batch = false) {
            var url = new Uri($"{(pro ? IpApiProUrl : IpApiUrl)}{(batch ? IpApiBatchUrl : IpApiSingleUrl)}");
            url.AddQuery("fields", IpApiFields);
            if (pro) url = url.AddQuery("key", Config.IpApiProKey);
            return url;
        }
        private static async Task<IpApi.Response?> _GetData(RunnerPlayer player) => await _GetData(player.IP);
        private static async Task<IpApi.Response?> _GetData(IPAddress ip) {
            var url = GetApiUrl(string.IsNullOrWhiteSpace(Config.IpApiProKey), false).ToString().Replace("{ip}", ip.ToString());
            IpApi.Response? response;
            try { response = await GeoApi.httpClient.GetFromJsonAsync<IpApi.Response>(url, Converter.Settings); } catch (Exception ex) {
                Log($"Failed to get geo data for {ip}: {ex.Message}");
                return null;
            }
            return response;
        }

        private static async Task<List<IpApi.Response>> _GetBatchData(List<RunnerPlayer> players) => await _GetBatchData(players.Select(p => p.IP));
        private static async Task<List<IpApi.Response>> _GetBatchData(IEnumerable<IPAddress> ips) {
            List<IpApi.Response> responses = new();
            var url = GetApiUrl(string.IsNullOrWhiteSpace(Config.IpApiProKey), true);
            foreach (var chunk in ips.Chunk(100)) {
                HttpResponseMessage httpResponse;
                try { httpResponse = await GeoApi.httpClient.PostAsJsonAsync(url, chunk); } catch (Exception ex) {
                    Log($"Failed to get geo data for {string.Join(", ", chunk.ToList())}: {ex.Message}");
                    continue;
                }
                var json = await httpResponse.Content.ReadAsStringAsync();
                responses.AddRange(JsonUtils.FromJson<List<IpApi.Response>>(json));
            }
            return responses;
        }

        #endregion
        #region Events
        public override Task OnConnected() {
            Task.Run(() => {
                AddData(this.Server).Wait();
            });
            return Task.CompletedTask;
        }
        public override Task OnDisconnected() {
            Task.Run(() => {
                PurgeCache(delay: Config.RemovePlayersAfterLeaveDelay).Wait();
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
                RemoveData(player, Config.RemovePlayersAfterLeaveDelay).Wait();
            });
            return Task.CompletedTask;
        }
        #endregion
        #region Configuration
        public class Configuration : ModuleConfiguration {
            public string IpApiProKey { get; set; } = string.Empty;
            public TimeSpan RemovePlayersAfterLeaveDelay { get; set; } = TimeSpan.FromMinutes(30);
            public TimeSpan RemoveEntriesFromCacheDelay { get; set; } = TimeSpan.FromHours(12);
        }
        #endregion
    }
}
#region Extensions
public static partial class Extensions {
    public static async Task<IpApi.Response> GetGeoData(this RunnerServer server) => await GeoApi.GetData(server);
    public static async Task<IpApi.Response> GetGeoData(this RunnerPlayer player) => await GeoApi.GetData(player);
}
#endregion
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
        [JsonIgnore]
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
        public string _Query { get; set; } = null!;
        [JsonIgnore]
        public IPAddress Query => IPAddress.Parse(_Query);

        [JsonIgnore]
        public DateTime CacheTime { get; private set; } = DateTime.Now;

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