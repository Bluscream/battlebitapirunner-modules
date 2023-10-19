using BattleBitAPI.Common;
using BBRAPIModules;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Modules {

    [Module("Advertise your discord, patreon, etc at the end of a round.", "1.0.0")]
    public class RoundEndAdvertise : BattleBitModule {
        public REAConfig Configuration { get; set; } = null!;
        private int index = 0;

        public override async Task OnGameStateChanged(GameState oldState, GameState newState) {
            if (newState != GameState.EndingGame)
                return;

            string message = GetNextMessage();

            switch (Configuration.MessageType) {
                case "AnnounceLong":
                    Server.AnnounceLong(message);
                    break;

                case "AnnounceShort":
                    Server.AnnounceShort(message);
                    break;

                case "Chat":
                    Server.SayToAllChat(message);
                    break;

                case "Timed":
                    int index = 0;
                    List<RunnerPlayer> players = new(Server.AllPlayers);

                    do {
                        RunnerPlayer player = players.ElementAt(index);
                        player.Message(message, Configuration.TimedMessageDuration);
                        index++;
                    } while (index < players.Count());
                    break;
            }
        }

        private string GetNextMessage() {
            if (!Configuration.UseDifferentMessages)
                return Configuration.Message;

            int oldIndex = index;
            index++;

            if (Configuration.Messages.Count == index)
                index = 0;

            return Configuration.Messages[oldIndex];
        }
    }

    public class REAConfig : ModuleConfiguration {
        public string MessageType { get; set; } = "AnnounceLong";
        public string Message { get; set; } = "Join our discord at discord.website.com!";

        public bool UseDifferentMessages { get; set; } = false;
        public float TimedMessageDuration { get; set; } = 5.0f;

        public List<string> Messages { get; set; } = new()
        {
            "Join our discord at discord.website.com!"
        };
    }
}