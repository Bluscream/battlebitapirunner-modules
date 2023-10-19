using BBRAPIModules;
using Commands;
using Humanizer;
using System;
using System.Text;

namespace Bluscream {
    [RequireModule(typeof(Bluscream.BluscreamLib))]
    [RequireModule(typeof(Bluscream.SteamApi))]
    [RequireModule(typeof(Commands.CommandHandler))]
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
        #endregion

        #region Methods
        // private static void Log(object _msg, string source = "SteamApiExample") => BluscreamLib.Log(_msg, source);
        #endregion

        #region Events
        public override void OnModulesLoaded() {
            if (SteamApi is null) {
                this.Logger.Error($"SteamApi could not be found! Is it installed?");
            } else {
                this.CommandHandler.Register(this);
                SteamApi.OnPlayerDataReceived += SteamApi_OnDataReceived;
            }
        }

        private void SteamApi_OnDataReceived(RunnerPlayer player, SteamWebApi.Response steamData) {
            this.Logger.Debug($"Recieved Steam Data for {player.fullstr()}: {steamData.ToJson(false)}");
        }
        #endregion

        #region Commands
        [CommandCallback("steam bans", Description = "Lists steam bans of a player", ConsoleCommand = true, Permissions = new[] { "command.steambans" })]
        public async void GetPlayerSteamBans(RunnerPlayer commandSource, RunnerPlayer? _player = null) {
            _player = _player ?? commandSource;
            var response = new StringBuilder();
            if (!string.IsNullOrEmpty(_player.Name)) response.AppendLine($"Name: {_player.str()} ({_player.Name.Length} chars)");
            if (!string.IsNullOrEmpty(_player.SteamID.ToString())) {
                var steam = await SteamApi?.GetData(_player);
                if (steam.Bans is null) {
                    commandSource.Message("Steam bans request failed, check connection and config!");
                    return;
                }
                response.AppendLine($"VAC Banned: {steam.Bans.VacBanned?.ToYesNo()} ({steam.Bans.NumberOfVacBans} times)");
                if (steam.Bans.VacBanned == true) response.AppendLine($"Last VAC Ban: {steam.Bans.DaysSinceLastBan} days ago");
                response.AppendLine($"Community Banned: {steam.Bans.CommunityBanned?.ToYesNo()}");
                response.AppendLine($"Trade Banned: {(steam.Bans.EconomyBan != "none").ToYesNo()}");
                response.AppendLine($"Game Banned: {(steam.Bans.NumberOfGameBans > 0).ToYesNo()} ({steam.Bans.NumberOfGameBans} times)");
            }
            commandSource.Message(response.ToString());
        }

        [CommandCallback("steam player", Description = "Lists steam summary of a player", ConsoleCommand = true, Permissions = new[] { "command.steamplayer" })]
        public async void GetPlayerSteamSummary(RunnerPlayer commandSource, RunnerPlayer? _player = null) {
            _player = _player ?? commandSource;
            var response = new StringBuilder();
            if (!string.IsNullOrEmpty(_player.Name)) response.AppendLine($"BattleBit Name: {_player.str()} ({_player.Name.Length} chars)");
            if (!string.IsNullOrEmpty(_player.SteamID.ToString())) {
                var steam = await SteamApi?.GetData(_player);
                response.AppendLine($"Steam ID 64: {_player.SteamID}\n");
                if (steam.Summary is null) {
                    commandSource.Message("Steam summary request failed, check connection and config!");
                    return;
                }
                if (!string.IsNullOrEmpty(steam.Summary.RealName)) response.AppendLine($"Real Name: {steam.Summary.RealName?.Quote()} ({steam.Summary.RealName?.Length} chars)");
                if (!string.IsNullOrEmpty(steam.Summary.PersonaName)) response.AppendLine($"Persona Name: {steam.Summary.PersonaName?.Quote()} ({steam.Summary.PersonaName?.Length} chars)");
                if (!string.IsNullOrEmpty(steam.Summary.CountryCode)) response.AppendLine($"Country Code: {steam.Summary.CountryCode}");
                if (steam.Summary.TimeCreated is not null) response.AppendLine($"Created: {steam.Summary.TimeCreated} ({steam.Summary.TimeCreated.Humanize()})");
                if (steam.Summary.PrimaryClanId is not null) response.AppendLine($"Primary Clan ID: {steam.Summary.PrimaryClanId}");
                if (!string.IsNullOrEmpty(steam.Summary.AvatarHash)) response.AppendLine($"Avatar Hash: {steam.Summary.AvatarHash}");
            }
            commandSource.Message(response.ToString());
        }
        #endregion
    }
}