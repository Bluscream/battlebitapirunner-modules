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