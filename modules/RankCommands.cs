using BattleBitAPI.Common;
using BattleBitBaseModules;
using BBRAPIModules;
using Commands;
using Humanizer;
using Permissions;
using System;
using System.Reflection;
using System.Text;

namespace Bluscream {
    [RequireModule(typeof(BluscreamLib))]
    [RequireModule(typeof(Commands.CommandHandler))]
    [RequireModule(typeof(Permissions.GranularPermissions))]
    [RequireModule(typeof(BasicProgression))]
    [Module("Rank Commands", "2.0.0.0")]
    public class RankCommands : BattleBitModule {

        public static ModuleInfo ModuleInfo = new() {
            Name = "Rank Commands",
            Description = "Rank commands for the Battlebit Modular API",
            Version = new Version(2, 0, 0, 0),
            Author = "Bluscream",
            WebsiteUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/"),
            UpdateUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/raw/master/modules/RankCommands.cs"),
            SupportUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/issues/new?title=RankCommands")
        };

        [ModuleReference]
        public Commands.CommandHandler CommandHandler { get; set; } = null!;

        [ModuleReference]
        public BasicProgression BasicProgression { get; set; } = null!;

        [ModuleReference]
        public Permissions.GranularPermissions GranularPermissions { get; set; } = null!;

        public override void OnModulesLoaded() {
            this.CommandHandler.Register(this);
        }

        public string GetCurrentRankInfoString(RunnerPlayer player) {
            var stats = this.BasicProgression.GetPlayerStats(player).Result;
            var sb = new StringBuilder($"{player.Name}:\n\n");
            sb.AppendLine($"Prestige: {stats.Progress.Prestige}");
            sb.AppendLine($"Level: {stats.Progress.Rank}");
            sb.AppendLine($"XP: {stats.Progress.EXP}");
            sb.AppendLine($"Time Played: {TimeSpan.FromSeconds(stats.Progress.PlayTimeSeconds).Humanize()}");
            return sb.ToString();
        }

        #region commands

        [CommandCallback("prestige", Description = "Gets or sets your prestige", ConsoleCommand = true, Permissions = new[] { "commands.prestige" })]
        public string SetPrestigeCommand(Context ctx, RunnerPlayer? target = null, ushort? prestige = null) {
            var invoker = (ctx.Source as ChatSource)?.Invoker;
            if (target != invoker && !GranularPermissions.HasPermission(invoker.SteamID, "commands.prestige.others")) return "<color=red>You do not have permission to set other players prestige!</color>";
            target = target ?? invoker;
            if (prestige is null) return GetCurrentRankInfoString(target);
            if (prestige.Value < 0 || prestige.Value > 6) return "<color=red>Prestige must be between 0 and 6!</color>";
            var stats = this.BasicProgression.GetPlayerStats(target).Result;
            stats.Progress.Prestige = prestige.Value;
            return $"Set {target.Name}'s prestige to {prestige.Value}, they will need to reconnect for the changes to take effect!";
        }

        [CommandCallback("rank", Description = "Gets or sets your rank", ConsoleCommand = true, Permissions = new[] { "commands.rank" })]
        public string SetRankCommand(Context ctx, RunnerPlayer? target = null, ushort? rank = null) {
            var invoker = (ctx.Source as ChatSource)?.Invoker;
            if (target != invoker && !GranularPermissions.HasPermission(invoker.SteamID, "commands.rank.others")) return "<color=red>You do not have permission to set other players rank!</color>";
            target = target ?? invoker;
            if (rank is null) return GetCurrentRankInfoString(target);
            if (rank.Value < 0 || rank.Value > 200) return "<color=red>Rank must be between 0 and 200!</color>";
            var stats = this.BasicProgression.GetPlayerStats(target).Result;
            stats.Progress.Rank = rank.Value;
            return $"Set {target.Name}'s rank to {rank.Value}, they will need to reconnect for the changes to take effect!";
        }

        [CommandCallback("xp", Description = "Gets or sets your xp", ConsoleCommand = true, Permissions = new[] { "commands.xp" })]
        public string SetXPCommand(Context ctx, RunnerPlayer? target = null, uint? xp = null) {
            var invoker = (ctx.Source as ChatSource)?.Invoker;
            if (target != invoker && !GranularPermissions.HasPermission(invoker.SteamID, "commands.xp.others")) return "<color=red>You do not have permission to set other players xp!</color>";
            target = target ?? invoker;
            if (xp is null) return GetCurrentRankInfoString(target);
            if (xp.Value < 0 || xp.Value > 99999) return "<color=red>XP must be between 0 and 99999!</color>";
            var stats = this.BasicProgression.GetPlayerStats(target).Result;
            stats.Progress.EXP = xp.Value;
            return $"Set {target.Name}'s xp to {xp.Value}, they will need to reconnect for the changes to take effect!";
        }

        #endregion commands
    }
}