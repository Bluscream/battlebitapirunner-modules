using BattleBitAPI.Common;
using BBRAPIModules;
using System;
using System.Threading.Tasks;

namespace Bluscream;

[RequireModule(typeof(BluscreamLib))]
[Module("ConsoleLogger", "2.0.0")]
public class ConsoleLogger : BattleBitModule {

    public override void OnModulesLoaded() {
        Console.WriteLine("[!] Modules have been loaded");
    }

    public override async Task OnConnected() {
        await Console.Out.WriteLineAsync("[!] Server connected to API");
    }

    public override async Task OnDisconnected() {
        await Console.Out.WriteLineAsync("[!] Server disconnected from API");
    }

    public override Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg) {
        Console.WriteLine($"[CHAT] {player.str()}: \"{msg}\"");
        return Task.FromResult(true);
    }

    public override Task OnPlayerConnected(RunnerPlayer player) {
        Console.WriteLine($"[+] {player.fullstr()} connected from {player.IP}");
        return Task.CompletedTask;
    }

    public override Task OnPlayerDisconnected(RunnerPlayer player) {
        Console.WriteLine($"[-] {player.fullstr()} disconnected");
        return Task.CompletedTask;
    }

    public override Task OnPlayerReported(RunnerPlayer from, RunnerPlayer to, ReportReason reason, string additional) {
        Console.WriteLine($"[Report] {from.fullstr()} reported {to.fullstr()} for {reason}: ({additional})");
        return Task.CompletedTask;
    }
}