using BattleBitAPI.Common;
using BBRAPIModules;
using Bluscream;
using System.Linq;
using System.Threading.Tasks;

namespace Bluscream {
    [RequireModule(typeof(Bluscream.BluscreamLib))]
    [RequireModule(typeof(BattleBitBaseModules.RichText))]
    [Module("Configure the loading screen text of your server", "1.0.0")]
    public class BluscreamTest : BattleBitModule {
        [ModuleReference]
        public ConfigurationInstance Configuration { get; set; } = null!;

        public override Task OnConnected() {
            this.Server.SetRulesScreenText("RulesScreenText");
            this.Server.SetNewPassword("vde");
            return Task.CompletedTask;
        }

        public override Task OnPlayerJoiningToServer(ulong steamID, PlayerJoiningArguments args) {
            return Task.CompletedTask;
        }
        public class ConfigurationInstance : ModuleConfiguration {
        }
    }

}
