using BattleBitAPI.Common;
using BattleBitAPI.Features;
using BBRAPIModules;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BBRModules {
    [Module("A module made to handle team limits and team swapping.", "1.0.0")]
    [RequireModule(typeof(PlaceholderLib))]
    public class TeamLimit : BattleBitModule {
        public static TeamLimitConfiguration Configuration { get; set; }
        public PlaceholderLib PlaceholderLib = null!;

        public override async Task<bool> OnPlayerRequestingToChangeTeam(RunnerPlayer player, Team requestedTeam) {
            if (Configuration.DenyAllTeamSwapping) {
                string message = new PlaceholderLib(Configuration.NoSwappingMessage)
                    .Run();
                player.SayToChat(message);
                return false;
            }

            int teamCount = GetTeamPlayerCount(requestedTeam);
            int playersOnline = GetPlayersOnline();
            int extraPlayers = GetExtraPlayerCount();

            if (teamCount >= ((int)(playersOnline / 2) + extraPlayers)) {
                string message = new PlaceholderLib(Configuration.TeamFullMessage, "maxPlayers", teamCount + extraPlayers)
                    .Run();

                player.SayToChat(message);
                return false;
            }

            return true;
        }

        private int GetExtraPlayerCount() {
            string mapSize = Server.MapSize.ToString().Substring(1);

            if (!Configuration.ForMapSizes.ContainsKey(mapSize) || Configuration.ForMapSizes[mapSize] == -1)
                return Configuration.ExtraPlayerCount;

            return Configuration.ForMapSizes[mapSize];
        }

        private int GetTeamPlayerCount(Team team) => team == Team.TeamA ? Server.AllTeamAPlayers.Count() : Server.AllTeamBPlayers.Count();
        private int GetPlayersOnline() => Server.AllPlayers.Count();
    }

    public class TeamLimitConfiguration : ModuleConfiguration {
        // Do not allow anyone to swap, whatsoever.
        public bool DenyAllTeamSwapping { get; set; } = false;
        // How many players over the limit (half the number of players online) would you allow? (e.g. player count is 32, limit is 2, so team limit is now 34)
        public int ExtraPlayerCount { get; set; } = 2;
        // The message to send when a player tries to swap to a team that is full (DenyAllTeamSwapping is false)
        public string TeamFullMessage { get; set; } = "{#ffaaaa}[SERVER]{/} You cannot swap teams as it is full! (Limit: {#ffaaaa}{maxPlayers}{/})";
        // The message to send when a player tries to change teams (DenyAllTeamSwapping is true)
        public string NoSwappingMessage { get; set; } = "{#ffaaaa}[SERVER]{/} Swapping teams is disabled on this server.";

        public Dictionary<string, int> ForMapSizes { get; set; } = new()
        {
            // Any number that isn't -1 will set the extra player count for that map size. Not add, set. 0 means they can only swap teams if it is unbalanced.
            {"127vs127", 4},
            {"64vs64", 4},
            {"32vs32", -1},
            {"16vs16", 0},
            {"8vs8", 0},
        };
    }
}
