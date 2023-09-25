
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
| allow add    | void            | Adds a player to the allowlist      | Moderator       | ['RunnerPlayer commandSource', 'ulong steamID'] | {}         |
| allow remove | void            | Removes a player from the allowlist | Moderator       | ['RunnerPlayer commandSource', 'ulong steamID'] | {}         |

## Public Methods
| Function Name   | Parameters                                       | Defaults   |
|:----------------|:-------------------------------------------------|:-----------|
|                 |                                                  |            |
|                 |                                                  |            |
|                 |                                                  |            |
| Task            | ['ulong steamID', 'PlayerJoiningArguments args'] | {}         |
| AllowAdd        | ['RunnerPlayer commandSource', 'ulong steamID']  | {}         |
| AllowRemove     | ['RunnerPlayer commandSource', 'ulong steamID']  | {}         |
|                 |                                                  |            |
|                 |                                                  |            |
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
| Command   | Function Name   | Description                       | Allowed Roles   | Parameters                                | Defaults      |
|:----------|:----------------|:----------------------------------|:----------------|:------------------------------------------|:--------------|
| help      | void            | Shows this help message           |                 | ['RunnerPlayer player', 'int page = 1']   | {'page': '1'} |
| cmdhelp   | void            | Shows help for a specific command |                 | ['RunnerPlayer player', 'string command'] | {}            |
| modules   | void            | Lists all loaded modules          | Admin           | ['RunnerPlayer commandSource']            | {}            |

## Public Methods
| Function Name      | Parameters                                                       | Defaults      |
|:-------------------|:-----------------------------------------------------------------|:--------------|
|                    |                                                                  |               |
|                    |                                                                  |               |
|                    |                                                                  |               |
|                    |                                                                  |               |
|                    |                                                                  |               |
|                    |                                                                  |               |
|                    |                                                                  |               |
| void               | ['']                                                             | {}            |
| Register           | ['BattleBitModule module']                                       | {}            |
| Task               | ['RunnerPlayer player', 'ChatChannel channel', 'string message'] | {}            |
| HelpCommand        | ['RunnerPlayer player', 'int page = 1']                          | {'page': '1'} |
| CommandHelpCommand | ['RunnerPlayer player', 'string command']                        | {}            |
| ListModules        | ['RunnerPlayer commandSource']                                   | {}            |
|                    |                                                                  |               |
|                    |                                                                  |               |
|                    |                                                                  |               |
|                    |                                                                  |               |
|                    |                                                                  |               |
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
| Command       | Function Name   | Description                                    | Allowed Roles   | Parameters                                                                                       | Defaults                              |
|:--------------|:----------------|:-----------------------------------------------|:----------------|:-------------------------------------------------------------------------------------------------|:--------------------------------------|
| Say           | void            | Prints a message to all players                | Moderator       | ['RunnerPlayer commandSource', 'string message']                                                 | {}                                    |
| SayToPlayer   | void            | Prints a message to all players                | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string message']                          | {}                                    |
| AnnounceShort | void            | Prints a short announce to all players         | Moderator       | ['RunnerPlayer commandSource', 'string message']                                                 | {}                                    |
| AnnounceLong  | void            | Prints a long announce to all players          | Moderator       | ['RunnerPlayer commandSource', 'string message']                                                 | {}                                    |
| Message       | void            | Messages a specific player                     | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string message', 'float? timeout = null'] | {'timeout': 'null'}                   |
| Clear         | void            | Clears the chat                                | Moderator       | ['RunnerPlayer commandSource']                                                                   | {}                                    |
| Kick          | void            | Kicks a player                                 | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? reason = null']                   | {'reason': 'null'}                    |
| Ban           | void            | Bans a player                                  | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target']                                            | {}                                    |
| Kill          | void            | Kills a player                                 | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| Gag           | void            | Gags a player                                  | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| Ungag         | void            | Ungags a player                                | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| Mute          | void            | Mutes a player                                 | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| Unmute        | void            | Unmutes a player                               | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| Silence       | void            | Mutes and gags a player                        | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| Unsilence     | void            | Unmutes and ungags a player                    | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| LockSpawn     | void            | Prevents a player or all players from spawning | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer? target = null', 'string? message = null']          | {'target': 'null', 'message': 'null'} |
| UnlockSpawn   | void            | Allows a player or all players to spawn        | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer? target = null', 'string? message = null']          | {'target': 'null', 'message': 'null'} |
| tp2me         | void            | Teleports a player to you                      | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target']                                            | {}                                    |
| tpme2         | void            | Teleports you to a player                      | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target']                                            | {}                                    |
| tp            | void            | Teleports a player to another player           | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'RunnerPlayer destination']                | {}                                    |
| tp2pos        | void            | Teleports a player to a position               | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'int x', 'int y', 'int z']                 | {}                                    |
| tpme2pos      | void            | Teleports you to a position                    | Moderator       | ['RunnerPlayer commandSource', 'int x', 'int y', 'int z']                                        | {}                                    |
| freeze        | void            | Freezes a player                               | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| unfreeze      | void            | Unfreezes a player                             | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| Inspect       | void            | Inspects a player or stops inspection          | Moderator       | ['RunnerPlayer commandSource', 'RunnerPlayer? target = null']                                    | {'target': 'null'}                    |

