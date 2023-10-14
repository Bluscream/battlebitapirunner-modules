using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Http;

using BBRAPIModules;
using Bluscream;
using System.Linq;
using SteamWebApi;

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

        public Configuration Configuration { get; set; }
        internal static HttpClient httpClient = new HttpClient();
        public delegate void DataReceivedHandler(RunnerPlayer player, SteamWebApi.Response steamData);
        public event DataReceivedHandler OnPlayerDataReceived;

        public IReadOnlyDictionary<RunnerPlayer, SteamWebApi.Response> Players { get { return _Players; } }
        private Dictionary<RunnerPlayer, SteamWebApi.Response> _Players { get; set; } = new Dictionary<RunnerPlayer, SteamWebApi.Response>();

        #region Methods
        private static void Log(object _msg, string source = "SteamApi") => BluscreamLib.Log(_msg, source);
        private async Task AddAllData(RunnerServer? server = null) {
            server = server ?? this.Server;
            foreach (var player in server.AllPlayers) {
                await AddData(player);
            }
        }
        private async Task RemoveAllData(RunnerServer? server = null, TimeSpan? delay = null) {
            server = server ?? this.Server;
            if (delay is not null && delay != TimeSpan.Zero) await Task.Delay(delay.Value);
            foreach (var player in server.AllPlayers) {
                await RemoveData(player);
            }
        }
        private async Task AddData(RunnerPlayer player) {
            if (Players.ContainsKey(player)) return;
            SteamWebApi.Response? steamData = await _GetData(player);
            if (steamData is null) return;
            _Players.Add(player, steamData);
            OnPlayerDataReceived?.Invoke(player, steamData);
        }
        private async Task RemoveData(RunnerPlayer player, TimeSpan? delay = null) {
            if (delay is not null && delay != TimeSpan.Zero) await Task.Delay(delay.Value); // Todo: Make configurable
            if (_Players.ContainsKey(player))
                _Players.Remove(player);
        }
        #endregion

        #region Api
        public async Task<long> GetBanCount(RunnerPlayer player) {
            var bans = (await GetData(player)!).Bans;
            if (bans is null) return -1;
            var banCount = bans.NumberOfVacBans + bans.NumberOfGameBans;
            if (bans.CommunityBanned) banCount++;
            if (bans.EconomyBan != "none") banCount++;
            return banCount;
        }
        public async Task<SteamWebApi.Response>? GetData(RunnerPlayer player) {
            if (!Players.ContainsKey(player)) {
                Log($"For some reason we dont have data for \"{player.Name}\", getting it now...");
                await AddData(player);
            }
            return Players[player];
        }
        public async Task<SteamWebApi.Response?> _GetData(RunnerPlayer player) => await _GetData(player.SteamID);
        public async Task<SteamWebApi.Response?> _GetData(ulong steamId64) {
            if (string.IsNullOrWhiteSpace(Configuration.SteamWebApiKey)) {
                Console.WriteLine("Steam Web API Key is not set up in config, can't continue!");
                return null!;
            }
            BansResponse bansResponse;
            try {
                var url = Configuration.GetPlayerBansUrl.Replace("{steamId64}", steamId64.ToString()).Replace("{Configuration.SteamWebApiKey}", Configuration.SteamWebApiKey);
                this.Logger.Debug($"GET {url}");
                var httpResponse = await SteamApi.httpClient.GetAsync(url);
                var json = await httpResponse.Content.ReadAsStringAsync();
                bansResponse = SteamWebApi.BansResponse.FromJson(json);
            } catch (Exception ex) {
                this.Logger.Error($"Failed to get steam bans for {steamId64}: {ex.Message}");
                return null;
            }

            SummaryResponse summaryResponse;
            try {
                var url = Configuration.GetPlayerSummaryUrl.Replace("{steamId64}", steamId64.ToString()).Replace("{Configuration.SteamWebApiKey}", Configuration.SteamWebApiKey);
                this.Logger.Debug($"GET {url}");
                var httpResponse = await SteamApi.httpClient.GetAsync(url);
                var json = await httpResponse.Content.ReadAsStringAsync();
                summaryResponse = SteamWebApi.SummaryResponse.FromJson(json);
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
        public TimeSpan RemoveDelay { get; set; } = TimeSpan.FromMinutes(1);
    }
}
#region json
namespace SteamWebApi {
    public class Response {
        public SteamWebApi.BansPlayer? Bans { get; set; }
        public SteamWebApi.SummaryPlayer? Summary { get; set; }
    }

    public partial class BansResponse {
        [JsonPropertyName("players")]
        public List<BansPlayer> Players { get; set; } = null!;

        public static BansResponse FromJson(string json) => JsonSerializer.Deserialize<BansResponse>(json, Converter.Settings);
    }

    public partial class BansPlayer {
        [JsonPropertyName("SteamId")]
        public string SteamId { get; set; } = null!;

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
        public string EconomyBan { get; set; } = null!;
    }

    public partial class SummaryResponse {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("response")]
        public virtual SummaryResponseResponse? Response { get; set; }

        public static SummaryResponse FromJson(string json) => JsonSerializer.Deserialize<SummaryResponse>(json, Converter.Settings);
    }

    public partial class SummaryResponseResponse {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("players")]
        public virtual List<SummaryPlayer>? Players { get; set; }
    }

    public partial class SummaryPlayer {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("steamid")]
        public virtual string? _SteamId64 { get; set; }
        [JsonIgnore]
        public virtual long? SteamId64 { get { return long.Parse(_SteamId64); } }

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
    }

    public static class Serialize {
        public static string ToJson(this BansResponse self) => JsonSerializer.Serialize(self, Converter.Settings);
        public static string ToJson(this SummaryResponse self) => JsonSerializer.Serialize(self, Converter.Settings);
    }
}
#endregion