
# 0 Modules in Commands.cs

| Description   | Version   |
|---------------|-----------|
# 0 Modules in Common.cs

| Description   | Version   |
|---------------|-----------|
# 0 Modules in Messages.cs

| Description   | Version   |
|---------------|-----------|
# 0 Modules in WebSocketServer.cs

| Description   | Version   |
|---------------|-----------|
# 1 Modules in BattleMetricsRCON.cs

| Description       | Version   |
|:------------------|:----------|
| BattleMetricsRCON | 1.0.0     |
# 0 Modules in RCONServer.cs

| Description   | Version   |
|---------------|-----------|
# 1 Modules in Allowlist.cs

| Description                                                        | Version   |
|:-------------------------------------------------------------------|:----------|
| Block players who are not on the allowlist from joining the server | 1.0.0     |

## Commands
| Command      | Function Name   | Description                         | Allowed Roles   | Parameters                                      | Defaults   |
|:-------------|:----------------|:------------------------------------|:----------------|:------------------------------------------------|:-----------|
| allow add    | AllowAdd        | Adds a player to the allowlist      | Moderator       | ['RunnerPlayer commandSource', 'ulong steamID'] | {}         |
| allow remove | AllowRemove     | Removes a player from the allowlist | Moderator       | ['RunnerPlayer commandSource', 'ulong steamID'] | {}         |
# 1 Modules in Announcements.cs

| Description                                                                                 | Version   |
|:--------------------------------------------------------------------------------------------|:----------|
| Periodically execute announcements and messages based on configurable delays and conditions | 1.0.0     |
# 1 Modules in BasicProgression.cs

| Description                                      | Version   |
|:-------------------------------------------------|:----------|
| Provide basic persistent progression for players | 1.0.0     |
# 1 Modules in BasicServerSettings.cs

| Description                     | Version   |
|:--------------------------------|:----------|
| Configure basic server settings | 1.0.0     |
# 1 Modules in CommandHandler.cs

| Description                                | Version   |
|:-------------------------------------------|:----------|
| Basic in-game chat command handler library | 1.0.0     |

## Commands
| Command   | Function Name      | Description                       | Allowed Roles   | Parameters                                | Defaults      |
|:----------|:-------------------|:----------------------------------|:----------------|:------------------------------------------|:--------------|
| help      | HelpCommand        | Shows this help message           |                 | ['RunnerPlayer player', 'int page = 1']   | {'page': '1'} |
| cmdhelp   | CommandHelpCommand | Shows help for a specific command |                 | ['RunnerPlayer player', 'string command'] | {}            |
| modules   | ListModules        | Lists all loaded modules          | Admin           | ['RunnerPlayer commandSource']            | {}            |
# 1 Modules in DiscordWebhooks.cs

| Description                                                                               | Version   |
|:------------------------------------------------------------------------------------------|:----------|
| Send some basic events to Discord and allow for other modules to send messages to Discord | 1.0.0     |
# 1 Modules in LoadingScreenText.cs

| Description                                      | Version   |
|:-------------------------------------------------|:----------|
| Configure the loading screen text of your server | 1.0.0     |
# 1 Modules in ModeratorTools.cs

| Description           | Version   |
|:----------------------|:----------|
| Basic moderator tools | 1.0.0     |