## Public Methods
| Function Name          | Parameters                                                                                       | Defaults                              |
|:-----------------------|:-------------------------------------------------------------------------------------------------|:--------------------------------------|
|                        |                                                                                                  |                                       |
|                        |                                                                                                  |                                       |
| void                   | ['']                                                                                             | {}                                    |
| Task                   | ['']                                                                                             | {}                                    |
| Say                    | ['RunnerPlayer commandSource', 'string message']                                                 | {}                                    |
| SayToPlayer            | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string message']                          | {}                                    |
| AnnounceShort          | ['RunnerPlayer commandSource', 'string message']                                                 | {}                                    |
| AnnounceLong           | ['RunnerPlayer commandSource', 'string message']                                                 | {}                                    |
| Message                | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string message', 'float? timeout = null'] | {'timeout': 'null'}                   |
| Clear                  | ['RunnerPlayer commandSource']                                                                   | {}                                    |
| Kick                   | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? reason = null']                   | {'reason': 'null'}                    |
| Ban                    | ['RunnerPlayer commandSource', 'RunnerPlayer target']                                            | {}                                    |
| Kill                   | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| Gag                    | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| Ungag                  | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| Mute                   | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| Unmute                 | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| Silence                | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| Unsilence              | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| LockSpawn              | ['RunnerPlayer commandSource', 'RunnerPlayer? target = null', 'string? message = null']          | {'target': 'null', 'message': 'null'} |
| UnlockSpawn            | ['RunnerPlayer commandSource', 'RunnerPlayer? target = null', 'string? message = null']          | {'target': 'null', 'message': 'null'} |
| TeleportPlayerToMe     | ['RunnerPlayer commandSource', 'RunnerPlayer target']                                            | {}                                    |
| TeleportMeToPlayer     | ['RunnerPlayer commandSource', 'RunnerPlayer target']                                            | {}                                    |
| TeleportPlayerToPlayer | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'RunnerPlayer destination']                | {}                                    |
| TeleportPlayerToPos    | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'int x', 'int y', 'int z']                 | {}                                    |
| TeleportMeToPos        | ['RunnerPlayer commandSource', 'int x', 'int y', 'int z']                                        | {}                                    |
| Freeze                 | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| Unfreeze               | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string? message = null']                  | {'message': 'null'}                   |
| Inspect                | ['RunnerPlayer commandSource', 'RunnerPlayer? target = null']                                    | {'target': 'null'}                    |
| Task                   | ['RunnerPlayer player', 'ChatChannel channel', 'string msg']                                     | {}                                    |
| Task                   | ['RunnerPlayer player', 'OnPlayerSpawnArguments request']                                        | {}                                    |
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
| setmotd   | void            | Sets the MOTD  | Admin           | ['RunnerPlayer commandSource', 'string motd'] | {}         |
| motd      | void            | Shows the MOTD |                 | ['RunnerPlayer commandSource']                | {}         |

## Public Methods
| Function Name   | Parameters                                    | Defaults   |
|:----------------|:----------------------------------------------|:-----------|
|                 |                                               |            |
|                 |                                               |            |
|                 |                                               |            |
| void            | ['']                                          | {}         |
| Task            | ['GameState oldState', 'GameState newState']  | {}         |
| Task            | ['RunnerPlayer player']                       | {}         |
| SetMOTD         | ['RunnerPlayer commandSource', 'string motd'] | {}         |
| ShowMOTD        | ['RunnerPlayer commandSource']                | {}         |
|                 |                                               |            |
|                 |                                               |            |
|                 |                                               |            |
# 1 Modules in PermissionsCommands.cs

| Description                                                   | Version   |
|:--------------------------------------------------------------|:----------|
| Provide addperm and removeperm commands for PlayerPermissions | 1.0.0     |

