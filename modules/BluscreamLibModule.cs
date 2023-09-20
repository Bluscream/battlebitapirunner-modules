// Version 2.0
using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using BattleBitAPI.Common;
using BBRAPIModules;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Discord.WebSocket;
using System.IO;
using Bluscream;
//using JsonExtensions;
//using System.Runtime.InteropServices;
//using System.Text.RegularExpressions;
//using System.Reflection.Metadata;
//using System.Threading.Tasks;
//using System.Net.Http;
//using System.Text;
//using System.Linq;
//using System.Net;

namespace Bluscream {

    public static class Extensions {
        public static string str(this RunnerPlayer player) => $"\"{player.Name}\"";
        public static string fullstr(this RunnerPlayer player) => $"{player.str()} ({player.SteamID})";
        public static string ToYesNoString(this bool input) => input ? "Yes" : "No";
        public static string ToEnabledDisabledString(this bool input) => input ? "Enabled" : "Disabled";
    }
    public static class MoreRoles {
        public const Roles Staff = Roles.Admin | Roles.Moderator;
        public const Roles Member = Roles.Admin | Roles.Moderator | Roles.Special | Roles.Vip;
        public const Roles All = Roles.Admin | Roles.Moderator | Roles.Special | Roles.Vip | Roles.None;
    }
    public abstract class BaseInfo {
        public bool Available { get; internal set; } = true;
        public string Name { get; internal set; } = "None";
        public string DisplayName { get; internal set; } = "Unknown";
        public string Description { get; internal set; } = "Unknown";
    }
    public class GameModeInfo : BaseInfo {
        public static GameModeInfo FromName(string name) => BluscreamLib.GameModes.First(m => m.Name == name);
    }
    public class MapInfo : BaseInfo {
        public (string Name, MapSize[] Sizes)[]? SupportedGamemodes { get; internal set; }

        public static MapInfo FromName(string name) => BluscreamLib.Maps.First(m => m.Name == name);
    }
    public enum MapDayNight : byte {
        Day,
        Night,
        None
    }

    public static class BluscreamLib {