## Commands
| Command       | Function Name          | Description                                    | Allowed Roles   | Parameters                                                                                       | Defaults                              |
|:--------------|:-----------------------|:-----------------------------------------------|:----------------|:-------------------------------------------------------------------------------------------------|:--------------------------------------|
| Say           | Say                    | Prints a message to all players                | Moderator       | ['RunnerPlayer commandSource', 'string message']                                                 | {}                                    |
| SayToPlayer   | SayToPlayer            | Prints a message to all players                | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string message']                          | {}                                    |
| AnnounceShort | AnnounceShort          | Prints a short announce to all players         | Moderator       | ['RunnerPlayer commandSource', 'string message']                                                 | {}                                    |
| AnnounceLong  | AnnounceLong           | Prints a long announce to all players          | Moderator       | ['RunnerPlayer commandSource', 'string message']                                                 | {}                                    |
| Message       | Message                | Messages a specific player                     | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string message', 'float? timeout = null'] | {'timeout': 'null'}                   |
| Clear         | Clear                  | Clears the chat                                | Moderator       | ['RunnerPlayer commandSource']                                                                   | {}                                    |
| Kick          | Kick                   | Kicks a player                                 | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? reason = null']                   | {'reason': 'null'}                    |
| Ban           | Ban                    | Bans a player                                  | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target']                                            | {}                                    |
| Kill          | Kill                   | Kills a player                                 | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| Gag           | Gag                    | Gags a player                                  | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| Ungag         | Ungag                  | Ungags a player                                | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| Mute          | Mute                   | Mutes a player                                 | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| Unmute        | Unmute                 | Unmutes a player                               | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| Silence       | Silence                | Mutes and gags a player                        | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| Unsilence     | Unsilence              | Unmutes and ungags a player                    | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| LockSpawn     | LockSpawn              | Prevents a player or all players from spawning | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer? target = null', 'string? message = null']          | {'target': 'null', 'message': 'null'} |
| UnlockSpawn   | UnlockSpawn            | Allows a player or all players to spawn        | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer? target = null', 'string? message = null']          | {'target': 'null', 'message': 'null'} |
| tp2me         | TeleportPlayerToMe     | Teleports a player to you                      | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target']                                            | {}                                    |
| tpme2         | TeleportMeToPlayer     | Teleports you to a player                      | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target']                                            | {}                                    |
| tp            | TeleportPlayerToPlayer | Teleports a player to another player           | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'RunnerPlayer destination']                | {}                                    |
| tp2pos        | TeleportPlayerToPos    | Teleports a player to a position               | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'int x', 'int y', 'int z']                 | {}                                    |
| tpme2pos      | TeleportMeToPos        | Teleports you to a position                    | Moderator       | ['RunnerPlayer commandSource', 'int x', 'int y', 'int z']                                        | {}                                    |
| freeze        | Freeze                 | Freezes a player                               | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| unfreeze      | Unfreeze               | Unfreezes a player                             | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| Inspect       | Inspect                | Inspects a player or stops inspection          | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer? target = null']                                    | {'target': 'null'}                    |
# 1 Modules in ModuleUpdates.cs

| Description                                                      | Version   |
|:-----------------------------------------------------------------|:----------|
| Check for and download module updates from the module repository | 1.0.0     |
# 1 Modules in MOTD.cs

| Description                                              | Version   |
|:---------------------------------------------------------|:----------|
| Show a message of the day to players who join the server | 1.0.0     |

## Commands
| Command   | Function Name   | Description    | Allowed Roles   | Parameters                                    | Defaults   |
|:----------|:----------------|:---------------|:----------------|:----------------------------------------------|:-----------|
| setmotd   | SetMOTD         | Sets the MOTD  | Admin           | ['RunnerPlayer commandSource', 'string motd'] | {}         |
| motd      | ShowMOTD        | Shows the MOTD |                 | ['RunnerPlayer commandSource']                | {}         |
# 1 Modules in PermissionsCommands.cs

| Description                                                   | Version   |
|:--------------------------------------------------------------|:----------|
| Provide addperm and removeperm commands for PlayerPermissions | 1.0.0     |

## Commands
| Command    | Function Name           | Description                          | Allowed Roles    | Parameters                                                                | Defaults                 |
|:-----------|:------------------------|:-------------------------------------|:-----------------|:--------------------------------------------------------------------------|:-------------------------|
| addperm    | AddPermissionCommand    | Adds a permission to a player        | Admin            | ['RunnerPlayer commandSource', 'RunnerPlayer player', 'Roles permission'] | {}                       |
| removeperm | RemovePermissionCommand | Removes a permission from a player   | Admin            | ['RunnerPlayer commandSource', 'RunnerPlayer player', 'Roles permission'] | {}                       |
| clearperms | ClearPermissionCommand  | Removes all permission from a player | Admin            | ['RunnerPlayer commandSource', 'RunnerPlayer player']                     | {}                       |
| listperms  | ListPermissionCommand   | Lists player permissions             | Admin, Moderator | ['RunnerPlayer commandSource', 'RunnerPlayer? targetPlayer = null']       | {'targetPlayer': 'null'} |
# 1 Modules in PlayerFinder.cs

| Description                                                       | Version   |
|:------------------------------------------------------------------|:----------|
| Library functions for finding players by partial names or SteamID | 1.0.0     |
# 1 Modules in PlayerPermissions.cs

| Description                                     | Version   |
|:------------------------------------------------|:----------|
| Library for persistent server roles for players | 1.0.0     |
# 1 Modules in ProfanityFilter.cs

