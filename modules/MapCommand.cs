using BattleBitAPI.Common;
using BBRAPIModules;
using Commands;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BattleBitBaseModules;

[RequireModule(typeof(CommandHandler))]
public class MapCommand : BattleBitModule {

    [ModuleReference]
    public CommandHandler CommandHandler { get; set; }

    public override void OnModulesLoaded() {
        this.CommandHandler.Register(this);
    }

    [CommandCallback("map", Description = "Changes the map", AllowedRoles = Roles.Admin)]
    public void SetMap(RunnerPlayer commandSource, string mapName, string? gameMode = null) // , string? gameSize = null)
    {
        var map = ResolveNameMatch(mapName, Maps);
        if (string.IsNullOrWhiteSpace(map)) {
            commandSource.Message($"Map {mapName} could not be found"); return;
        }
        this.Server.MapRotation.SetRotation(mapName);
        if (gameMode != null) {
            var mode = ResolveNameMatch(gameMode, GameModes);
            if (string.IsNullOrWhiteSpace(map)) {
                commandSource.Message($"GameMode {mapName} could not be found"); return;
            }
            this.Server.GamemodeRotation.SetRotation(mode);
        }
        /*if (gameSize != null) {
            var size = GetSizeFromString(gameSize);
            if (size == MapSize.None) {
                commandSource.Message($"Size {gameSize} could not be found"); return;
            }
            this.Server.MapSize = size;
        }*/
        this.Server.ForceEndGame();
    }

    [CommandCallback("gamemode", Description = "Changes the gamemode", AllowedRoles = Roles.Admin)]
    public void SetGameMode(RunnerPlayer commandSource, string gameMode) {
        var mode = ResolveNameMatch(gameMode, GameModes);
        if (string.IsNullOrWhiteSpace(mode)) {
            commandSource.Message($"GameMode {mode} could not be found");
        }
        this.Server.GamemodeRotation.SetRotation(mode);
        this.Server.ForceEndGame();
    }

    [CommandCallback("maps", Description = "Lists all maps")]
    public void ListMaps(RunnerPlayer commandSource) {
        commandSource.Message(string.Join(", ", Maps));
    }
    [CommandCallback("gamemodes", Description = "Lists all gamemodes")]
    public void ListGameMods(RunnerPlayer commandSource) {
        commandSource.Message(string.Join(", ", GameModes));
    }
    [CommandCallback("sizes", Description = "Lists all game sizes")]
    public void ListGameSizes(RunnerPlayer commandSource) {
        commandSource.Message(string.Join(", ", new[] { "8v8", "16v16", "32v32", "64v64", "127v127" }));
    }
    public static readonly FieldInfo modulesField = typeof(RunnerServer).GetField("modules", BindingFlags.NonPublic | BindingFlags.Instance);
    [CommandCallback("modules", Description = "Lists all loaded modules", AllowedRoles = Roles.Admin | Roles.Moderator)]
    public void ListModules(RunnerPlayer commandSource) {
        List<BattleBitModule> modules = (List<BattleBitModule>)modulesField.GetValue(Server);
        commandSource.Message(string.Join(", ", modules.Select(m => m.GetType().ToString())));
    }

    [CommandCallback("start", Description = "Force starts the round", AllowedRoles = Roles.Admin | Roles.Moderator)]
    public void ForceStartRound(RunnerPlayer commandSource) {
        commandSource.Message("Forcing round to start...");
        this.Server.ForceStartGame();
    }
    [CommandCallback("end", Description = "Force ends the round", AllowedRoles = Roles.Admin | Roles.Moderator)]
    public void ForceEndRound(RunnerPlayer commandSource) {
        commandSource.Message("Forcing round to end...");
        this.Server.ForceEndGame();
    }
    [CommandCallback("exec", Description = "Executes a command on the server", AllowedRoles = Roles.Admin)]
    public void ExecServerCommand(RunnerPlayer commandSource, string command) {
        commandSource.Message($"Executing {command}");
        this.Server.ExecuteCommand(command);
        commandSource.Message($"Executed {command}");
    }

    internal static string? ResolveNameMatch(string input, List<string> matches) {
        var lower = input.ToLowerInvariant().Trim();
        foreach (var match in matches) {
            if (lower == match.ToLowerInvariant())
                return match;
        }
        foreach (var match in matches) {
            if (match.ToLowerInvariant().Contains(lower))
                return match;
        }
        return null;
    }

    internal static MapSize GetSizeFromString(string input) {
        switch (input) {
            case "16":
            case "8v8":
            case "8vs8":
                return MapSize._8v8;
            case "32":
            case "16v16":
            case "16vs16":
                return MapSize._16vs16;
            case "64":
            case "32v32":
            case "32vs32":
                return MapSize._32vs32;
            case "128":
            case "64v64":
            case "64vs64":
                return MapSize._64vs64;
            case "256":
            case "127v127":
            case "127vs127":
                return MapSize._127vs127;
            default:
                return MapSize.None;
        }
    }
    public static List<string> GameModes { get; set; } = new()
    {
        "TDM",
        "AAS",
        "RUSH",
        "CONQ",
        "DOMI",
        "ELI",
        "INFCONQ",
        "FRONTLINE",
        "GunGameFFA",
        "FFA",
        "GunGameTeam",
        "SuicideRush",
        "CatchGame",
        "Infected",
        "CashRun",
        "VoxelFortify",
        "VoxelTrench",
        "CaptureTheFlag"
    };
    public static List<string> Maps { get; set; } = new()
    {
        "Azagor",
        "Basra",
        "Construction",
        "District",
        "Dustydew",
        "Eduardovo",
        "Frugis",
        "Isle",
        "Lonovo",
        "MultuIslands",
        "Namak",
        "OilDunes",
        "River",
        "Salhan",
        "SandySunset",
        "TensaTown",
        "Valley",
        "Wakistan",
        "WineParadise"
    };
}