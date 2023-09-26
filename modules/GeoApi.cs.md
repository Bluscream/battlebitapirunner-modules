# 1 Modules in GeoApi.cs

| Description                                            | Version   |
|:-------------------------------------------------------|:----------|
| IP and geolocation data provider API for other modules | 2.0.0     |

## Commands
| Command    | Function Name   | Description                                  | Allowed Roles   | Parameters                                                    | Defaults           |
|:-----------|:----------------|:---------------------------------------------|:----------------|:--------------------------------------------------------------|:-------------------|
| playerinfo | void            | Displays info about a player                 | Admin           | ['RunnerPlayer commandSource', 'RunnerPlayer? player = null'] | {'player': 'null'} |
| playerlist | void            | Lists players and their respective countries |                 | ['RunnerPlayer commandSource']                                | {}                 |

## Public Methods
| Function Name   | Parameters                                                                                                                              | Defaults              |
|:----------------|:----------------------------------------------------------------------------------------------------------------------------------------|:----------------------|
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
| description     | ['string name', 'string description', 'Version version', 'string author', 'Uri websiteUrl', 'Uri updateUrl', 'Uri supportUrl']          | {}                    |
| description     | ['string name', 'string description', 'Version version', 'string author', 'string websiteUrl', 'string updateUrl', 'string supportUrl'] | {}                    |
| description     | ['string name', 'string description', 'string version', 'string author', 'string websiteUrl', 'string updateUrl', 'string supportUrl']  | {}                    |
|                 |                                                                                                                                         |                       |
| ModuleInfo      | ['']                                                                                                                                    | {'ModuleInfo': 'new'} |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
| void            | ['object _msg', 'string source = "GeoApi"']                                                                                             | {}                    |
| Task            | ['RunnerPlayer player']                                                                                                                 | {}                    |
| Task            | ['RunnerPlayer player']                                                                                                                 | {}                    |
| Task            | ['IPAddress ip']                                                                                                                        | {}                    |
| void            | ['']                                                                                                                                    | {}                    |
| Task            | ['']                                                                                                                                    | {}                    |
| Task            | ['']                                                                                                                                    | {}                    |
| Task            | ['RunnerPlayer player']                                                                                                                 | {}                    |
| Task            | ['RunnerPlayer player']                                                                                                                 | {}                    |
| GetPlayerInfo   | ['RunnerPlayer commandSource', 'RunnerPlayer? player = null']                                                                           | {'player': 'null'}    |
| ListPlayers     | ['RunnerPlayer commandSource']                                                                                                          | {}                    |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
| RemoveDelay     | ['1']                                                                                                                                   | {}                    |
|                 |                                                                                                                                         |                       |
| string          | ['this RunnerPlayer player']                                                                                                            | {}                    |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
| Response        | ['string json']                                                                                                                         | {}                    |
|                 |                                                                                                                                         |                       |
| string          | ['this Response self']                                                                                                                  | {}                    |
|                 |                                                                                                                                         |                       |
| T               | ['string jsonText']                                                                                                                     | {}                    |
| T               | ['FileInfo file']                                                                                                                       | {}                    |
| string          | ['this T self']                                                                                                                         | {}                    |
| void            | ['this T self', 'FileInfo file']                                                                                                        | {}                    |
|                 |                                                                                                                                         |                       |
| readonly        | ['JsonSerializerDefaults.General']                                                                                                      | {'Settings': 'new'}   |
|                 |                                                                                                                                         |                       |
| bool            | ['Type t']                                                                                                                              | {}                    |
| long            | ['ref Utf8JsonReader reader', 'Type typeToConvert', 'JsonSerializerOptions options']                                                    | {}                    |
| void            | ['Utf8JsonWriter writer', 'long value', 'JsonSerializerOptions options']                                                                | {}                    |
| readonly        | ['']                                                                                                                                    | {'Singleton': 'new'}  |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
| DateOnly        | ['ref Utf8JsonReader reader', 'Type typeToConvert', 'JsonSerializerOptions options']                                                    | {}                    |
| void            | ['Utf8JsonWriter writer', 'DateOnly value', 'JsonSerializerOptions options']                                                            | {}                    |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
| TimeOnly        | ['ref Utf8JsonReader reader', 'Type typeToConvert', 'JsonSerializerOptions options']                                                    | {}                    |
| void            | ['Utf8JsonWriter writer', 'TimeOnly value', 'JsonSerializerOptions options']                                                            | {}                    |
|                 |                                                                                                                                         |                       |
| bool            | ['Type t']                                                                                                                              | {}                    |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
|                 |                                                                                                                                         |                       |
| void            | ['Utf8JsonWriter writer', 'DateTimeOffset value', 'JsonSerializerOptions options']                                                      | {}                    |
| DateTimeOffset  | ['ref Utf8JsonReader reader', 'Type typeToConvert', 'JsonSerializerOptions options']                                                    | {}                    |
| readonly        | ['']                                                                                                                                    | {'Singleton': 'new'}  |
|                 |                                                                                                                                         |                       |
| IPAddress       | ['ref Utf8JsonReader reader', 'Type typeToConvert', 'JsonSerializerOptions options']                                                    | {}                    |
| void            | ['Utf8JsonWriter writer', 'IPAddress value', 'JsonSerializerOptions options']                                                           | {}                    |
|                 |                                                                                                                                         |                       |
| IPEndPoint      | ['ref Utf8JsonReader reader', 'Type typeToConvert', 'JsonSerializerOptions options']                                                    | {}                    |
| void            | ['Utf8JsonWriter writer', 'IPEndPoint value', 'JsonSerializerOptions options']                                                          | {}                    |