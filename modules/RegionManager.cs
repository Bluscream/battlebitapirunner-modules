
using BattleBitAPI.Common;
using BBRAPIModules;
using RegionManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace RegionManager {

    public class RegionFlags {
        public bool CanEnter { get; set; }
        public bool CanExit { get; set; }
        public string EntryDenyMessage { get; set; }
        public string ExitDenyMessage { get; set; }
        public bool IsInvincible { get; set; }
        public bool CanBleed { get; set; }

        public RegionFlags() {
            CanEnter = true;
            CanExit = true;
            EntryDenyMessage = "You are not allowed to enter this region.";
            ExitDenyMessage = "You are not allowed to leave this region.";
            IsInvincible = false;
            CanBleed = true;
        }
    }

    public class Region {

        public RegionFlags Flags { get; set; } = new RegionFlags();


        // Square
        public Vector3 Start { get; set; }
        public Vector3 End { get; set; }


        // Circle
        public Vector3 Center { get; set; }
        public double Radius { get; set; }
        public RegionShape Shape { get; set; }
    }

    public enum RegionShape {
        Square,
        Circle,
    }

    [Module("RegionManager", "1.0.0")]
    public class RegionManager : BattleBitModule {
        public RegionManagerConfiguration ServerConfig { get; set; }


        public override void OnModulesLoaded() {
            InitializeRegions();
        }

        private Dictionary<Region, List<RunnerPlayer>> PopulateRegions() {
            var PlayersByRegion = new Dictionary<Region, List<RunnerPlayer>>();

            foreach (var region in ServerConfig.Regions) {
                PlayersByRegion[region] = new List<RunnerPlayer>();

                foreach (var player in Server.AllPlayers) {
                    if (player.IsAlive) {
                        if (IsInsideRegion(region, player.Position)) {
                            if (region.Flags.CanEnter) {
                                PlayersByRegion[region].Add(player);
                            } else {
                                if (region.Shape == RegionShape.Circle) {
                                    // Teleport the player outside the circle's edge if CanEnter is false
                                    Vector3 directionToCenter = Vector3.Normalize(region.Center - player.Position);
                                    Vector3 teleportDestination = region.Center + directionToCenter * (float)region.Radius;
                                    player.Teleport(new Vector3((int)teleportDestination.X, (int)teleportDestination.Y, (int)teleportDestination.Z));
                                } else if (region.Shape == RegionShape.Square) {
                                    Vector3 direction = CalculateApproachDirection(region, player.Position);
                                    // Calculate the teleport destination based on the approach direction
                                    Vector3 teleportDestination = player.Position - direction;
                                    player.Teleport(new Vector3((int)teleportDestination.X, (int)teleportDestination.Y, (int)teleportDestination.Z));
                                }
                            }
                        }
                    }
                }
            }

            return PlayersByRegion;
        }

        private Vector3 CalculateApproachDirection(Region region, Vector3 playerPosition) {
            Vector3 direction = Vector3.Zero;

            if (playerPosition.X < region.Start.X)
                direction.X = -1;
            else if (playerPosition.X > region.End.X)
                direction.X = 1;

            if (playerPosition.Z < region.Start.Z)
                direction.Z = -1;
            else if (playerPosition.Z > region.End.Z)
                direction.Z = 1;

            return direction;
        }

        public List<RunnerPlayer> GetPlayersInRegion(Region region) {
            Dictionary<Region, List<RunnerPlayer>> PlayersByRegion = PopulateRegions();

            if (PlayersByRegion.TryGetValue(region, out var players)) {
                return players;
            }
            return new List<RunnerPlayer>(); // No players in the region
        }


        public int GetPlayerCountInRegion(Region region) {
            var playersInRegion = GetPlayersInRegion(region);
            return playersInRegion.Count;
        }

        public Dictionary<Team, int> GetPlayerCountByTeam(Region region) {
            var playersInRegion = GetPlayersInRegion(region);
            var playerCountByTeam = playersInRegion.GroupBy(player => player.Team)
                                                   .ToDictionary(group => group.Key, group => group.Count());

            return playerCountByTeam;
        }

        public double GetTeamControlPercentage(Region region, Team targetTeam) {
            var playerCountByTeam = GetPlayerCountByTeam(region);

            if (playerCountByTeam.ContainsKey(targetTeam)) {
                var totalPlayersInRegion = playerCountByTeam.Values.Sum();
                var targetTeamPlayersInRegion = playerCountByTeam[targetTeam];

                return (double)targetTeamPlayersInRegion / totalPlayersInRegion * 100.0;
            } else {
                return 0.0; // Target team has no players in the region
            }
        }

        public Team? GetControllingTeam(Region region) {
            var playerCountByTeam = GetPlayerCountByTeam(region);

            if (playerCountByTeam.Count == 1) {
                return playerCountByTeam.Keys.First();
            } else {
                var controllingTeam = playerCountByTeam.OrderByDescending(kv => kv.Value)
                                                       .FirstOrDefault().Key;

                return controllingTeam;
            }
        }

        private bool IsInsideRegion(Region region, Vector3 position) {
            switch (region.Shape) {
                case RegionShape.Square:
                    return IsInsideSquareRegion(region, position);
                case RegionShape.Circle:
                    return IsInsideCircleRegion(region, position);
                default:
                    return false;
            }


        }
        private bool IsInsideSquareRegion(Region region, Vector3 position) {
            return position.X >= region.Start.X && position.X <= region.End.X &&
                   position.Y >= region.Start.Y && position.Y <= region.End.Y &&
                   position.Z >= region.Start.Z && position.Z <= region.End.Z;
        }

        private bool IsInsideCircleRegion(Region region, Vector3 position) {
            float distanceXZ = (float)Math.Sqrt(Math.Pow(position.X - region.Center.X, 2) + Math.Pow(position.Z - region.Center.Z, 2));
            return distanceXZ <= region.Radius;
        }

        private void InitializeRegions() {
            // TODO: Get regions from db, or set in config

            // TEST DATA
            /* ServerConfig.Regions.Add(new Region
            {
                Shape = RegionShape.Square,
                Start = new Vector3(20, 20, 20),
                End = new Vector3(0, 0, 0)
            }); */

            ServerConfig.Regions.Add(new Region {
                Shape = RegionShape.Circle,
                Center = new Vector3(5, 5, 5),
                Radius = 3,
                Flags = new RegionFlags {
                    CanEnter = true,
                    CanExit = true,
                    EntryDenyMessage = "You are not allowed to enter this region.",
                    ExitDenyMessage = "You are not allowed to leave this region.",
                    IsInvincible = false,
                    CanBleed = true
                }
            });

        }

    }
}

public class RegionManagerConfiguration : ModuleConfiguration {
    public List<Region> Regions { get; set; } = new List<Region>();
}