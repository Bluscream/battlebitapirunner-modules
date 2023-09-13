// Version 1.1
using BattleBitAPI.Common;
using BBRAPIModules;
using Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BattleBitBaseModules;

[RequireModule(typeof(CommandHandler))]
public class MoreCommands : BattleBitModule {

    [ModuleReference]
    public CommandHandler CommandHandler { get; set; }

    public override void OnModulesLoaded() {
        this.CommandHandler.Register(this);
    }

    [CommandCallback("map", Description = "Changes the map", AllowedRoles = Roles.Admin | Roles.Moderator)]
    public void SetMap(RunnerPlayer commandSource, string? mapName = null, string? gameMode = null, string? dayNight = null) // , string? gameSize = null)
    {
        if (mapName is null) {
            commandSource.Message($"Current Map:\n\nName: {Maps[this.Server.Map]} ({this.Server.Map})\nMode: {GameModes[this.Server.Gamemode]} ({this.Server.Gamemode})\nSize: {this.Server.MapSize}"); return;
        }
        var map = ResolveNameMatch(mapName, Maps);
        if (!map.HasValue) {
            commandSource.Message($"Map {mapName} could not be found"); return;
        }
        var oldMaps = this.Server.MapRotation.GetMapRotation();
        this.Server.MapRotation.SetRotation(map.Value.Key);

        var oldModes = this.Server.GamemodeRotation.GetGamemodeRotation();
        var mode = ResolveNameMatch(this.Server.Gamemode, GameModes);
        if (gameMode != null) {
            mode = ResolveNameMatch(gameMode, GameModes);
            if (!mode.HasValue) {
                commandSource.Message($"GameMode {gameMode} could not be found"); return;
            }
            this.Server.GamemodeRotation.SetRotation(mode.Value.Key);
        }

        /*if (gameSize != null) {
            var size = GetSizeFromString(gameSize);
            if (size == MapSize.None) {
                commandSource.Message($"Size {gameSize} could not be found"); return;
            }
            this.Server.MapSize = size;
        }*/
        var oldVoteDay = this.Server.ServerSettings.CanVoteDay;
        var oldVoteNight = this.Server.ServerSettings.CanVoteNight;
        var DayNight = ParseDayNight(dayNight);
        if (DayNight != MapDayNight.None) {
            // DayNight = (MapDayNight)this.Server.DayNight;
            switch (DayNight) {
                case MapDayNight.Day:
                    this.Server.ServerSettings.CanVoteDay = true;
                    this.Server.ServerSettings.CanVoteNight = false;
                    break;
                case MapDayNight.Night:
                    this.Server.ServerSettings.CanVoteDay = false;
                    this.Server.ServerSettings.CanVoteNight = true;
                    break;
            }
        }
        var msg = new StringBuilder();
        if (map.HasValue) msg.Append($"Changing map to {GetStringValue(map)}");
        if (mode.HasValue) msg.Append($" ({GetStringValue(mode)})");
        if (DayNight != MapDayNight.None) msg.Append($" [{DayNight}]");

        this.Server.SayToAllChat(msg.ToString());
        this.Server.AnnounceShort(msg.ToString());
        Task.Delay(TimeSpan.FromSeconds(1)).Wait();
        this.Server.ForceEndGame();
        Task.Delay(TimeSpan.FromMinutes(1)).Wait();
        this.Server.MapRotation.SetRotation(oldMaps.ToArray());
        this.Server.GamemodeRotation.SetRotation(oldModes.ToArray());
        this.Server.ServerSettings.CanVoteDay = oldVoteDay;
        this.Server.ServerSettings.CanVoteNight = oldVoteNight;
    }

    [CommandCallback("gamemode", Description = "Changes the gamemode", AllowedRoles = Roles.Admin | Roles.Moderator)]
    public void SetGameMode(RunnerPlayer commandSource, string gameMode, string? dayNight = null) => SetMap(commandSource, this.Server.Map, gameMode, dayNight);

    [CommandCallback("time", Description = "Changes the map time", AllowedRoles = Roles.Admin | Roles.Moderator)]
    public void SetMapTime(RunnerPlayer commandSource, string dayNight) => SetGameMode(commandSource, this.Server.Gamemode, dayNight);

    [CommandCallback("maprestart", Description = "Restarts the current map", AllowedRoles = Roles.Admin | Roles.Moderator)]
    public void RestartMap(RunnerPlayer commandSource) => SetMapTime(commandSource, this.Server.DayNight.ToString());

    [CommandCallback("votetime", Description = "Changes the allowed map times for votes", AllowedRoles = Roles.Admin | Roles.Moderator)]
    public void SetMapVoteTime(RunnerPlayer commandSource, string dayNightAll) {
        var DayNight = ParseDayNight(dayNightAll);
        var msg = $"Players can now vote for ";
        switch (DayNight) {
            case MapDayNight.Day:
                this.Server.ServerSettings.CanVoteDay = true;
                this.Server.ServerSettings.CanVoteNight = false;
                msg += "Day";
                break;
            case MapDayNight.Night:
                this.Server.ServerSettings.CanVoteDay = false;
                this.Server.ServerSettings.CanVoteNight = true;
                msg += "Night";
                break;
            default:
                this.Server.ServerSettings.CanVoteDay = true;
                this.Server.ServerSettings.CanVoteNight = true;
                msg += "All";
                break;
        }
        commandSource.Message(msg);
    }

