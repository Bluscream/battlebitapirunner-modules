using System;
using System.Text;

using BBRAPIModules;

using Commands;

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
        private static void Log(object _msg, string source = "SteamApiExample") => BluscreamLib.Log(_msg, source);
        #endregion

        #region Events
        public override void OnModulesLoaded() {
            if (SteamApi is null) {
                Log($"SteamApi could not be found! Is it installed?");
            } else {
                this.CommandHandler.Register(this);
                SteamApi.OnPlayerDataReceived += SteamApi_OnDataReceived;
            }
        }

        private void SteamApi_OnDataReceived(RunnerPlayer player, SteamWebApi.Response steamData) {
            Log($"\"{player.Name}\" has been banned {SteamApi.GetBanCount(player).Result} times on steam.");
        }
        #endregion

        #region Commands
        [CommandCallback("playerbans", Description = "Lists steam bans of a player", ConsoleCommand = true, Permissions = new[] { "command.playerbans" })]
        public async void GetPlayerBans(RunnerPlayer commandSource, RunnerPlayer? _player = null) {
            _player = _player ?? commandSource;
            var response = new StringBuilder();
            if (!string.IsNullOrEmpty(_player.Name)) response.AppendLine($"Name: {_player.str()} ({_player.Name.Length} chars)");
            if (!string.IsNullOrEmpty(_player.SteamID.ToString())) {
                var bans = (await SteamApi?.GetData(_player)).Bans;
                if (bans is null) {
                    commandSource.Message("Steam bans request failed, check connection and config!");
                    return;
                }
                response.AppendLine($"VAC Banned: {bans.VacBanned.ToYesNo()} ({bans.NumberOfVacBans} times)");
                if (bans.VacBanned) response.AppendLine($"Last VAC Ban: {bans.DaysSinceLastBan} days ago");
                response.AppendLine($"Community Banned: {bans.CommunityBanned.ToYesNo()}");
                response.AppendLine($"Trade Banned: {(bans.EconomyBan != "none").ToYesNo()}");
                response.AppendLine($"Game Banned: {(bans.NumberOfGameBans > 0).ToYesNo()} ({bans.NumberOfGameBans} times)");
            }
            commandSource.Message(response.ToString());
        }
        #endregion
    }
}