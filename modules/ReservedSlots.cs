using BattleBitAPI.Common;
using BBRAPIModules;
using Permissions;
using System.Threading.Tasks;

namespace BattleBitBaseModules;

/// <summary>
/// Author: @RainOrigami, Bluscream
/// </summary>

[RequireModule(typeof(PlayerPermissions))]
[Module("Reserved Slots", "1.0.0")]
public class ReservedSlots : BattleBitModule
{
    public ReservedSlotsConfiguration Configuration { get; set; } = null!;
    public PlayerPermissions PlayerPermissions { get; set; } = null!;

    public override Task OnPlayerJoiningToServer(ulong steamID, PlayerJoiningArguments args)
    {
        if (this.Server.MaxPlayerCount - this.Server.CurrentPlayerCount > this.Configuration.ReservedSlots)
        {
            return Task.CompletedTask;
        }

        if ((this.PlayerPermissions.GetPlayerRoles(steamID) & this.Configuration.AllowedRoles) > 0)
        {
            return Task.CompletedTask;
        }

        // Reject player because server is full and player doesn't have required role
        // TODO: verify if this is the correct way to reject player
        args.Stats = null;

        return Task.CompletedTask;
    }
}

public class ReservedSlotsConfiguration : ModuleConfiguration
{
    public int ReservedSlots { get; set; } = 2;
    public Roles AllowedRoles { get; set; } = Roles.Admin | Roles.Moderator;
}
