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

        public static Configuration Config { get; set; }

        public delegate void DataReceivedHandler(Response steamData);
        public static event DataReceivedHandler OnDataReceived;

        internal static HttpClient httpClient = new HttpClient();

        public static IReadOnlyDictionary<ulong, Response> Cache { get { return _Cache; } }
        private static Dictionary<ulong, Response> _Cache { get; set; } = new();

        #region Methods
        private static void Log(object _msg, string source = "SteamApi") => BluscreamLib.Log(_msg, source);
        private static Response? GetCacheData(ulong steamId64, bool checkValid = true) => _Cache.Select(c => c.Value).FirstOrDefault(c => c.SteamId64 == steamId64 && (!checkValid || c.IsValid(Config.RemoveEntriesFromCacheDelay)));
        private static bool HasCacheData(ulong steamId64, bool checkValid = true) => GetCacheData(steamId64, checkValid) is not null;
        #endregion

        #region Api
        public async Task<long> GetBanCount(RunnerPlayer player) {
            var bans = (await Get(player)).Bans;
            if (bans is null) return -1;
            var banCount = bans.NumberOfVacBans.Value + bans.NumberOfGameBans.Value;
            if (bans.CommunityBanned == true) banCount++;
            if (bans.EconomyBan != "none") banCount++;
            return banCount;
        }

        private static async Task<Dictionary<ulong, Response>> Fetch(RunnerServer server) => await Fetch(server.AllPlayers.Select(p => p.SteamID));
        private static async Task<Dictionary<ulong, Response>> Fetch(RunnerPlayer player) => await Fetch(new[] { player.SteamID });
        private static async Task<Dictionary<ulong, Response>> Fetch(List<RunnerPlayer> players) => await Fetch(players.Select(p => p.SteamID));
        private static async Task<Dictionary<ulong, Response>> Fetch(IEnumerable<ulong> SteamId64s) {
            if (string.IsNullOrWhiteSpace(Config.SteamWebApiKey)) {
                BluscreamLib.Logger.Error("Steam Web API Key is not set up in config, can't continue!");
                throw new("Steam Web API Key is not set up in config, can't continue!");
            }
            SteamId64s = SteamId64s.Distinct();
            var apiKey = Config.SteamWebApiKey;
            Dictionary<ulong, Response> responses = new();
            foreach (var chunk in SteamId64s.Chunk(100)) {
                var steamIdChunk = string.Join(",", SteamId64s);
                SummaryResponse? summariesResponse = null!;
                var summariesUrl = Config.GetPlayerSummaryUrl.Replace("{steamids}", steamIdChunk).Replace("{apikey}", apiKey);
                BluscreamLib.Logger.Debug(summariesUrl);
                try { summariesResponse = await httpClient.GetFromJsonAsync<SummaryResponse>(summariesUrl); } catch (Exception ex) {
                    BluscreamLib.Logger.Error($"Failed to get steam summary for {steamIdChunk}: {ex.Message}");
                }
                foreach (var player in summariesResponse?.Response?.Players) {
                    if (responses.ContainsKey(player.SteamId64)) {
                        responses[player.SteamId64].Summary = player;
                    } else {
                        responses[player.SteamId64] = new Response() { Summary = player };
                    }
                }
                BansResponse? bansResponse = null!;
                var bansUrl = Config.GetPlayerBansUrl.Replace("{steamids}", steamIdChunk).Replace("{apikey}", apiKey);
                BluscreamLib.Logger.Debug(bansUrl);
                try { bansResponse = await httpClient.GetFromJsonAsync<BansResponse>(bansUrl); } catch (Exception ex) {
                    BluscreamLib.Logger.Error($"Failed to get steam bans for {steamIdChunk}: {ex.Message}");
                }
                foreach (var player in bansResponse?.Players) {
                    if (responses.ContainsKey(player.SteamId64)) {
                        responses[player.SteamId64].Bans = player;
                    } else {
                        responses[player.SteamId64] = new Response() { Bans = player };
                    }
                }
            }
            return responses;
        }

        public static async Task<IEnumerable<Response>> Get(RunnerServer server) => (await Get(server.AllPlayers.Select(p => p.SteamID))).Values;
        public static async Task<Response> Get(RunnerPlayer player) => (await Get(player.SteamID));
        public static async Task<Response> Get(ulong SteamId64) => (await Get(new List<ulong>() { SteamId64 })).First().Value;
        public static async Task<Dictionary<ulong, Response>> Get(IEnumerable<ulong> SteamId64s) {
            var responses = new Dictionary<ulong, Response>();
            var needSteamId64s = SteamId64s.Where(s => !HasCacheData(s)).ToList();
            if (needSteamId64s.Count == 0) return Cache.Where(c => SteamId64s.Contains(c.Key)).ToDictionary(c => c.Key, c => c.Value);
            Console.WriteLine("get1");
            var steamDataResponse = await Fetch(needSteamId64s);
            Console.WriteLine("get2");
            if (steamDataResponse is null) return responses;
            foreach (var steamData in steamDataResponse) {
                responses[steamData.Key] = steamData.Value;
                _Cache[steamData.Key] = steamData.Value;
            }
            return responses;
        }
        public static List<Response> RemoveData(RunnerServer server, bool refresh = false, TimeSpan? delay = null) => RemoveData(server.AllPlayers.Select(p => p.SteamID), refresh, delay);
        public static Response RemoveData(RunnerPlayer player, bool refresh = false, TimeSpan? delay = null) => RemoveData(player.SteamID, refresh, delay);
        public static Response RemoveData(ulong steamId64, bool refresh = false, TimeSpan? delay = null) => RemoveData(new[] { steamId64 }, refresh, delay).First();
        public static List<Response> RemoveData(IEnumerable<ulong> steamIds64, bool refresh = false, TimeSpan? delay = null) {
            if (delay is not null && delay != TimeSpan.Zero) Task.Delay(delay.Value).Wait();
            List<Response> oldEntries = new();
            foreach (var steamId64 in steamIds64) {
                var steamData = GetCacheData(steamId64);
                if (steamData is not null) {
                    oldEntries.Add(steamData);
                    _Cache.Remove(steamId64);
                }
            }
            if (refresh) Task.Run(() => {
                Get(steamIds64).Wait();
            });
            Log($"Found and removed {oldEntries.Count} / {steamIds64.Count()} entries from cache.");
            return oldEntries;
        }
        #endregion

        #region Events
        public override Task OnConnected() {
            Task.Run(() => {
                Get(this.Server).Wait();
            });
            return Task.CompletedTask;
        }
        public override Task OnDisconnected() {
            Task.Run(() => {
                RemoveData(this.Server, delay: Config.RemovePlayersAfterLeaveDelay);
            });
            return Task.CompletedTask;
        }
        public override Task OnPlayerConnected(RunnerPlayer player) {
            Task.Run(() => {
                Get(player).Wait();
            });
            return Task.CompletedTask;
        }
        public override Task OnPlayerDisconnected(RunnerPlayer player) {
            Task.Run(() => {
                RemoveData(player, delay: Config.RemovePlayersAfterLeaveDelay);
            });
            return Task.CompletedTask;
        }
        #endregion
    }
    public class Configuration : ModuleConfiguration {
        public string SteamWebApiKey { get; set; } = string.Empty;
        public string GetPlayerBansUrl { get; set; } = "http://api.steampowered.com/ISteamUser/GetPlayerBans/v1/?steamids={steamids}&key={apikey}";
        public string GetPlayerSummaryUrl { get; set; } = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?steamids={steamids}&key={apikey}";
        public TimeSpan RemovePlayersAfterLeaveDelay { get; set; } = TimeSpan.FromMinutes(30);
        public TimeSpan RemoveEntriesFromCacheDelay { get; set; } = TimeSpan.FromHours(12);
    }
}
#region Extensions
public static partial class Extensions {
    public static async Task<Dictionary<RunnerPlayer, Response?>> GetSteamData(this RunnerServer server) => (await SteamApi.Get(server)).ToDictionary(a => server.GetPlayersBySteamId64(a.SteamId64).First(), b => b);
    public static async Task<Response?> GetSteamData(this RunnerPlayer player) => await SteamApi.Get(player);
}
#endregion
#region json
namespace SteamWebApi {
    public class Response {
        public BansPlayer? Bans { get; set; }
        public SummaryPlayer? Summary { get; set; }
        public ulong SteamId64 => Bans?.SteamId64 ?? Summary?.SteamId64 ?? 0;

        [JsonIgnore]
        public DateTime CacheTime { get; private set; } = DateTime.Now;

        public bool IsValid(TimeSpan timeSpan) => Bans is not null && Summary is not null && CacheTime > (DateTime.Now - timeSpan);

        public IEnumerable<RunnerPlayer> GetPlayers(RunnerServer server) => server.GetPlayersBySteamId64(SteamId64);
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