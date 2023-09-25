// Version 2.0
using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Security.Principal;
using System.Net;

using BattleBitAPI.Common;
using BBRAPIModules;

using Bluscream;

namespace Bluscream {
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
    public class ModuleInfo {
        public bool Loaded { get; set; }
        public bool Enabled { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public Version Version { get; set; }
        public string Author { get; set; }
        public Uri? WebsiteUrl { get; set; }
        public Uri? UpdateUrl { get; set; }
        public Uri? SupportUrl { get; set; }
        public ModuleInfo() { }
        public ModuleInfo(string name, string description, Version version, string author, Uri websiteUrl, Uri updateUrl, Uri supportUrl) {
            Name = name;
            Description = description;
            Version = version;
            Author = author;
            WebsiteUrl = websiteUrl;
            UpdateUrl = updateUrl;
            SupportUrl = supportUrl;
        }
        public ModuleInfo(string name, string description, Version version, string author, string websiteUrl, string updateUrl, string supportUrl) :
            this(name, description, version, author, websiteUrl.ToUri(), updateUrl.ToUri(), supportUrl.ToUri()) { }
        public ModuleInfo(string name, string description, string version, string author, string websiteUrl, string updateUrl, string supportUrl) :
            this(name, description, version.ToVersion(), author, websiteUrl.ToUri(), updateUrl.ToUri(), supportUrl.ToUri()) { }
    }
    [RequireModule(typeof(DevMinersBBModules.Telemetry))]
    [RequireModule(typeof(Permissions.PlayerPermissions))]
    [Module("Bluscream's Library", "2.0.0")]
    public class BluscreamLib : BattleBitModule {
        public static ModuleInfo ModuleInfo = new() {
            Name = "Bluscream's Library",
            Description = "Generic library for common code used by multiple modules.",
            Version = new Version(2, 0),
            Author = "Bluscream",
            WebsiteUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/"),
            UpdateUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/raw/master/modules/BluscreamLib.cs"),
            SupportUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/issues/new?title=BluscreamLib")
        };
//         [ModuleReference]
// #if DEBUG
//         public Permissions.PlayerPermissions? PlayerPermissions { get; set; } = null!;
// #else
//         public dynamic? PlayerPermissions { get; set; } = null!;
// #endif

        public BluscreamLibConfiguration Configuration { get; set; } = null!;

        #region Methods

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
        #endregion
        #region Data
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
        #endregion
    }
    public class CommandConfiguration {
        public bool Enabled { get; set; } = true;
        public List<string>? AllowedRoles { get; set; } = new() { "All" };
    }
    public class BluscreamLibConfiguration : ModuleConfiguration {
        public string TimeStampFormat { get; set; } = "HH:mm:ss";
    }
}
#region Utils
namespace Bluscream {
public static partial class Utils {
        public static FileInfo getOwnPath() {
            return new FileInfo(Path.GetDirectoryName(Environment.ProcessPath));
        }

        public static bool IsAlreadyRunning(string appName) {
            System.Threading.Mutex m = new System.Threading.Mutex(false, appName);
            if (m.WaitOne(1, false) == false) {
                return true;
            }
            return false;
        }

        internal static void Exit() {
            Environment.Exit(0);
            var currentP = Process.GetCurrentProcess();
            currentP.Kill();
        }

        public static void RestartAsAdmin(string[] arguments) {
            if (IsAdmin()) return;
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = Environment.CurrentDirectory;
            proc.FileName = Assembly.GetEntryAssembly().CodeBase;
            proc.Arguments += arguments.ToString();
            proc.Verb = "runas";
            try {
                Process.Start(proc);
                Exit();
            } catch (Exception ex) {
                Console.WriteLine($"Unable to restart as admin: {ex.Message}");
                Exit();
            }
        }

        internal static bool IsAdmin() {
            bool isAdmin;
            try {
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            } catch (UnauthorizedAccessException) {
                isAdmin = false;
            } catch (Exception) {
                isAdmin = false;
            }
            return isAdmin;
        }