| Description                             | Version   |
|:----------------------------------------|:----------|
| Bad word filter to remove chat messages | 1.0.0     |
# 1 Modules in RichText.cs

| Description                        | Version   |
|:-----------------------------------|:----------|
| Library for easily using Rich Text | 1.0.0     |
# 1 Modules in Rotation.cs

| Description                                            | Version   |
|:-------------------------------------------------------|:----------|
| Configure the map and game mode rotation of the server | 1.0.0     |
# 1 Modules in SpectateControl.cs

| Description                           | Version   |
|:--------------------------------------|:----------|
| Allow only specific Roles to spectate | 1.0.0     |
# 1 Modules in Voting.cs

| Description               | Version   |
|:--------------------------|:----------|
| Simple chat voting system | 1.0.0     |

## Commands
| Command   | Function Name    | Description         | Allowed Roles   | Parameters                                                      | Defaults   |
|:----------|:-----------------|:--------------------|:----------------|:----------------------------------------------------------------|:-----------|
| vote      | StartVoteCommand | Votes for an option | Moderator       | ['RunnerPlayer commandSource', 'string text', 'string options'] | {}         |
# 0 Modules in .NETCoreApp,Version=v6.0.AssemblyAttributes.cs

| Description   | Version   |
|---------------|-----------|
# 0 Modules in BattleBitBaseModules.AssemblyInfo.cs

| Description   | Version   |
|---------------|-----------|
# 0 Modules in .NETCoreApp,Version=v6.0.AssemblyAttributes.cs

| Description   | Version   |
|---------------|-----------|
# 0 Modules in BattleBitBaseModules.AssemblyInfo.cs

| Description   | Version   |
|---------------|-----------|
# 0 Modules in ExampleModule.cs

| Description   | Version   |
|---------------|-----------|
# 0 Modules in ExampleModule2.cs

| Description   | Version   |
|---------------|-----------|
# 0 Modules in ExampleModuleIntegration.cs

| Description   | Version   |
|---------------|-----------|
# 0 Modules in Zombies.cs

| Description   | Version   |
|---------------|-----------|

## Commands
| Command    | Function Name         | Description                                     | Allowed Roles   | Parameters                                     | Defaults   |
|:-----------|:----------------------|:------------------------------------------------|:----------------|:-----------------------------------------------|:-----------|
| fullgear   | FullGearCommand       | Gives you full gear                             | Admin           | ['RunnerPlayer player']                        | {}         |
| addtickets | AddTicketsCommand     | Adds tickets to zombies                         | Admin           | ['RunnerPlayer player', 'int tickets']         | {}         |
| list       | ListCommand           | List all players and their status               |                 | ['RunnerPlayer player']                        | {}         |
| zombie     | ZombieCommand         | Check whether you're a zombie or not            |                 | ['RunnerPlayer player']                        | {}         |
| switch     | SwitchCommand         | Switch a player to the other team.              | Moderator       | ['RunnerPlayer source', 'RunnerPlayer target'] | {}         |
| afk        | LastHumanAFKOrCamping | Make zombies win because humans camp or are AFK | Moderator       | ['RunnerPlayer caller']                        | {}         |
| resetbuild | ResetBuildCommand     | Reset the build phase.                          | Moderator       | ['RunnerPlayer caller']                        | {}         |
| map        | MapCommand            | Current map name                                |                 | ['RunnerPlayer caller']                        | {}         |
| pos        | PosCommand            | Current position                                | Admin           | ['RunnerPlayer caller']                        | {}         |
# 1 Modules in Telemetry.cs

| Description   | Version   |
|:--------------|:----------|
| Telemetry     | 1.1.0     |
# 1 Modules in AdvancedVoting.cs

| Description               | Version   |
|:--------------------------|:----------|
| More chat voting commands | 2.0.0     |

## Commands
| Command      | Function Name              | Description                   | Allowed Roles   | Parameters                                                             | Defaults   |
|:-------------|:---------------------------|:------------------------------|:----------------|:-----------------------------------------------------------------------|:-----------|
| votemap      | StartMapVoteCommand        | Starts a vote for a map       |                 | ['RunnerPlayer commandSource', 'string mapName']                       | {}         |
| votegamemode | StartGameModeVoteCommand   | Starts a vote for a gamemode  |                 | ['RunnerPlayer commandSource', 'string gameModeName']                  | {}         |
| votemaptime  | StartMapTimeVoteCommand    | Starts a vote for map time    |                 | ['RunnerPlayer commandSource', 'string dayTime']                       | {}         |
| voterestart  | StartMapRestartVoteCommand | Starts a vote for map restart |                 | ['RunnerPlayer commandSource']                                         | {}         |
| voteban      | StartVoteBanCommand        | Starts a voteban for a player |                 | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string reason'] | {}         |
# 1 Modules in BluscreamLib.cs

