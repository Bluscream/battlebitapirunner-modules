using BattleBitAPI.Common;
using BBRAPIModules;
using Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Bluscream.BluscreamLib;
using static Bluscream.Extensions;

namespace Bluscream;

[Module("More chat voting commands", "2.0.0")]
[RequireModule(typeof(BluscreamLib))]
[RequireModule(typeof(CommandHandler))]
public class AdvancedVoting : BattleBitModule {
    public static ModuleInfo ModuleInfo = new() {
        Name = "Bluscream's Library",
        Description = "Generic library for common code used by multiple modules.",
        Version = new Version(2, 0),
        Author = "Bluscream",
        WebsiteUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/"),
        UpdateUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/raw/master/modules/BluscreamLib.cs"),
        SupportUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/issues/new?title=BluscreamLib")
    };
    private bool activeVote = false;
    private string voteText = "";
    private string[] voteOptions = new string[0];
    private Dictionary<ulong, int> votes = new();
    private DateTime endOfVote = DateTime.MinValue;
    public AdvancedVotingConfiguration Configuration { get; set; }
    [ModuleReference]
    public dynamic? RichText { get; set; }
    [ModuleReference]
    public CommandHandler CommandHandler { get; set; }
    [ModuleReference]
    public TempBans TempBans { get; set; }
    #region Events
    public override void OnModulesLoaded() {
        this.CommandHandler.Register(this);
    }
    #endregion
    #region Commands
    //[CommandCallback("vote", Description = "Votes for an option", AllowedRoles = Roles.Moderator)]
    //public void StartVoteCommand(RunnerPlayer commandSource, string text, string options) {
    //    StartVote(commandSource, text, options, (int totalVotes, int winnerVotes, string won) => {});
    //}

    [CommandCallback("votemap", Description = "Starts a vote for a map")]
    public void StartMapVoteCommand(RunnerPlayer commandSource, string mapName) {
        var map = mapName.ToMap();
        if (map is null) {
            commandSource.Message($"\"{mapName}\" is not a valid map!");
            return;
        }
        StartVote(commandSource, $"Vote to change map to {map}", string.Join("|", MapDisplayNames), (int totalVotes, int winnerVotes, string won) => {
            if (Extensions.EvalToBool(Configuration.VoteWinningConditions["votemap"]) == false) return;
            this.Server.ChangeMap(map);
        });
    }

    [CommandCallback("votegamemode", Description = "Starts a vote for a gamemode")]
    public void StartGameModeVoteCommand(RunnerPlayer commandSource, string gameModeName) {
        var mode = gameModeName.ToGameMode();
        if (mode is null) {
            commandSource.Message($"\"{gameModeName}\" is not a valid game mode!");
            return;
        }
        StartVote(commandSource, "Vote for game mode change", string.Join("|", GameModeDisplayNames), (int totalVotes, int winnerVotes, string won) => {
            if (Extensions.EvalToBool(Configuration.VoteWinningConditions["votegamemode"]) == false) return;
            this.Server.ChangeGameMode(mode);
        });
    }

    [CommandCallback("votemaptime", Description = "Starts a vote for map time")]
    public void StartMapTimeVoteCommand(RunnerPlayer commandSource, string dayTime) {
        MapDayNight time = GetDayNightFromString(dayTime);
        if (time == MapDayNight.None) {
            commandSource.Message($"\"{dayTime}\" is not a valid time!");
            return;
        }
        StartVote(commandSource, "Vote for time change", "Day|Night", (int totalVotes, int winnerVotes, string won) => {
            if (Extensions.EvalToBool(Configuration.VoteWinningConditions["votemaptime"]) == false) return;
            this.Server.ChangeTime(time);
        });
    }

    [CommandCallback("voterestart", Description = "Starts a vote for map restart")]
    public void StartMapRestartVoteCommand(RunnerPlayer commandSource) {
        StartVote(commandSource, "Vote for map restart", "Yes|No", (int totalVotes, int winnerVotes, string won) => {

            if (Extensions.EvalToBool(Configuration.VoteWinningConditions["voterestart"]) == false) return;
            this.Server.ChangeMap();
        });
    }

