using System;
using System.Threading.Tasks;
using BBRAPIModules;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;

namespace DiscordStatus;

public class DiscordStatus : BattleBitModule {
    public DiscordConfiguration Configuration { get; set; }
    private DiscordSocketClient discordClient;
    private bool discordReady = false;

    public override void OnModulesLoaded() {
        if (string.IsNullOrEmpty(Configuration.DiscordBotToken)) {
            this.Unload();
            throw new Exception("API Key is not set. Please set it in the configuration file.");
        }
        Task.Run(() => connectDiscord());
    }

    public override void OnModuleUnloading() {
        Task.Run(() => disconnectDiscord());
    }

    public override Task OnConnected() {
        Task.Run(() => updateDiscordStatus(getStatus()));
        return Task.CompletedTask;
    }

    public override Task OnPlayerConnected(RunnerPlayer player) {
        Task.Run(() => updateDiscordStatus(getStatus()));
        return Task.CompletedTask;
    }

    public override Task OnPlayerDisconnected(RunnerPlayer player) {
        Task.Run(() => updateDiscordStatus(getStatus()));
        return Task.CompletedTask;
    }

    public override Task OnRoundStarted() {
        Task.Run(() => updateDiscordStatus(getStatus()));
        return Task.CompletedTask;
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