        public static Process StartProcess(FileInfo file, params string[] args) => StartProcess(file.FullName, file.DirectoryName, args);
        public static Process StartProcess(string file, string workDir = null, params string[] args) {
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.FileName = file;
            proc.Arguments = string.Join(" ", args);
            Console.WriteLine("Starting Process: {0} {1}", proc.FileName, proc.Arguments);
            if (workDir != null) {
                proc.WorkingDirectory = workDir;
                Console.WriteLine("Working Directory: {0}", proc.WorkingDirectory);
            }
            return Process.Start(proc);
        }

        public static IPEndPoint ParseIPEndPoint(string endPoint) {
            string[] ep = endPoint.Split(':');
            if (ep.Length < 2) return null;
            IPAddress ip;
            if (ep.Length > 2) {
                if (!IPAddress.TryParse(string.Join(":", ep, 0, ep.Length - 1), out ip)) {
                    return null;
                }
            } else {
                if (!IPAddress.TryParse(ep[0], out ip)) {
                    return null;
                }
            }
            int port;
            if (!int.TryParse(ep[ep.Length - 1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port)) {
                return null;
            }
            return new IPEndPoint(ip, port);
        }
    }
}
#endregion
#region Extensions
namespace Bluscream {
public static class Extensions {
        #region Roles
        public static string ToRoleString(this Roles roles) {
            if (roles == Roles.None) {
                return string.Empty;
            }
            if (roles.HasFlag(Roles.Admin) && roles.HasFlag(Roles.Moderator) && roles.HasFlag(Roles.Special) && roles.HasFlag(Roles.Vip)) {
                return "All";
            }
            var roleStrings = new List<string>();
            if (roles.HasFlag(Roles.Admin)) {
                roleStrings.Add(nameof(Roles.Admin));
            }
            if (roles.HasFlag(Roles.Moderator)) {
                roleStrings.Add(nameof(Roles.Moderator));
            }
            if (roles.HasFlag(Roles.Special)) {
                roleStrings.Add(nameof(Roles.Special));
            }
            if (roles.HasFlag(Roles.Vip)) {
                roleStrings.Add(nameof(Roles.Vip));
            }
            return string.Join(",", roleStrings);
        }
        public static List<string> ToRoleStringList(Roles roles) {
            var roleStrings = new List<string>();
            if (roles == Roles.None) {
                return roleStrings;
            }
            if (roles.HasFlag(Roles.Admin) && roles.HasFlag(Roles.Moderator) && roles.HasFlag(Roles.Special) && roles.HasFlag(Roles.Vip)) {
                roleStrings.Add("All");
                return roleStrings;
            }
            if (roles.HasFlag(Roles.Admin)) {
                roleStrings.Add(nameof(Roles.Admin));
            }
            if (roles.HasFlag(Roles.Moderator)) {
                roleStrings.Add(nameof(Roles.Moderator));
            }
            if (roles.HasFlag(Roles.Special)) {
                roleStrings.Add(nameof(Roles.Special));
            }
            if (roles.HasFlag(Roles.Vip)) {
                roleStrings.Add(nameof(Roles.Vip));
            }
            return roleStrings;
        }
        public static Roles ParseRoles(this string rolesString) {
            if (string.IsNullOrEmpty(rolesString) || rolesString.Equals("All", StringComparison.OrdinalIgnoreCase)) {
                return MoreRoles.All;
            }
            Roles result = Roles.None;
            var separators = new[] { ',', '|' };
            var roleStrings = rolesString.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            foreach (var roleString in roleStrings) {
                if (Enum.TryParse<Roles>(roleString, true, out var role)) {
                    result |= role;
                }
            }
            return result;
        }
        public static Roles ParseRoles(List<string>? rolesList) {
            if (rolesList is null || rolesList.Count == 0 || rolesList.Any(role => role.Equals("All", StringComparison.OrdinalIgnoreCase))) {
                return MoreRoles.All;
            }

            Roles result = Roles.None;

            foreach (var roleString in rolesList) {
                if (Enum.TryParse<Roles>(roleString, true, out var role)) {
                    result |= role;
                }
            }

            return result;
        }

