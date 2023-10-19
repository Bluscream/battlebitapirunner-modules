using BBRAPIModules;
using SteamWebApi;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bluscream;

[RequireModule(typeof(Bluscream.SteamApi))]
[Module("Kick users with VAC bans", "1.1.0")]
public class VacLimiter : BattleBitModule {
    public VacLimiterServerConfiguration ServerConfiguration { get; set; } = null!;

    [ModuleReference]
    public dynamic? GranularPermissions { get; set; }

    [ModuleReference]
    public Bluscream.SteamApi SteamApi { get; set; } = null!;

    public override void OnModulesLoaded() {
        SteamApi.OnPlayerDataReceived += SteamApi_OnPlayerDataReceived;
    }

    public override Task OnConnected() {
        this.Logger.Info($"Setting up VAC limiter with age threshold of {this.ServerConfiguration.VACAgeThreshold} days which will {(this.ServerConfiguration.Kick ? "kick" : "")}{(this.ServerConfiguration.Kick && this.ServerConfiguration.Ban ? " and " : "")}{(this.ServerConfiguration.Ban ? "ban" : "")} players with VAC bans.");

        return Task.CompletedTask;
    }

    private void SteamApi_OnPlayerDataReceived(RunnerPlayer player, Response steamData) {
        CheckBans(player, steamData);
    }

    private void CheckBans(RunnerPlayer player, Response steamData) {
        if (!this.Server.IsConnected || !this.IsLoaded) {
            this.Logger?.Info($"Server is not connected or module is not loaded anymore. Skipping VAC ban check for player {player.Name} ({player.SteamID}).");
            return;
        }
        if (steamData is null || steamData.Bans is null) {
            this.Logger?.Info($"Steam Data not available! Skipping VAC ban check for player {player.Name} ({player.SteamID}).");
            return;
        }

        if (steamData.Bans.VacBanned == false && steamData.Bans.NumberOfVacBans == 0) {
            this.Logger.Info($"Player {player.Name} ({player.SteamID}) has no VAC bans on record.");
            return;
        }

        if (this.GranularPermissions is not null && ServerConfiguration.IgnoredPermissions?.Any(p => this.GranularPermissions.HasPermission(player.SteamID, p)) == true) {
            this.Logger.Info($"Player {player.Name} ({player.SteamID}) has an ignored permission, skipping...");
            return;
        }

        if (steamData.Bans.DaysSinceLastBan >= this.ServerConfiguration.VACAgeThreshold) {
            this.Logger.Info($"Player {player.Name} ({player.SteamID}) has a VAC ban from {steamData.Bans.DaysSinceLastBan} days ago on record, but it is older than the threshold of {this.ServerConfiguration.VACAgeThreshold} days.");
            return;
        }

        this.Logger.Info($"Player {player.Name} ({player.SteamID}) has a VAC ban from {steamData.Bans.DaysSinceLastBan} days ago on record. {(this.ServerConfiguration.Kick ? "Kicking" : "")}{(this.ServerConfiguration.Kick && this.ServerConfiguration.Ban ? " and " : "")}{(this.ServerConfiguration.Ban ? "banning" : "")} player.");

        if (this.ServerConfiguration.Kick) {
            this.Server.Kick(player, string.Format(this.ServerConfiguration.KickMessage, steamData.Bans.DaysSinceLastBan, this.ServerConfiguration.VACAgeThreshold));
        }

        if (this.ServerConfiguration.Ban) {
            this.Server.ExecuteCommand($"ban {player.SteamID}");
            player.Kick(string.Format(this.ServerConfiguration.KickMessage, steamData.Bans.DaysSinceLastBan, this.ServerConfiguration.VACAgeThreshold));
        }
    }
}

public class VacLimiterServerConfiguration : ModuleConfiguration {
    public int VACAgeThreshold { get; set; } = 365;
    public bool Kick { get; set; } = true;
    public bool Ban { get; set; } = false;
    public int CacheAge { get; set; } = 7;
    public string KickMessage { get; set; } = "You have a VAC ban from {0} days ago on record. You are not allowed to play on this server with VAC bans less than {1} days old.";
    public string[] IgnoredPermissions { get; set; } = Array.Empty<string>();
}