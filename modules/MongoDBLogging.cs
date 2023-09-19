using BattleBitAPI.Common;
using BBRAPIModules;
using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// Author: @Axiom
/// Version: 1.1.4
/// Dependencies: MongoDB.Driver.Core, MongoDB.Driver, MongoDB.Bson, MongoDB.Libmongocrypt, DnsClient
/// 1.1.4 Changes: 
///- {"server_ip", this.Server.GameIP.ToString()} & {"server", this.Server.ServerName} removed from all queries as you are storing at server level anyway. Use following to drop from your tables (if you want to)
///- Refactored the OnConnected, OnDisconnected, OnPlayerConnected, OnPlayerDisconnected because it was just repeating itself. 
///- OnPlayerReported: "status" has been changed to "pending_action" true/false, it should have been a bool in the first place. "Resolution_approach" is now string.Empty instead of "". 
///- [optional addition] If you want to log every action your staff are taking in-game through chat commands, see additional example below. If using with ModeratorTools, just import it into the module using the usual [RequireModule(typeof(MongoDBLogging.MongoDBLogging))] && public MongoDBLogging.MongoDBLogging? MongoDBLogging { get; set; }  
/// 1.1.3 changes: Removed newtonsoft dependency
/// 1.1.2 changes: Added the ability to turn on and off Discord Webhooks for Reported players, by default, is disabled. If you enable it, IT WILL make sure you have a webhookURL. ALso added a queue system for both Discord and Mongo.
/// 1.0.3 changes: Added Server IP Address to Logs, added a "pending action" tack onto the PlayerReported state so that server owners can intergrate means of updating it to "actioned" with "resolution_approach".
/// 1.0.2 changes: Added channel to chat logs, Changed OnplayerReport "Reason" to "reason.ToString()", this will get the name of the reason rather than the number
/// </summary>

namespace Axiom {
    [Module("MongoDBLogging", "1.1.4")]
    public class MongoDBLogging : BattleBitModule
    {
        public MongoDBLoggingConfiguration Configuration { get; set; }
        private IMongoCollection<BsonDocument> ServerAPILogs;
        private IMongoCollection<BsonDocument> PlayerConnectionLogs;
        private IMongoCollection<BsonDocument> ChatLogs;
        private IMongoCollection<BsonDocument> PlayerReportLogs;
        HttpClient client = new HttpClient();
        private string DiscordWebhook;
        private bool DiscordWebhookEnabled;
        private Queue<BsonDocument> FailedLogQueue = new Queue<BsonDocument>();
        private Queue<string> FailedDiscordMessagesQueue = new Queue<string>();


        public override void OnModulesLoaded()
        {
            // Configuration Validation
            PropertyInfo[] properties = typeof(MongoDBLoggingConfiguration).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                var propertyValue = property.GetValue(this.Configuration)?.ToString();
                if (string.IsNullOrEmpty(propertyValue))
                {
                    // If the DiscordWebhookEnabled is true, then DiscordWebhook must not be empty
                    if (property.Name == "DiscordWebhook" && this.Configuration.DiscordWebhookEnabled)
                    {
                        this.Unload();
                        throw new Exception($"When DiscordWebhookEnabled is true, {property.Name} must not be empty. Please set it in the configuration file.");
                    }
                    else if (property.Name != "DiscordWebhook")
                    {
                        // Unload and throw exception for other properties
                        this.Unload();
                        throw new Exception($"{property.Name} is not set. Please set it in the configuration file.");
                    }
                }
            }

            Task.Run(PeriodicDiscordRetry);
            Task.Run(PeriodicMongoRetry);
            MongoDBLoggingInit();
        }

        public void MongoDBLoggingInit()
        {
            var DatabaseName = this.Configuration.DatabaseName;
            DiscordWebhook = this.Configuration.DiscordWebhook;
            DiscordWebhookEnabled = this.Configuration.DiscordWebhookEnabled;
            ServerAPILogs = GetCollection(DatabaseName, this.Configuration.CollectionNames.ServerAPILogs);
            PlayerConnectionLogs = GetCollection(DatabaseName, this.Configuration.CollectionNames.PlayerConnectionLogs);
            ChatLogs = GetCollection(DatabaseName, this.Configuration.CollectionNames.ChatLogs);
            PlayerReportLogs = GetCollection(DatabaseName, this.Configuration.CollectionNames.PlayerReportLogs);
        }

        private IMongoCollection<BsonDocument> GetCollection(string databaseName, string collectionName)
        {
            return new MongoClient(this.Configuration?.ConnectionString)
                .GetDatabase(databaseName)
                .GetCollection<BsonDocument>(collectionName);
        }

        private async Task<bool> InsertLogAsync(IMongoCollection<BsonDocument> collection, BsonDocument document)
        {
            try
            {
                await collection.InsertOneAsync(document);
                return true;
            }
            catch (MongoException ex)
            {
                Console.WriteLine($"An error occurred while connecting to MongoDB: {ex.Message}");
                FailedLogQueue.Enqueue(document);
                return false;
            }
        }

