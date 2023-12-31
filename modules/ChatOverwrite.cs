﻿using BattleBitAPI.Common;
using BattleBitBaseModules;
using BBRAPIModules;
using Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatOverwrite;

[RequireModule(typeof(RichText))]
[RequireModule(typeof(GranularPermissions))]
[RequireModule(typeof(Bluscream.SteamApi))]
[Module("Overwrite chat messages", "1.0.0")]
public class ChatOverwrite : BattleBitModule {

    [ModuleReference]
    public Bluscream.SteamApi? SteamApi { get; set; } = null!;

    public ChatOverwriteConfiguration Configuration { get; set; } = null!;

    [ModuleReference]
    public dynamic? CommandHandler { get; set; }

    [ModuleReference]
    public GranularPermissions GranularPermissions { get; set; } = null!;

    public override Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg) {
        if (this.CommandHandler is not null && this.CommandHandler.IsCommand(msg)) {
            this.Logger.Debug($"Ignoring message {msg} from {player.Name} because it is a command");
            return Task.FromResult(true);
        }

        if (this.GranularPermissions.HasPermission(player.SteamID, "ChatOverwrite.Bypass")) {
            this.Logger.Debug($"Ignoring message {msg} from {player.Name} because they have the ChatOverwrite.Bypass permission");
            return Task.FromResult(true);
        }

        string? permission = this.Configuration.Overwrites.Keys.FirstOrDefault(k => this.GranularPermissions.HasPermission(player.SteamID, k));

        if (String.IsNullOrEmpty(permission)) {
            this.Logger.Debug($"Ignoring message {msg} from {player.Name} because they do not have any ChatOverwrite permissions");
            return Task.FromResult(true);
        }

        OverwriteMessage overwriteMessage = this.Configuration.Overwrites[permission];

        this.Logger.Debug($"Overwriting message {msg} from {player.Name} with permission {permission}");

        string gradientName = FormatTextWithGradient(player.Name, overwriteMessage.GradientColors ?? Array.Empty<string>());

        string teamName = player.Team == Team.TeamA ? "US" : "RU";
        char? squadLetter = player.InSquad ? player.SquadName.ToString()[0] : null;

        foreach (RunnerPlayer chatTarget in this.Server.AllPlayers) {
            if (channel == ChatChannel.SquadChat && (chatTarget.Team != player.Team || !chatTarget.InSquad || !player.InSquad || chatTarget.SquadName != player.SquadName)) {
                continue;
            }

            if (channel == ChatChannel.TeamChat && chatTarget.Team != player.Team) {
                continue;
            }

            string nameColor = player.Team == chatTarget.Team && player.InSquad && chatTarget.InSquad && player.SquadName == chatTarget.SquadName ? "green" : (player.Team == chatTarget.Team ? "blue" : "red");
            string teamAndSquadIndicator = teamName;
            if (squadLetter != null) {
                teamAndSquadIndicator += $"-{squadLetter}";
            }
            teamAndSquadIndicator = $"[{teamAndSquadIndicator}]";

            string textColor = channel == ChatChannel.TeamChat ? "#01C4F4" : (channel == ChatChannel.SquadChat ? "#0CF401" : "#FFFFFF");

            var prefixes = this.Configuration.NamePrefixes.Where(k => this.GranularPermissions.HasPermission(player.SteamID, k.Key)).Select(k => k.Value);
            var suffixes = this.Configuration.NameSuffixes.Where(k => this.GranularPermissions.HasPermission(player.SteamID, k.Key)).Select(k => k.Value);

            var playerName = string.Join("", prefixes) + GetPlayerName(player) + string.Join("", suffixes);

            var now = string.IsNullOrWhiteSpace(Configuration.TimeStampFormat) ? "" : new Bluscream.DateTimeWithZone(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById(Configuration.TimeZone)).LocalTime.ToString(Configuration.TimeStampFormat);

            chatTarget.SayToChat(string.Format(overwriteMessage.Text, nameColor, playerName, teamAndSquadIndicator, textColor, msg, gradientName, now));
            //                                                                                               0                 1                  2                                       3              4               5             6
        }

        return Task.FromResult(false);
    }

    public string GetPlayerName(RunnerPlayer player) {
        if (SteamApi is not null) {
            var steam = player.GetSteamData()?.Result;
            if (steam?.Summary is not null) {
                if (!string.IsNullOrWhiteSpace(steam.Summary.RealName)) return steam.Summary.RealName;
                else if (!string.IsNullOrWhiteSpace(steam.Summary.PersonaName)) return steam.Summary.PersonaName;
            }
        }
        return string.IsNullOrWhiteSpace(player.Name) ? player.SteamID.ToString() : player.Name;
    }

    private static string FormatTextWithGradient(string text, string[] gradientColors) {
        if (string.IsNullOrEmpty(text) || gradientColors.Length == 0) {
            return text;
        }

        int segmentCount = gradientColors.Length;
        int segmentLength = text.Length / segmentCount;
        int remainder = text.Length % segmentCount;

        StringBuilder formattedName = new StringBuilder();
        int currentIndex = 0;

        for (int i = 0; i < segmentCount; i++) {
            int currentSegmentLength = segmentLength + (i < remainder ? 1 : 0);
            string currentColor = gradientColors[i];
            string segmentText = text.Substring(currentIndex, currentSegmentLength);

            formattedName.Append($"<color=\"{currentColor}\">{segmentText}</color>");
            currentIndex += currentSegmentLength;
        }

        return formattedName.ToString();
    }
}

public class ChatOverwriteConfiguration : ModuleConfiguration {
    public string TimeStampFormat { get; set; } = "HH:mm:ss";

    [Obsolete]
    public string TimeZone { get; set; } = System.TimeZone.CurrentTimeZone.StandardName;

    public Dictionary<string, OverwriteMessage> Overwrites { get; set; } = new()
    {
        { "ChatOverwrite.Normal", new("[{6}] <color=\"{0}\">{1}</color>{2} : <color={3}>{4}") },
        { "ChatOverwrite.Rainbow", new("{5}{2} : <color=\"{3}\">{4}", new string[] { "red", "orange", "yellow", "green", "blue", "purple" }) },
        { "ChatOverwrite.Large", new("<size=150%><color=\"{0}\">{1}</color>{2} : <color={3}>{4}") },
        { "ChatOverwrite.Sus", new("<color=\"{0}\">{1}</color>{2} : <color={3}>I am an idiot and cheat in online games. Please go to my Steam profile and report me!") },
        { "ChatOverwrite.Staff", new("[{6}] <size=101%><color=\"{0}\">{1}</color>{2} : <color={3}>{4}") }
    };
    public Dictionary<string, string> NamePrefixes { get; set; } = new()
    {
        { "ChatPrefix.Clan", $"<color=#bc226b>[VDE]{Colors.None} "}
    };
    public Dictionary<string, string> NameSuffixes { get; set; } = new()
    {
        { "ChatSuffix.Admin", $" <color=#FF0006>[Server Admin]{Colors.None}"},
        { "ChatSuffix.Moderator", $" <color=\"orange\">[Server Mod]{Colors.None}"},
        { "ChatSuffix.Supporter", $" <color=\"purple\">[Server Support]{Colors.None}"},
        { "ChatSuffix.VIP", $" <color=\"gold\">[VIP]{Colors.None}"},
        { "ChatSuffix.Cheater", $" <color=\"darkred\">[Cheater]{Colors.None}"}
    };
}

public record OverwriteMessage(string Text, string[]? GradientColors = null);