## Commands
| Command    | Function Name   | Description                          | Allowed Roles    | Parameters                                                                | Defaults                 |
|:-----------|:----------------|:-------------------------------------|:-----------------|:--------------------------------------------------------------------------|:-------------------------|
| addperm    | void            | Adds a permission to a player        | Admin            | ['RunnerPlayer commandSource', 'RunnerPlayer player', 'Roles permission'] | {}                       |
| removeperm | void            | Removes a permission from a player   | Admin            | ['RunnerPlayer commandSource', 'RunnerPlayer player', 'Roles permission'] | {}                       |
| clearperms | void            | Removes all permission from a player | Admin            | ['RunnerPlayer commandSource', 'RunnerPlayer player']                     | {}                       |
| listperms  | void            | Lists player permissions             | Admin, Moderator | ['RunnerPlayer commandSource', 'RunnerPlayer? targetPlayer = null']       | {'targetPlayer': 'null'} |

## Public Methods
| Function Name           | Parameters                                                                | Defaults                 |
|:------------------------|:--------------------------------------------------------------------------|:-------------------------|
|                         |                                                                           |                          |
|                         |                                                                           |                          |
|                         |                                                                           |                          |
| void                    | ['']                                                                      | {}                       |
| AddPermissionCommand    | ['RunnerPlayer commandSource', 'RunnerPlayer player', 'Roles permission'] | {}                       |
| RemovePermissionCommand | ['RunnerPlayer commandSource', 'RunnerPlayer player', 'Roles permission'] | {}                       |
| ClearPermissionCommand  | ['RunnerPlayer commandSource', 'RunnerPlayer player']                     | {}                       |
| ListPermissionCommand   | ['RunnerPlayer commandSource', 'RunnerPlayer? targetPlayer = null']       | {'targetPlayer': 'null'} |
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
| Command   | Function Name   | Description         | Allowed Roles   | Parameters                                                      | Defaults   |
|:----------|:----------------|:--------------------|:----------------|:----------------------------------------------------------------|:-----------|
| vote      | void            | Votes for an option | Moderator       | ['RunnerPlayer commandSource', 'string text', 'string options'] | {}         |

## Public Methods
| Function Name    | Parameters                                                      | Defaults   |
|:-----------------|:----------------------------------------------------------------|:-----------|
|                  |                                                                 |            |
|                  |                                                                 |            |
|                  |                                                                 |            |
|                  |                                                                 |            |
| void             | ['']                                                            | {}         |
| StartVoteCommand | ['RunnerPlayer commandSource', 'string text', 'string options'] | {}         |
| async            | ['RunnerPlayer player', 'ChatChannel channel', 'string msg']    | {}         |
|                  |                                                                 |            |
|                  |                                                                 |            |
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
| Command    | Function Name   | Description                                     | Allowed Roles   | Parameters                                     | Defaults   |
|:-----------|:----------------|:------------------------------------------------|:----------------|:-----------------------------------------------|:-----------|
| fullgear   | void            | Gives you full gear                             | Admin           | ['RunnerPlayer player']                        | {}         |
| addtickets | void            | Adds tickets to zombies                         | Admin           | ['RunnerPlayer player', 'int tickets']         | {}         |
| list       | void            | List all players and their status               |                 | ['RunnerPlayer player']                        | {}         |
| zombie     | void            | Check whether you're a zombie or not            |                 | ['RunnerPlayer player']                        | {}         |
| switch     | async           | Switch a player to the other team.              | Moderator       | ['RunnerPlayer source', 'RunnerPlayer target'] | {}         |
| afk        | async           | Make zombies win because humans camp or are AFK | Moderator       | ['RunnerPlayer caller']                        | {}         |
| resetbuild | void            | Reset the build phase.                          | Moderator       | ['RunnerPlayer caller']                        | {}         |
| map        | void            | Current map name                                |                 | ['RunnerPlayer caller']                        | {}         |
| pos        | void            | Current position                                | Admin           | ['RunnerPlayer caller']                        | {}         |

