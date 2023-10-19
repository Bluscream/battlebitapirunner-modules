using BattleBitAPI.Common;
using BBRAPIModules;
using System;
using System.Threading.Tasks;

namespace BattleBitBaseModules;

[Module("Crazy? I was crazy once", "1.0.0")]
public class BattleBitCrazy : BattleBitModule {
    public CrazyModuleConfiguration Configuration { get; set; }

    public override void OnModulesLoaded() {
        if (string.IsNullOrEmpty(Configuration.Text)) {
            Unload();
            throw new Exception("Text is not set. Please set it in the configuration file.");
        }

        if (Configuration.Chance < 0 || Configuration.Chance > 100) {
            Unload();
            throw new Exception("Chance must be between 0 and 100.");
        }
    }

    public override Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg) {
        if (!msg.Contains("crazy", StringComparison.OrdinalIgnoreCase) || Random.Shared.Next(0, 100) >= Configuration.Chance) {
            return Task.FromResult(true);
        }

        Task.Run(async () => {
            await Task.Delay(420);
            Server.SayToAllChat(Configuration.Text);
            Console.WriteLine($"{player.Name} ({player.SteamID}) was crazy once.");
        });

        return Task.FromResult(true);
    }
}

public class CrazyModuleConfiguration : ModuleConfiguration {
    public string Text { get; set; } = "Crazy? I was crazy once!";
    public int Chance { get; set; } = 5;
}