        private async Task<bool> SendToDiscordAsync(string message)
        {
            try
            {
                var payload = new { content = message };
                string jsonPayload = JsonSerializer.Serialize(payload);

                var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync(DiscordWebhook, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Successfully written to Discord with status code {response.StatusCode}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Failed to write to Discord with status code {response.StatusCode}, reason: {await response.Content.ReadAsStringAsync()}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while sending to Discord: {ex.Message}");
                FailedDiscordMessagesQueue.Enqueue(message);
                return false;
            }
        }

        #region Server API Connection Logs
        public override async Task OnConnected()
        {
            await LogConnectionTypeAsync("Connected");
        }

        public override async Task OnDisconnected()
        {
            await LogConnectionTypeAsync("Disconnected");
        }

        private async Task LogConnectionTypeAsync(string connectionType)
        {
            var doc = new BsonDocument
            {
                {"timestamp", DateTime.UtcNow},
                {"connection_type", connectionType}
            };
            await InsertLogAsync(ServerAPILogs, doc);
        }
        #endregion

        #region Player Connection Logs
        public override async Task OnPlayerConnected(RunnerPlayer player)
        {
            await LogPlayerConnectionAsync(player, "Connected");
        }

        public override async Task OnPlayerDisconnected(RunnerPlayer player)
        {
            await LogPlayerConnectionAsync(player, "Disconnected");
        }

        private async Task LogPlayerConnectionAsync(RunnerPlayer player, string connectionType)
        {
            var doc = new BsonDocument
            {
                {"steam_id", player.SteamID.ToString()},
                {"username", player.Name},
                {"connection_type", connectionType},
                {"timestamp", DateTime.UtcNow},
                {"server_ip", this.Server.GameIP.ToString()},
                {"server_name", this.Server.ServerName}
            };
            await InsertLogAsync(PlayerConnectionLogs, doc);
        }
        #endregion

        public override async Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg)
        {
            if (msg.Length > 0)
            {
                var doc = new BsonDocument
                {
                    {"steam_id", player.SteamID.ToString()},
                    {"username", player.Name},
                    {"channel", channel.ToString()},
                    {"message", msg},
                    {"timestamp", DateTime.UtcNow}
                };
                return await InsertLogAsync(ChatLogs, doc);
            }
            else
            {
                return false;
            }
        }

        public override async Task OnPlayerReported(RunnerPlayer from, RunnerPlayer to, ReportReason reason, string additional)
        {
            var doc = new BsonDocument
            {
                {"reporting_steam_id", from.SteamID.ToString()},
                {"reporting_username", from.Name},
                {"reported_steam_id", to.SteamID.ToString()},
                {"reported_username", to.Name},
                {"reason_type", reason.ToString()},
                {"reason", additional},
                {"pending_action", true },
                {"resolution_approach", String.Empty },
                {"timestamp", DateTime.UtcNow}
            };
            await InsertLogAsync(PlayerReportLogs, doc);

            
            if (DiscordWebhookEnabled)
            {
                var ReportID = doc["_id"].ToString();
                var payload = $"Report ID: {ReportID}\nReporting Player: {from.Name} - ({from.SteamID})\nReported Player: {to.Name} - ({to.SteamID})\nReason: {reason.ToString()}\nAdditional Info: {additional}";
                Console.WriteLine($"writing payload {payload}");
                await SendToDiscordAsync(payload);
            }
        }

        public async Task RetryFailedLogs(IMongoCollection<BsonDocument> collection)
        {
            while (FailedLogQueue.Count > 0)
            {
                var log = FailedLogQueue.Dequeue();
                bool success = await InsertLogAsync(collection, log);

                if (!success)
                {
                    FailedLogQueue.Enqueue(log);
                }
            }
        }

        public async Task PeriodicMongoRetry()
        {
            while (true)
            {
                await RetryFailedLogs(ServerAPILogs);
                await RetryFailedLogs(PlayerConnectionLogs);
                await RetryFailedLogs(ChatLogs);
                await RetryFailedLogs(PlayerReportLogs);
                await Task.Delay(5 * 60 * 1000); 
            }
        }

        public async Task RetryFailedDiscordMessages()
        {
            while (FailedDiscordMessagesQueue.Count > 0)
            {
                var message = FailedDiscordMessagesQueue.Dequeue();
                var success = await SendToDiscordAsync(message); 
                if (!success)
                {
                    FailedDiscordMessagesQueue.Enqueue(message);
                }
            }
        }
        public async Task PeriodicDiscordRetry()
        {
            while (true)
            {
                await RetryFailedDiscordMessages();
                await Task.Delay(5 * 60 * 1000);
            }
        }

    }

    public class MongoDBLoggingConfiguration : ModuleConfiguration
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public CollectionNamesConfiguration CollectionNames { get; set; } = new CollectionNamesConfiguration();
        public string DiscordWebhook { get; set; } = string.Empty;
        public bool DiscordWebhookEnabled { get; set; } = false;
    }

    public class CollectionNamesConfiguration
    {
        public string ServerAPILogs { get; set; } = "ServerAPILogs";
        public string PlayerConnectionLogs { get; set; } = "PlayerConnectionLogs";
        public string ChatLogs { get; set; } = "ChatLogs";
        public string PlayerReportLogs { get; set; } = "PlayerReportLogs";
    }
}