        #endregion
        #region Server
        public static RunnerPlayer GetPlayerBySteamId64(this RunnerServer server, ulong steamId64) => server.AllPlayers.Where(p=>p.SteamID==steamId64).First();
        public static string GetPlayerNameBySteamId64(this RunnerServer server, ulong steamId64) {
            var player = server.GetPlayerBySteamId64(steamId64);
            return player?.Name ?? steamId64.ToString();
        }
        #endregion
        #region Player
        public static string str(this RunnerPlayer player) => $"\"{player.Name}\"";
        public static string fullstr(this RunnerPlayer player) => $"{player.str()} ({player.SteamID})";
        public static Roles GetRoles(this RunnerPlayer player, Permissions.PlayerPermissions permissionsModule) => permissionsModule.GetPlayerRoles(player.SteamID);
        public static bool HasAnyRoleOf(this RunnerPlayer player, Permissions.PlayerPermissions permissionsModule, Roles needsAnyRole) => (player.GetRoles(permissionsModule) & needsAnyRole) != 0;
        #endregion
        #region Map
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
        #endregion
        #region String
        public static bool EvalToBool(string expression) {
            System.Data.DataTable table = new System.Data.DataTable();
            table.Columns.Add("expression", string.Empty.GetType(), expression);
            System.Data.DataRow row = table.NewRow();
            table.Rows.Add(row);
            return bool.Parse((string)row["expression"]);
        }
        public static double EvalToDouble(string expression) {
            System.Data.DataTable table = new System.Data.DataTable();
            table.Columns.Add("expression", string.Empty.GetType(), expression);
            System.Data.DataRow row = table.NewRow();
            table.Rows.Add(row);
            return double.Parse((string)row["expression"]);
        }
        public static string EvalToString(string expression) {
            System.Data.DataTable table = new System.Data.DataTable();
            table.Columns.Add("expression", string.Empty.GetType(), expression);
            System.Data.DataRow row = table.NewRow();
            table.Rows.Add(row);
            return (string)row["expression"];
        }
        public static int EvalToInt(string expression) {
            System.Data.DataTable table = new System.Data.DataTable();
            table.Columns.Add("expression", string.Empty.GetType(), expression);
            System.Data.DataRow row = table.NewRow();
            table.Rows.Add(row);
            return int.Parse((string)row["expression"]);
        }