## Public Methods
| Function Name       | Parameters                                                                         | Defaults   |
|:--------------------|:-----------------------------------------------------------------------------------|:-----------|
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
| bool                | ['Vector2[] polygon', 'Vector2 point']                                             | {}         |
| void                | ['']                                                                               | {}         |
| async               | ['']                                                                               | {}         |
| Task                | ['RunnerPlayer player', 'GameRole requestedRole']                                  | {}         |
| async               | ['RunnerPlayer player']                                                            | {}         |
| Task                | ['RunnerPlayer player', 'Team requestedTeam']                                      | {}         |
| Task                | ['ulong steamID', 'PlayerJoiningArguments args']                                   | {}         |
| async               | ['RunnerPlayer player', 'OnPlayerSpawnArguments request']                          | {}         |
| async               | ['RunnerPlayer player']                                                            | {}         |
| Task                | ['RunnerPlayer player']                                                            | {}         |
| Task                | ['OnPlayerKillArguments<RunnerPlayer> args']                                       | {}         |
| async               | ['RunnerPlayer player']                                                            | {}         |
| Task                | ['long oldSessionID', 'long newSessionID']                                         | {}         |
| Task                | ['RunnerPlayer player', 'ChatChannel channel', 'string msg']                       | {}         |
| async               | ['Squad<RunnerPlayer> squad', 'int newPoints']                                     | {}         |
| FullGearCommand     | ['RunnerPlayer player']                                                            | {}         |
| AddTicketsCommand   | ['RunnerPlayer player', 'int tickets']                                             | {}         |
| ListCommand         | ['RunnerPlayer player']                                                            | {}         |
| ZombieCommand       | ['RunnerPlayer player']                                                            | {}         |
| void                | ['RunnerPlayer source', 'RunnerPlayer target']                                     | {}         |
| void                | ['RunnerPlayer caller']                                                            | {}         |
| ResetBuildCommand   | ['RunnerPlayer caller']                                                            | {}         |
| MapCommand          | ['RunnerPlayer caller']                                                            | {}         |
| PosCommand          | ['RunnerPlayer caller']                                                            | {}         |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
| loadout             | ['RunnerPlayer player', 'ZombiePersistence loadout']                               | {}         |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
| requestedPercentage | ['string name', 'float requestedPercentage', 'Action<RunnerPlayer> applyToPlayer'] | {}         |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
| Reset               | ['']                                                                               | {}         |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
|                     |                                                                                    |            |
# 1 Modules in Telemetry.cs

| Description   | Version   |
|:--------------|:----------|
| Telemetry     | 1.1.0     |
# 1 Modules in AdvancedVoting.cs

| Description               | Version   |
|:--------------------------|:----------|
| More chat voting commands | 2.0.0     |

## Commands
| Command      | Function Name   | Description                   | Allowed Roles   | Parameters                                                             | Defaults   |
|:-------------|:----------------|:------------------------------|:----------------|:-----------------------------------------------------------------------|:-----------|
| votemap      | void            | Starts a vote for a map       |                 | ['RunnerPlayer commandSource', 'string mapName']                       | {}         |
| votegamemode | void            | Starts a vote for a gamemode  |                 | ['RunnerPlayer commandSource', 'string gameModeName']                  | {}         |
| votemaptime  | void            | Starts a vote for map time    |                 | ['RunnerPlayer commandSource', 'string dayTime']                       | {}         |
| voterestart  | void            | Starts a vote for map restart |                 | ['RunnerPlayer commandSource']                                         | {}         |
| voteban      | void            | Starts a voteban for a player |                 | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string reason'] | {}         |

## Public Methods
| Function Name              | Parameters                                                                                                        | Defaults                       |
|:---------------------------|:------------------------------------------------------------------------------------------------------------------|:-------------------------------|
|                            |                                                                                                                   |                                |
| ModuleInfo                 | ['']                                                                                                              | {'ModuleInfo': 'new'}          |
|                            |                                                                                                                   |                                |
|                            |                                                                                                                   |                                |
|                            |                                                                                                                   |                                |
|                            |                                                                                                                   |                                |
|                            |                                                                                                                   |                                |
|                            |                                                                                                                   |                                |
|                            |                                                                                                                   |                                |
| void                       | ['']                                                                                                              | {}                             |
| StartMapVoteCommand        | ['RunnerPlayer commandSource', 'string mapName']                                                                  | {}                             |
| StartGameModeVoteCommand   | ['RunnerPlayer commandSource', 'string gameModeName']                                                             | {}                             |
| StartMapTimeVoteCommand    | ['RunnerPlayer commandSource', 'string dayTime']                                                                  | {}                             |
| StartMapRestartVoteCommand | ['RunnerPlayer commandSource']                                                                                    | {}                             |
| StartVoteBanCommand        | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string reason']                                            | {}                             |
| string                     | ['string input', 'int winnerVotes', 'int totalVotes', 'RunnerServer server']                                      | {}                             |
| StartVote                  | ['RunnerPlayer commandSource', 'string text', 'string options', 'Action<int', 'int', 'string> voteEndedCallback'] | {}                             |
| async                      | ['RunnerPlayer player', 'ChatChannel channel', 'string msg']                                                      | {}                             |
|                            |                                                                                                                   |                                |
|                            |                                                                                                                   |                                |
|                            |                                                                                                                   |                                |
|                            |                                                                                                                   |                                |
| votemap                    | ['']                                                                                                              | {'AllowedRoles': 'Extensions'} |
| votegamemode               | ['']                                                                                                              | {'AllowedRoles': 'Extensions'} |
| votemaptime                | ['']                                                                                                              | {'AllowedRoles': 'Extensions'} |
| voterestart                | ['']                                                                                                              | {'AllowedRoles': 'Extensions'} |
| voteban                    | ['']                                                                                                              | {'AllowedRoles': 'Extensions'} |
|                            |                                                                                                                   |                                |
|                            |                                                                                                                   |                                |
| VoteBanDuration            | ['30']                                                                                                            | {}                             |
# 1 Modules in BluscreamLib.cs

