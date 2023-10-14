using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using System.Text;
using System.Net;

using TimeSpanParserUtil;
using Humanizer;

using BBRAPIModules;

using Bluscream;
using static Bluscream.BluscreamLib;
using Bans;
using BattleBitBaseModules;
using System.CodeDom.Compiler;

namespace Bluscream {
    [RequireModule(typeof(BluscreamLib))]
    [RequireModule(typeof(Commands.CommandHandler))]
    [RequireModule(typeof(BattleBitBaseModules.RichText))]
    [Module("Basic temp banning", "2.0.0")]
    public class TempBans : BattleBitModule {
        public static ModuleInfo ModuleInfo = new() {
            Name = "Temporary Bans",
            Description = "Rudimentary support for temporary bans stored in a json file",
            Version = new Version(2,0,0),
            Author = "Bluscream",
            WebsiteUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/"),
            UpdateUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/raw/master/modules/TempBans.cs"),
            SupportUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/issues/new?title=TempBans")
        };
        [ModuleReference]
        public Commands.CommandHandler CommandHandler { get; set; } = null!;

        public static TempBansConfiguration Configuration { get; set; } = null!;

        public static FileInfo BanListFile { get; set; } = null!;
        public static BanList Bans { get; set; } = null!;

        public static void Log(object msg) {
            if (Configuration.LogToConsole) BluscreamLib.Log(msg, "TempBans");
        }
        #region Events
        public override void OnModulesLoaded() {
            BanListFile = new FileInfo(Configuration.BanFilePath);
            Bans = new BanList(BanListFile);
            this.CommandHandler.Register(this);
        }
        public override void OnModuleUnloading() {
            Bans.Save();
            base.OnModuleUnloading();
        }

        public override Task OnConnected() {
            CheckAllPlayers();
            return Task.CompletedTask;
        }
        public override Task OnPlayerConnected(RunnerPlayer player) {
            CheckPlayer(player);
            return Task.CompletedTask;
        }
        #endregion
        #region Commands
        [Commands.CommandCallback("tempban", Description = "Bans a player for a specified time period", ConsoleCommand = true, Permissions = new[] { "command.tempban" })]
        public void TempBanCommand(RunnerPlayer commandSource, RunnerPlayer target, string duration, string? reason = null, string? note = null) {
            var span = TimeSpanParser.Parse(duration);
            var ban = TempBanPlayer(target, span, reason, note, Configuration.DefaultServers, invoker: commandSource);
            if (ban is null) {
                commandSource.SayToChat($"Failed to ban {target.str()}"); return;
            }
            commandSource.SayToChat($"{target.str()} has been banned for {ban.Remaining.Humanize()}");
        }
        [Commands.CommandCallback("tempbanid", Description = "Bans a player for a specified time period by Steam ID 64", ConsoleCommand = true, Permissions = new[] { "command.tempbanid" })]
        public void TempBanIdCommand(RunnerPlayer commandSource, string targetSteamId64, string duration, string? reason = null, string? note = null) {
            var success = ulong.TryParse(targetSteamId64, out var result);
            if (!success) {
                var msg = $"{targetSteamId64} is not a valid Steam ID 64";
                this.Logger.Debug(msg);
                commandSource.SayToChat(msg);
                return;
            }
            success = TimeSpanParser.TryParse(duration, out var span);
            if (!success) {
                var msg = $"{duration} is not a valid timespan!";
                this.Logger.Debug(msg);
                commandSource.SayToChat(msg);
                return;
            }
            var bannedUntil = DateTime.UtcNow + span;
            var ban = TempBanPlayer(targetSteamId64: result, dateTime: bannedUntil, reason: reason, note: note, servers: Configuration.DefaultServers, invokerName: commandSource.Name, invokerSteamId64: commandSource.SteamID, invokerIp: commandSource.IP);
            if (ban is null) {
                commandSource.SayToChat($"Failed to ban {targetSteamId64}"); return;
            }
            commandSource.SayToChat($"{ban.Target} has been banned for {ban.Remaining.Humanize()}");
        }
        [Commands.CommandCallback("untempbanid", Description = "Unbans a player for a specified time period by Steam ID 64", ConsoleCommand = true, Permissions = new[] { "command.untempbanid" })]
        public void UnTempBanIdCommand(RunnerPlayer commandSource, string targetSteamId64) {
            var success = ulong.TryParse(targetSteamId64, out var result);
            if (!success) {
                var msg = $"{targetSteamId64} is not a valid Steam ID 64";
                this.Logger.Debug(msg);
                commandSource.SayToChat(msg);
                return;
            }
            var ban = Bans.Get(result);
            if (ban is null) {
                commandSource.SayToChat($"Player {targetSteamId64} is not banned!"); return;
            }
            Bans.Remove(ban);
        }
        [Commands.CommandCallback("tempbanip", Description = "Bans a player for a specified time period by IP", ConsoleCommand = true, Permissions = new[] { "command.tempbanip" })]
        public void TempBanIpCommand(RunnerPlayer commandSource, string targetIp, string duration, string? reason = null, string? note = null) {
            var success = IPAddress.TryParse(targetIp, out var result);
            if (!success) {
                commandSource.SayToChat($"{targetIp} is not a valid IP Address"); return;
            }
            var span = TimeSpanParser.Parse(duration);
            var bannedUntil = DateTime.UtcNow + span;
            var ban = TempBanPlayer(targetIp: result, dateTime: bannedUntil, reason: reason, note: note, servers: Configuration.DefaultServers, invokerName: commandSource.Name, invokerSteamId64: commandSource.SteamID, invokerIp: commandSource.IP);
            if (ban is null) {
                commandSource.SayToChat($"Failed to ban {targetIp}"); return;
            }
            commandSource.SayToChat($"{ban.Target} has been banned for {ban.Remaining.Humanize()}");
        }

