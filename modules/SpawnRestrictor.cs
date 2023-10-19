using BattleBitAPI.Common;
using BBRAPIModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Restrictor;

[Module("This module will restrict the spawning and entering of certain assets based on the number of players in the server. It will reevaluate the spawning rules every time a player connects or disconnects.", "1.0.0")]

public class SpawnRestrictor : BattleBitModule {
    public SpawnRestrictorConfiguration Configuration { get; set; } = null!;

    readonly Dictionary<string, object> SpawningRuleMap = new Dictionary<string, object>
    {
        { "Flags", SpawningRule.Flags },
        { "SquadMates", SpawningRule.SquadMates },
        { "SquadCaptain", SpawningRule.SquadCaptain },
        { "Tanks", SpawningRule.Tanks },
        { "Transports", SpawningRule.Transports },
        { "Boats", SpawningRule.Boats },
        { "Helicopters", SpawningRule.Helicopters },
        { "APCs", SpawningRule.APCs },
        { "RallyPoints", SpawningRule.RallyPoints }
    };

    readonly Dictionary<string, object> VehicleRuleMap = new Dictionary<string, object>
    {
        { "Tanks", VehicleType.Tank },
        { "Transports", VehicleType.Transport },
        { "Boats", VehicleType.SeaVehicle },
        { "Helicopters", VehicleType.Helicopters },
        { "APCs", VehicleType.APC },
    };

    public override async Task OnPlayerConnected(RunnerPlayer player) {
        await Restrictor();
    }

    public override async Task OnPlayerDisconnected(RunnerPlayer player) {
        await Restrictor();
    }

    public async Task Restrictor() {
        int totalPlayerCount = this.Server.AllPlayers.Count();
        var spawnRule = new SpawningRule();
        var vehicleRule = new VehicleType();

        PropertyInfo[] SpawnConfigs = typeof(SpawnRestrictorConfiguration).GetProperties();
        foreach (PropertyInfo config in SpawnConfigs) {
            var propertyValue = config.GetValue(Configuration, null);

            if (propertyValue != null && totalPlayerCount >= (int)propertyValue) {
                try {
                    SpawningRuleMap.TryGetValue(config.Name, out object? spRule);
                    VehicleRuleMap.TryGetValue(config.Name, out object? veRule);

                    spawnRule |= (SpawningRule)spRule!;
                    vehicleRule |= (VehicleType)veRule!;
                } catch (Exception ex) {
                    Console.WriteLine($"I couldn't find {config.Name} in the mapping: {ex.Message}");
                }
            }
        }

        foreach (RunnerPlayer runnerPlayer in this.Server.AllPlayers) {
            try {
                runnerPlayer.Modifications.AllowedVehicles = vehicleRule;
                runnerPlayer.Modifications.SpawningRule = spawnRule;
            } catch (Exception ex) {
                Console.WriteLine($"I couldn't set the spawning rule for {runnerPlayer.Name}: {ex.Message}");
            }
        }
        this.Server.UILogOnServer($"Setting the spawn and vehicle rules for all players to: {spawnRule.ToString()}", 15);
        Console.WriteLine($"Setting the spawn and vehicle rules for all players to: {spawnRule.ToString()}");
        await Task.CompletedTask;
    }
}

public class SpawnRestrictorConfiguration : ModuleConfiguration {
    public int Flags { get; set; } = 0;
    public int SquadMates { get; set; } = 0;
    public int SquadCaptain { get; set; } = 0;
    public int Tanks { get; set; } = 0;
    public int Transports { get; set; } = 0;
    public int Boats { get; set; } = 0;
    public int Helicopters { get; set; } = 0;
    public int APCs { get; set; } = 0;
    public int RallyPoints { get; set; } = 0;
}