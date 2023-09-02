using BattleBitAPI.Common;
using BBRAPIModules;
using Commands;
using Permissions;

namespace PermissionsManager;

[RequireModule(typeof(PlayerPermissions))]
[RequireModule(typeof(CommandHandler))]
public class PermissionsCommands : BattleBitModule
{
    [ModuleReference]
    public PlayerPermissions PlayerPermissions { get; set; }
    [ModuleReference]
    public CommandHandler CommandHandler { get; set; }

    public override void OnModulesLoaded()
    {
        this.CommandHandler.Register(this);
    }

    [CommandCallback("addperm", Description = "Adds a permission to a player", AllowedRoles = Roles.Admin)]
    public void AddPermissionCommand(RunnerPlayer commandSource, RunnerPlayer player, Roles permission)
    {
        this.PlayerPermissions.AddPlayerRoles(player.SteamID, permission);
        commandSource.Message($"Added permission {permission} to {player.Name}");
    }

    [CommandCallback("addperm", Description = "Adds a permission to a player", AllowedRoles = Roles.Admin)]
    public void AddIdPermissionCommand(RunnerPlayer commandSource, ulong steamId64, Roles permission) {
        this.PlayerPermissions.AddPlayerRoles(steamId64, permission);
        commandSource.Message($"Added permission {permission} to {steamId64}");
    }

    [CommandCallback("removeperm", Description = "Removes a permission from a player", AllowedRoles = Roles.Admin)]
    public void RemovePermissionCommand(RunnerPlayer commandSource, RunnerPlayer player, Roles permission)
    {
        this.PlayerPermissions.RemovePlayerRoles(player.SteamID, permission);
        commandSource.Message($"Removed permission {permission} from {player.Name}");
    }
    [CommandCallback("removeperm", Description = "Removes a permission from a player", AllowedRoles = Roles.Admin)]
    public void RemoveIdPermissionCommand(RunnerPlayer commandSource, ulong steamId64, Roles permission) {
        this.PlayerPermissions.RemovePlayerRoles(steamId64, permission);
        commandSource.Message($"Removed permission {permission} from {steamId64}");
    }
}