        public static string GetStringValue(KeyValuePair<string, string?>? match) {
            if (!match.HasValue) return string.Empty;
            if (!string.IsNullOrWhiteSpace(match.Value.Value)) return match.Value.Value;
            return match.Value.Key ?? "Unknown";
        }
        public static KeyValuePair<string, string?>? ResolveNameMatch(string input, IDictionary<string, string?> matches) {
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
        public static T? ResolveGameModeMapNameMatch<T>(string input, IEnumerable<T> matches) where T : BaseInfo {
            var lower = input.ToLowerInvariant().Trim();
            foreach (var match in matches) {
                if (lower == match.Name?.ToLowerInvariant()) return match;
                else if (lower == match.DisplayName?.ToLowerInvariant()) return match;
            }
            foreach (var match in matches) {
                if (match.DisplayName?.ToLowerInvariant().Contains(lower) == true) return match;
                else if ((match.DisplayName?.ToLowerInvariant().Contains(lower) == true)) return match;
            }
            return null;
        }

        public static MapDayNight GetDayNightFromString(string input) {
            if (string.IsNullOrWhiteSpace(input)) return MapDayNight.None;
            input = input.Trim().ToLowerInvariant();
            if (input.Contains("day")) return MapDayNight.Day;
            else if (input.Contains("night")) return MapDayNight.Night;
            return MapDayNight.None;
        }
        public static MapSize GetMapSizeFromString(string input) {
            switch (input) {
                case "16":
                case "8v8":
                case "_8v8":
                case "8vs8":
                    return MapSize._8v8;
                case "32":
                case "16v16":
                case "_16v16":
                case "16vs16":
                    return MapSize._16vs16;
                case "64":
                case "32v32":
                case "_32v32":
                case "32vs32":
                    return MapSize._32vs32;
                case "128":
                case "64v64":
                case "_64v64":
                case "64vs64":
                    return MapSize._64vs64;
                case "256":
                case "127v127":
                case "_127v127":
                case "127vs127":
                    return MapSize._127vs127;
                default:
                    return MapSize.None;
            }
        }

        public static void Log(object _msg, string source = "") {
            var msg = _msg.ToString();
            if (string.IsNullOrWhiteSpace(msg)) return;
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {source} > {msg.Trim()}");
        }
        public static void ChangeTime(this RunnerServer Server, MapDayNight dayNight = MapDayNight.None) => ChangeMap(Server, dayNight: dayNight);
        public static void ChangeGameMode(this RunnerServer Server, GameModeInfo? gameMode = null, MapDayNight dayNight = MapDayNight.None) => ChangeMap(Server, gameMode: gameMode, dayNight: dayNight);
        public static void ChangeMap(this RunnerServer Server, MapInfo? map = null, GameModeInfo? gameMode = null, MapDayNight dayNight = MapDayNight.None) {
            map = map ?? MapInfo.FromName(Server.Map);
            gameMode = gameMode ?? GameModeInfo.FromName(Server.Gamemode);
            dayNight = dayNight == MapDayNight.None ? (MapDayNight)Server.DayNight : dayNight;

            var oldMaps = Server.MapRotation.GetMapRotation();
            Server.MapRotation.SetRotation(map.Name);
            var oldModes = Server.GamemodeRotation.GetGamemodeRotation();
            Server.GamemodeRotation.SetRotation(gameMode.Name);

            var oldVoteDay = Server.ServerSettings.CanVoteDay;
            var oldVoteNight = Server.ServerSettings.CanVoteNight;
            if (dayNight != MapDayNight.None) {
                switch (dayNight) {
                    case MapDayNight.Day:
                        Server.ServerSettings.CanVoteDay = true;
                        Server.ServerSettings.CanVoteNight = false;
                        break;
                    case MapDayNight.Night:
                        Server.ServerSettings.CanVoteDay = false;
                        Server.ServerSettings.CanVoteNight = true;
                        break;
                }
            }
            var msg = new StringBuilder();
            if (map is not null) msg.Append($"Changing map to {map.DisplayName}");
            if (gameMode is not null) msg.Append($" ({gameMode.DisplayName})");
            if (dayNight != MapDayNight.None) msg.Append($" [{dayNight}]");

            Server.SayToAllChat(msg.ToString());
            Server.AnnounceShort(msg.ToString());
            Task.Delay(TimeSpan.FromSeconds(1)).Wait();
            Server.ForceEndGame();
            Task.Delay(TimeSpan.FromMinutes(1)).Wait();
            Server.MapRotation.SetRotation(oldMaps.ToArray());
            Server.GamemodeRotation.SetRotation(oldModes.ToArray());
            Server.ServerSettings.CanVoteDay = oldVoteDay;
            Server.ServerSettings.CanVoteNight = oldVoteNight;
        }

        public static IReadOnlyList<string> GameModeNames { get { return GameModes.Where(m => m.Available).Select(m => m.Name).ToList(); } }
        public static IReadOnlyList<string> GameModeDisplayNames { get { return Maps.Where(m => m.Available).Select(m => m.DisplayName).ToList(); } }
        public static IReadOnlyList<GameModeInfo> GameModes = new GameModeInfo[] {
            new GameModeInfo() {
                Name = "TDM",
                DisplayName = "Team Deathmatch",
                Description = "Kill the enemy team"
            },
            new GameModeInfo() {
                Name = "AAS",
                DisplayName = "AAS"
            },
            new GameModeInfo() {
                Name = "RUSH",
                DisplayName = "Rush",
                Description = "Plant or defuse bombs"
            },
            new GameModeInfo() {
                Name = "CONQ",
                DisplayName = "Conquest",
                Description = "Capture and hold positions"
            },
            new GameModeInfo() {
                Name = "DOMI",
                DisplayName = "Domination"
            },
            new GameModeInfo() {
                Name = "ELI",
                DisplayName = "Elimination"
            },
            new GameModeInfo() {
                Name = "INFCONQ",
                DisplayName = "Infantry Conquest",
                Description = "Conquest without strong vehicles"
            },
            new GameModeInfo() {
                Name = "FRONTLINE",
                DisplayName = "Frontline"
            },
            new GameModeInfo() {
                Name = "GunGameFFA",
                DisplayName = "Gun Game (Free For All)",
                Description = "Get through the loadouts as fast as possible"
            },
            new GameModeInfo() {
                Name = "FFA",
                DisplayName = "Free For All",
                Description = "Team Deathmatch without teams"
            },
            new GameModeInfo() {
                Name = "GunGameTeam",
                DisplayName = "Gun Game (Team)",
                Description = "Get through the loadouts as fast as possible"
            },
            new GameModeInfo() {
                Name = "SuicideRush",
                DisplayName = "Suicide Rush"
            },
            new GameModeInfo() {
                Name = "CatchGame",
                DisplayName = "Catch Game"
            },
            new GameModeInfo() {
                Name = "Infected",
                DisplayName = "Infected",
                Description = "Zombies"
            },
            new GameModeInfo() {
                Name = "CashRun",
                DisplayName = "Cash Run"
            },
            new GameModeInfo() {
                Name = "VoxelFortify",
                DisplayName = "Voxel Fortify"
            },
            new GameModeInfo() {
                Name = "VoxelTrench",
                DisplayName = "Voxel Trench"
            },
            new GameModeInfo() {
                Name = "CaptureTheFlag",
                DisplayName = "Capture The Flag"
            },
        };
        public static IReadOnlyList<string> MapNames { get { return Maps.Where(m => m.Available).Select(m => m.Name).ToList(); } }
        public static IReadOnlyList<string> MapDisplayNames { get { return Maps.Where(m => m.Available).Select(m => m.DisplayName).ToList(); } }
        public static IReadOnlyList<MapInfo> Maps = new MapInfo[] {
            new MapInfo() {
                Name = "Azagor",
                SupportedGamemodes = new[] {
                    ("TDM", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("RUSH", new[] { MapSize._16vs16, MapSize._32vs32, }),
                    ("CONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("INFCONQ", new[] { MapSize._64vs64, MapSize._127vs127, }),
                    ("DOMI", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("ELI", new[] { MapSize._16vs16, }),
                    ("FRONTLINE", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }) }
            },
            new MapInfo() {
                Name = "Basra",
                Description = "Has a shipwreck",
                SupportedGamemodes = new[] {
                    ("TDM", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("CONQ", new[] { MapSize._64vs64, MapSize._127vs127, }),
                    ("INFCONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("ELI", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("FRONTLINE", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }) }
            },
            new MapInfo() {
                Name = "Construction",
                SupportedGamemodes = new[] {
                    ("TDM", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("CONQ", new[] { MapSize._32vs32, }),
                    ("INFCONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("DOMI", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("ELI", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("CashRun", new[] { MapSize._16vs16, }) }
            },
            new MapInfo() {
                Name = "District",
                SupportedGamemodes = new[] {
                    ("RUSH", new[] { MapSize._16vs16, MapSize._32vs32, }),
                    ("CONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("INFCONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("DOMI", new[] { MapSize._16vs16, MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("FRONTLINE", new[] { MapSize._32vs32, MapSize._64vs64, }),
                    ("CTF", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }) }
            },
            new MapInfo() {
                Name = "Dustydew",
                DisplayName = "Dusty Dew",
                SupportedGamemodes = new[] {
                    ("TDM", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("RUSH", new[] { MapSize._16vs16, MapSize._32vs32, }),
                    ("CONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("INFCONQ", new[] { MapSize._16vs16, MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("DOMI", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("ELI", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("GunGameFFA", new[] { MapSize._8v8, }),
                    ("GunGameTeam", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("FFA", new[] { MapSize._8v8, }) }
            },
            new MapInfo() {
                Name = "Eduardovo",
                SupportedGamemodes = new[] {
                    ("TDM", new[] { MapSize._16vs16, }),
                    ("CONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("INFCONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("DOMI", new[] { MapSize._16vs16, MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("FRONTLINE", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("GunGameFFA", new[] { MapSize._16vs16, }),
                    ("CashRun", new[] { MapSize._16vs16, MapSize._32vs32, }) }
            },
            new MapInfo() {
                Name = "Frugis",
                Description = "Inspired by Paris, France",
                SupportedGamemodes = new[] {
                    ("TDM", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("RUSH", new[] { MapSize._16vs16, MapSize._32vs32, }),
                    ("CONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("INFCONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("DOMI", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("FRONTLINE", new[] { MapSize._32vs32, }),
                    ("GunGameFFA", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("GunGameTeam", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("CashRun", new[] { MapSize._16vs16, MapSize._32vs32, }),
                    ("CTF", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }) }
            },
            new MapInfo() {
                Name = "Isle",
                SupportedGamemodes = new[] {
                    ("CONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("INFCONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("DOMI", new[] { MapSize._16vs16, MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("FRONTLINE", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("CTF", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }) }
            },
            new MapInfo() {
                Name = "Lonovo",
                SupportedGamemodes = new[] {
                    ("TDM", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("RUSH", new[] { MapSize._32vs32, }),
                    ("CONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("INFCONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("DOMI", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("ELI", new[] { MapSize._16vs16, }),
                    ("GunGameFFA", new[] { MapSize._8v8, }),
                    ("GunGameTeam", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("FFA", new[] { MapSize._8v8, }),
                    ("CashRun", new[] { MapSize._16vs16, MapSize._32vs32, }) }
            },
            new MapInfo() {
                Name = "MultuIslands",
                DisplayName = "Multu Islands",
                SupportedGamemodes = new[] {
                    ("RUSH", new[] { MapSize._8v8, MapSize._16vs16, MapSize._32vs32, }),
                    ("CONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("INFCONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("DOMI", new[] { MapSize._16vs16, MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("CTF", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }) }
            },
            new MapInfo() {
                Name = "Namak",
                SupportedGamemodes = new[] {
                    ("RUSH", new[] { MapSize._16vs16, MapSize._32vs32, }),
                    ("CONQ", new[] { MapSize._32vs32, MapSize._64vs64, }),
                    ("INFCONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("DOMI", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }) }
            },
            new MapInfo() {
                Name = "OilDunes",
                DisplayName = "Oil Dunes",
                SupportedGamemodes = new[] {
                    ("CONQ", new[] { MapSize._32vs32, }),
                    ("INFCONQ", new[] { MapSize._32vs32, MapSize._64vs64, }),
                    ("DOMI", new[] { MapSize._16vs16, MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("CashRun", new[] { MapSize._16vs16, MapSize._32vs32, }) }
            },
            new MapInfo() {
                Name = "River",
                SupportedGamemodes = new[] {
                    ("TDM", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("CONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("INFCONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("DOMI", new[] { MapSize._16vs16, MapSize._32vs32, MapSize._64vs64, }),
                    ("ELI", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("FRONTLINE", new[] { MapSize._32vs32, MapSize._64vs64, }),
                    ("GunGameFFA", new[] { MapSize._8v8, }),
                    ("GunGameTeam", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("FFA", new[] { MapSize._8v8, }),
                    ("CashRun", new[] { MapSize._16vs16, MapSize._32vs32, }) }
            },
            new MapInfo() {
                Name = "Salhan",
                SupportedGamemodes = new[] {
                    ("TDM", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("RUSH", new[] { MapSize._8v8, MapSize._16vs16, MapSize._32vs32, }),
                    ("CONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("INFCONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("DOMI", new[] { MapSize._16vs16, MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("ELI", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("FRONTLINE", new[] { MapSize._32vs32, }),
                    ("CashRun", new[] { MapSize._16vs16, MapSize._32vs32, }) }
            },
            new MapInfo() {
                Name = "SandySunset",
                DisplayName = "SandySunset",
                Description = "Sniper's paradise",
                SupportedGamemodes = new[] {
                    ("TDM", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("CONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("INFCONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("DOMI", new[] { MapSize._16vs16, MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("FRONTLINE", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("CashRun", new[] { MapSize._16vs16, MapSize._32vs32, }),
                    ("CTF", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }) }
            },
            new MapInfo() {
                Name = "TensaTown",
                DisplayName  = "Tensa Town",
                SupportedGamemodes = new[] {
                    ("TDM", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("RUSH", new[] { MapSize._16vs16, MapSize._32vs32, }),
                    ("CONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("INFCONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("DOMI", new[] { MapSize._16vs16, MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("ELI", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("FRONTLINE", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("CashRun", new[] { MapSize._16vs16, MapSize._32vs32, }),
                    ("CTF", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }) }
            },
            new MapInfo() {
                Name = "Valley",
                SupportedGamemodes = new[] {
                    ("TDM", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("RUSH", new[] { MapSize._16vs16, MapSize._32vs32, }),
                    ("CONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("INFCONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("DOMI", new[] { MapSize._16vs16, MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("FRONTLINE", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("CashRun", new[] { MapSize._16vs16, MapSize._32vs32, }) }
            },
            new MapInfo() {
                Name = "Wakistan",
                Description = "I don't know why anyone would want to play this map",
                SupportedGamemodes = new[] {
                    ("CONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("INFCONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("DOMI", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("FRONTLINE", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }) }
            },
            new MapInfo() {
                Name = "WineParadise",
                DisplayName = "Wine Paradise",
                SupportedGamemodes = new[] {
                    ("TDM", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("CONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("INFCONQ", new[] { MapSize._32vs32, }),
                    ("DOMI", new[] { MapSize._16vs16, MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("GunGameFFA", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("GunGameTeam", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("FFA", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("CashRun", new[] { MapSize._16vs16, MapSize._32vs32, }) }
            },
            new MapInfo() {
                Name = "Old_District",
                DisplayName = "Old District",
                Description = "Old version of the map District",
                SupportedGamemodes = new[] {
                    ("TDM", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("RUSH", new[] { MapSize._16vs16, MapSize._32vs32, }),
                    ("CONQ", new[] { MapSize._32vs32, MapSize._64vs64 }),
                    ("INFCONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("DOMI", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("ELI", new[] { MapSize._16vs16, }),
                    ("CashRun", new[] { MapSize._16vs16, MapSize._32vs32, }) }
            },
            new MapInfo() {
                Name = "Old_Eduardovo",
                DisplayName = "Old Eduardovo",
                Description = "Old version of the map Eduardovo",
                SupportedGamemodes = new[] {
                    ("TDM", new[] { MapSize._8v8, MapSize._16vs16, MapSize._32vs32, }),
                    ("CONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("ELI", new[] { MapSize._16vs16, MapSize._32vs32, }),
                    ("FRONTLINE", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }) }
            },
            new MapInfo() {
                Name = "Old_MultuIslands",
                DisplayName = "Old Multu Islands",
                Description = "Old version of the map Multu Islands",
                SupportedGamemodes = new[] {
                    ("TDM", new[] { MapSize._8v8, MapSize._16vs16 }),
                    ("CONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }) }
            },
            new MapInfo() {
                Name = "Old_Namak",
                DisplayName = "Old Namak",
                Description = "Old version of the map Namak",
                SupportedGamemodes = new[] {
                    ("TDM", new[] { MapSize._8v8, MapSize._16vs16 }),
                    ("CONQ", new[] { MapSize._16vs16, MapSize._32vs32, MapSize._64vs64, }),
                    ("DOMI", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("GunGameFFA", new[] { MapSize._8v8 }),
                    ("GunGameTeam", new[] { MapSize._8v8, MapSize._16vs16, }),
                    ("FFA", new[] { MapSize._8v8 }) }
            },
            new MapInfo() {
                Name = "Old_OilDunes",
                DisplayName = "Old Oil Dunes",
                SupportedGamemodes = new[] {
                    ("TDM", new[] { MapSize._16vs16, MapSize._32vs32 }),
                    ("CONQ", new[] { MapSize._32vs32, MapSize._64vs64, MapSize._127vs127, }),
                    ("ELI", new[] { MapSize._16vs16, MapSize._32vs32, }) }
            },
            new MapInfo() {
                Name = "ZalfiBay",
                DisplayName = "Zalfi Bay",
                SupportedGamemodes = new[] {
                    ("CONQ", new [] {MapSize._32vs32,MapSize._64vs64,MapSize._127vs127,}),
                    ("INFCONQ", new [] {MapSize._32vs32, MapSize._64vs64,MapSize._127vs127,}),
                    ("DOMI", new [] { MapSize._16vs16, MapSize._32vs32,MapSize._64vs64,MapSize._127vs127,}),
                    ("FRONTLINE", new [] {MapSize._32vs32,MapSize._64vs64,MapSize._127vs127,}),
                    ("CTF", new [] {MapSize._32vs32,MapSize._64vs64,MapSize._127vs127,}) }
            },
            new MapInfo() {
                Available = false,
                Name = "Polygon",
                Description = "Tutorial map"
            }
        };
    }

    [RequireModule(typeof(DevMinersBBModules.Telemetry))]
    [Module("Bluscream's Library", "2.0.0")]
    public class BluscreamLibModule : BattleBitModule {
        public BluscreamLibConfiguration Configuration { get; set; } = null!;
        public static class ModuleInfo {
            public const string Name = "Bluscream's Library";
            public const string Description = "Generic library for common code used by multiple modules.";
            public static readonly Version Version = new Version(2, 0);
            public const string UpdateUrl = "https://github.com/Bluscream/battlebitapirunner-modules/raw/master/modules/BluscreamLib.cs";
            public const string Author = "Bluscream";
        }
    }
    public class BluscreamLibConfiguration : ModuleConfiguration {
        public string TimeStampFormat { get; set; } = "HH:mm:ss";
    }
}
#region json
namespace JsonExtensions {
public static class JsonUtils {
    public static T FromJson<T>(string jsonText) => JsonSerializer.Deserialize<T>(jsonText, Converter.Settings);
    public static T FromJsonFile<T>(FileInfo file) => FromJson<T>(File.ReadAllText(file.FullName));
    public static string ToJson<T>(this T self) => JsonSerializer.Serialize(self, Converter.Settings);
    public static void ToFile<T>(this T self, FileInfo file) => File.WriteAllText(file.FullName, ToJson(self));
}
//public class JsonBase {
//    public FileInfo File { get; set; }
//    public JsonBase(FileInfo file) { File = file; }
//}
//public class JsonDict : JsonBase {
//    public dynamic Content { get; set; }
//    public void Load<T>() { Content = JsonUtils.FromJsonFile<T>(File); }
//    public void Save() => Content.ToFile(File);
//    public JsonDict(FileInfo file) { File = file; }
//}
public static class Converter {
    public static readonly JsonSerializerOptions Settings = new(JsonSerializerDefaults.General) {
        Converters =
        {
            new DateOnlyConverter(),
            new TimeOnlyConverter(),
            IsoDateTimeOffsetConverter.Singleton
        },
    };
}
public class ParseStringConverter : JsonConverter<long> {
    public override bool CanConvert(Type t) => t == typeof(long);

    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var value = reader.GetString();
        long l;
        if (Int64.TryParse(value, out l)) {
            return l;
        }
        throw new Exception("Cannot unmarshal type long");
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options) {
        JsonSerializer.Serialize(writer, value.ToString(), options);
        return;
    }

    public static readonly ParseStringConverter Singleton = new ParseStringConverter();
}
public class DateOnlyConverter : JsonConverter<DateOnly> {
    private readonly string serializationFormat;
    public DateOnlyConverter() : this(null) { }

    public DateOnlyConverter(string? serializationFormat) {
        this.serializationFormat = serializationFormat ?? "yyyy-MM-dd";
    }

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var value = reader.GetString();
        return DateOnly.Parse(value!);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(serializationFormat));
}
public class TimeOnlyConverter : JsonConverter<TimeOnly> {
    private readonly string serializationFormat;

    public TimeOnlyConverter() : this(null) { }

    public TimeOnlyConverter(string? serializationFormat) {
        this.serializationFormat = serializationFormat ?? "HH:mm:ss.fff";
    }

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var value = reader.GetString();
        return TimeOnly.Parse(value!);
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(serializationFormat));
}
public class IsoDateTimeOffsetConverter : JsonConverter<DateTimeOffset> {
    public override bool CanConvert(Type t) => t == typeof(DateTimeOffset);

    private const string DefaultDateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";

    private DateTimeStyles _dateTimeStyles = DateTimeStyles.RoundtripKind;
    private string? _dateTimeFormat;
    private CultureInfo? _culture;

    public DateTimeStyles DateTimeStyles {
        get => _dateTimeStyles;
        set => _dateTimeStyles = value;
    }

    public string? DateTimeFormat {
        get => _dateTimeFormat ?? string.Empty;
        set => _dateTimeFormat = (string.IsNullOrEmpty(value)) ? null : value;
    }

    public CultureInfo Culture {
        get => _culture ?? CultureInfo.CurrentCulture;
        set => _culture = value;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) {
        string text;


        if ((_dateTimeStyles & DateTimeStyles.AdjustToUniversal) == DateTimeStyles.AdjustToUniversal
            || (_dateTimeStyles & DateTimeStyles.AssumeUniversal) == DateTimeStyles.AssumeUniversal) {
            value = value.ToUniversalTime();
        }

        text = value.ToString(_dateTimeFormat ?? DefaultDateTimeFormat, Culture);

        writer.WriteStringValue(text);
    }

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        string? dateText = reader.GetString();

        if (string.IsNullOrEmpty(dateText) == false) {
            if (!string.IsNullOrEmpty(_dateTimeFormat)) {
                return DateTimeOffset.ParseExact(dateText, _dateTimeFormat, Culture, _dateTimeStyles);
            } else {
                return DateTimeOffset.Parse(dateText, Culture, _dateTimeStyles);
            }
        } else {
            return default(DateTimeOffset);
        }
    }

    public static readonly IsoDateTimeOffsetConverter Singleton = new IsoDateTimeOffsetConverter();
}
}
#endregion