| Description         | Version   |
|:--------------------|:----------|
| Bluscream's Library | 2.0.0     |
# 1 Modules in ConsoleLogger.cs

| Description   | Version   |
|:--------------|:----------|
| ConsoleLogger | 2.0.0     |
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
| GameModes | void            | Shows the current gamemode rotation |                 | ['RunnerPlayer commandSource'] | {}         |

## Public Methods
| Function Name   | Parameters                                   | Defaults   |
|:----------------|:---------------------------------------------|:-----------|
|                 |                                              |            |
|                 |                                              |            |
|                 |                                              |            |
| void            | ['']                                         | {}         |
| Task            | ['']                                         | {}         |
| Task            | ['GameState oldState', 'GameState newState'] | {}         |
|                 |                                              |            |
| GameModes       | ['RunnerPlayer commandSource']               | {}         |
| string          | ['string Gamemode']                          | {}         |
|                 |                                              |            |
|                 |                                              |            |
# 1 Modules in Logger.cs

| Description   | Version   |
|:--------------|:----------|
| Logger        | 2.0.0     |

## Commands
| Command    | Function Name   | Description                  | Allowed Roles   | Parameters                                             | Defaults   |
|:-----------|:----------------|:-----------------------------|:----------------|:-------------------------------------------------------|:-----------|
| playerbans | async           | Lists bans of a player       |                 | ['RunnerPlayer commandSource', 'RunnerPlayer _player'] | {}         |
| playerinfo | async           | Displays info about a player |                 | ['RunnerPlayer commandSource', 'RunnerPlayer player']  | {}         |

## Public Methods
| Function Name        | Parameters                                                                           | Defaults                       |
|:---------------------|:-------------------------------------------------------------------------------------|:-------------------------------|
|                      |                                                                                      |                                |
| ModuleInfo           | ['']                                                                                 | {'ModuleInfo': 'new'}          |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
| void                 | ['RunnerPlayer commandSource', 'RunnerPlayer _player']                               | {}                             |
| void                 | ['RunnerPlayer commandSource', 'RunnerPlayer player']                                | {}                             |
| void                 | ['']                                                                                 | {}                             |
| Task                 | ['']                                                                                 | {}                             |
| async                | ['RunnerPlayer player']                                                              | {}                             |
| Task                 | ['RunnerPlayer player', 'ChatChannel channel', 'string msg']                         | {}                             |
| Task                 | ['RunnerPlayer player']                                                              | {}                             |
| Task                 | ['RunnerPlayer from', 'RunnerPlayer to', 'ReportReason reason', 'string additional'] | {}                             |
| Task                 | ['']                                                                                 | {}                             |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
| playerbans           | ['']                                                                                 | {'AllowedRoles': 'Extensions'} |
| playerinfo           | ['']                                                                                 | {'AllowedRoles': 'Extensions'} |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
| Dictionary           | ['']                                                                                 | {'randomReplacements': 'new'}  |
| OnApiModulesLoaded   | ['']                                                                                 | {}                             |
| OnApiConnected       | ['']                                                                                 | {}                             |
| OnApiDisconnected    | ['']                                                                                 | {}                             |
| OnPlayerConnected    | ['']                                                                                 | {}                             |
| OnPlayerDisconnected | ['']                                                                                 | {}                             |
| OnPlayerChatMessage  | ['']                                                                                 | {}                             |
| OnPlayerChatCommand  | ['']                                                                                 | {}                             |
| OnPlayerReported     | ['']                                                                                 | {}                             |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
| Response             | ['string json']                                                                      | {}                             |
|                      |                                                                                      |                                |
| string               | ['this Response self']                                                               | {}                             |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
|                      |                                                                                      |                                |
| BanResponse          | ['string json']                                                                      | {}                             |
|                      |                                                                                      |                                |
| string               | ['this BanResponse self']                                                            | {}                             |
# 1 Modules in MapRotation.cs