    [CommandCallback("voteban", Description = "Starts a voteban for a player")]
    public void StartVoteBanCommand(RunnerPlayer commandSource, RunnerPlayer target, string reason) {
        if (target is null) {
            commandSource.Message($"Player \"{target}\" could not be found!");
            return;
        }
        StartVote(commandSource, "Vote for map restart", "Yes|No", (int totalVotes, int winnerVotes, string won) => {
            if (Extensions.EvalToBool(Configuration.VoteWinningConditions["voteban"]) == false) return;
            TempBans.TempBanPlayer(target, TimeSpan.FromMinutes(30), $"Votebanned by {winnerVotes} votes", "voteban", invoker: commandSource);
        });
    }
    #endregion
    #region Internals
    public static string ParseVoteCondition(string input, int winnerVotes, int totalVotes, RunnerServer server) {
        input = input.Replace("{winnerVotes}", winnerVotes.ToString());
        input = input.Replace("{totalVotes}", totalVotes.ToString());
        input = input.Replace("{allPlayers}", server.AllPlayers.Count().ToString());
        return input;
    }
    public void StartVote(RunnerPlayer commandSource, string text, string options, Action<int, int, string> voteEndedCallback) {
        if (this.activeVote) {
            commandSource.Message("There is already an active vote.");
            return;
        }

        this.activeVote = true;
        this.voteText = text;
        this.voteOptions = options.Split('|');

        if (this.voteOptions.Length >= 10) {
            commandSource.Message("You can only have up to 9 options.");
            this.activeVote = false;
            return;
        }

        this.votes.Clear();
        this.endOfVote = DateTime.Now.AddSeconds(this.Configuration.VoteDuration);

        this.Server.SayToAllChat($"{this.RichText?.Size(125)}A vote has been started!");

        StringBuilder messageText = new($"{this.RichText?.Size(125)}{this.voteText}{this.RichText?.Size(100)}{Environment.NewLine}");
        for (int i = 0; i < this.voteOptions.Length; i++) {
            messageText.AppendLine($"Type {i + 1} in chat for {this.RichText?.FromColorName("yellow")}{this.voteOptions[i]}{this.RichText?.Color()}");
        }

        messageText.AppendLine($"{this.RichText?.Size(125)}You have {this.Configuration.VoteDuration} seconds to vote.");

        foreach (RunnerPlayer player in this.Server.AllPlayers) {
            player.Message($"{this.RichText?.Size(125)}{messageText}", this.Configuration.VoteDuration);
        }

        this.Server.SayToAllChat(messageText.ToString());

        Task.Run(() => voteStartedHandler(voteEndedCallback));
    }
    private void voteStartedHandler(Action<int, int, string> voteEndedCallback) {
        while (this.IsLoaded && this.Server.IsConnected && this.activeVote) {
            if (DateTime.Now > this.endOfVote) {
                break;
            }

            Task.Delay(1000).Wait();
        }

        if (!this.IsLoaded || !this.Server.IsConnected || !this.activeVote) {
            return;
        }

        this.activeVote = false;

        if (this.votes.Count == 0) {
            this.Server.SayToAllChat($"{this.RichText?.Size(125)}The vote has ended!{Environment.NewLine}Nobody voted.");
            return;
        }

        int[] voteCounts = new int[this.voteOptions.Length];
        foreach (int vote in this.votes.Values) {
            voteCounts[vote - 1]++;
        }

        int maxVotes = voteCounts.Max();
        int maxVoteIndex = voteCounts.ToList().IndexOf(maxVotes);
        var won = this.voteOptions[maxVoteIndex];
        var sum = voteCounts.Sum();
        this.Server.SayToAllChat($"{this.RichText?.Size(125)}The vote has ended!{Environment.NewLine}{this.RichText?.FromColorName("yellow")}{won}{this.RichText?.Color()} won with {maxVotes} votes.");


        Task.Run(() => voteEndedCallback(sum, maxVotes, won));
    }
    public override async Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg) {
        if (!this.activeVote) {
            return true;
        }

        msg = new string(msg.Where(c => char.IsDigit(c)).Distinct().ToArray());
        if (msg.Length == 0) {
            return true;
        }

        if (msg.Length > 1) {
            player.SayToChat("Could not find a unique vote option in your message.");
            return true;
        }

        if (!int.TryParse(msg, out int vote)) {
            return true;
        }

        if (vote < 1 || vote > this.voteOptions.Length) {
            return true;
        }

        if (this.votes.ContainsKey(player.SteamID)) {
            this.votes.Remove(player.SteamID);
        }

        this.votes.Add(player.SteamID, vote);

        player.SayToChat($"You voted for {this.RichText?.FromColorName("yellow")}{this.voteOptions[vote - 1]}{this.RichText?.Color()}. You can change your vote any time.");

        await Task.CompletedTask;

        return true;
    }
    #endregion
}

public class AdvancedVotingConfiguration : ModuleConfiguration {
    public int VoteDuration { get; set; } = 60;
    public Dictionary<string, string> VoteWinningConditions { get; set; } = new Dictionary<string, string>() {
        {"votemap", "{winnerVotes} > ({allPlayers} / 2)" },
        {"votegamemode", "{winnerVotes} > ({allPlayers} / 2)" },
        {"votemaptime", "{winnerVotes} > ({allPlayers} / 2)" },
        {"voterestart", "{winnerVotes} > ({allPlayers} / 2)" },
        {"voteban", "{winnerVotes} > ({allPlayers} / 2)" },
    };

}