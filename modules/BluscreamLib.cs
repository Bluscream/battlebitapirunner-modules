using BattleBitAPI.Common;
using BattleBitAPI.Server;
using BBRAPIModules;
using Bluscream;
using Discord;
using Discord.Webhook;
using Humanizer;
using log4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Bluscream {
    #region Requires
    //[RequireModule(typeof(Bluscream.GeoApi))]
    //[RequireModule(typeof(Bluscream.SteamApi))]
    [RequireModule(typeof(DevMinersBBModules.ModuleUsageStats))]
    [RequireModule(typeof(Permissions.GranularPermissions))]
    #endregion
    [Module("Bluscream's Library", "2.0.2")]
    public class BluscreamLib : BattleBitModule {
        public static ModuleInfo ModuleInfo = new() {
            Name = "Bluscream's Library",
            Description = "Generic library for common code used by multiple modules.",
            Version = new Version(2, 0, 2),
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
        public static new readonly ILog Logger = LogManager.GetLogger(typeof(BluscreamLib));
        #region Events
        #endregion
        #region EventHandlers
        public BluscreamLib() {
            Logger.Debug("Constructor called");
        }
        public override void OnModulesLoaded() {
            var MapsFile = new FileInfo(Config.MapsFile);
            try {
                Maps = MapList.FromUrl(Config.MapsUrl);
                Maps.ToJsonFile(MapsFile, true);
                Logger.Info($"Updated {MapsFile.Name} ({Maps.Count} maps) from {Config.MapsUrl}");
            } catch (Exception ex) {
                Logger.Info($"Unable to get new {MapsFile.Name}: {ex.Message}");
                Logger.Info($"Using {Config.MapsFile} if it exists");
                Maps = MapsFile.Exists ? MapList.FromFile(MapsFile) : new();
            }
            Logger.Info($"Loaded {Maps.Count} maps");

            var GameModesFile = new FileInfo(Config.GameModesFile);
            try {
                GameModes = GameModeList.FromUrl(Config.GameModesUrl);
                GameModes.ToJsonFile(GameModesFile, true);
                Logger.Info($"Updated {GameModesFile.Name} ({GameModes.Count} gamemodes)  from {Config.GameModesUrl}");
            } catch (Exception ex) {
                Logger.Info($"Unable to get new {GameModesFile.Name}: {ex.Message}");
                Logger.Info($"Using {Config.GameModesFile} if it exists");
                GameModes = GameModesFile.Exists ? GameModeList.FromFile(GameModesFile) : new();
            }
            Logger.Info($"Loaded {GameModes.Count} gamemodes");
        }
        #endregion
        #region Methods
        public static bool IsAlreadyRunning(string? appName = null) {
            if (string.IsNullOrWhiteSpace(appName)) {
                appName = Process.GetCurrentProcess().ProcessName;
            }
            Mutex m = new Mutex(false, appName);
            if (m.WaitOne(1, false) == false) {
                return true;
            }
            return false;
        }
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
        public static List<T> ResolveGameModeMapNameMatch<T>(string input, IEnumerable<T> matches) where T : BaseInfo {
            var lower = input.ToLowerInvariant().Trim();
            foreach (var match in matches) {
                if (lower == match.Name.ToLowerInvariant()) return new() { match };
                else if (lower == match.DisplayName.ToLowerInvariant()) return new() { match };
            }
            var result = new List<T>();
            foreach (var match in matches) {
                if (match.Name.ToLowerInvariant().Contains(lower)) result.Add(match);
                else if (match.DisplayName.ToLowerInvariant().Contains(lower)) result.Add(match);
            }
            return result;
        }

        public static MapDayNight? GetDayNightFromString(string input) {
            if (string.IsNullOrWhiteSpace(input)) return null;
            input = input.Trim().ToLowerInvariant();
            if (input.Contains("day")) return MapDayNight.Day;
            else if (input.Contains("night")) return MapDayNight.Night;
            return null;
        }
        public static MapSize GetMapSizeFromString(string input) {
            switch (input.Trim().ToLowerInvariant()) {
                case "tiny":
                case "8":
                case "8v8":
                case "_8v8":
                case "8vs8":
                    return MapSize._8v8;
                case "small":
                case "16":
                case "16v16":
                case "_16v16":
                case "16vs16":
                    return MapSize._16vs16;
                case "medium":
                case "32":
                case "32v32":
                case "_32v32":
                case "32vs32":
                    return MapSize._32vs32;
                case "big":
                case "large":
                case "64":
                case "64v64":
                case "_64v64":
                case "64vs64":
                    return MapSize._64vs64;
                case "ultra":
                case "127":
                case "127v127":
                case "_127v127":
                case "127vs127":
                    return MapSize._127vs127;
                default:
                    return MapSize.None;
            }
        }

        [Obsolete("Use this.Logger instead for non-static methods!")]
        public static void Log(object _msg, string source = "BluscreamLib") {
            var msg = _msg.ToString();
            if (string.IsNullOrWhiteSpace(msg)) return;
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {source} > {msg.Trim()}");
        }
        #endregion
        #region Data
        public static List<GameModeInfo> GameModes = new();
        public static IReadOnlyList<string> GameModeNames => GameModes.Where(m => m.Available == true).Select(m => m.Name).ToList() ?? new();
        public static IReadOnlyList<string> GameModeDisplayNames => Maps.Where(m => m.Available == true).Select(m => m.DisplayName ?? m.Name).ToList() ?? new();
        public static List<MapInfo> Maps = new();
        public static IReadOnlyList<string> MapNames => Maps.Where(m => m.Available == true).Select(m => m.Name).ToList() ?? new();
        public static IReadOnlyList<string> MapDisplayNames => Maps.Where(m => m.Available == true).Select(m => m.DisplayName ?? m.Name).ToList() ?? new();
        public static IReadOnlyList<string> MapSizeNames => (IReadOnlyList<string>)Enum.GetNames(typeof(MapSize));
        #endregion
        #region Configuration
        public static Configuration Config { get; set; } = null!;
        public class Configuration : ModuleConfiguration {
            public string TimeStampFormat { get; set; } = "HH:mm:ss";
            public string MapsFile { get; set; } = "data/maps.json";
            public Uri MapsUrl { get; set; } = new Uri("https://raw.githubusercontent.com/Bluscream/battlebitapirunner-modules/master/data/maps.json");
            public string GameModesFile { get; set; } = "data/gamemodes.json";
            public Uri GameModesUrl { get; set; } = new Uri("https://raw.githubusercontent.com/Bluscream/battlebitapirunner-modules/master/data/gamemodes.json");
        }
        #endregion
    }

    #region Extensions
    public static partial class Extensions {
        #region Stats
        public static string str(this PlayerStats stats) => $"Banned: {stats.IsBanned.ToYesNo()} | Roles: {stats.Roles.ToRoleString()} | PlayTime: {TimeSpan.FromSeconds(stats.Progress.PlayTimeSeconds).Humanize()} | Rank: {stats.Progress.Rank} | Prestige: {stats.Progress.Prestige} | XP: {stats.Progress.EXP}";
        #endregion
        #region Team
        public static string ToCountryCode(this Team team) => team == Team.TeamA ? "US" : "RU";
        #endregion
        #region Squad
        public static char ToLetter(this Squads squad) => squad.ToString()[0];
        public static char ToLetter(this Squad<RunnerPlayer> squad) => squad.Name.ToLetter();
        #endregion
        #region Events
        public delegate void PlayerKickedHandler(object targetPlayer, string? reason);
        public static event PlayerKickedHandler OnPlayerKicked = delegate { };
        #endregion
        #region Roles
        public static string ToRoleString(this Roles roles) => string.Join(",", roles.ToRoleStringList());
        public static List<string> ToRoleStringList(this Roles roles) {
            var roleStrings = new List<string>();
            if (roles == Roles.None) {
                roleStrings.Add("None");
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
            if (string.IsNullOrEmpty(rolesString)) {
                return Roles.None;
            }
            if (rolesString.Equals("All", StringComparison.OrdinalIgnoreCase)) {
                return Roles.Admin | Roles.Moderator | Roles.Vip | Roles.Special;
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
        public static Roles ParseRoles(this List<string>? rolesList) {
            if (rolesList is null || rolesList.Count == 0) {
                return Roles.None;
            }
            if (rolesList.Any(role => role.Equals("All", StringComparison.OrdinalIgnoreCase))) {
                return Roles.Admin | Roles.Moderator | Roles.Vip | Roles.Special;
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
        public static string str(this RunnerServer server) => $"\"{server.ServerName}\" ({server.AllPlayers.Count()} players)";
        public static void SayToTeamChat(this RunnerServer server, Team team, string message) {
            foreach (var player in server.AllPlayers) {
                if (player.Team == team)
                    player.SayToChat(message);
            }
        }
        public static void SayToSquadChat(this RunnerServer server, Team team, Squads squad, string message) {
            foreach (var player in server.AllPlayers) {
                if (player.Team == team && player.Squad.Name == squad)
                    player.SayToChat(message);
            }
        }
        public static IEnumerable<string> GetPlayerNamesBySteamId64(this RunnerServer server, ulong steamId64) => GetPlayersBySteamId64(server, steamId64).Select(p => p.Name);
        public static IEnumerable<RunnerPlayer> GetPlayersBySteamId64(this RunnerServer server, ulong steamId64) => server.AllPlayers.Where(p => p.SteamID == steamId64);
        public static IEnumerable<RunnerPlayer> GetPlayersByIp(this RunnerServer server, IPAddress ip) => server.AllPlayers.Where(p => p.IP == ip);
        public static MapInfo GetCurrentMap(this RunnerServer server) => BluscreamLib.Maps.Where(p => p.Name == server.Map).First();
        public static GameModeInfo GetCurrentGameMode(this RunnerServer server) => BluscreamLib.GameModes.Where(p => p.Name == server.Gamemode).First();
        public static void Kick(this RunnerServer server, ulong steamId64, string? reason = null) {
            BluscreamLib.Logger.Warn($"Kicking Player {steamId64} for \"{reason}\"");
            server.Kick(steamId64, reason);
            OnPlayerKicked?.Invoke(steamId64, reason);
        }
        #endregion
        #region Player
        public static string str(this RunnerPlayer player) => $"\"{player.Name}\"";
        public static string fullstr(this RunnerPlayer player) => $"{player.str()} ({player.SteamID})";
        public static void Kick(this BattleBitAPI.Player<RunnerPlayer> player, string? reason = null) => Kick(player as RunnerPlayer, reason);
        public static void Kick(this RunnerPlayer player, string? reason = null) {
            BluscreamLib.Logger.Warn($"Kicking Player {player.str()} for \"{reason}\"");
            player.Kick(reason);
            OnPlayerKicked?.Invoke(player, reason);
        }
        #region Permissions
        //public static Roles GetRoles(this RunnerPlayer player, Permissions.PlayerPermissions permissionsModule) => permissionsModule.GetPlayerRoles(player.SteamID);
        //public static bool HasRole(this RunnerPlayer player, Permissions.PlayerPermissions permissionsModule, Roles role) => permissionsModule.HasPlayerRole(player.SteamID, role);
        //public static bool HasAnyRoleOf(this RunnerPlayer player, Permissions.PlayerPermissions permissionsModule, Roles needsAnyRole) => needsAnyRole > 0 && (player.GetRoles(permissionsModule) & needsAnyRole) != 0;
        //public static bool HasNoRoleOf(this RunnerPlayer player, Permissions.PlayerPermissions permissionsModule, Roles needsNoRole) => needsNoRole > 0 && (player.GetRoles(permissionsModule) & needsNoRole) == 0;
        //public static bool HasAllRolesOf(this RunnerPlayer player, Permissions.PlayerPermissions permissionsModule, Roles needsAllRole) => needsAllRole > 0 && (player.GetRoles(permissionsModule) & needsAllRole) == needsAllRole;
        //public static bool HasOnlyThisRole(this RunnerPlayer player, Permissions.PlayerPermissions permissionsModule, Roles role) => role > 0 && player.GetRoles(permissionsModule) == role;
        //public static bool HasOnlyTheseRoles(this RunnerPlayer player, Permissions.PlayerPermissions permissionsModule, Roles roles) => player.HasOnlyTheseRoles(permissionsModule, roles);
        public static List<string> GetPlayerPermissions(this RunnerPlayer player, Permissions.GranularPermissions permissionsModule) => permissionsModule.GetPlayerPermissions(player.SteamID).ToList();
        public static List<string> GetAllPlayerPermissions(this RunnerPlayer player, Permissions.GranularPermissions permissionsModule) => permissionsModule.GetAllPlayerPermissions(player.SteamID).ToList();
        public static bool HasAnyPermissionOf(this RunnerPlayer player, Permissions.GranularPermissions permissionsModule, List<string> needsAnyPermission) => player.GetAllPlayerPermissions(permissionsModule).ContainsAny(needsAnyPermission.ToArray());
        public static bool HasAllPermissionsOf(this RunnerPlayer player, Permissions.GranularPermissions permissionsModule, List<string> needsAllPermissions) => player.GetAllPlayerPermissions(permissionsModule).ContainsAll(needsAllPermissions.ToArray());
        public static List<string> GetPlayerGroups(this RunnerPlayer player, Permissions.GranularPermissions permissionsModule) => permissionsModule.GetPlayerGroups(player.SteamID).ToList();
        public static bool HasAnyGroupOf(this RunnerPlayer player, Permissions.GranularPermissions permissionsModule, List<string> needsAnyGroup) => player.GetPlayerGroups(permissionsModule).ContainsAny(needsAnyGroup.ToArray());
        public static bool HasAllGroupsOf(this RunnerPlayer player, Permissions.GranularPermissions permissionsModule, List<string> needsAllGroups) => player.GetPlayerGroups(permissionsModule).ContainsAll(needsAllGroups.ToArray());
        #endregion
        public static void SayToTeamChat(this RunnerPlayer player, RunnerServer server, string message) => server.SayToTeamChat(player.Team, message);
        public static void SayToSquadChat(this RunnerPlayer player, RunnerServer server, string message) => server.SayToSquadChat(player.Team, player.SquadName, message);
        #endregion
        #region GameServer
        public static void SayToTeamChat(this GameServer<RunnerPlayer> server, Team team, string message) {
            foreach (var player in server.AllPlayers) {
                if (player.Team == team)
                    player.SayToChat(message);
            }
        }
        public static void SayToSquadChat(this GameServer<RunnerPlayer> server, Team team, Squads squad, string message) {
            foreach (var player in server.AllPlayers) {
                if (player.Team == team && player.Squad.Name == squad)
                    player.SayToChat(message);
            }
        }
        public static void Kick(this GameServer<RunnerPlayer> server, ulong steamId64, string? reason = null) {
            BluscreamLib.Logger.Warn($"Kicking Player {steamId64} for \"{reason}\"");
            server.Kick(steamId64, reason);
            OnPlayerKicked?.Invoke(steamId64, reason);
        }
        #endregion
        #region Squad
        public static void SayToChat(this Squad<RunnerPlayer> squad, string message) => squad.Server.SayToSquadChat(squad.Team, squad.Name, message);
        #endregion
        #region Map
        public static void ChangeTime(this RunnerServer Server, MapDayNight? dayNight = null) => ChangeMap(Server, dayNight: dayNight);
        public static void ChangeGameMode(this RunnerServer Server, GameModeInfo? gameMode = null, MapDayNight? dayNight = null, MapSize mapSize = MapSize.None) => ChangeMap(Server, gameMode: gameMode, dayNight: dayNight, mapSize: mapSize);
        //public static void ChangeMap(this RunnerServer Server, MapInfo? map = null, GameModeInfo? gameMode = null, string? dayNight = null, MapSize mapSize = MapSize.None) => ChangeMap(Server, map, gameMode, dayNight?.ParseDayNight(), mapSize: mapSize);
        public static void ChangeMap(this RunnerServer Server, MapInfo? map = null, GameModeInfo? gameMode = null, MapDayNight? dayNight = null, MapSize mapSize = MapSize.None) {
            map = map ?? MapInfo.FromName(Server.Map);
            gameMode = gameMode ?? GameModeInfo.FromName(Server.Gamemode);
            //dayNight = dayNight ?? Server.DayNight;

            if (mapSize != MapSize.None) Server.SetServerSizeForNextMatch(mapSize);

            var oldMaps = Server.MapRotation.GetMapRotation();
            Server.MapRotation.SetRotation(map.Name);
            var oldModes = Server.GamemodeRotation.GetGamemodeRotation();
            Server.GamemodeRotation.SetRotation(gameMode.Name);

            var oldVoteDay = Server.ServerSettings.CanVoteDay;
            var oldVoteNight = Server.ServerSettings.CanVoteNight;
            if (dayNight is not null) {
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
            if (dayNight is not null) msg.Append($" [{dayNight}]");
            if (gameMode is not null) msg.Append($" ({gameMode.DisplayName})");

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

        public static string FromFile(FileInfo file) => file.ReadAllText();
        public static void ToFile(this string self, FileInfo file) {
            file?.Directory?.Create();
            file?.WriteAllText(self);
        }

        public static bool ContainsAny(this string input, IEnumerable<string> values, StringComparison stringComparison = StringComparison.Ordinal) => values.Any(value => input.Contains(value, stringComparison));
        public static string SanitizeDiscord(this string input) => input.Replace("`", "`\\`").Replace("@", "\\@");
        public static string ReplaceDiscord(this string input, string key, object? replacement) {
            if (replacement is null) return input;
            return input.Replace($"{{{key}}}", replacement?.ToString()?.SanitizeDiscord());
        }
        public static bool EvalToBool(this string expression) {
            System.Data.DataTable table = new System.Data.DataTable();
            table.Columns.Add("expression", string.Empty.GetType(), expression);
            System.Data.DataRow row = table.NewRow();
            table.Rows.Add(row);
            return bool.Parse((string)row["expression"]);
        }
        public static double EvalToDouble(this string expression) {
            System.Data.DataTable table = new System.Data.DataTable();
            table.Columns.Add("expression", string.Empty.GetType(), expression);
            System.Data.DataRow row = table.NewRow();
            table.Rows.Add(row);
            return double.Parse((string)row["expression"]);
        }
        public static string EvalToString(this string expression) {
            System.Data.DataTable table = new System.Data.DataTable();
            table.Columns.Add("expression", string.Empty.GetType(), expression);
            System.Data.DataRow row = table.NewRow();
            table.Rows.Add(row);
            return (string)row["expression"];
        }
        public static int EvalToInt(this string expression) {
            System.Data.DataTable table = new System.Data.DataTable();
            table.Columns.Add("expression", string.Empty.GetType(), expression);
            System.Data.DataRow row = table.NewRow();
            table.Rows.Add(row);
            return int.Parse((string)row["expression"]);
        }

        public static ParsedPlayer ParsePlayer(this string input, PlayerFinder.PlayerFinder playerFinder, RunnerServer? server = null) {
            if (string.IsNullOrWhiteSpace(input)) return null!;
            if (playerFinder.ByNamePart(input) is RunnerPlayer _player) return new ParsedPlayer(player: _player, server: server);
            if (IPAddress.TryParse(input, out var ip)) return new ParsedPlayer(ip: ip, server: server);
            if (ulong.TryParse(input, out var steamId64)) return new ParsedPlayer(steamId64: steamId64, server: server);
            return null;
        }

        //public static object? ParsePlayer(this string input, PlayerFinder.PlayerFinder playerFinder) {
        //    if (string.IsNullOrWhiteSpace(input)) return null!;
        //    if (playerFinder.ByNamePart(input) is RunnerPlayer _player) return _player;
        //    if (IPAddress.TryParse(input, out var ip)) return ip;
        //    if (ulong.TryParse(input, out var steamId64)) return steamId64;
        //    return null;
        //}

        public static MapInfo? ToMap(this string mapName) => BluscreamLib.Maps.Where(m => m.Name.ToLowerInvariant() == mapName.ToLowerInvariant()).First();
        public static List<MapInfo> ParseMap(this string input) => BluscreamLib.ResolveGameModeMapNameMatch(input, BluscreamLib.Maps);
        public static GameModeInfo? ToGameMode(this string gameModeName) => BluscreamLib.GameModes.Where(m => m.Name.ToLowerInvariant() == gameModeName.ToLowerInvariant()).First();
        public static List<GameModeInfo> ParseGameMode(this string input) => BluscreamLib.ResolveGameModeMapNameMatch(input, BluscreamLib.GameModes);
        public static MapDayNight? ParseDayNight(this string input) => BluscreamLib.GetDayNightFromString(input);
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
        public static bool ContainsAll(this string value, params string[] values) {
            foreach (string one in values) {
                if (!value.Contains(one)) {
                    return false;
                }
            }
            return true;
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

        public static string PrintablePath(this FileSystemInfo file) => file.FullName.Replace(@"\\", @"\");

        #endregion DirectoryInfo
        #region FileInfo

        public static string GetMd5Hash(this FileInfo file) {
            using var md5 = MD5.Create();
            using var stream = file.OpenRead();
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

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
            //if (!file.Exists) file.Create().Close();
            File.WriteAllText(file.FullName, text);
        }

        public static void WriteAllBytes(this FileInfo file, byte[] bytes) {
            file.Directory.Create();
            //if (!file.Exists) file.Create().Close();
            File.WriteAllBytes(file.FullName, bytes);
        }

        public static string ReadAllText(this FileInfo file) => File.ReadAllText(file.FullName);

        public static List<string> ReadAllLines(this FileInfo file) => File.ReadAllLines(file.FullName).ToList();

        #endregion FileInfo
        #region Object

        public static void Reply(this BattleBitModule module, object obj, RunnerPlayer? targetPlayer) {
            var msg = obj.ToString();
            if (msg is null) return;
            if (targetPlayer is null) { module.Logger.Info(msg); return; }
            if (msg.ContainsAny(new[] { "\n", "<br>" }, StringComparison.OrdinalIgnoreCase) || msg.Length > 250) targetPlayer.Message(msg);
            else targetPlayer.SayToChat(msg);
        }
        public static Embed ToDiscordEmbed(this object obj) {
            var embed = new EmbedBuilder()
                .WithTimestamp(DateTimeOffset.UtcNow)
                .WithFooter(new EmbedFooterBuilder() { Text = obj.GetType().Name });
            foreach (var prop in obj.GetType().GetProperties()) {
                var value = prop.GetValue(obj);
                if (value is not null) {
                    embed.AddField(prop.Name, value?.ToString() ?? string.Empty);
                }
            }
            foreach (var prop in obj.GetType().GetFields()) {
                var value = prop.GetValue(obj);
                if (value is not null) {
                    embed.AddField(prop.Name, value?.ToString() ?? string.Empty);
                }
            }
            return embed.Build();
        }
        public static string? GetMd5Hash(this object obj) {
            if (obj is null) return null;
            using (MD5 md5 = MD5.Create()) {
                byte[] inputBytes = Encoding.UTF8.GetBytes(obj.ToJson(false));
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++) {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString().ToLowerInvariant();
            }
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

        public static string Join(this IEnumerable<string> strings, string separator) {
            return string.Join(separator, strings);
        }

        public static T PopFirst<T>(this IEnumerable<T> list) => list.ToList().PopAt(0);
        public static T PopLast<T>(this IEnumerable<T> list) => list.ToList().PopAt(list.Count() - 1);
        public static T PopAt<T>(this List<T> list, int index) {
            T r = list.ElementAt<T>(index);
            list.RemoveAt(index);
            return r;
        }

        public static bool ContainsAll<T>(this IEnumerable<T> values, List<T> value) => ContainsAll(values, value.ToArray());
        public static bool ContainsAll<T>(this IEnumerable<T> values, T[] value) {
            foreach (T one in value) {
                if (!values.Contains(one)) {
                    return false;
                }
            }
            return true;
        }
        public static bool ContainsAny<T>(this IEnumerable<T> values, List<T> value) => ContainsAny(values, value.ToArray());
        public static bool ContainsAny<T>(this IEnumerable<T> values, T[] value) {
            return value.Any(values.Contains);
        }



        #endregion List
        #region Uri
        public static async Task SendWebhook(this Uri url, string jsonContent) {
            using (var httpClient = new HttpClient()) {
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(url, content);
                if (!response.IsSuccessStatusCode) {
                    throw new Exception($"Error sending webhook: {response.StatusCode}");
                }
            }
        }
        public static async Task SendDiscordWebhook(this Uri url, DiscordEmbed discordEmbed) {
            using (var client = new DiscordWebhookClient(url.ToString())) {
                var embed = new EmbedBuilder()
                    .WithTitle(discordEmbed.Title)
                    .WithDescription(discordEmbed.Description)
                    .WithTimestamp(DateTimeOffset.UtcNow)
                    .WithFooter(new EmbedFooterBuilder() { Text = discordEmbed.Footer });
                foreach (var field in discordEmbed.Fields) {
                    embed.AddField(field.Name, field.Value, field.Inline);
                }
                await client.SendMessageAsync(embeds: new[] { embed.Build() });
            }
        }
        public static bool ContainsKey(this NameValueCollection collection, string key) {
            if (collection.Get(key) == null) {
                return collection.AllKeys.Contains(key);
            }

            return true;
        }
        public static NameValueCollection ParseQueryString(this Uri uri) {
            return HttpUtility.ParseQueryString(uri.Query);
        }
        public static Uri AddQuery(this Uri uri, string name, string value) {
            var httpValueCollection = uri.ParseQueryString();
            httpValueCollection.Remove(name);
            httpValueCollection.Add(name, value);
            var ub = new UriBuilder(uri);
            ub.Query = httpValueCollection.ToString();
            return ub.Uri;
        }
        public static Uri RemoveQuery(this Uri uri, string name) {
            var httpValueCollection = uri.ParseQueryString();
            httpValueCollection.Remove(name);
            var ub = new UriBuilder(uri);
            ub.Query = httpValueCollection.ToString();
            return ub.Uri;
        }
        public static FileInfo Download(this Uri url, DirectoryInfo destinationPath, string? fileName = null) {
            fileName = fileName ?? url.AbsolutePath.Split("/").Last();
            Console.WriteLine("todo download");
            return new FileInfo(Path.Combine(destinationPath.FullName, fileName));
        }
        #endregion Uri
        #region Enum
        public static string? GetDescription(this Enum value) {
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

        public static T? GetValueFromDescription<T>(string description, bool returnDefault = false) {
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
        public static async Task<TResult?> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {
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
        #region EventHandler
        static public void RaiseEvent(this EventHandler @event, object sender, EventArgs e) {
            if (@event != null)
                @event(sender, e);
        }
        static public void RaiseEvent<T>(this EventHandler<T> @event, object sender, T e)
            where T : EventArgs {
            if (@event != null)
                @event(sender, e);
        }
        #endregion
        #region Process
        public static Process? Start(this ProcessStartInfo processStartInfo) => Process.Start(processStartInfo);
        public static void Exit(this Process process) {
            process.CloseMainWindow();
            process.Close();
            process.Kill();
        }
        public static void Start(this Process process, string? args = null) {
            var startInfo = new ProcessStartInfo() {
                FileName = process.MainModule.FileName,
                Arguments = args
            };
            startInfo.Start();
        }
        public static void Start(this Process process, IEnumerable<string>? args = null) => process.Start(args?.Join(" "));
        public static void Restart(this Process process) {
            process.Start(process.GetCommandLineList());
            process.Exit();
        }
        public static string GetCommandLine(this Process process) {
            Console.WriteLine($"Getting command line for process: {process.Id}");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                Console.WriteLine("Platform is Windows");
                using (var searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
                using (var objects = searcher.Get()) {
                    Debug.WriteLine($"got searcher & objects");
                    var obj = objects.Cast<ManagementBaseObject>().SingleOrDefault();
                    Debug.WriteLine($"got obj");
                    string commandLine = obj?["CommandLine"]?.ToString();
                    Debug.WriteLine($"Command line: {commandLine}");
                    return commandLine;
                }
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                Console.WriteLine("Platform is Linux");
                try {
                    string commandLine = File.ReadAllText($"/proc/{process.Id}/cmdline");
                    Console.WriteLine($"Command line: {commandLine}");
                    return commandLine;
                } catch (Exception ex) {
                    Console.WriteLine($"Failed to get command line: {ex}");
                    return "";
                }
            } else {
                Debug.WriteLine("Platform is not supported");
                throw new PlatformNotSupportedException();
            }
        }
        public static List<string> GetCommandLineList(this Process process) => Regex.Matches(GetCommandLine(process), @"[\""].+?[\""]|[^ ]+").Cast<Match>().Select(m => m.Value).ToList();
    }
    #endregion
    #endregion
    #region json
    public static class JsonUtils {
        public static T FromUrl<T>(Uri url) {
            using (var client = new HttpClient()) {
                var response = client.GetAsync(url.ToString()).Result;
                return FromJson<T>(response.Content.ReadAsStringAsync().Result);
            }
        }
        public static T FromJson<T>(string jsonText) => JsonSerializer.Deserialize<T>(jsonText, Converter.Settings);
        public static T FromJsonFile<T>(FileInfo file) => FromJson<T>(file.ReadAllText());
        public static string ToJson<T>(this T self, bool indented = false) => JsonSerializer.Serialize(self, new JsonSerializerOptions(Converter.Settings) { WriteIndented = indented });
        public static void ToJsonFile<T>(this T self, FileInfo file, bool indented = false) {
            file?.Directory?.Create();
            file?.WriteAllText(ToJson(self, indented));
        }
    }
    public static class Converter {
        public static readonly JsonSerializerOptions Settings = new(JsonSerializerDefaults.General) {
            Converters = {
                 new JsonStringEnumConverter(),
                //new ParseStringConverter(),
                //new DateOnlyConverter(),
                //new TimeOnlyConverter(),
                IsoDateTimeOffsetConverter.Singleton,
                new IPAddressConverter(),
                new IPEndPointConverter()
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
    #endregion
    #region Defines
    public static class Runner {
        public static Process Process => Process.GetCurrentProcess();
        public static string Name => System.IO.Path.GetFileNameWithoutExtension(Process.MainModule.ModuleName);
        public static string WindowTitle => Process.MainWindowTitle;
        public static FileInfo Path { get; set; } = new FileInfo(Process.MainModule.FileName);
        public static Collection<string> CommandLine { get; set; } = new ProcessStartInfo() { Arguments = Environment.CommandLine }.ArgumentList;
        public static FileInfo DllFile = Path.Directory.CombineFile(Name.Ext("dll"));
        public static Version Version { get; set; } = FileVersionInfo.GetVersionInfo(DllFile.FullName).FileVersion.ToVersion() ?? FileVersionInfo.GetVersionInfo(DllFile.FullName).ProductVersion.ToVersion();
        public static Uri WebsiteUrl { get; set; } = new Uri("https://github.com/BattleBit-Community-Servers/BattleBitAPIRunner");
        public static Uri SupportUrl { get; set; } = new Uri("https://github.com/BattleBit-Community-Servers/BattleBitAPIRunner/issues/new");
        public static FileInfo AppSettingsFile = Path.Directory.CombineFile("appsettings.json");
        public static AppSettingsContent AppSettings = JsonSerializer.Deserialize<AppSettingsContent>(AppSettingsFile.ReadAllText());
        public static List<ModuleInfo> Modules { get; set; } = GetModuleInfoFromFiles(GetModuleFiles());

        public delegate void RunnerStoppingHandler();
        public static event RunnerStoppingHandler OnApiRunnerStopping = delegate { };
        public delegate void RunnerRestartingHandler(FileInfo path, Collection<string> commandLine);
        public static event RunnerRestartingHandler OnApiRunnerRestarting = delegate { };

        public static bool IsAlreadyRunning() => BluscreamLib.IsAlreadyRunning(Process.ProcessName);
        public static void Exit() {
            OnApiRunnerStopping?.Invoke();
            Process.Exit();
        }
        public static void Start(IEnumerable<string>? args = null) {
            Process.Start(args?.Join(" ") ?? Environment.CommandLine);
        }
        public static void Restart() {
            OnApiRunnerRestarting?.Invoke(Path, CommandLine);
            Process.Restart();
        }
        public static Dictionary<Process, List<string>> GetRunningGameServers() {
            Console.WriteLine("GetRunningGameServers");
            var processes = Process.GetProcessesByName("BattleBit").Concat(Process.GetProcessesByName("BattleBitEAC")).ToList();
            Console.WriteLine($"Got {processes.Count} processes");
            Dictionary<Process, List<string>> servers = new();
            foreach (var process in processes) {
                Console.WriteLine(process.MainModule.FileName);
                Console.WriteLine(process.MainModule.ModuleName);
                Console.WriteLine("test0");
                Console.WriteLine(process.GetCommandLine());
                Console.WriteLine("test1.5");
                Console.WriteLine(process.GetCommandLineList().ToJson());
                Console.WriteLine("test1");
                var cmdLine = process.GetCommandLineList();
                Console.WriteLine("test3");
                if (cmdLine.Contains("-batchmode"))
                    servers[process] = process.GetCommandLineList();
            }
            Console.WriteLine("test2");
            return servers;
        }
        public static Dictionary<Process, List<string>> GetRunningGameServersByApiPort(int? apiPort = null) {
            Console.WriteLine($"GetRunningGameServersByApiPort({apiPort})");
            apiPort = apiPort ?? AppSettings.Port;
            var allServers = GetRunningGameServers();
            Dictionary<Process, List<string>> servers = new();
            foreach (var (process, commandline) in allServers) {
                foreach (var arg in commandline) {
                    if (arg.Contains("-ApiEndPoint=") && arg.Contains($":{apiPort}"))
                        servers[process] = commandline;
                }
            }
            return servers;
        }
        public static Dictionary<Process, List<string>> GetRunningGameServersByName(string name) {
            Console.WriteLine("GetRunningGameServersByName");
            var allServers = GetRunningGameServers();
            Dictionary<Process, List<string>> servers = new();
            foreach (var (process, commandline) in allServers) {
                Console.WriteLine(process.MainModule.FileName);
                Console.WriteLine(process.MainModule.ModuleName);
                Console.WriteLine(process.MainWindowTitle);
                foreach (var arg in commandline) {
                    Console.WriteLine(arg);
                    if (arg.Contains("-Name=") && arg.Contains($":{name}"))
                        servers[process] = commandline;
                }
            }
            return servers;
        }

        internal static IEnumerable<FileInfo> GetModuleFilesFromFolder(DirectoryInfo directory) => directory.GetFiles("*.cs", SearchOption.TopDirectoryOnly).ToList();
        internal static IEnumerable<FileInfo> GetModuleFiles() {
            var moduleFiles = new List<FileInfo>();
            if (AppSettings?.ModulesPath != null)
                moduleFiles.AddRange(GetModuleFilesFromFolder(AppSettings.ModulesPath));
            if (AppSettings?.Modules == null) return moduleFiles;
            moduleFiles.AddRange(AppSettings.Modules.Where(file => file.Exists));
            return moduleFiles;
        }
        internal static KeyValuePair<string?, string?> GetVersionAndDescriptionFromFile(FileSystemInfo file) {
            var text = File.ReadAllText(file.FullName);
            var regex = new Regex(@"\[Module\(""(.*)"", ""(.*)""\)\]");
            var matches = regex.Matches(text);
            foreach (Match match in matches) return new(match.Groups[2].Value, match.Groups[1].Value);
            return new(null, null);
        }
        internal static List<ModuleInfo> GetModuleInfoFromFiles(IEnumerable<FileInfo> files) {
            List<ModuleInfo> modules = new();
            foreach (var file in files) {
                if (file.Extension.ToLowerInvariant() == ".cs") {
                    var (version, description) = GetVersionAndDescriptionFromFile(file);
                    modules.Add(ModuleInfo.FromFile(file, version, description));
                }
            }
            BluscreamLib.Log($"Loaded {modules.Count} modules's infos...");
            return modules;
        }

        public class AppSettingsContent {
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            [JsonPropertyName("IP")]
            public virtual string? Ip { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            [JsonPropertyName("Port")]
            public virtual int? Port { get; set; }

            [JsonPropertyName("IPAddress")]
            public virtual object? IpAddress { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            [JsonPropertyName("ModulesPath")]
            public virtual string _ModulesPath { get; set; }
            [JsonIgnore]
            public virtual DirectoryInfo ModulesPath => new DirectoryInfo(_ModulesPath);

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            [JsonPropertyName("Modules")]
            public virtual List<string> _Modules { get; set; } = new();
            [JsonIgnore]
            public virtual IEnumerable<FileInfo> Modules => _Modules.Select(m => new FileInfo(m));

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            [JsonPropertyName("DependencyPath")]
            public virtual string? DependencyPath { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            [JsonPropertyName("ConfigurationPath")]
            public virtual string? ConfigurationPath { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            [JsonPropertyName("LogLevel")]
            public virtual string? LogLevel { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            [JsonPropertyName("WarningThreshold")]
            public virtual long? WarningThreshold { get; set; }
        }
    }
    public class ParsedPlayer {
        public string? Name { get; set; }
        public ulong? SteamId64 { get; set; }
        public IPAddress? IP { get; set; }
        public RunnerPlayer? Player { get; set; }
        //#if DEBUG
        //        public SteamWebApi.Response? SteamData { get; set; }
        //        public IpApi.Response? GeoData { get; set; }
        //#else
        //        public dynamic? SteamData { get; set; }
        //        public dynamic? GeoData { get; set; }
        //#endif
        public ParsedPlayer(string? name = null, ulong? steamId64 = null, IPAddress? ip = null, RunnerPlayer? player = null, RunnerServer? server = null) {
            if (player is not null) {
                Name = player.Name;
                SteamId64 = player.SteamID;
                IP = player.IP;
                Player = player;
            }
            //if (IP is not null && GeoData is null) GeoData = GeoApi.GetData(IP)?.Result;
            //if (GeoData is not null) {
            //    IP = GeoData.Query;
            //    GeoData = GeoData;
            //}
            //if (SteamId64 is not null && SteamData is null) SteamData = SteamApi.Get(SteamId64.Value)?.Result;
            //if (SteamData is not null) {
            //    SteamId64 = SteamData.SteamId64;
            //    Name = SteamData.Summary?.DisplayName;
            //    SteamData = SteamData;
            //}
            if (server is not null) {
                if (Player is null && SteamId64 is not null) Player = server.GetPlayersBySteamId64(SteamId64.Value).FirstOrDefault();
                if (Player is null && IP is not null) Player = server.GetPlayersByIp(IP).FirstOrDefault();
            }
        }
    }
    public class ModuleInfo {
        public bool? Loaded { get; set; }
        public bool? Enabled { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public Version? Version { get; set; }
        public string? _Version { get; set; }
        public string? Author { get; set; }
        public Uri? WebsiteUrl { get; set; }
        public Uri? UpdateUrl { get; set; }
        public Uri? SupportUrl { get; set; }
        public FileInfo? Path { get; set; }
        public string? Hash { get; set; }
        public ModuleInfo() { }
        public ModuleInfo(string name, string description, Version version, string author, Uri websiteUrl, Uri updateUrl, Uri supportUrl) {
            Name = name;
            Description = description;
            Version = version;
            _Version = version.ToString();
            Author = author;
            WebsiteUrl = websiteUrl;
            UpdateUrl = updateUrl;
            SupportUrl = supportUrl;
        }
        public ModuleInfo(string name, string description, Version version, string author, string websiteUrl, string updateUrl, string supportUrl) :
            this(name, description, version, author, websiteUrl.ToUri(), updateUrl.ToUri(), supportUrl.ToUri()) { }
        public ModuleInfo(string name, string description, string version, string author, string websiteUrl, string updateUrl, string supportUrl) :
            this(name, description, version.ToVersion(), author, websiteUrl.ToUri(), updateUrl.ToUri(), supportUrl.ToUri()) { }

        public static ModuleInfo FromFile(FileInfo file, string version, string description) => new ModuleInfo() {
            Path = file,
            Name = file.FileNameWithoutExtension(),
            _Version = version ?? "Unknown",
            Version = version.ToVersion(),
            Description = description,
            Hash = file.GetMd5Hash()
        };
    }
    public struct DateTimeWithZone {
        private readonly DateTime utcDateTime;
        private readonly TimeZoneInfo timeZone;

        public DateTimeWithZone(DateTime dateTime, TimeZoneInfo timeZone) {
            var dateTimeUnspec = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
            utcDateTime = TimeZoneInfo.ConvertTimeToUtc(dateTimeUnspec, timeZone);
            this.timeZone = timeZone;
        }

        public DateTime UniversalTime { get { return utcDateTime; } }

        public TimeZoneInfo TimeZone { get { return timeZone; } }

        public DateTime LocalTime {
            get {
                return TimeZoneInfo.ConvertTime(utcDateTime, timeZone);
            }
        }
    }
    public class DiscordEmbed {
        public string Title { get; set; }
        public string Description { get; set; }
        public List<EmbedField> Fields { get; set; }
        public string Timestamp { get; set; }
        public string Footer { get; set; }

        public DiscordEmbed(object obj) {
            Title = obj.GetType().Name;
            Fields = new List<EmbedField>();
            Timestamp = DateTime.UtcNow.ToString("o");
            foreach (var prop in obj.GetType().GetProperties()) {
                var value = prop.GetValue(obj);
                if (value is not null) {
                    Fields.Add(new EmbedField { Name = prop.Name, Value = value?.ToString() ?? string.Empty });
                }
            }
            foreach (var prop in obj.GetType().GetFields()) {
                var value = prop.GetValue(obj);
                if (value is not null) {
                    Fields.Add(new EmbedField { Name = prop.Name, Value = value?.ToString() ?? string.Empty });
                }
            }
        }
        public class EmbedField {
            public string Name { get; set; } = null!;
            public string Value { get; set; } = null!;
            public bool Inline { get; set; } = false;
        }
    }
    public enum DisconnectReason {
        Unknown = 0,
        RemoteConnectionRequestedToTerminate = 1,
        Timeout = 2,
        ManyExceptions = 3,
        TooManyUnverifiedPackages = 4,
        WeirdPackageIndex = 5,
        SyncPackageWasNull = 6,
        ServerIsEndingGame = 7,
        BannedFromServer = 8,
        KickedFromServer = 9,
        AntiCheatAuthFail = 10,
        ServerChangedMap = 11,
        WasAlreadyInGame = 12,
        VersionDoesNotMatch = 13,
        CorruptedGameFiles = 14,
        ServerFull = 15,
        PingLimit = 16,
        AfkLimit = 17
    }
    public enum NetworkCommuncation : byte {
        NetworkPadding = 0,
        ClientConnected = 1,
        ClientDisconnected = 2,
        RPC = 3,
        Stream = 4,
        SpawnObject = 5,
        DestroyObject = 6,
        VoiceOverIP = 7,
        PlayerSpawn = 8,
        ForceSpawn = 9,
        VehicleStream = 10,
        PlayerPositionStream = 11,
        BodyHitRequest = 12,
        ArmorHitRequest = 13,
        SetTime = 14,
        SpawnVehicle = 15,
        SavePrefs = 16,
        GadgetRPC = 17,
        CombinedReliable = 18,
        ThrownGadgetStream = 19,
        ReplicaGadgetStream = 20,
        VehicleDebrisStream = 21,
        VehicleSeatBehaviourStream = 22,
        PlayerPerspective = 23,
        MagazineStream = 24,
        DroneStream = 25,
        ThrowableStream = 26,
        PickableStream = 27,
        TreeStream = 28,
        SupplyBoxStream = 29,
        AntiCheatSnapshot = 30,
        ServerSnapshot = 31,
        ClientTick = 32,
        ProjectileExplosionRequest = 33,
        PlayerSpectatorData = 34,
        ReportPlayer = 35,
        Count = 36
    }
    public enum NetworkFail : byte {
        UnknownError = 0,
        NoResponse = 1,
        ServerIsFull = 2,
        UnmatchVersion = 3,
        Banned = 4,
        SameSteamIDPlayerAlreadyPlaying = 5,
        SteamVacAuthFail = 6,
        HailSteamIDsDoesNotMatch = 7,
        VersionMatchFail = 8,
        SteamWebApiFail = 9,
        BannedFromMasterServer = 10,
        UnableToParseMasterServerData = 11,
        RankUnmatch = 12,
        AntiCheatAuthFail = 13,
        SteamVerifyFail = 14,
        QueueCanceled = 15,
        QueueOrderMismatch = 16,
        ServerChangedMap = 17,
        MissingTicket = 18,
        AlreadyInGame = 19,
        CorruptedGameFiles = 20,
        InvalidToken = 246,
        InvalidSteamTicket = 248,
        AlreadyInQueue = 249,
        ServerFull = 250,
        ReservedSlotExpired = 251,
        Success = 127,
        WrongMapLoaded = 252
    }
    public enum PackageType : byte {
        Browser_ServerInfoRequest = 128,
        Browser_PingRequest = 129,
        Game_Hail = 127,
        Game_Reliable = 1,
        Game_Unreliable = 2,
        Game_PingResponse = 5,
        Game_ReliableFragmantedData = 6,
        Game_ReliableFragmantedEnd = 7,
        Game_PingRequest = 8,
        Game_Disconnect = byte.MaxValue,
        ServerDeployer_PingRequest = 200,
        ServerDeployer_ConsoleExecute = 201,
        RemoteControl_Request = 233,
        MTUDiscovery_Request = 240,
        Connecting_Accepted = 244,
        Connecting_ReservedSlotExpired = 245,
        Connecting_InvalidToken = 246,
        Connecting_DenyJoin = 252,
        Connecting_QueueIndex = 253,
        Connecting_MapRequest = 130,
        Connecting_KeepAlive = 131,
        SlotReserving_SlotReserved = 247,
        SlotReserving_InvalidSteamTicket = 248,
        SlotReserving_WrongPassword = 249,
        SlotReserving_ServerFull = 250,
        SlotReserving_ReserveSlot = 251
    }
    #region Data
    public abstract class BaseInfo {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("Available")]
        public bool? Available { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("Name")]
        public string Name { get; set; } = null!;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("DisplayName")]
        public string? DisplayName_ { get; set; }

        [JsonIgnore]
        public string DisplayName => DisplayName_ ?? Name ?? "Unknown Map";
        public override string ToString() => DisplayName_ is null ? $"{DisplayName_} ({Name})" : DisplayName;
    }
    #region Maps
    public class SupportedGamemode {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("GameMode")]
        public virtual string GameMode { get; set; } = null!;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("SupportedMapSizes")]
        public virtual List<long> _SupportedMapSizes { get; set; } = new();
        [JsonIgnore]
        public virtual List<MapSize> SupportedMapSizes => _SupportedMapSizes.ConvertAll(size => { if (Enum.IsDefined(typeof(MapSize), size)) { return (MapSize)Enum.ToObject(typeof(MapSize), size); } else { return MapSize.None; } });

        public GameModeInfo? GetGameMode() => BluscreamLib.GameModes.First(g => g.Name == GameMode);
    }
    public partial class ImageUrls {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("DiscordIcon")]
        public virtual Uri? DiscordIcon { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("EndGame")]
        public virtual Uri? EndGame { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("LoadingScreen")]
        public virtual Uri? LoadingScreen { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("MainMap")]
        public virtual Uri? MainMap { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("ServerList")]
        public virtual Uri? ServerList { get; set; }
    }
    public class MapInfo : BaseInfo {

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("ImageUrls")]
        public virtual ImageUrls? ImageUrls { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("SupportedGamemodes")]
        public virtual List<SupportedGamemode>? SupportedGamemodes { get; set; }

        public static MapInfo FromName(string name) => BluscreamLib.Maps.First(m => m.Name == name);
    }
    public static class MapList {
        public static List<MapInfo> FromFile(FileInfo file) => JsonUtils.FromJsonFile<List<MapInfo>>(file);
        public static List<MapInfo> FromUrl(string url) => FromUrl(new Uri(url));
        public static List<MapInfo> FromUrl(Uri url) => JsonUtils.FromUrl<List<MapInfo>>(url);
    }
    #endregion
    #region GameModes
    public class GameModeInfo : BaseInfo {
        public static GameModeInfo FromName(string name) => BluscreamLib.GameModes.First(m => m.Name == name);
    }
    public static class GameModeList {
        public static List<GameModeInfo> FromFile(FileInfo file) => JsonUtils.FromJsonFile<List<GameModeInfo>>(file);
        public static List<GameModeInfo> FromUrl(string url) => FromUrl(new Uri(url));
        public static List<GameModeInfo> FromUrl(Uri url) => JsonUtils.FromUrl<List<GameModeInfo>>(url);
    }
    #endregion
    #region Sizes
    public partial class SizeInfo {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("Available")]
        public virtual bool? Available { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("EnumValue")]
        public virtual long? EnumValue { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("PlayersPerTeam")]
        public virtual long? PlayersPerTeam { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("TotalSize")]
        public virtual long? TotalSize { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("ShortName")]
        public virtual string ShortName { get; set; } = null!;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("LongName")]
        public virtual string LongName { get; set; } = null!;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("Description")]
        public virtual string Description { get; set; } = null!;

        public static List<SizeInfo>? FromJson(string json) => JsonSerializer.Deserialize<List<SizeInfo>>(json, Bluscream.Converter.Settings);
    }
}
#endregion
#endregion
#endregion