        public static MapInfo? ToMap(this string mapName) => BluscreamLib.Maps.Where(m => m.Name.ToLowerInvariant() == mapName.ToLowerInvariant()).First();
        public static MapInfo? ParseMap(this string input) => BluscreamLib.ResolveGameModeMapNameMatch(input, BluscreamLib.Maps);
        public static GameModeInfo? ToGameMode(this string gameModeName) => BluscreamLib.GameModes.Where(m => m.Name.ToLowerInvariant() == gameModeName.ToLowerInvariant()).First();
        public static GameModeInfo? ParseGameMode(this string input) => BluscreamLib.ResolveGameModeMapNameMatch(input, BluscreamLib.GameModes);
        public static MapDayNight ParseDayNight(this string input) => BluscreamLib.GetDayNightFromString(input);
        public static string Base64Encode(this string plainText) {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }
        public static string Base64Decode(this string base64EncodedData) {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }
        public static string GetDigits(this string input) {
            return new string(input.Where(char.IsDigit).ToArray());
        }
        public static string Format(this string input, params string[] args) {
            return string.Format(input, args);
        }
        public static IEnumerable<string> SplitToLines(this string input) {
            if (input == null) {
                yield break;
            }
            using (System.IO.StringReader reader = new System.IO.StringReader(input)) {
                string line;
                while ((line = reader.ReadLine()) != null) {
                    yield return line;
                }
            }
        }
        public static string ToTitleCase(this string source, string langCode = "en-US") {
            return new CultureInfo(langCode, false).TextInfo.ToTitleCase(source);
        }
        public static bool Contains(this string source, string toCheck, StringComparison comp) {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
        public static bool IsNullOrEmpty(this string source) {
            return string.IsNullOrEmpty(source);
        }
        public static bool IsNullOrWhiteSpace(this string source) {
            return string.IsNullOrWhiteSpace(source);
        }
        public static string[] Split(this string source, string split, int count = -1, StringSplitOptions options = StringSplitOptions.None) {
            if (count != -1) return source.Split(new string[] { split }, count, options);
            return source.Split(new string[] { split }, options);
        }
        public static string Remove(this string Source, string Replace) {
            return Source.Replace(Replace, string.Empty);
        }
        public static string ReplaceLastOccurrence(this string Source, string Find, string Replace) {
            int place = Source.LastIndexOf(Find);
            if (place == -1)
                return Source;
            string result = Source.Remove(place, Find.Length).Insert(place, Replace);
            return result;
        }
        public static string EscapeLineBreaks(this string source) {
            return Regex.Replace(source, @"\r\n?|\n", @"\$&");
        }
        public static string Ext(this string text, string extension) {
            return text + "." + extension;
        }
        public static string Quote(this string text) {
            return SurroundWith(text, "\"");
        }
        public static string Enclose(this string text) {
            return SurroundWith(text, "(", ")");
        }
        public static string Brackets(this string text) {
            return SurroundWith(text, "[", "]");
        }
        public static string SurroundWith(this string text, string surrounds) {
            return surrounds + text + surrounds;
        }
        public static string SurroundWith(this string text, string starts, string ends) {
            return starts + text + ends;
        }
        public static string RemoveInvalidFileNameChars(this string filename) {
            return string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
        }
        public static string ReplaceInvalidFileNameChars(this string filename) {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }
        public static Uri ToUri(this string url) {
            var success = Uri.TryCreate(url, new UriCreationOptions() { DangerousDisablePathAndQueryCanonicalization = false }, out var uri);
            if (url.IsNullOrWhiteSpace() || !success) {
                BluscreamLib.Log($"Unable to parse: {url} as URI!");
            }
            return uri;
        }
        public static Version ToVersion(this string version) {
            var success = System.Version.TryParse(version, out var Version);
            if (version.IsNullOrWhiteSpace() || !success) {
                BluscreamLib.Log($"Unable to parse: {version} as version!");
            }
            return Version;
        }
        #endregion String
        #region bool
        public static string ToYesNo(this bool input) => input ? "Yes" : "No";
        public static string ToEnabledDisabled(this bool input) => input ? "Enabled" : "Disabled";
        public static string ToOnOff(this bool input) => input ? "On" : "Off";
        #endregion bool
        #region Reflection

        public static Dictionary<string, object> ToDictionary(this object instanceToConvert) {
            return instanceToConvert.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .ToDictionary(
                propertyInfo => propertyInfo.Name,
                propertyInfo => Extensions.ConvertPropertyToDictionary(propertyInfo, instanceToConvert));
        }

        public static object ConvertPropertyToDictionary(PropertyInfo propertyInfo, object owner) {
            Type propertyType = propertyInfo.PropertyType;
            object propertyValue = propertyInfo.GetValue(owner);

            if (!propertyType.Equals(typeof(string)) && (typeof(ICollection<>).Name.Equals(propertyValue.GetType().BaseType.Name) || typeof(Collection<>).Name.Equals(propertyValue.GetType().BaseType.Name))) {
                var collectionItems = new List<Dictionary<string, object>>();
                var count = (int)propertyType.GetProperty("Count").GetValue(propertyValue);
                PropertyInfo indexerProperty = propertyType.GetProperty("Item");
                for (var index = 0; index < count; index++) {
                    object item = indexerProperty.GetValue(propertyValue, new object[] { index });
                    PropertyInfo[] itemProperties = item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

                    if (itemProperties.Any()) {
                        Dictionary<string, object> dictionary = itemProperties
                            .ToDictionary(
                            subtypePropertyInfo => subtypePropertyInfo.Name,
                            subtypePropertyInfo => Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, item));
                        collectionItems.Add(dictionary);
                    }
                }

                return collectionItems;
            }

            if (propertyType.IsPrimitive || propertyType.Equals(typeof(string))) {
                return propertyValue;
            }

            PropertyInfo[] properties = propertyType.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (properties.Any()) {
                return properties.ToDictionary(
                                    subtypePropertyInfo => subtypePropertyInfo.Name,
                                    subtypePropertyInfo => (object)Extensions.ConvertPropertyToDictionary(subtypePropertyInfo, propertyValue));
            }

            return propertyValue;
        }

        #endregion Reflection
        #region DateTime

        public static bool ExpiredSince(this DateTime dateTime, int minutes) {
            return (dateTime - DateTime.Now).TotalMinutes < minutes;
        }

