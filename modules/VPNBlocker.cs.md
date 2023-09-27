# 1 Modules in VPNBlocker.cs

| Description                                            | Version   |
|:-------------------------------------------------------|:----------|
| Using the GeoApi to block certain players from joining | 2.0.2     |

## Commands
| Command     | Function Name   | Description                                   | Allowed Roles   | Parameters                                                                                  | Defaults                          |
|:------------|:----------------|:----------------------------------------------|:----------------|:--------------------------------------------------------------------------------------------|:----------------------------------|
| blockplayer | void            | Toggles blocking for a specific player's item |                 | ['BBRAPIModules.RunnerPlayer commandSource', 'RunnerPlayer target', 'string list = ""']     | {}                                |
| block       | void            | Toggles blocking for a specific item          |                 | ['BBRAPIModules.RunnerPlayer commandSource', 'string? list = null', 'string? entry = null'] | {'list': 'null', 'entry': 'null'} |

## Public Methods
| Function Name            | Parameters                                                                                            | Defaults                           |
|:-------------------------|:------------------------------------------------------------------------------------------------------|:-----------------------------------|
|                          |                                                                                                       |                                    |
| ModuleInfo               | ['']                                                                                                  | {'ModuleInfo': 'new'}              |
|                          |                                                                                                       |                                    |
|                          |                                                                                                       |                                    |
|                          |                                                                                                       |                                    |
|                          |                                                                                                       |                                    |
|                          |                                                                                                       |                                    |
| FormatString             | ['string format', 'RunnerPlayer player', 'IpApi.Response geoData']                                    | {}                                 |
| entry                    | ['BlockListConfiguration config', 'string entry']                                                     | {}                                 |
| CheckWhitelistRoles      | ['RunnerPlayer player', 'BlockConfiguration config']                                                  | {}                                 |
| CheckPlayer              | ['RunnerPlayer player', 'IpApi.Response geoData']                                                     | {}                                 |
| ToggleBoolEntry          | ['BBRAPIModules.RunnerPlayer commandSource', 'BlockConfiguration config']                             | {}                                 |
| ToggleStringListEntry    | ['BBRAPIModules.RunnerPlayer commandSource', 'BlockListConfiguration config', 'string? entry = null'] | {'entry': 'null'}                  |
| void                     | ['']                                                                                                  | {}                                 |
| ToggleBlockPlayerCommand | ['BBRAPIModules.RunnerPlayer commandSource', 'RunnerPlayer target', 'string list = ""']               | {}                                 |
| ToggleBlockCommand       | ['BBRAPIModules.RunnerPlayer commandSource', 'string? list = null', 'string? entry = null']           | {'list': 'null', 'entry': 'null'}  |
|                          |                                                                                                       |                                    |
|                          |                                                                                                       |                                    |
| block                    | ['']                                                                                                  | {'AllowedRoles': 'Extensions'}     |
| blockplayer              | ['']                                                                                                  | {'AllowedRoles': 'Extensions'}     |
|                          |                                                                                                       |                                    |
|                          |                                                                                                       |                                    |
| List                     | ['']                                                                                                  | {}                                 |
|                          |                                                                                                       |                                    |
|                          |                                                                                                       |                                    |
|                          |                                                                                                       |                                    |
|                          |                                                                                                       |                                    |
|                          |                                                                                                       |                                    |
|                          |                                                                                                       |                                    |
|                          |                                                                                                       |                                    |
| FailTimeout              | ['1']                                                                                                 | {}                                 |
| BlockProxies             | ['']                                                                                                  | {'Enabled': 'true'}                |
| BlockServers             | ['']                                                                                                  | {'Enabled': 'true'}                |
| BlockMobile              | ['']                                                                                                  | {'Enabled': 'false'}               |
| BlockFailed              | ['']                                                                                                  | {'Enabled': 'true'}                |
| ISPs                     | ['']                                                                                                  | {'Enabled': 'true', 'List': 'new'} |
| Continents               | ['']                                                                                                  | {'Enabled': 'true'}                |
| Countries                | ['']                                                                                                  | {'Enabled': 'true'}                |