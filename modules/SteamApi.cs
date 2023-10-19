using Amazon.Runtime.Internal.Util;
using BBRAPIModules;
using Bluscream;
using SteamWebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace Bluscream {
    [RequireModule(typeof(BluscreamLib))]
    [Module("Steam Web API data provider API for other modules", "2.0.2")]
    public class SteamApi : BattleBitModule {
        public static ModuleInfo ModuleInfo = new() {
            Name = "SteamApi",
            Description = "Steam Web API data provider API for other modules",
            Version = new Version(2, 0, 2),
            Author = "Bluscream",
            WebsiteUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/"),
            UpdateUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/raw/master/modules/SteamApi.cs"),
            SupportUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/issues/new?title=SteamApi")
        };

        public static Configuration Configuration { get; set; }

        public delegate void DataReceivedHandler(Response steamData);
        public static event DataReceivedHandler OnDataReceived;

        internal static HttpClient httpClient = new HttpClient();
        public static IReadOnlyDictionary<ulong, Response> Cache { get { return _Cache; } }
        private static Dictionary<ulong, Response> _Cache { get; set; } = new();

        #region Methods
        private static void Log(object _msg, string source = "SteamApi") => BluscreamLib.Log(_msg, source);
        #endregion

        #region Api
        public async Task<long> GetBanCount(RunnerPlayer player) {
            var bans = (await GetData(player)!).Bans;
            if (bans is null) return -1;
            var banCount = bans.NumberOfVacBans.Value + bans.NumberOfGameBans.Value;
            if (bans.CommunityBanned == true) banCount++;
            if (bans.EconomyBan != "none") banCount++;
            return banCount;
        }

        public static async Task<Response>? GetData(RunnerServer server) => await GetData(server.AllPlayers.Select(p => p.SteamID));
        public static async Task<Response>? GetData(RunnerPlayer player) => await GetData(player.SteamID);
        public static async Task<Response>? GetData(ulong steamId64) => await GetData(IPAddress.Parse(ip));
        public async Task<Response>? GetData(RunnerPlayer player) {
            if (!Players.ContainsKey(player)) {
                Log($"For some reason we dont have data for \"{player.Name}\", getting it now...");
                await AddData(player);
            }
            return Players[player];
        }

        private static async Task AddData(RunnerServer server) => await AddData(server.AllPlayers.Select(p => p.SteamID));
        private static async Task AddData(RunnerPlayer player) => await AddData(player.SteamID);
        private static async Task AddData(ulong SteamId64) => await AddData(new List<ulong>() { SteamId64 });
        private static async Task AddData(List<ulong> SteamId64s) {
            if (Cache.Keys.ContainsAll(SteamId64s)) return;
            var steamDataResponse = await _GetData(SteamId64s);
            if (steamDataResponse is null) return;
            foreach (var steamData in steamDataResponse) {
                _Cache[steamData.Key] = steamData.Value;
                OnDataReceived?.Invoke(geoData.Query, geoData);
            }
        }
        public static async Task<Response> RemoveData(RunnerPlayer player, TimeSpan? delay = null) => await RemoveData(player.SteamID, delay);
        public static async Task<Response> RemoveData(ulong steamId64, TimeSpan? delay = null) {
            if (delay is not null && delay != TimeSpan.Zero) await Task.Delay(delay.Value);
            var steamData = Cache[steamId64];
            _Cache.Remove(steamId64);
            return steamData;
        }
        public static async Task<List<Response>> PurgeCache(bool force = false, bool refresh = false, TimeSpan? delay = null) {
            if (delay is not null && delay != TimeSpan.Zero) await Task.Delay(delay.Value);
            List<Response> oldEntries = new();
            var oldTime = DateTime.Now - Configuration.RemoveEntriesFromCacheDelay;
            foreach (var entry in _Cache) {
                if (force || entry.Value.CacheTime < oldTime) {
                    var removed = await RemoveData(entry.Key);
                    oldEntries.Add(removed);
                }
            }
            if (refresh) await AddData(oldEntries.Select(e => e.SteamId64).ToList());
            Log($"Found and removed {oldEntries.Count} entries older than {oldTime} from cache.");
            return oldEntries;
        }

        private static async Task<Dictionary<ulong, Response>> _GetData(List<RunnerPlayer> players) => await _GetData(players.Select(p => p.SteamID));
        public static async Task<Dictionary<ulong, Response>> _GetData(RunnerPlayer player) => await _GetData(new[] { player.SteamID });
        private static async Task<Dictionary<ulong, Response>> _GetData(IEnumerable<ulong> SteamId64s) {
            if (string.IsNullOrWhiteSpace(Configuration.SteamWebApiKey)) {
                throw new("Steam Web API Key is not set up in config, can't continue!");
            }
            var steamIdList = string.Join(",", SteamId64s);
            var apiKey = Configuration.SteamWebApiKey;
            Dictionary<ulong, Response> responses = new();
            var summariesUrl = Configuration.GetPlayerSummaryUrl.Replace("{steamId64}", steamIdList).Replace("{apikey}", apiKey);
            var bansUrl = Configuration.GetPlayerBansUrl.Replace("{steamId64}", steamIdList).Replace("{apikey}", apiKey);
            foreach (var chunk in SteamId64s.Chunk(100)) {
                SummaryResponse? summariesResponse = null!;
                try { summariesResponse = await httpClient.GetFromJsonAsync<SummaryResponse>(summariesUrl); } catch (Exception ex) {
                    Log($"Failed to get steam summary for {string.Join(", ", chunk.ToList())}: {ex.Message}");
                }
                foreach (var player in summariesResponse?.Response?.Players) {
                    _AddData(ref responses, player.SteamId64, player);
                }
                BansResponse? bansResponse = null!;
                try { bansResponse = await httpClient.GetFromJsonAsync<BansResponse>(bansUrl); } catch (Exception ex) {
                    Log($"Failed to get steam bans for {string.Join(", ", chunk.ToList())}: {ex.Message}");
                }
                foreach (var player in bansResponse?.Players) {
                    _AddData(ref responses, player.SteamId64, player);
                }
            }
            return responses;
        }

        private static void _AddData(ref Dictionary<ulong, Response> dict, ulong steamId64, object data) {
            if (!dict.ContainsKey(steamId64)) dict[steamId64] = new Response();
            if (data is BansResponse bans) dict[steamId64].Bans = bans.Players.FirstOrDefault();
            if (data is SummaryResponse summaries) dict[steamId64].Summary = summaries.Response.Players.FirstOrDefault();
        }

        public async Task<Response?> _GetData(RunnerPlayer player) => await _GetData(player.SteamID);
        public async Task<Response?> _GetData(ulong steamId64) {
            BansResponse bansResponse;
            try {
                var url = Configuration.GetPlayerBansUrl.Replace("{steamId64}", steamId64.ToString()).Replace("{Configuration.SteamWebApiKey}", Configuration.SteamWebApiKey);
                this.Logger.Debug($"GET {url}");
                var httpResponse = await SteamApi.httpClient.GetAsync(url);
                var json = await httpResponse.Content.ReadAsStringAsync();
                bansResponse = JsonUtils.FromJson<BansResponse>(json);
            } catch (Exception ex) {
                this.Logger.Error($"Failed to get steam bans for {steamId64}: {ex.Message}");
                return null;
            }

            SummaryResponse summaryResponse;
            try {
                var url = ;
                this.Logger.Debug($"GET {url}");
                var httpResponse = await SteamApi.httpClient.GetAsync(url);
                var json = await httpResponse.Content.ReadAsStringAsync();
                summaryResponse = JsonUtils.FromJson<SummaryResponse>(json);
            } catch (Exception ex) {
                this.Logger.Error($"Failed to get steam summary for {steamId64}: {ex.Message}");
                return null;
            }
            return new SteamWebApi.Response() { Bans = bansResponse.Players.FirstOrDefault(), Summary = summaryResponse?.Response?.Players?.FirstOrDefault() };
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
                RemoveAllData(this.Server, Configuration.RemoveDelay).Wait();
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
                RemoveData(player, Configuration.RemoveDelay).Wait();
            });
            return Task.CompletedTask;
        }
        #endregion
    }
    public class Configuration : ModuleConfiguration {
        public string SteamWebApiKey { get; set; } = string.Empty;
        public string GetPlayerBansUrl { get; set; } = "http://api.steampowered.com/ISteamUser/GetPlayerBans/v1/?steamids={steamId64}&key={Configuration.SteamWebApiKey}";
        public string GetPlayerSummaryUrl { get; set; } = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?steamids={steamId64}&key={Configuration.SteamWebApiKey}";
        public TimeSpan RemovePlayersAfterLeaveDelay { get; set; } = TimeSpan.FromMinutes(30);
        public TimeSpan RemoveEntriesFromCacheDelay { get; set; } = TimeSpan.FromHours(12);
    }
}
#region Extensions
public static partial class Extensions {
    public static async Task<Response?> GetSteamData(this RunnerServer server) => await SteamApi.GetData(server);
    public static async Task<Response?> GetSteamData(this RunnerPlayer player) => await SteamApi.GetData(player);
}
#endregion
#region json
namespace SteamWebApi {
    public class Response {
        public SteamWebApi.BansPlayer? Bans { get; set; }
        public SteamWebApi.SummaryPlayer? Summary { get; set; }
        public ulong SteamId64 => Bans?.SteamId64 ?? Summary?.SteamId64 ?? 0;

