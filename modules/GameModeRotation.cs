using BattleBitAPI.Common;
using BBRAPIModules;
using Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BattleBitBaseModules;

/// <summary>
/// Author: @RainOrigami expanded by @_dx2
/// Version: 1.1
/// </summary>
public class GameModeRotation : BattleBitModule
{
    public GameModeRotationConfiguration Configuration { get; set; }
    public List<string> ActiveGamemodes { get; set; } = new();
    private int currentGamemodesIndex = 0;

    public override void OnModulesLoaded()
    {
        this.CommandHandler.Register(this);
        foreach (var rotation in Configuration.GameModes)
        {
            ActiveGamemodes.AddRange(rotation);
        }
        ActiveGamemodes = ActiveGamemodes.Distinct().ToList()
            .ConvertAll(gm => { 
                var name = FindGameMode(gm);
                if (name == null) Console.WriteLine($"{Server.ServerName} GameModeRotation: {gm} not found, did you type correctly?");
                return name??"";
            });
    }

    public override Task OnConnected()
    {
        var gamemodesText = "";
        foreach (var gamemode in ActiveGamemodes)
        {
            gamemodesText += gamemode + ", ";
        }
        Console.WriteLine($"{Server.ServerName} GameModeRotation: Loaded {gamemodesText} gamemodes");

        Server.GamemodeRotation.SetRotation(Configuration.GameModes[currentGamemodesIndex]);
        currentGamemodesIndex = currentGamemodesIndex + 1 == Configuration.GameModes.Length ? 0 : currentGamemodesIndex + 1;

        return Task.CompletedTask;
    }

    public override Task OnGameStateChanged(GameState oldState, GameState newState)
    {
        if (newState == GameState.WaitingForPlayers)
        {
            string debugText = "";
            foreach (var gm in Configuration.GameModes[currentGamemodesIndex])
            {
                debugText += gm + ", ";
            }
            Console.WriteLine($"{Server.ServerName} GameModeRotation: New Match starting, next gamemodes will be: {debugText}");
            Server.GamemodeRotation.SetRotation(Configuration.GameModes[currentGamemodesIndex]);
            currentGamemodesIndex = currentGamemodesIndex + 1 == Configuration.GameModes.Length ? 0 : currentGamemodesIndex + 1;
        }
        return Task.CompletedTask;
    }

    [ModuleReference]
    public CommandHandler CommandHandler { get; set; }

    [CommandCallback("GameModes", Description = "Shows the current gamemode rotation")]
    public void GameModes(RunnerPlayer commandSource)
    {
        string modes = "";
        foreach (var mode in ActiveGamemodes)
        {
            modes += mode + ", ";
        }
        Server.MessageToPlayer(commandSource, $"The current Gamemode rotation is: {modes}");
    }

    public static string? FindGameMode(string Gamemode)
    {
        switch (Gamemode.Trim().ToLower())
        {
            case "teamdeathmatch":
            case "tdm":
                return "TDM";
            case "advanceandsecure":
            case "aas":
                return "AAS";
            case "rush":
                return "RUSH";
            case "conquest":
            case "conq":
                return "CONQ";
            case "domination":
            case "domi":
                return "DOMI";
            case "elimination":
            case "eli":
                return "ELI";
            case "infantryconquest":
            case "infconq":
                return "INFCONQ";
            case "frontline":
            case "front":
                return "FRONTLINE";
            case "gungamefreeforall":
            case "gungameffa":
            case "ggffa":
                return "GunGameFFA";
            case "freeforall":
            case "ffa":
                return "FFA";
            case "gungameteam":
            case "ggt":
                return "GunGameTeam";
            case "suiciderush":
            case "sr":
                return "SuicideRush";
            case "catchgame":
            case "catch":
                return "CatchGame";
            case "infected":
                return "Infected";
            case "cashrun":
                return "CashRun";
            case "voxelfortify":
            case "voxelf":
                return "VoxelFortify";
            case "voxeltrench":
            case "voxelt":
                return "VoxelTrench";
            case "capturetheflag":
            case "ctf":
                return "CaptureTheFlag";
            default:
                break;
        }
        return null;
    }
}

public class GameModeRotationConfiguration : ModuleConfiguration
{
    //This works a bit differently from the default rotation module. You can have every mode enabled all the time
    //like the default rotation module OR you can separate this in multiple lists,
    //where each one will appear in a separate match. 
    public string[][] GameModes { get; set; } = new[]
    {
        new []{
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
        },
        new[]
        {
            "CONQ",
            "DOMI"
        }
    };
}