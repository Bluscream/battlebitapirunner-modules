using BBRAPIModules;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace VacLimiter;

[Module("Kick users with VAC bans", "1.0.0")]
public class VacLimiter : BattleBitModule
{
    public static VacLimiterConfiguration Configuration { get; set; } = null!;
    public static VacLimiterCache Cache { get; set; } = null!;

    public VacLimiterServerConfiguration ServerConfiguration { get; set; } = null!;

    private static ConcurrentBag<CacheRequest> playersToCheck = new();
    private static bool isLoaded = false;
    private static HttpClient httpClient = null!;

    public override void OnModulesLoaded()
    {
        if (string.IsNullOrWhiteSpace(Configuration.SteamAPIKey))
        {
            Console.WriteLine("Steam API token is not set. Please set it in the configuration file.");
            this.Unload();
            return;
        }

        if (httpClient == null)
        {
            httpClient = new()
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            httpClient.DefaultRequestHeaders.Add("User-Agent", "BattleBit");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            Task.Run(playerChecker);
        }

        isLoaded = true;
    }

    public override void OnModuleUnloading()
    {
        isLoaded = false;
    }

    private static async Task playerChecker()
    {
        while (isLoaded)
        {
            CacheRequest[] playerBatch = playersToCheck.ToArray();
            playersToCheck.Clear();

            if (playerBatch.Length == 0)
            {
                await Task.Delay(Configuration.BatchDelay);
                continue;
            }

            HttpResponseMessage? response = null;

            do
            {
                try
                {
                    response = await httpClient.GetAsync("https://api.steampowered.com/ISteamUser/GetPlayerBans/v1/?key=" + Configuration.SteamAPIKey + "&steamids=" + string.Join(",", playerBatch.Select(x => x.Player.SteamID)));

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Failed to get player bans: {response.StatusCode} {response.ReasonPhrase}");
                        await Task.Delay(Configuration.RetryDelay);
                        continue;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to get player bans: {e.Message}");
                    await Task.Delay(Configuration.RetryDelay);
                    continue;
                }
            } while (response == null || !response.IsSuccessStatusCode);

            PlayerBanResponseModel? playerBanResponse = null;
            try
            {
                playerBanResponse = JsonSerializer.Deserialize<PlayerBanResponseModel>(await response.Content.ReadAsStringAsync());
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to parse player bans: {e.Message}");
                await Task.Delay(Configuration.RetryDelay);
                continue;
            }

            if (playerBanResponse == null)
            {
                Console.WriteLine($"Failed to parse player bans.");
                await Task.Delay(Configuration.RetryDelay);
                continue;
            }

            foreach (PlayerBansModel playerBans in playerBanResponse.players)
            {
                if (!ulong.TryParse(playerBans.SteamId, out ulong steamId))
                {
                    Console.WriteLine($"Failed to parse Steam ID {playerBans.SteamId}.");
                    continue;
                }

                Cache.LastCached[steamId] = DateTime.UtcNow;
                Cache.PlayerBans[steamId] = playerBans;

                foreach (CacheRequest request in playerBatch.Where(x => x.Player.SteamID == steamId))
                {
                    request.VacLimiter.CheckBans(request.Player, playerBans);
                }
            }

            Cache.Save();
            await Task.Delay(Configuration.BatchDelay);
        }
    }

    public override Task OnConnected()
    {
        Console.WriteLine($"Setting up VAC limiter with age threshold of {this.ServerConfiguration.VACAgeThreshold} days which will {(this.ServerConfiguration.Kick ? "kick" : "")}{(this.ServerConfiguration.Kick && this.ServerConfiguration.Ban ? " and " : "")}{(this.ServerConfiguration.Ban ? "ban" : "")} players with VAC bans.");

        return Task.CompletedTask;
    }

    public override Task OnPlayerConnected(RunnerPlayer player)
    {
        if (Cache.LastCached.TryGetValue(player.SteamID, out DateTime lastCached) && lastCached.AddDays(this.ServerConfiguration.CacheAge) > DateTime.UtcNow && Cache.PlayerBans.TryGetValue(player.SteamID, out PlayerBansModel? playerBans))
        {
            this.CheckBans(player, playerBans);
        }
        else
        {
            if (playersToCheck.Any(p => p.Player.SteamID == player.SteamID))
            {
                return Task.CompletedTask;
            }

            playersToCheck.Add(new(this, player));
        }

        return Task.CompletedTask;
    }

    private void CheckBans(RunnerPlayer player, PlayerBansModel playerBans)
    {
        if (!this.Server.IsConnected || !this.IsLoaded)
        {
            Console.WriteLine($"Server is not connected or module is not loaded anymore. Skipping VAC ban check for player {player.Name} ({player.SteamID}).");
            return;
        }

        if (!playerBans.VACBanned || playerBans.NumberOfVACBans == 0)
        {
            return;
        }

        if (playerBans.DaysSinceLastBan >= this.ServerConfiguration.VACAgeThreshold)
        {
            return;
        }

        Console.WriteLine($"Player {player.Name} ({player.SteamID}) has a VAC ban from {playerBans.DaysSinceLastBan} days ago on record. {(this.ServerConfiguration.Kick ? "Kicking" : "")}{(this.ServerConfiguration.Kick && this.ServerConfiguration.Ban ? " and " : "")}{(this.ServerConfiguration.Ban ? "banning" : "")} player.");

        if (this.ServerConfiguration.Kick)
        {
            this.Server.Kick(player, string.Format(this.ServerConfiguration.KickMessage, playerBans.DaysSinceLastBan, this.ServerConfiguration.VACAgeThreshold));
        }

        if (this.ServerConfiguration.Ban)
        {
            this.Server.ExecuteCommand($"ban {player.SteamID}");
        }
    }
}

public class VacLimiterConfiguration : ModuleConfiguration
{
    public string SteamAPIKey { get; set; } = string.Empty;
    public int RetryDelay { get; set; } = 5000;
    public int BatchDelay { get; set; } = 5000;
}

public class VacLimiterServerConfiguration : ModuleConfiguration
{
    public int VACAgeThreshold { get; set; } = 365;
    public bool Kick { get; set; } = true;
    public bool Ban { get; set; } = false;
    public int CacheAge { get; set; } = 7;
    public string KickMessage { get; set; } = "You have a VAC ban from {0} days ago on record. You are not allowed to play on this server with VAC bans less than {1} days old.";
}

public class VacLimiterCache : ModuleConfiguration
{
    public Dictionary<ulong, DateTime> LastCached { get; set; } = new();
    public Dictionary<ulong, PlayerBansModel> PlayerBans { get; set; } = new();
}

record CacheRequest(VacLimiter VacLimiter, RunnerPlayer Player);

public record PlayerBansModel(string SteamId, bool CommunityBanned, bool VACBanned, uint NumberOfVACBans, uint DaysSinceLastBan, uint NumberOfGameBans, string EconomyBan);
public record PlayerBanResponseModel(PlayerBansModel[] players);