| Description         | Version   |
|:--------------------|:----------|
| Bluscream's Library | 2.0.0     |
# 1 Modules in ConsoleLogger.cs

| Description   | Version   |
|:--------------|:----------|
| ConsoleLogger | 1.0.0     |
# 1 Modules in DiscordStatus.cs

| Description                                                                                                                     |   Version |
|:--------------------------------------------------------------------------------------------------------------------------------|----------:|
| Connects each server to a Discord Bot, and updates the Discord Bot's status with the server's player-count and map information. |       1.2 |
# 1 Modules in GameModeRotation.cs

| Description                                                                                                                                     |   Version |
|:------------------------------------------------------------------------------------------------------------------------------------------------|----------:|
| This version of gamemode rotation allows you to set up a different set of gamemodes each match, forcing a diversity of gamemodes of the server. |       1.1 |

## Commands
| Command   | Function Name   | Description                         | Allowed Roles   | Parameters                     | Defaults   |
|:----------|:----------------|:------------------------------------|:----------------|:-------------------------------|:-----------|
| GameModes | GameModes       | Shows the current gamemode rotation |                 | ['RunnerPlayer commandSource'] | {}         |
# 1 Modules in Logger.cs

| Description   | Version   |
|:--------------|:----------|
| Logger        | 2.0.0     |

## Commands
| Command    | Function Name   | Description                  | Allowed Roles   | Parameters                                             | Defaults   |
|:-----------|:----------------|:-----------------------------|:----------------|:-------------------------------------------------------|:-----------|
| playerbans | GetPlayerBans   | Lists bans of a player       |                 | ['RunnerPlayer commandSource', 'RunnerPlayer _player'] | {}         |
| playerinfo | GetPlayerInfo   | Displays info about a player |                 | ['RunnerPlayer commandSource', 'RunnerPlayer player']  | {}         |
# 1 Modules in MapRotation.cs

| Description                                                                                                                                                                                                                                                                                                                                                                               | Version   |
|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:----------|
| Adds a small tweak to the map rotation so that maps that were just played take more time to appear again, this works by counting how many matches happened since the maps were last played and before getting to the voting screen, the n least played ones are picked to appear on the voting screen . It also adds a command so that any player can know what maps are in the rotation. | 1.4.2     |

## Commands
| Command    | Function Name   | Description                                                                                | Allowed Roles   | Parameters                                        | Defaults   |
|:-----------|:----------------|:-------------------------------------------------------------------------------------------|:----------------|:--------------------------------------------------|:-----------|
| Maps       | Maps            | Shows the current map rotation                                                             |                 | ['RunnerPlayer commandSource']                    | {}         |
| AddMap     | AddMap          | Adds a map in the current rotation                                                         | Admin           | ['RunnerPlayer commandSource', 'string map']      | {}         |
| RemoveMap  | RemoveMap       | Removes a map from the current rotation                                                    | Admin           | ['RunnerPlayer commandSource', 'string map']      | {}         |
| AddGMMaps  | AddGMMaps       | Adds every map that supports the selected gamemode at the current map size to the rotation | Admin           | ['RunnerPlayer commandSource', 'string gamemode'] | {}         |
| MapCleanup | MapCleanup      | Removes a maps that don't support current gamemodes at current map size                    | Admin           | ['RunnerPlayer commandSource']                    | {}         |
| M          | M               | Shows how many matches since the last time a map was played                                |                 | ['RunnerPlayer commandSource']                    | {}         |
| CM         | CM              | Shows the Current Map name returned by Server.map                                          |                 | ['RunnerPlayer commandSource']                    | {}         |
# 1 Modules in MongoDBLogging.cs

| Description                                                                                                                                                                                              | Version   |
|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:----------|
| Provides the means for users to leverage MongoDB for Logging certain actions within their BattleBit Server. The module has out-of-the-box for ChatLogs, ConnectionLogs, PlayerReportLogs, ServerAPI logs | 1.1.4     |
# 1 Modules in MoreCommands.cs

| Description   | Version   |
|:--------------|:----------|
| More Commands | 2.0.0     |

