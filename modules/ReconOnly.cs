using BattleBitAPI.Common;
using BattleBitAPI.Server;
using BBRAPIModules;
using System.Threading.Tasks;

//
// Author: mocfunky
// Module: ReconOnly
// Module for BattleBit Modular API
//
namespace ReconOnly {
    [Module("This module forces players to become the Recon (Sniper) role.", "1.2")]
    public class ReconOnly : BattleBitModule {
        // Allow players to select Recon even without being in a squad.
        public override Task OnPlayerConnected(RunnerPlayer player) {
            player.SetNewRole(GameRole.Recon);
            return Task.CompletedTask;
        }

        // Deny players from changing roles to anything but Recon.
        public override async Task<bool> OnPlayerRequestingToChangeRole(RunnerPlayer player, GameRole requestedRole) {
            return GameRole.Recon == requestedRole;
        }

        // Switch the player's role to Recon when they leave or are kicked from a squad.
        public override Task OnPlayerLeftSquad(RunnerPlayer player, Squad<RunnerPlayer> squad) {
            player.SetNewRole(GameRole.Recon);
            return Task.CompletedTask;
        }

        // Switch the player's role to Recon when they join or create a squad.
        public override Task OnPlayerJoinedSquad(RunnerPlayer player, Squad<RunnerPlayer> squad) {
            player.SetNewRole(GameRole.Recon);
            return Task.CompletedTask;
        }

        // Disable spawning if not Recon.
        public override async Task<OnPlayerSpawnArguments?> OnPlayerSpawning(RunnerPlayer player, OnPlayerSpawnArguments request) {
            // Check if the player is in the Recon role
            if (player.Role != GameRole.Recon) {
                // Deny spawning for players who are not in the Recon role
                return null;
            }

            // Allow spawning for players in the Recon role
            return request;
        }


        // Customize server settings.
        public override Task OnConnected() {
            this.Server.ServerSettings.SquadRequiredToChangeRole = false;
            this.Server.ServerSettings.CanVoteDay = true;
            this.Server.ServerSettings.CanVoteNight = false;
            return Task.CompletedTask;
        }
        //
        // Sets tickets to 2000 on round start. (Since recons are slow at killing)
        //
        public async override Task OnRoundStarted() {
            this.Server.RoundSettings.MaxTickets = 2000;
            await base.OnRoundStarted();
        }
    }
}
