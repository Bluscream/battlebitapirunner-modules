using BBRAPIModules;
using System.Threading.Tasks;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using System.Text;
using System;

namespace BattleBitRCON;

public class RCONConfiguration : ModuleConfiguration
{
    public string RCONIP { get; set; } = "+";
    public int RCONPort { get; set; }
    public string Password { get; set; }
}

public class BattleMetricsRCON : BattleBitModule
{
    public RCONConfiguration BattleMetricsRCONConfiguration { get; set; }

    private WebSocketServer<RunnerPlayer>? wss;

    private void initializeWebSocketServer()
    {
        if (string.IsNullOrEmpty(BattleMetricsRCONConfiguration.RCONIP))
        {
            BattleMetricsRCONConfiguration.RCONIP = "+";
        }

        if (BattleMetricsRCONConfiguration.RCONPort == 0)
        {
            BattleMetricsRCONConfiguration.RCONPort = Server.GamePort + 1;
        }

        if (string.IsNullOrEmpty(BattleMetricsRCONConfiguration.Password))
        {
            BattleMetricsRCONConfiguration.Password = CreatePassword(32);
        }

        BattleMetricsRCONConfiguration.Save();

        wss = new WebSocketServer<RunnerPlayer>(
            Server,
            BattleMetricsRCONConfiguration.RCONIP,
            BattleMetricsRCONConfiguration.RCONPort,
            BattleMetricsRCONConfiguration.Password
        );
    }

    public override void OnModuleUnloading()
    {
        wss?.Stop();
        wss?.Dispose();
    }

    public override Task OnConnected()
    {
        if (wss is null)
        {
            try
            {
                initializeWebSocketServer();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to start RCON server: " + ex.Message);
                Console.ResetColor();

                this.Unload();
                return Task.CompletedTask;
            }
        }

        Task.Run(async () =>
        {
            try
            {
                await wss.Start();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to start RCON server: " + ex.Message);
                Console.ResetColor();

                this.Unload();
            }
        });

        return Task.CompletedTask;
    }

    public override Task OnDisconnected()
    {
        wss.Stop();

        return Task.CompletedTask;
    }

    public override async Task OnPlayerConnected(RunnerPlayer player)
    {
        await wss.BroadcastMessage(new Messages.OnPlayerConnected<RunnerPlayer>(player));
    }

    public override async Task OnPlayerDisconnected(RunnerPlayer player)
    {
        await wss.BroadcastMessage(new Messages.OnPlayerDisconnected<RunnerPlayer>(player));
    }

    public override async Task<bool> OnPlayerTypedMessage(
        RunnerPlayer player,
        ChatChannel channel,
        string msg
    )
    {
        await wss.BroadcastMessage(
            new Messages.OnPlayerTypedMessage<RunnerPlayer>(player, channel, msg)
        );

        return true;
    }

    public override async Task OnPlayerChangedRole(RunnerPlayer player, GameRole role)
    {
        await wss.BroadcastMessage(
            new Messages.OnPlayerChangedRole<RunnerPlayer>(player, role)
        );
    }

    public override async Task OnPlayerJoinedSquad(RunnerPlayer player, Squad<RunnerPlayer> squad)
    {
        await wss.BroadcastMessage(
            new Messages.OnPlayerJoinedSquad<RunnerPlayer>(player, squad)
        );
    }

    public override async Task OnSquadLeaderChanged(
        Squad<RunnerPlayer> squad,
        RunnerPlayer newLeader
    )
    {
        await wss.BroadcastMessage(
            new Messages.OnSquadLeaderChanged<RunnerPlayer>(squad, newLeader)
        );
    }

    public override async Task OnPlayerLeftSquad(RunnerPlayer player, Squad<RunnerPlayer> squad)
    {
        await wss.BroadcastMessage(new Messages.OnPlayerLeftSquad<RunnerPlayer>(player, squad));
    }

    public override async Task OnPlayerChangeTeam(RunnerPlayer player, Team team)
    {
        await wss.BroadcastMessage(new Messages.OnPlayerChangeTeam<RunnerPlayer>(player, team));
    }

    public override async Task OnSquadPointsChanged(Squad<RunnerPlayer> squad, int newPoints)
    {
        await wss.BroadcastMessage(
            new Messages.OnSquadPointsChanged<RunnerPlayer>(squad, newPoints)
        );
    }

    public override async Task OnPlayerSpawned(RunnerPlayer player)
    {
        await wss.BroadcastMessage(new Messages.OnPlayerSpawned<RunnerPlayer>(player));
    }

    public override async Task OnPlayerDied(RunnerPlayer player)
    {
        await wss.BroadcastMessage(new Messages.OnPlayerDied<RunnerPlayer>(player));
    }

    public override async Task OnPlayerGivenUp(RunnerPlayer player)
    {
        await wss.BroadcastMessage(new Messages.OnPlayerGivenUp<RunnerPlayer>(player));
    }

    public override async Task OnAPlayerDownedAnotherPlayer(
        OnPlayerKillArguments<RunnerPlayer> args
    )
    {
        await wss.BroadcastMessage(
            new Messages.OnAPlayerDownedAnotherPlayer<RunnerPlayer>(args)
        );
    }

    public override async Task OnAPlayerRevivedAnotherPlayer(RunnerPlayer from, RunnerPlayer to)
    {
        await wss.BroadcastMessage(
            new Messages.OnAPlayerRevivedAnotherPlayer<RunnerPlayer>(from, to)
        );
    }

    public override async Task OnPlayerReported(
        RunnerPlayer from,
        RunnerPlayer to,
        ReportReason reason,
        string additional
    )
    {
        await wss.BroadcastMessage(
            new Messages.OnPlayerReported<RunnerPlayer>(from, to, reason, additional)
        );
    }

    public override async Task OnGameStateChanged(GameState oldState, GameState newState)
    {
        await wss.BroadcastMessage(new Messages.OnGameStateChanged(oldState, newState));
    }

    public override async Task OnRoundStarted()
    {
        await wss.BroadcastMessage(new Messages.OnRoundStarted());
    }

    public override async Task OnRoundEnded()
    {
        await wss.BroadcastMessage(new Messages.OnRoundEnded());
    }

    // Taken from https://stackoverflow.com/a/54997.
    // Not the most secure, but seems good enough for this use.
    private string CreatePassword(int length)
    {
        const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
        StringBuilder res = new StringBuilder();
        Random rnd = new Random();
        while (0 < length--)
        {
            res.Append(valid[rnd.Next(valid.Length)]);
        }
        return res.ToString();
    }
}