## Commands
| Command       | Function Name     | Description                             | Allowed Roles   | Parameters                                                                                                     | Defaults                                                    |
|:--------------|:------------------|:----------------------------------------|:----------------|:---------------------------------------------------------------------------------------------------------------|:------------------------------------------------------------|
| map           | SetMap            | Changes the map                         |                 | ['RunnerPlayer commandSource', 'string? mapName = null', 'string? gameMode = null', 'string? dayNight = null'] | {'mapName': 'null', 'gameMode': 'null', 'dayNight': 'null'} |
| gamemode      | SetGameMode       | Changes the gamemode                    |                 | ['RunnerPlayer commandSource', 'string gameMode', 'string? dayNight = null']                                   | {'dayNight': 'null'}                                        |
| time          | SetMapTime        | Changes the map time                    |                 | ['RunnerPlayer commandSource', 'string dayNight']                                                              | {}                                                          |
| maprestart    | RestartMap        | Restarts the current map                |                 | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| allowvotetime | SetMapVoteTime    | Changes the allowed map times for votes |                 | ['RunnerPlayer commandSource', 'string dayNightAll']                                                           | {}                                                          |
| listmaps      | ListMaps          | Lists all maps                          |                 | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| listmodes     | ListGameMods      | Lists all gamemodes                     |                 | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| listsizes     | ListGameSizes     | Lists all game sizes                    |                 | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| listmodules   | ListModules       | Lists all loaded modules                |                 | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| start         | ForceStartRound   | Force starts the round                  |                 | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| end           | ForceEndRound     | Force ends the round                    |                 | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| exec          | ExecServerCommand | Executes a command on the server        |                 | ['RunnerPlayer commandSource', 'string command']                                                               | {}                                                          |
| bots          | SpawnBotCommand   | Spawns bots                             |                 | ['RunnerPlayer commandSource', 'int amount = 1']                                                               | {'amount': '1'}                                             |
| nobots        | KickBotsCommand   | Kicks all bots                          |                 | ['RunnerPlayer commandSource', 'int amount = 999']                                                             | {'amount': '999'}                                           |
| fire          | BotsFireCommand   | Toggles bots firing                     |                 | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| pos           | PosCommand        | Current position (logs to file)         |                 | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
# 1 Modules in ReconOnly.cs

| Description                                                   |   Version |
|:--------------------------------------------------------------|----------:|
| This module forces players to become the Recon (Sniper) role. |       1.2 |
# 1 Modules in RegionManager.cs

| Description   | Version   |
|:--------------|:----------|
| RegionManager | 1.0.0     |
# 1 Modules in Snipers.cs

| Description   | Version   |
|:--------------|:----------|
| Snipers       | 1.1.0     |
# 1 Modules in TempBans.cs

| Description        | Version   |
|:-------------------|:----------|
| Basic temp banning | 1.0.0     |

## Commands
| Command      | Function Name         | Description                                              | Allowed Roles   | Parameters                                                                                                                  | Defaults                           |
|:-------------|:----------------------|:---------------------------------------------------------|:----------------|:----------------------------------------------------------------------------------------------------------------------------|:-----------------------------------|
| tempban      | TempBanCommand        | Bans a player for a specified time period                |                 | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string duration', 'string? reason = null', 'string? note = null']    | {'reason': 'null', 'note': 'null'} |
| tempbanid    | TempBanIdCommand      | Bans a player for a specified time period by Steam ID 64 |                 | ['RunnerPlayer commandSource', 'string targetSteamId64', 'string duration', 'string? reason = null', 'string? note = null'] | {'reason': 'null', 'note': 'null'} |
| untempban    | UnTempBanCommand      | Unbans a player that is temporary banned                 |                 | ['RunnerPlayer commandSource', 'RunnerPlayer target']                                                                       | {}                                 |
| listtempbans | ListTempBannedCommand | Lists players that are temporarily banned                |                 | ['RunnerPlayer commandSource']                                                                                              | {}                                 |
# 0 Modules in .NETCoreApp,Version=v7.0.AssemblyAttributes.cs

| Description   | Version   |
|---------------|-----------|
# 0 Modules in Modules.AssemblyInfo.cs

| Description   | Version   |
|---------------|-----------|
# 0 Modules in .NETCoreApp,Version=v7.0.AssemblyAttributes.cs

| Description   | Version   |
|---------------|-----------|
# 0 Modules in Modules.AssemblyInfo.cs

| Description   | Version   |
|---------------|-----------|