| Description                                                                                                                                                                                                                                                                                                                                                                               | Version   |
|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:----------|
| Adds a small tweak to the map rotation so that maps that were just played take more time to appear again, this works by counting how many matches happened since the maps were last played and before getting to the voting screen, the n least played ones are picked to appear on the voting screen . It also adds a command so that any player can know what maps are in the rotation. | 1.4.2     |

## Commands
| Command    | Function Name   | Description                                                                                | Allowed Roles   | Parameters                                        | Defaults   |
|:-----------|:----------------|:-------------------------------------------------------------------------------------------|:----------------|:--------------------------------------------------|:-----------|
| Maps       | void            | Shows the current map rotation                                                             |                 | ['RunnerPlayer commandSource']                    | {}         |
| AddMap     | void            | Adds a map in the current rotation                                                         | Admin           | ['RunnerPlayer commandSource', 'string map']      | {}         |
| RemoveMap  | void            | Removes a map from the current rotation                                                    | Admin           | ['RunnerPlayer commandSource', 'string map']      | {}         |
| AddGMMaps  | void            | Adds every map that supports the selected gamemode at the current map size to the rotation | Admin           | ['RunnerPlayer commandSource', 'string gamemode'] | {}         |
| MapCleanup | void            | Removes a maps that don't support current gamemodes at current map size                    | Admin           | ['RunnerPlayer commandSource']                    | {}         |
| M          | void            | Shows how many matches since the last time a map was played                                |                 | ['RunnerPlayer commandSource']                    | {}         |
| CM         | void            | Shows the Current Map name returned by Server.map                                          |                 | ['RunnerPlayer commandSource']                    | {}         |

## Public Methods
| Function Name   | Parameters                                        | Defaults   |
|:----------------|:--------------------------------------------------|:-----------|
|                 |                                                   |            |
|                 |                                                   |            |
|                 |                                                   |            |
|                 |                                                   |            |
| async           | ['']                                              | {}         |
| Task            | ['GameState oldState', 'GameState newState']      | {}         |
| void            | ['']                                              | {}         |
| Maps            | ['RunnerPlayer commandSource']                    | {}         |
| AddMap          | ['RunnerPlayer commandSource', 'string map']      | {}         |
| RemoveMap       | ['RunnerPlayer commandSource', 'string map']      | {}         |
| AddGMMaps       | ['RunnerPlayer commandSource', 'string gamemode'] | {}         |
| MapCleanup      | ['RunnerPlayer commandSource']                    | {}         |
| M               | ['RunnerPlayer commandSource']                    | {}         |
| CM              | ['RunnerPlayer commandSource']                    | {}         |
|                 |                                                   |            |
|                 |                                                   |            |
|                 |                                                   |            |
|                 |                                                   |            |
|                 |                                                   |            |
|                 |                                                   |            |
|                 |                                                   |            |
|                 |                                                   |            |
|                 |                                                   |            |
# 1 Modules in MongoDBLogging.cs

| Description                                                                                                                                                                                              | Version   |
|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:----------|
| Provides the means for users to leverage MongoDB for Logging certain actions within their BattleBit Server. The module has out-of-the-box for ChatLogs, ConnectionLogs, PlayerReportLogs, ServerAPI logs | 1.1.4     |
# 1 Modules in MoreCommands.cs

| Description   | Version   |
|:--------------|:----------|
| More Commands | 2.0.0     |

## Commands
| Command       | Function Name   | Description                             | Allowed Roles   | Parameters                                                                                                     | Defaults                                                    |
|:--------------|:----------------|:----------------------------------------|:----------------|:---------------------------------------------------------------------------------------------------------------|:------------------------------------------------------------|
| map           | void            | Changes the map                         |                 | ['RunnerPlayer commandSource', 'string? mapName = null', 'string? gameMode = null', 'string? dayNight = null'] | {'mapName': 'null', 'gameMode': 'null', 'dayNight': 'null'} |
| gamemode      | void            | Changes the gamemode                    |                 | ['RunnerPlayer commandSource', 'string gameMode', 'string? dayNight = null']                                   | {'dayNight': 'null'}                                        |
| time          | void            | Changes the map time                    |                 | ['RunnerPlayer commandSource', 'string dayNight']                                                              | {}                                                          |
| maprestart    | void            | Restarts the current map                |                 | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| allowvotetime | void            | Changes the allowed map times for votes |                 | ['RunnerPlayer commandSource', 'string dayNightAll']                                                           | {}                                                          |
| listmaps      | void            | Lists all maps                          |                 | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| listmodes     | void            | Lists all gamemodes                     |                 | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| listsizes     | void            | Lists all game sizes                    |                 | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| listmodules   | void            | Lists all loaded modules                |                 | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| start         | void            | Force starts the round                  |                 | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| end           | void            | Force ends the round                    |                 | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| exec          | void            | Executes a command on the server        |                 | ['RunnerPlayer commandSource', 'string command']                                                               | {}                                                          |
| bots          | void            | Spawns bots                             |                 | ['RunnerPlayer commandSource', 'int amount = 1']                                                               | {'amount': '1'}                                             |
| nobots        | void            | Kicks all bots                          |                 | ['RunnerPlayer commandSource', 'int amount = 999']                                                             | {'amount': '999'}                                           |
| fire          | void            | Toggles bots firing                     |                 | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| pos           | void            | Current position (logs to file)         |                 | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |

