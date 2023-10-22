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

    public string FormatText(string input) {
        input = input
            .Replace("{servername}", this.Server.ServerName)
            .Replace("{maptime}", this.Server.DayNight.ToString().ToTitleCase())
            .Replace("{players}", this.Server.AllPlayers.Count().ToString())
            .Replace("{slots}", this.Server.MaxPlayerCount.ToString());
        var map = this.Server.GetCurrentMap();
        if (map is not null) {
            input = input
            .Replace("{mapname}", map.DisplayName)
            .Replace("{mapdescription}", string.Join("<br>", SplitByLength(map.Description, 150)));
        }
        var gameMode = this.Server.GetCurrentGameMode();
        if (gameMode is not null) {
            input = input
            .Replace("{gamemode}", gameMode.DisplayName)
            .Replace("{gamemodedescription}", string.Join("<br>", SplitByLength(gameMode.Description, 150)));
        }
        return input;
    }

    public void UpdateLoadingScreenText(string[] template) {
        var newText = FormatText(string.Join("<br>", template));
        foreach (var replacement in Config.randomReplacements) {
            newText = newText.Replace($"{{random.{replacement.Key}}}", FormatText(replacement.Value[Random.Shared.Next(replacement.Value.Length)]));
        }
        this.Server.LoadingScreenText = newText;
        this.Logger.Warn($"Changed Loading Screen Text:\n{this.Server.LoadingScreenText}");
    }

    // method to split a long string after 200 chars but only at a space
    public static IEnumerable<string> SplitByLength(string str, int maxLength) {
        if (string.IsNullOrEmpty(str)) {
            yield break;
        }
        var words = str.Split(' ');
        var currentLine = string.Empty;
        foreach (var currentWord in words) {
            if ((currentLine + currentWord).Length > maxLength) {
                yield return currentLine.Trim();
                currentLine = string.Empty;
            }
            currentLine += currentWord + " ";
        }
        if (currentLine.Length > 0) {
            yield return currentLine.Trim();
        }
    }

    /*2023-10-22 07:15:27,828 [RunnerServer of 127.0.0.1:30030] ERROR - Method OnPlayerJoiningToServer on module LoadingScreenText threw an exception
System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation.
---> System.NullReferenceException: Object reference not set to an instance of an object.
at BattleBitBaseModules.LoadingScreenText.SplitByLength(String str, Int32 maxLength)+MoveNext() in ./modules\LoadingScreenText.cs:line 67
at System.String.Join(String separator, IEnumerable`1 values)
at BattleBitBaseModules.LoadingScreenText.FormatText(String input) in ./modules\LoadingScreenText.cs:line 46
at BattleBitBaseModules.LoadingScreenText.UpdateLoadingScreenText(String[] template) in ./modules\LoadingScreenText.cs:line 57
at BattleBitBaseModules.LoadingScreenText.OnPlayerJoiningToServer(UInt64 steamID, PlayerJoiningArguments args) in ./modules\LoadingScreenText.cs:line 24
--- End of inner exception stack trace ---
at System.RuntimeMethodHandle.InvokeMethod(Object target, Span`1& arguments, Signature sig, Boolean constructor, Boolean wrapExceptions)
at System.Reflection.RuntimeMethodInfo.Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
at BBRAPIModules.RunnerServer.invokeOnModules(String method, Object[] parameters)
    */

    public class LoadingScreenTextConfiguration : ModuleConfiguration {

        public Dictionary<string, string[]> randomReplacements = new Dictionary<string, string[]>() {
            { "welcome", new[] { "Enjoy your stay!", "Have a good one!", "Get Ready for battle!" } },
        };

        public string[] ConnectLoadingScreenText { get; set; } = new[] {
            $"{Colors.SkyBlue}{{servername}}{Colors.None} ({Colors.SkyBlue}{{players}}{Colors.None}/{Colors.SkyBlue}{{slots}}{Colors.None} players)",
            "<br>",
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
            $"{{gamemode}}{Colors.Black}: {Colors.None}{{gamemodedescription}}",
            "<br>",
            $"{{mapname}} {Colors.Black}({Colors.None}{{maptime}}{Colors.Black}): {Colors.None}{{mapdescription}}"
        };

        public string[] MapChangeLoadingScreenText { get; set; } = new[] {
            $"{Colors.SkyBlue}Welcome to {Colors.None}{{servername}}{Colors.SkyBlue}!",
            "<br>",
            "{random.welcome}"
        };
    }
}