        [JsonIgnore]
        public DateTime CacheTime { get; private set; } = DateTime.Now;
    }

    public partial class BansResponse {
        [JsonPropertyName("players")]
        public List<BansPlayer> Players { get; set; } = null!;

    }

    public partial class BansPlayer {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("SteamId")]
        public virtual string _SteamId64 { get; set; } = null!;
        [JsonIgnore]
        public virtual ulong SteamId64 { get { return ulong.Parse(_SteamId64); } }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("CommunityBanned")]
        public bool? CommunityBanned { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("VACBanned")]
        public bool? VacBanned { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("NumberOfVACBans")]
        public long? NumberOfVacBans { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("DaysSinceLastBan")]
        public long? DaysSinceLastBan { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("NumberOfGameBans")]
        public long? NumberOfGameBans { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("EconomyBan")]
        public string? EconomyBan { get; set; } = null!;
    }

    public partial class SummaryResponse {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("response")]
        public virtual SummaryResponseResponse? Response { get; set; }

    }

    public partial class SummaryResponseResponse {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("players")]
        public virtual List<SummaryPlayer>? Players { get; set; }
    }

    public partial class SummaryPlayer {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("steamid")]
        public virtual string _SteamId64 { get; set; } = null!;
        [JsonIgnore]
        public virtual ulong SteamId64 { get { return ulong.Parse(_SteamId64); } }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("communityvisibilitystate")]
        public virtual long? CommunityVisibilityState { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("profilestate")]
        public virtual long? ProfileState { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("personaname")]
        public virtual string? PersonaName { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("profileurl")]
        public virtual Uri? ProfileUrl { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("avatar")]
        public virtual Uri? Avatar { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("avatarmedium")]
        public virtual Uri? AvatarMedium { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("avatarfull")]
        public virtual Uri? AvatarFull { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("avatarhash")]
        public virtual string? AvatarHash { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("personastate")]
        public virtual long? PersonaState { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("realname")]
        public virtual string? RealName { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("primaryclanid")]
        public virtual string? _PrimaryClanId { get; set; }
        [JsonIgnore]
        public virtual long? PrimaryClanId { get { return long.Parse(_PrimaryClanId); } }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("timecreated")]
        public virtual long? _TimeCreated { get; set; }
        [JsonIgnore]
        public virtual DateTime? TimeCreated { get { return DateTimeOffset.FromUnixTimeSeconds((long)_TimeCreated).DateTime; } }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("personastateflags")]
        public virtual long? PersonaStateFlags { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("commentpermission")]
        public virtual long? CommentPermission { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("loccountrycode")]
        public virtual string? CountryCode { get; set; }
        [JsonIgnore]
        public string CountryFlagEmoji => string.IsNullOrWhiteSpace(CountryCode) ? "🌎" : $":flag_{CountryCode?.ToLowerInvariant()}:";

        [JsonIgnore]
        public virtual string DisplayName => RealName ?? PersonaName ?? "Unknown";
    }
}
#endregion