using BBRAPIModules;
using Commands;
using Humanizer;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluscream {

    [RequireModule(typeof(Bluscream.BluscreamLib))]
    [RequireModule(typeof(Bluscream.SteamApi))]
    [RequireModule(typeof(Commands.CommandHandler))]
    [RequireModule(typeof(PlayerFinder.PlayerFinder))]
    [Module("Example usage of the SteamApi module", "2.0.0")]
    public class SteamApiExample : BattleBitModule {

        public static ModuleInfo ModuleInfo = new() {
            Name = "SteamApiExample",
            Description = "Example usage of the SteamApi module",
            Version = new Version(2, 0, 0),
            Author = "Bluscream",
            WebsiteUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/"),
            UpdateUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/raw/master/modules/SteamApiExample.cs"),
            SupportUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/issues/new?title=SteamApiExample")
        };

        #region References

        [ModuleReference]
        public CommandHandler CommandHandler { get; set; } = null!;

        [ModuleReference]
        public Bluscream.SteamApi SteamApi { get; set; } = null!;

        [ModuleReference]
        public PlayerFinder.PlayerFinder PlayerFinder { get; set; } = null!;

        #endregion References

        #region Methods

        // private static void Log(object _msg, string source = "SteamApiExample") => BluscreamLib.Log(_msg, source);

        #endregion Methods

        #region Events

        public override void OnModulesLoaded() {
            if (SteamApi is null) {
                this.Logger.Error($"SteamApi could not be found! Is it installed?");
            } else {
                this.CommandHandler.Register(this);
                SteamApi.OnDataReceived += SteamApi_OnDataReceived;
            }
        }

        private void SteamApi_OnDataReceived(SteamWebApi.Response steamData) {
            var player = steamData.GetPlayers(this.Server).FirstOrDefault();
            this.Logger.Debug($"Recieved Steam Data for {player?.fullstr() ?? steamData.Summary?.DisplayName}: {steamData.ToJson(false)}");
        }

        #endregion Events

        #region Commands

        [CommandCallback("steam bans", Description = "Lists steam bans of a player", ConsoleCommand = true, Permissions = new[] { "command.steambans" })]
        public async Task<string> GetPlayerSteamBans(Context ctx, string? _player = null) {
            var player = _player?.ParsePlayer(this.PlayerFinder, this.Server) ?? new ParsedPlayer(player: (ctx.Source as ChatSource)?.Invoker, server: this.Server);
            if (!player.SteamId64.HasValue) {
                return $"Could not find player {_player.Quote()}";
            }
            var steamData = await SteamApi.Get(player.SteamId64.Value);
            var response = new StringBuilder();
            if (!string.IsNullOrEmpty(player.Name)) response.AppendLine($"Name: {player.Name.Quote()} ({player.Name.Length} chars)");
            if (!string.IsNullOrEmpty(player.SteamId64.ToString())) {
                response.AppendLine($"VAC Banned: {steamData?.Bans?.VacBanned?.ToYesNo()} ({steamData?.Bans?.NumberOfVacBans} times)");
                if (steamData?.Bans?.VacBanned == true) response.AppendLine($"Last VAC Ban: {steamData?.Bans.DaysSinceLastBan} days ago");
                response.AppendLine($"Community Banned: {steamData?.Bans?.CommunityBanned?.ToYesNo()}");
                response.AppendLine($"Trade Banned: {(steamData?.Bans?.EconomyBan != "none").ToYesNo()}");
                response.AppendLine($"Game Banned: {(steamData?.Bans?.NumberOfGameBans > 0).ToYesNo()} ({steamData?.Bans?.NumberOfGameBans} times)");
            }
            return response.ToString();
        }

        [CommandCallback("steam player", Description = "Lists steam summary of a player", ConsoleCommand = true, Permissions = new[] { "command.steamplayer" })]
        public async Task<string> GetPlayerSteamSummary(Context ctx, string? _player = null) {
            var player = _player?.ParsePlayer(this.PlayerFinder, this.Server) ?? new ParsedPlayer(player: (ctx.Source as ChatSource)?.Invoker, server: this.Server);
            if (!player.SteamId64.HasValue) {
                return $"Could not find player {_player.Quote()}";
            }
            var steamData = await SteamApi.Get(player.SteamId64.Value);
            var response = new StringBuilder();
            if (!string.IsNullOrEmpty(player.Name)) response.AppendLine($"BattleBit Name: {player.Name.Quote()} ({player.Name.Length} chars)");
            if (!string.IsNullOrEmpty(player.SteamId64.ToString())) {
                if (!string.IsNullOrEmpty(steamData?.Summary?.RealName)) response.AppendLine($"Real Name: {steamData?.Summary?.RealName?.Quote()} ({steamData?.Summary.RealName?.Length} chars)");
                if (!string.IsNullOrEmpty(steamData?.Summary?.PersonaName)) response.AppendLine($"Persona Name: {steamData?.Summary?.PersonaName?.Quote()} ({steamData?.Summary.PersonaName?.Length} chars)");
                if (!string.IsNullOrEmpty(steamData?.Summary?.CountryCode)) response.AppendLine($"Country Code: {steamData?.Summary?.CountryCode}");
                if (steamData?.Summary?.TimeCreated is not null) response.AppendLine($"Created: {steamData?.Summary.TimeCreated} ({steamData?.Summary.TimeCreated.Humanize()})");
                if (steamData?.Summary?.PrimaryClanId is not null) response.AppendLine($"Primary Clan ID: {steamData?.Summary?.PrimaryClanId}");
                if (!string.IsNullOrEmpty(steamData?.Summary?.AvatarHash)) response.AppendLine($"Avatar Hash: {steamData?.Summary?.AvatarHash}");
            }
            return response.ToString();
        }

        #endregion Commands
    }
}