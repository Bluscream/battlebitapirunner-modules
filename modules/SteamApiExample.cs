using System;
using System.Threading.Tasks;
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
        [ModuleReference]
#if DEBUG
        public Permissions.PlayerPermissions? PlayerPermissions { get; set; }
#else
        public dynamic? PlayerPermissions { get; set; }
#endif
        #endregion

        public CommandsConfiguration SteamApiExampleCommandsConfiguration { get; set; }

        #region Commands
        [CommandCallback("playerbans", Description = "Lists steam bans of a player")]
        public async void GetPlayerBans(RunnerPlayer commandSource, RunnerPlayer? _player = null) {
            var cmdName = $"\"{CommandHandler.CommandConfiguration.CommandPrefix}playerbans\""; var cmdConfig = SteamApiExampleCommandsConfiguration.playerbans;
            if (!cmdConfig.Enabled) { commandSource.Message($"Command {cmdName} is not enabled on this server!"); return; }
            if (PlayerPermissions is not null && !Extensions.HasAnyRoleOf(commandSource, PlayerPermissions, Extensions.ParseRoles(cmdConfig.AllowedRoles))) { commandSource.Message($"You do not have permissions to run {cmdName} on this server!"); return; }
            _player = _player ?? commandSource;
            var response = new StringBuilder();
            if (!string.IsNullOrEmpty(_player.Name)) response.AppendLine($"Name: {_player.str()} ({_player.Name.Length} chars)");
            if (!string.IsNullOrEmpty(_player.SteamID.ToString())) {
                var bans = (await SteamApi?.GetData(_player) ).Bans;
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

        #region Events
        public override void OnModulesLoaded() {
            if (SteamApi is null) {
                BluscreamLib.Log($"SteamApi could not be found! Is it installed?");
            } else {
                this.CommandHandler.Register(this);
            }
        }
        public override Task OnPlayerConnected(RunnerPlayer player) {
            Task.Delay(TimeSpan.FromSeconds(1)).Wait();
            BluscreamLib.Log($"\"{player.Name}\" has been banned {SteamApi.GetBanCount(player).Result} times on steam.");
            return Task.CompletedTask;
        }
        public override Task OnPlayerDisconnected(RunnerPlayer player) {
            BluscreamLib.Log($"\"{player.Name}\" was banned {SteamApi.GetBanCount(player).Result} times on steam.");
            return Task.CompletedTask;
        }
        #endregion

        #region Configuration
        public class CommandsConfiguration : ModuleConfiguration {
            public CommandConfiguration playerbans { get; set; } = new CommandConfiguration() { AllowedRoles = Extensions.ToRoleStringList(MoreRoles.Staff) };
        }
        #endregion
    }
}