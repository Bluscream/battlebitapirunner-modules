using BattleBitAPI.Common;
using BBRAPIModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bluscream;

namespace BattleBitBaseModules;

[RequireModule(typeof(Bluscream.BluscreamLib))]
[RequireModule(typeof(BattleBitBaseModules.RichText))]
[Module("Configure the loading screen text of your server", "1.0.0")]
public class LoadingScreenText : BattleBitModule {

    [ModuleReference]
    public LoadingScreenTextConfiguration Config { get; set; } = null!;

    public override Task OnConnected() {
        UpdateLoadingScreenText(this.Config.ConnectLoadingScreenText); return Task.CompletedTask;
    }

    public override Task OnPlayerJoiningToServer(ulong steamID, PlayerJoiningArguments args) {
        UpdateLoadingScreenText(this.Config.ConnectLoadingScreenText); return Task.CompletedTask;
    }

    public override Task OnRoundEnded() {
        UpdateLoadingScreenText(this.Config.MapChangeLoadingScreenText); return Task.CompletedTask;
    }

    //public override Task OnSessionChanged(long oldSessionID, long newSessionID) {
    //    UpdateLoadingScreenText(this.Config.MapChangeLoadingScreenText); return Task.CompletedTask;
    //}
    //public override Task OnGameStateChanged(GameState oldState, GameState newState) {
    //    switch (newState) {
    //        case GameState.EndingGame:
    //            UpdateLoadingScreenText(this.Config.MapChangeLoadingScreenText);
    //            break;
    //        default:
    //            UpdateLoadingScreenText(this.Config.ConnectLoadingScreenText);
    //            break;
    //    }
    //    return Task.CompletedTask;
    //}

    public string FormatText(string input) => input
            .Replace("{servername}", this.Server.ServerName)
            .Replace("{gamemode}", this.Server.GetCurrentGameMode().DisplayName)
            .Replace("{mapname}", this.Server.GetCurrentMap().DisplayName)
            .Replace("{mapdescription}", this.Server.GetCurrentMap().Description)
            .Replace("{gamemodedescription}", this.Server.GetCurrentGameMode().Description)
            .Replace("{maptime}", this.Server.DayNight.ToString().ToTitleCase())
            .Replace("{players}", this.Server.AllPlayers.Count().ToString())
            .Replace("{slots}", this.Server.MaxPlayerCount.ToString());

    public void UpdateLoadingScreenText(string[] template) {
        var newText = FormatText(string.Join("<br>", template));
        foreach (var replacement in Config.randomReplacements) {
            newText = newText.Replace($"{{random.{replacement.Key}}}", FormatText(replacement.Value[Random.Shared.Next(replacement.Value.Length)]));
        }
        this.Server.LoadingScreenText = newText;
        this.Logger.Warn($"Changed Loading Screen Text:\n{this.Server.LoadingScreenText}");
    }

    public class LoadingScreenTextConfiguration : ModuleConfiguration {

        public Dictionary<string, string[]> randomReplacements = new Dictionary<string, string[]>() {
            { "welcome", new[] { "Enjoy your stay!", "Have a good one!", "Get Ready for battle!" } },
        };

        public string[] ConnectLoadingScreenText { get; set; } = new[] {
            $"{Colors.SkyBlue}{{servername}}{Colors.None} ({Colors.SkyBlue}{{players}}{Colors.None}/{Colors.SkyBlue}{{slots}}{Colors.None} players)",
            "{random.welcome}",
            "<br>",
            "<br>",
            "<br>",
            "<br>",
            "<br>",
            "<br>",
            "<br>",
            "<br>",
            "<br>",
            "<br>",
            "<br>",
            $"{Colors.Gray}{{gamemode}}: {Colors.None}{{gamemodedescription}}",
            $"{Colors.Gray}{{mapname}} ({{maptime}}): {Colors.None}{{mapdescription}}",
            "<br>"
        };

        public string[] MapChangeLoadingScreenText { get; set; } = new[] {
            $"{Colors.SkyBlue}Welcome to {Colors.None}{{servername}}{Colors.SkyBlue}!",
            "<br>",
            "{{random.welcome}}"
        };
    }
}