using BattleBitAPI.Common;
using BBRAPIModules;
using Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Text.Json;
using System.Globalization;
using Bluscream;
using static Bluscream.BluscreamLib;
using JsonExtensions;
using System.Runtime.CompilerServices;
using System.IO;
using Bans;
using TimeSpanParserUtil;
using Humanizer;
using System.Net;

namespace Bluscream {


    [RequireModule(typeof(BluscreamLibModule))]
    [RequireModule(typeof(CommandHandler))]
    [Module("Basic temp banning", "1.0.0")]
    public class TempBans : BattleBitModule
    {
        [ModuleReference]
        public CommandHandler CommandHandler { get; set; }

        public static TempBansConfiguration Configuration { get; set; }


        public static FileInfo BanListFile { get; set; }
        public static BanList Bans { get; set; }

        public static void Log(object msg) {
            if (Configuration.LogToConsole) BluscreamLib.Log(msg, "TempBans");
        }

        public override void OnModulesLoaded() {
            BanListFile = new FileInfo(Configuration.BanFilePath);
            Bans = new BanList(BanListFile);
            this.CommandHandler.Register(this);
        }

        public override void OnModuleUnloading() {
            Bans.Save();
            base.OnModuleUnloading();
        }

        [CommandCallback("tempban", Description = "Bans a player for a specified time period", AllowedRoles = Roles.Admin | Roles.Moderator)]
        public void TempBanCommand(RunnerPlayer commandSource, RunnerPlayer target, string duration, string? reason = null, string? note = null)
        {
            var span = TimeSpanParser.Parse(duration);
            var ban = TempBanPlayer(target, span, reason, note, Configuration.DefaultServers, invoker: commandSource);
            if (ban is null) {
                commandSource.Message($"Failed to ban {target.str()}"); return;
            }
            commandSource.Message($"{target.str()} has been banned for {ban.Remaining.Humanize()}");
        }

        [CommandCallback("untempban", Description = "Bans a player for a specified time period", AllowedRoles = Roles.Admin | Roles.Moderator)]
        public void UnTempBanCommand(RunnerPlayer commandSource, RunnerPlayer target) {
            var ban = Bans.Get(target);
            if (ban is null) {
                commandSource.Message($"Player{target.str()} is not banned!"); return;
            }
            Bans.Remove(ban);
        }

        public override Task OnConnected() {
            CheckAllPlayers();
            return Task.CompletedTask;
        }

        public override Task OnPlayerConnected(RunnerPlayer player) {
            CheckPlayer(player);
            return Task.CompletedTask;
        }
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
            TempBanPlayer(target, DateTime.Now + timeSpan, reason, note, servers, invoker);
        public BanEntry? TempBanPlayer(RunnerPlayer target, DateTime dateTime, string? reason = null, string? note = null, List<string>? servers = null, RunnerPlayer? invoker = null) {
            servers = servers ?? Configuration.DefaultServers;
            var newban = new BanEntry() {
                Target = new PlayerEntry() { SteamId64 = target.SteamID, Name = target.Name, IpAddress = target.IP.ToString() },
                BannedUntil = dateTime,
                Reason = reason,
                Note = note,
                Servers = servers.Distinct().ToList(),
                Invoker = new PlayerEntry() { SteamId64 = invoker?.SteamID, Name = invoker?.Name, IpAddress = invoker?.IP.ToString() },
            };
            var ban = Bans.Add(newban);
            KickBannedPlayer(ban);
            return ban;
        }

        public void KickBannedPlayer(BanEntry banEntry) {
            var allServers = banEntry.Servers?.Contains("*") == true;
            var currentServer = banEntry.Servers?.Contains($"{this.Server.GameIP}:{this.Server.GamePort}") == true;
            if (allServers || currentServer) {
                Server.Kick(banEntry.Target.SteamId64.Value, Configuration.KickNoticeTemplate
                    .Replace("{servername}", this.Server.ServerName)
                    .Replace("{invoker}", banEntry.Invoker?.Name)
                    .Replace("{reason}", banEntry.Reason)
                    .Replace("{note}", banEntry.Note)
                    .Replace("{until}", banEntry.BannedUntil.ToString())
                    .Replace("{remaining}", banEntry.Remaining.Humanize())
                );
                TempBans.Log($"Kicked tempbanned player {banEntry.Target.Name} ({banEntry.Target.SteamId64}): Banned until {banEntry.BannedUntil}");
            }
        }

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
        public string KickNoticeTemplate { get; set; } = "You are banned on {servername}!\n\nBanned by: {invoker}\nReason: \"{reason}\"\nUntil: {until}\n\nTry again in {remaining}";
    }
}

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
        public string? IpAddress { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("Hwid")]
        public string? Hwid { get; set; }
    }
    public partial class BanEntry {
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
        public DateTime? BannedUntil { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("Servers")]
        public List<string> Servers { get; set; } = new List<string>() { "*" };
    }
    public partial class BanEntry {
        [JsonIgnore]
        public TimeSpan Remaining => BannedUntil.Value - DateTime.Now;
    }

        public class BanList {
        public static FileInfo File { get; set; }
        public static List<BanEntry> Entries { get; set; } = new List<BanEntry>();

        public BanList(FileInfo file) {
            File = file;
            Load();
        }

        public List<BanEntry> Purge() {
            var removed = Entries.Where(b => b.Remaining.TotalSeconds < 1).ToList();
            foreach (var banEntry in removed) {
                Entries.Remove(banEntry);
            }
            return removed;
        }

        public BanEntry? Get(RunnerPlayer player) {
            var result = Entries.FirstOrDefault(b => (b.Target != null) && (b.Target?.SteamId64 == player.SteamID), null);
            if (result is not null && result.BannedUntil.HasValue) {
                if (result.Remaining.TotalSeconds <= 0) {
                    TempBans.Log($"Temporary ban for player {player.str()} expired {result.Remaining.Humanize()} ago, removing ...");
                    Entries.Remove(result);
                    result = null;
                }
            }
            return result;
        }

        public BanEntry? Add(BanEntry entry, bool overwrite = false) {
            var exists = Entries.FirstOrDefault(b => (b?.Target != null) && (b?.Target?.SteamId64 == entry.Target?.SteamId64), null);
            if (exists != null ) {
                if (!overwrite) {
                    TempBans.Log($"Tried to add duplicate ban but overwrite was not enabled!");
                    TempBans.Log(JsonSerializer.Serialize(entry, new JsonSerializerOptions() { WriteIndented = true }));
                    return null;
                } else {
                    Entries.Remove(exists);
                    Entries.Add(entry);
                    Save();
                    return entry;
                    }
            } else {
                Entries.Add(entry);
                Save();
                return entry;
            }
        }
        public void Remove(BanEntry entry) {
            Entries.Remove(entry);
            Save();
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
        public void Save() => Entries.ToFile(File);
    }
}