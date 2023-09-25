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