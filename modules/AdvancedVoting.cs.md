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