// VERSION 1.3
// MADE BY @SENTENNIAL
using BBRAPIModules;
using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace DiscordStatus;

[Module("Connects each server to a Discord Bot, and updates the Discord Bot's status with the server's player-count and map information.", "1.3")]
public class DiscordStatus : BattleBitModule {
    public DiscordConfiguration Configuration { get; set; }
    private DiscordSocketClient discordClient;
    private bool discordReady = false;

    public override Task OnConnected() {
        if (string.IsNullOrEmpty(Configuration.DiscordBotToken)) {
            Unload();
            throw new Exception("API Key is not set. Please set it in the configuration file.");
        }
        Task.Run(() => connectDiscord()).ContinueWith(t => Console.WriteLine($"Error during Discord connection {t.Exception}"), TaskContinuationOptions.OnlyOnFaulted);
        Task.Run(UpdateTimer).ContinueWith(t => Console.WriteLine($"Error during Discord Status update {t.Exception}"), TaskContinuationOptions.OnlyOnFaulted);
        return Task.CompletedTask;
    }

    private async void UpdateTimer() {
        while (this.IsLoaded && this.Server.IsConnected) {
            if (discordReady)
                await updateDiscordStatus(getStatus());
            await Task.Delay(10000);
        }
    }

    public override void OnModuleUnloading() {
        Task.Run(() => disconnectDiscord());
    }

    private async Task connectDiscord() {
        var config = new DiscordSocketConfig {
            GatewayIntents = GatewayIntents.AllUnprivileged
        };
        discordClient = new DiscordSocketClient(config);
        discordClient.Ready += ReadyAsync;
        await discordClient.LoginAsync(TokenType.Bot, Configuration.DiscordBotToken);
        await discordClient.StartAsync();
    }

    private string getStatus() {
        return "" + Server.CurrentPlayerCount + "/" + Server.MaxPlayerCount +
            "(" + Server.InQueuePlayerCount + ") on " + Server.Map + " " + Server.Gamemode;
    }

    private async Task disconnectDiscord() {
        discordReady = false;
        try {
            await discordClient.StopAsync();
        } catch (Exception) {
        }
    }

    private Task ReadyAsync() {
        discordReady = true;
        Task.Run(() => updateDiscordStatus(getStatus()));
        return Task.CompletedTask;
    }

    private async Task updateDiscordStatus(string status) {
        if (discordReady == false) {
            return;
        }
        try {
            await discordClient.SetGameAsync(status);
        } catch (Exception) {
        }
    }
}

public class DiscordConfiguration : ModuleConfiguration {
    public string DiscordBotToken { get; set; } = string.Empty;
}