    [CommandCallback("listmaps", Description = "Lists all maps")]
    public void ListMaps(RunnerPlayer commandSource) {
        commandSource.Message(string.Join(", ", Maps));
    }
    [CommandCallback("listmodes", Description = "Lists all gamemodes")]
    public void ListGameMods(RunnerPlayer commandSource) {
        commandSource.Message(string.Join(", ", GameModes));
    }
    [CommandCallback("listsizes", Description = "Lists all game sizes")]
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
        this.Server.ExecuteCommand(command);
        commandSource.Message($"Executed {command}");
    }
    [CommandCallback("bots", Description = "Spawns bots", AllowedRoles = Roles.Admin)]
    public void SpawnBotCommand(RunnerPlayer commandSource, int amount = 1) {
        this.Server.ExecuteCommand($"join bot {amount}");
        commandSource.Message($"Spawned {amount} bots, use !nobots to remove them");
    }
    [CommandCallback("nobots", Description = "Kicks all bots", AllowedRoles = Roles.Admin)]
    public void KickBotsCommand(RunnerPlayer commandSource, int amount = 999) {
        this.Server.ExecuteCommand($"remove bot {amount}");
        commandSource.Message($"Kicked {amount} bots");
    }
    [CommandCallback("fire", Description = "Toggles bots firing", AllowedRoles = Roles.Admin)]
    public void BotsFireCommand(RunnerPlayer commandSource) {
        this.Server.ExecuteCommand($"bot fire");
        commandSource.Message($"Toggled bots firing");
    }

    internal MapDayNight ParseDayNight(string input) {
        if (string.IsNullOrWhiteSpace(input)) return MapDayNight.None;
        input = input.Trim().ToLowerInvariant();
        if (input.Contains("day")) return MapDayNight.Day;
        else if (input.Contains("night")) return MapDayNight.Night;
        return MapDayNight.None;
    }

    internal static string GetStringValue(KeyValuePair<string, string?>? match) {
        if (match is null || !match.HasValue) return string.Empty;
        if (!string.IsNullOrWhiteSpace(match.Value.Value)) return match.Value.Value;
        return match.Value.Key ?? "Unknown";
    }

    internal static KeyValuePair<string, string?>? ResolveNameMatch(string input, Dictionary<string, string?> matches) {
        var lower = input.ToLowerInvariant().Trim();
        foreach (var match in matches) {
            if (lower == match.Key.ToLowerInvariant() || (match.Value is not null && lower == match.Value.ToLowerInvariant()))
                return match;
        }
        foreach (var match in matches) {
            if (match.Key.ToLowerInvariant().Contains(lower) || (match.Value is not null && match.Value.ToLowerInvariant().Contains(lower)))
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
    public static Dictionary<string, string?> GameModes { get; set; } = new()
    {
        { "TDM", "Team Deathmatch" },
        { "AAS", "AAS" },
        { "RUSH", "Rush" },
        { "CONQ", "Conquest" },
        { "DOMI", "Domination" },
        { "ELI", "Elimination" },
        { "INFCONQ", "Infantry Conquest" },
        { "FRONTLINE", "Frontline" },
        { "GunGameFFA", "Gun Game (Free For All)" },
        { "FFA", "Free For All" },
        { "GunGameTeam", "Gun Game (Team)" },
        { "SuicideRush", "Suicide Rush" },
        { "CatchGame", "Catch Game" },
        { "Infected", "Infected" },
        { "CashRun", "Cash Run" },
        { "VoxelFortify", "Voxel Fortify" },
        { "VoxelTrench", "Voxel Trench" },
        { "CaptureTheFlag", "Capture The Flag" },
    };
    public static Dictionary<string, string?> Maps { get; set; } = new()
    {
        { "Azagor", "Azagor" },
        { "Basra", "Basra" },
        { "Construction", "Construction" },
        { "District", "District" },
        { "Dustydew", "Dustydew" },
        { "Eduardovo", "Eduardovo" },
        { "Frugis", "Frugis" },
        { "Isle", "Isle" },
        { "Lonovo", "Lonovo" },
        { "MultuIslands", "Multu Islands" },
        { "Namak", "Namak" },
        { "OilDunes", "Oil Dunes" },
        { "River", "River" },
        { "Salhan", "Salhan" },
        { "SandySunset", "Sandy Sunset" },
        { "TensaTown", "Tensa Town" },
        { "Valley", "Valley" },
        { "Wakistan", "Wackistan" },
        { "WineParadise", "WineParadise" },
        { "Old_District", "Old District" },
        { "Old_Eduardovo", "Old Eduardovo" },
        { "Old_MultuIslands", "Old Multu Islands" },
        { "Old_Namak", "Old Namak" },
        { "Old_OilDunes", "Old Oil Dunes" },
    };
    public enum MapDayNight : byte {
        Day,
        Night,
        None
    }
}