        [Commands.CommandCallback("untempban", Description = "Unbans a player that is temporary banned", ConsoleCommand = true, Permissions = new[] { "command.untempban" })]
        public void UnTempBanCommand(RunnerPlayer commandSource, RunnerPlayer target) {
            var ban = Bans.Get(target);
            if (ban is null) {
                commandSource.SayToChat($"Player {target.str()} is not banned!"); return;
            }
            Bans.Remove(ban);
        }

        [Commands.CommandCallback("listtempbans", Description = "Lists players that are temporarily banned", ConsoleCommand = true, Permissions = new[] { "command.listtempbans" })]
        public void ListTempBannedCommand(RunnerPlayer commandSource) {
            commandSource.Message($"{Bans.Get().Count} Bans\n\n" + string.Join("\n", Bans.Get().Select(b=>$"{b.Target.DisplayName} by {b.Invoker?.DisplayName}: {b.Remaining.Humanize()}")));
        }
        #endregion
        #region Methods
        public void CheckAllPlayers() {
            foreach (var player in this.Server.AllPlayers) {
                CheckPlayer(player);
            }
        }
        public void CheckPlayer(RunnerPlayer player) {
            var banned = Bans.Get(player);
            if (banned is not null) {
                KickBannedPlayer(banned);
            }
        }

