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