        public static TimeSpan StripMilliseconds(this TimeSpan time) {
            return new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);
        }

        #endregion DateTime
        #region DirectoryInfo

        public static DirectoryInfo Combine(this Environment.SpecialFolder specialFolder, params string[] paths) => Combine(new DirectoryInfo(Environment.GetFolderPath(specialFolder)), paths);

        public static FileInfo CombineFile(this Environment.SpecialFolder specialFolder, params string[] paths) => CombineFile(new DirectoryInfo(Environment.GetFolderPath(specialFolder)), paths);

        public static DirectoryInfo Combine(this DirectoryInfo dir, params string[] paths) {
            var final = dir.FullName;
            foreach (var path in paths) {
                final = Path.Combine(final, path.ReplaceInvalidFileNameChars());
            }
            return new DirectoryInfo(final);
        }

        public static FileInfo CombineFile(this DirectoryInfo dir, params string[] paths) {
            var final = dir.FullName;
            foreach (var path in paths) {
                final = Path.Combine(final, path);
            }
            return new FileInfo(final);
        }

        public static void ShowInExplorer(this DirectoryInfo dir) {
            Utils.StartProcess("explorer.exe", null, dir.FullName.Quote());
        }

        public static string PrintablePath(this FileSystemInfo file) => file.FullName.Replace(@"\\", @"\");

        #endregion DirectoryInfo
        #region FileInfo

        public static FileInfo Backup(this FileInfo file, bool overwrite = true, string extension = ".bak") {
            return file.CopyTo(file.FullName + extension, overwrite);
        }

        public static FileInfo Combine(this FileInfo file, params string[] paths) {
            var final = file.DirectoryName;
            foreach (var path in paths) {
                final = Path.Combine(final, path);
            }
            return new FileInfo(final);
        }

        public static string FileNameWithoutExtension(this FileInfo file) {
            return Path.GetFileNameWithoutExtension(file.Name);
        }
        public static string Extension(this FileInfo file) {
            return Path.GetExtension(file.Name);
        }

        public static void AppendLine(this FileInfo file, string line) {
            try {
                if (!file.Exists) file.Create();
                File.AppendAllLines(file.FullName, new string[] { line });
            } catch { }
        }

        public static void WriteAllText(this FileInfo file, string text) {
            file.Directory.Create();
            if (!file.Exists) file.Create().Close();
            File.WriteAllText(file.FullName, text);
        }

        public static string ReadAllText(this FileInfo file) => File.ReadAllText(file.FullName);

        public static List<string> ReadAllLines(this FileInfo file) => File.ReadAllLines(file.FullName).ToList();

        public static void ShowInExplorer(this FileInfo file) {
            Utils.StartProcess("explorer.exe", null, "/select, " + file.FullName.Quote());
        }

        #endregion FileInfo
        #region Object
        public static string ToJSON(this object obj, bool indented = true) {
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions {
                WriteIndented = indented,
                Converters =
            {
                    new JsonStringEnumConverter(),
                    new IPAddressConverter(),
                    new IPEndPointConverter()
                }
            });
        }
        #endregion Object
        #region Int

        public static int Percentage(this int total, int part) {
            return (int)((double)part / total * 100);
        }

        #endregion Int
        #region Dict

        public static void AddSafe(this IDictionary<string, string> dictionary, string key, string value) {
            if (!dictionary.ContainsKey(key))
                dictionary.Add(key, value);
        }

        #endregion Dict
        #region List

        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)
    => self.Select((item, index) => (item, index));

        public static string ToQueryString(this NameValueCollection nvc) {
            if (nvc == null) return string.Empty;

            StringBuilder sb = new StringBuilder();

            foreach (string key in nvc.Keys) {
                if (string.IsNullOrWhiteSpace(key)) continue;

                string[] values = nvc.GetValues(key);
                if (values == null) continue;

                foreach (string value in values) {
                    sb.Append(sb.Length == 0 ? "?" : "&");
                    sb.AppendFormat("{0}={1}", key, value);
                }
            }

            return sb.ToString();
        }