        public BanEntry? TempBanPlayer(RunnerPlayer target, TimeSpan timeSpan, string? reason = null, string? note = null, List<string>? servers = null, RunnerPlayer? invoker = null) =>
            TempBanPlayer(target, DateTime.UtcNow + timeSpan, reason, note, servers, invoker);
        public BanEntry? TempBanPlayer(RunnerPlayer target, DateTime dateTime, string? reason = null, string? note = null, List<string>? servers = null, RunnerPlayer? invoker = null) =>
            TempBanPlayer(target.SteamID, target.Name, target.IP, dateTime, reason, note, servers, invoker?.Name, invoker?.SteamID, invoker?.IP);
        public BanEntry? TempBanPlayer(ulong? targetSteamId64 = null, string? targetName = null, IPAddress? targetIp = null,
            DateTime? dateTime = null, string? reason = null, string? note = null, List<string>? servers = null,
            string? invokerName = null, ulong? invokerSteamId64 = null, IPAddress? invokerIp = null) {
            servers = servers ?? Configuration.DefaultServers;
            dateTime = dateTime ?? DateTime.MaxValue;
            var newban = new BanEntry() {
                Target = new PlayerEntry() { SteamId64 = targetSteamId64, Name = targetName, IpAddress = targetIp },
                BannedUntilUtc = dateTime,
                Reason = reason,
                Note = note,
                Servers = servers.Distinct().ToList(),
                Invoker = new PlayerEntry() { SteamId64 = invokerSteamId64, Name = invokerName, IpAddress = invokerIp },
            };
            var (msg, ban) = Bans.Add(newban);
            this.Logger.Info($"{msg}: {ban?.ToJson(true)}");
            if (ban is not null) KickBannedPlayer(ban);
            return ban;
        }

        public void KickBannedPlayer(BanEntry? banEntry) {
            if (banEntry == null) return;
            var allServers = banEntry.Servers?.Contains("*") == true;
            var currentServer = banEntry.Servers?.Contains($"{this.Server.GameIP}:{this.Server.GamePort}") == true;
            if (allServers || currentServer) {
                var kickMsg = Configuration.KickNoticeTemplate.Join("\r\n")
                    .Replace("{servername}", this.Server.ServerName)
                    .Replace("{invoker}", banEntry.Invoker?.fullstr())
                    .Replace("{reason}", banEntry.Reason)
                    .Replace("{note}", banEntry.Note)
                    .Replace("{until}", banEntry.BannedUntilUtc.ToString())
                    .Replace("{remaining}", banEntry.Remaining.Humanize()
                );
                foreach (var player in this.Server.AllPlayers) {
                    if (banEntry.Target.SteamId64 == player.SteamID || banEntry.Target.IpAddress == player.IP)
                    Server.Kick(player.SteamID, kickMsg);
                    TempBans.Log($"Kicked tempbanned player {player.fullstr()}: Banned until {banEntry.BannedUntilUtc} UTC");
                }
            }
        }
        #endregion
        //public override Task OnPlayerJoiningToServer(RunnerPlayer player, PlayerJoiningArguments request)
        //{
        //    return Task.FromResult(request as PlayerJoiningArguments);
        //}
    }

    //public static class TempBanExtensions {
    //    public static bool TempBanPlayer(this RunnerPlayer player, TimeSpan timeSpan) => TempBans.TempBanPlayer(player, timeSpan);
    //    public static bool TempBanPlayer(this RunnerPlayer player, DateTime dateTime) => TempBans.TempBanPlayer(player, dateTime);
    // }
    public class TempBansConfiguration : ModuleConfiguration {
        public bool LogToConsole { get; set; } = true;
        public string BanFilePath { get; set; } = "./data/bans.json";
        public List<string> DefaultServers { get; set; } = new List<string>() { "*" };
        public List<string> KickNoticeTemplate { get; set; } = new() {
            $"{Colors.Red}You are banned{Colors.None} on {Colors.Orange}{{servername}}{Colors.None}!",
            $"Banned by: {{invoker}}{Colors.None}",
            $"Reason: {{reason}}{Colors.None}",
            $"Until: {{until}} UTC",
            $"Try again in {{remaining}}"
        };
    }
}
#region json
namespace Bans {
    public partial class PlayerEntry {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("SteamId64")]
        public ulong? SteamId64 { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("IpAddress")]
        public IPAddress? IpAddress { get; set; }
        //[JsonIgnore]
        //public IPAddress? IpAddress { get { IPAddress.TryParse(_IpAddress, out var result); return result; } set { _IpAddress = value?.ToString(); } }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("Hwid")]
        public string? Hwid { get; set; }