## Public Methods
| Function Name           | Parameters                                                                                                     | Defaults                                                    |
|:------------------------|:---------------------------------------------------------------------------------------------------------------|:------------------------------------------------------------|
|                         |                                                                                                                |                                                             |
| ModuleInfo              | ['']                                                                                                           | {'ModuleInfo': 'new'}                                       |
|                         |                                                                                                                |                                                             |
|                         |                                                                                                                |                                                             |
|                         |                                                                                                                |                                                             |
|                         |                                                                                                                |                                                             |
|                         |                                                                                                                |                                                             |
| void                    | ['']                                                                                                           | {}                                                          |
| GetCurrentMapInfoString | ['']                                                                                                           | {}                                                          |
| SetMap                  | ['RunnerPlayer commandSource', 'string? mapName = null', 'string? gameMode = null', 'string? dayNight = null'] | {'mapName': 'null', 'gameMode': 'null', 'dayNight': 'null'} |
| SetGameMode             | ['RunnerPlayer commandSource', 'string gameMode', 'string? dayNight = null']                                   | {'dayNight': 'null'}                                        |
| SetMapTime              | ['RunnerPlayer commandSource', 'string dayNight']                                                              | {}                                                          |
| RestartMap              | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| SetMapVoteTime          | ['RunnerPlayer commandSource', 'string dayNightAll']                                                           | {}                                                          |
| ListMaps                | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| ListGameMods            | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| ListGameSizes           | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| ListModules             | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| ForceStartRound         | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| ForceEndRound           | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| ExecServerCommand       | ['RunnerPlayer commandSource', 'string command']                                                               | {}                                                          |
| SpawnBotCommand         | ['RunnerPlayer commandSource', 'int amount = 1']                                                               | {'amount': '1'}                                             |
| KickBotsCommand         | ['RunnerPlayer commandSource', 'int amount = 999']                                                             | {'amount': '999'}                                           |
| BotsFireCommand         | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
| PosCommand              | ['RunnerPlayer commandSource']                                                                                 | {}                                                          |
|                         |                                                                                                                |                                                             |
| map                     | ['']                                                                                                           | {'AllowedRoles': 'Extensions'}                              |
| gamemode                | ['']                                                                                                           | {'AllowedRoles': 'Extensions'}                              |
| time                    | ['']                                                                                                           | {'AllowedRoles': 'Extensions'}                              |
| maprestart              | ['']                                                                                                           | {'AllowedRoles': 'Extensions'}                              |
| allowvotetime           | ['']                                                                                                           | {'AllowedRoles': 'Extensions'}                              |
| listmaps                | ['']                                                                                                           | {'AllowedRoles': 'Extensions'}                              |
| listmodes               | ['']                                                                                                           | {'AllowedRoles': 'Extensions'}                              |
| listsizes               | ['']                                                                                                           | {'AllowedRoles': 'Extensions'}                              |
| listmodules             | ['']                                                                                                           | {'AllowedRoles': 'Extensions'}                              |
| start                   | ['']                                                                                                           | {'AllowedRoles': 'Extensions'}                              |
| end                     | ['']                                                                                                           | {'AllowedRoles': 'Extensions'}                              |
| exec                    | ['']                                                                                                           | {'AllowedRoles': 'Extensions'}                              |
| bots                    | ['']                                                                                                           | {'AllowedRoles': 'Extensions'}                              |
| nobots                  | ['']                                                                                                           | {'AllowedRoles': 'Extensions'}                              |
| fire                    | ['']                                                                                                           | {'AllowedRoles': 'Extensions'}                              |
| pos                     | ['']                                                                                                           | {'AllowedRoles': 'Extensions'}                              |
|                         |                                                                                                                |                                                             |
|                         |                                                                                                                |                                                             |
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
| Command      | Function Name   | Description                                              | Allowed Roles   | Parameters                                                                                                                  | Defaults                           |
|:-------------|:----------------|:---------------------------------------------------------|:----------------|:----------------------------------------------------------------------------------------------------------------------------|:-----------------------------------|
| tempban      | void            | Bans a player for a specified time period                |                 | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string duration', 'string? reason = null', 'string? note = null']    | {'reason': 'null', 'note': 'null'} |
| tempbanid    | void            | Bans a player for a specified time period by Steam ID 64 |                 | ['RunnerPlayer commandSource', 'string targetSteamId64', 'string duration', 'string? reason = null', 'string? note = null'] | {'reason': 'null', 'note': 'null'} |
| untempban    | void            | Unbans a player that is temporary banned                 |                 | ['RunnerPlayer commandSource', 'RunnerPlayer target']                                                                       | {}                                 |
| listtempbans | void            | Lists players that are temporarily banned                |                 | ['RunnerPlayer commandSource']                                                                                              | {}                                 |

