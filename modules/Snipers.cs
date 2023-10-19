using BattleBitAPI.Common;
using BattleBitAPI.Server;
using BBRAPIModules;
using System.Threading.Tasks;

//
// Author mocfunky
// Snipers Only
// Module for BattleBit Modular API
//

namespace mocfunky {
    [Module("Snipers", "1.1.0")]
    public class Snipers : BattleBitModule {
        //
        // V - This allows users to select Recon even while not in a squad. - V
        //
        public override Task OnPlayerConnected(RunnerPlayer player) {
            this.Server.ServerSettings.SquadRequiredToChangeRole = false;
            player.SetNewRole(GameRole.Recon);
            return Task.CompletedTask;
        }
        //
        // V - This denies player from changing roles to anything but Recon. - V
        //
        public override async Task<bool> OnPlayerRequestingToChangeRole(RunnerPlayer player, GameRole requestedRole) {
            return GameRole.Recon == requestedRole;
        }
        //
        // V - Switch the player's role to Recon when they leave or are kicked from a squad - V
        //
        public override Task OnPlayerLeftSquad(RunnerPlayer player, Squad<RunnerPlayer> squad) {
            player.SetNewRole(GameRole.Recon);
            return Task.CompletedTask;
        }
        //
        // V - Switch the player's role to Recon when they join or create a squad. V
        //
        public override Task OnPlayerJoinedSquad(RunnerPlayer player, Squad<RunnerPlayer> squad) {
            player.SetNewRole(GameRole.Recon);
            return Task.CompletedTask;
        }

        //
        // V - Gives ability to disable Day or Night maps (Night disabled by default, feel free to comment out or delete if you do not need these) - V
        //
        public override Task OnConnected() {
            this.Server.ServerSettings.CanVoteDay = true;
            this.Server.ServerSettings.CanVoteNight = false;
            return Task.CompletedTask;
        }
    }
}