        [JsonIgnore]
        public string DisplayName { get { return Name ?? SteamId64.ToString() ?? "Unknown"; } }

        public string fullstr() {
            var sb = new StringBuilder();
            if (Name is not null) sb.Append(Name.Quote());
            if (SteamId64 is not null) sb.Append($" ({SteamId64})");
            return sb.ToString();
        }
    }
    public partial class BanEntry {
        [JsonPropertyName("Id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("Target")]
        public PlayerEntry Target { get; set; } = null!;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("Invoker")]
        public PlayerEntry? Invoker { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("Reason")]
        public string? Reason { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("Note")]
        public string? Note { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("BannedUntil")]
        public DateTime? BannedUntilUtc { get; set; }

        [JsonPropertyName("BannedAt")]
        public DateTime? BannedAt { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("Servers")]
        public List<string> Servers { get; set; } = new List<string>() { "*" };

        [JsonIgnore]
        public TimeSpan Remaining { get { return BannedUntilUtc.Value - DateTime.UtcNow; } }
    }

        public class BanList {
        public static FileInfo File { get; set; }
        public static List<BanEntry> Entries { get; set; } = new List<BanEntry>();

        public BanList(FileInfo file) {
            File = file;
            Load();
        }

        public List<BanEntry> Purge() {
            var removed = Entries.Where(b => b.Remaining.TotalSeconds <= 0).ToList();
            foreach (var banEntry in removed) {
                Entries.Remove(banEntry);
            }
            return removed;
        }

        public List<BanEntry> Get() => Entries;
        public BanEntry? Get(RunnerPlayer player) => Get(player.SteamID);
        public BanEntry? Get(ulong steamId64) {
            var result = Entries.FirstOrDefault(b => (b.Target != null) && (b.Target?.SteamId64 == steamId64), null);
            if (result is not null && result.BannedUntilUtc.HasValue) {
                if (result.Remaining.TotalSeconds <= 0) {
                    TempBans.Log($"Temporary ban for player {result.Target} expired {result.Remaining.Humanize()} ago, removing ...");
                    Entries.Remove(result);
                    result = null;
                }
            }
            return result;
        }

        public KeyValuePair<string, BanEntry?> Add(BanEntry entry, bool overwrite = false) {
            entry.BannedAt = DateTime.UtcNow;
            entry.Id = entry.GetMd5Hash();
            var exists = Entries.FirstOrDefault(b => (b?.Target != null) && (b?.Target?.SteamId64 == entry.Target?.SteamId64), null);
            if (exists != null ) {
                if (!overwrite) {
                    var msg = "Tried to add duplicate ban but overwrite was not enabled!";
                    return new(msg, null);
                } else {
                    Entries.Remove(exists);
                    Entries.Add(entry);
                    Save();
                    return new("Successfully overwrote existing tempban", Entries.Where(e => e == entry).FirstOrDefault());
                }
            } else {
                Entries.Add(entry);
                Save();
                return new("Successfully add new tempban", Entries.Where(e => e == entry).FirstOrDefault());
            }
        }
        public bool Remove(BanEntry entry) {
            var success = Entries.Remove(entry);
            Save();
            return success;
        }

        public void Load() {
            try {
                Entries = JsonUtils.FromJsonFile<List<BanEntry>>(File);
                var purged = Purge().Count;
                TempBans.Log($"Loaded {Entries.Count} bans from \"{File.Name}\" ({purged} purged)");
            } catch (Exception ex) {
                TempBans.Log($"Failed to load banlist from {File}: \"{ex.Message}\" Backing up and creating a new one...");
                if(File.Exists) File.MoveTo(File.Name + ".bak", true);
                Save();
            }
        }
        public void Save() => Entries.ToFile(File, indented: true);
    }
}
#endregion