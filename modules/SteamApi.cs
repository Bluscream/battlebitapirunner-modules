using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Http;

using BBRAPIModules;
using Bluscream;
using System.Linq;

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
            var url = Configuration.GetPlayerBansUrl.Replace("{steamId64}", steamId64.ToString()).Replace("{Configuration.SteamWebApiKey}", Configuration.SteamWebApiKey);
            HttpResponseMessage httpResponse;
            try { httpResponse = await SteamApi.httpClient.GetAsync(url); } catch (Exception ex) {
                Log($"Failed to get steam data for {steamId64}: {ex.Message}");
                return null;
            }
            var json = await httpResponse.Content.ReadAsStringAsync();
            var response = SteamWebApi.BanResponse.FromJson(json);
            return new SteamWebApi.Response() { Bans = response.Players.First() };
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
        public TimeSpan RemoveDelay { get; set; } = TimeSpan.FromMinutes(1);
    }
}
#region json
namespace SteamWebApi {
    public class Response {
        public SteamWebApi.Player? Bans { get; set; }
    }

    public partial class BanResponse {
        [JsonPropertyName("players")]
        public List<Player> Players { get; set; } = null!;
    }

    public partial class Player {
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

    public partial class BanResponse {
        public static BanResponse FromJson(string json) => JsonSerializer.Deserialize<BanResponse>(json, Converter.Settings);
    }

    public static class Serialize {
        public static string ToJson(this BanResponse self) => JsonSerializer.Serialize(self, Converter.Settings);
    }
}
#endregion