## Public Methods
| Function Name         | Parameters                                                                                                                                                   | Defaults                                                                 |
|:----------------------|:-------------------------------------------------------------------------------------------------------------------------------------------------------------|:-------------------------------------------------------------------------|
|                       |                                                                                                                                                              |                                                                          |
| ModuleInfo            | ['']                                                                                                                                                         | {'ModuleInfo': 'new'}                                                    |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
| void                  | ['object msg']                                                                                                                                               | {}                                                                       |
| void                  | ['']                                                                                                                                                         | {}                                                                       |
| void                  | ['']                                                                                                                                                         | {}                                                                       |
| Task                  | ['']                                                                                                                                                         | {}                                                                       |
| Task                  | ['RunnerPlayer player']                                                                                                                                      | {}                                                                       |
| TempBanCommand        | ['RunnerPlayer commandSource', 'RunnerPlayer target', 'string duration', 'string? reason = null', 'string? note = null']                                     | {'reason': 'null', 'note': 'null'}                                       |
| TempBanIdCommand      | ['RunnerPlayer commandSource', 'string targetSteamId64', 'string duration', 'string? reason = null', 'string? note = null']                                  | {'reason': 'null', 'note': 'null'}                                       |
| UnTempBanCommand      | ['RunnerPlayer commandSource', 'RunnerPlayer target']                                                                                                        | {}                                                                       |
| ListTempBannedCommand | ['RunnerPlayer commandSource']                                                                                                                               | {}                                                                       |
| CheckAllPlayers       | ['']                                                                                                                                                         | {}                                                                       |
| CheckPlayer           | ['RunnerPlayer player']                                                                                                                                      | {}                                                                       |
| timeSpan              | ['RunnerPlayer target', 'TimeSpan timeSpan', 'string? reason = null', 'string? note = null', 'List<string>? servers = null', 'RunnerPlayer? invoker = null'] | {'reason': 'null', 'note': 'null', 'servers': 'null', 'invoker': 'null'} |
| dateTime              | ['RunnerPlayer target', 'DateTime dateTime', 'string? reason = null', 'string? note = null', 'List<string>? servers = null', 'RunnerPlayer? invoker = null'] | {'reason': 'null', 'note': 'null', 'servers': 'null', 'invoker': 'null'} |
|                       |                                                                                                                                                              |                                                                          |
| KickBannedPlayer      | ['BanEntry banEntry']                                                                                                                                        | {}                                                                       |
|                       |                                                                                                                                                              |                                                                          |
| tempban               | ['']                                                                                                                                                         | {'AllowedRoles': 'Extensions'}                                           |
| tempbanid             | ['']                                                                                                                                                         | {'AllowedRoles': 'Extensions'}                                           |
| untempban             | ['']                                                                                                                                                         | {'AllowedRoles': 'Extensions'}                                           |
| listtempbans          | ['']                                                                                                                                                         | {'AllowedRoles': 'Extensions'}                                           |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
| List                  | ['']                                                                                                                                                         | {}                                                                       |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
| string                | ['']                                                                                                                                                         | {}                                                                       |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
| List                  | ['']                                                                                                                                                         | {}                                                                       |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
| List                  | ['']                                                                                                                                                         | {}                                                                       |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
|                       |                                                                                                                                                              |                                                                          |
| overwrite             | ['BanEntry entry', 'bool overwrite = false']                                                                                                                 | {'overwrite': 'false'}                                                   |
| Remove                | ['BanEntry entry']                                                                                                                                           | {}                                                                       |
| Load                  | ['']                                                                                                                                                         | {}                                                                       |
| Save                  | ['']                                                                                                                                                         | {}                                                                       |
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