        public static bool GetBool(this NameValueCollection collection, string key, bool defaultValue = false) {
            if (!collection.AllKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) return false;
            var trueValues = new string[] { true.ToString(), "yes", "1" };
            if (trueValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
            var falseValues = new string[] { false.ToString(), "no", "0" };
            if (falseValues.Contains(collection[key], StringComparer.OrdinalIgnoreCase)) return true;
            return defaultValue;
        }

        public static string GetString(this NameValueCollection collection, string key) {
            if (!collection.AllKeys.Contains(key)) return collection[key];
            return null;
        }

        public static string Join(this List<string> strings, string separator) {
            return string.Join(separator, strings);
        }

        public static T PopFirst<T>(this IEnumerable<T> list) => list.ToList().PopAt(0);

        public static T PopLast<T>(this IEnumerable<T> list) => list.ToList().PopAt(list.Count() - 1);

        public static T PopAt<T>(this List<T> list, int index) {
            T r = list.ElementAt<T>(index);
            list.RemoveAt(index);
            return r;
        }

        #endregion List
        #region Uri

        public static bool ContainsKey(this NameValueCollection collection, string key) {
            if (collection.Get(key) == null) {
                return collection.AllKeys.Contains(key);
            }

            return true;
        }

        public static NameValueCollection ParseQueryString(this Uri uri) {
            return HttpUtility.ParseQueryString(uri.Query);
        }

        public static void OpenIn(this Uri uri, string browser) {
            var url = uri.ToString();
            try {
                Process.Start(browser, url);
            } catch {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start \"{browser}\" {url}") { CreateNoWindow = true });
            }
        }

        public static void OpenInDefault(this Uri uri) {
            var url = uri.ToString();
            try {
                Process.Start(url);
            } catch {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
        }
        public static FileInfo Download(this Uri url, DirectoryInfo destinationPath, string? fileName = null) {
            fileName = fileName ?? url.AbsolutePath.Split("/").Last();
            Console.WriteLine("todo download");
            return new FileInfo(Path.Combine(destinationPath.FullName, fileName));
        }
        #endregion Uri
        #region Enum

        public static string GetDescription(this Enum value) {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null) {
                FieldInfo field = type.GetField(name);
                if (field != null) {
                    DescriptionAttribute attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null) {
                        return attr.Description;
                    }
                }
            }
            return null;
        }

        public static T GetValueFromDescription<T>(string description, bool returnDefault = false) {
            var type = typeof(T);
            if (!type.IsEnum) throw new InvalidOperationException();
            foreach (var field in type.GetFields()) {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null) {
                    if (attribute.Description == description)
                        return (T)field.GetValue(null);
                } else {
                    if (field.Name == description)
                        return (T)field.GetValue(null);
                }
            }
            if (returnDefault) return default(T);
            else throw new ArgumentException("Not found.", "description");
        }

        #endregion Enum
        #region Task

        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource()) {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task) {
                    timeoutCancellationTokenSource.Cancel();
                    return await task;
                } else {
                    return default(TResult);
                }
            }
        }

        #endregion Task
    }
}
#endregion
#region json
namespace Bluscream {
public static class JsonUtils {
    public static T FromJson<T>(string jsonText) => JsonSerializer.Deserialize<T>(jsonText, Converter.Settings);
    public static T FromJsonFile<T>(FileInfo file) => FromJson<T>(File.ReadAllText(file.FullName));
    public static string ToJson<T>(this T self) => JsonSerializer.Serialize(self, Converter.Settings);
    public static void ToFile<T>(this T self, FileInfo file) => File.WriteAllText(file.FullName, ToJson(self));
}
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
public class IPAddressConverter : JsonConverter<IPAddress> {
    public override IPAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return IPAddress.Parse(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options) {
        writer.WriteStringValue(value.ToString());
    }
}
public class IPEndPointConverter : JsonConverter<IPEndPoint> {
    public override IPEndPoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var ipEndPointString = reader.GetString();
        var endPointParts = ipEndPointString.Split(':');
        var ip = IPAddress.Parse(endPointParts[0]);
        var port = int.Parse(endPointParts[1]);
        return new IPEndPoint(ip, port);
    }

    public override void Write(Utf8JsonWriter writer, IPEndPoint value, JsonSerializerOptions options) {
        writer.WriteStringValue(value.ToString());
    }
}
}
#endregion