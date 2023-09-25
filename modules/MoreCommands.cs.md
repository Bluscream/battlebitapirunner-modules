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