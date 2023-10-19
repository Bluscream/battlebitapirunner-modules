using BattleBitAPI.Common;
using BBRAPIModules;
using MySql.Data.MySqlClient;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace MariaDBLogging {
    [Module("", "")]
    public class MariaDBLogging : BattleBitModule {
        public MariaDBLoggingConfiguration Configuration { get; set; }
        private MySqlConnection DbConnection;

        public override void OnModulesLoaded() {
            // Configuration Validation
            PropertyInfo[] properties = typeof(MariaDBLoggingConfiguration).GetProperties();
            foreach (PropertyInfo property in properties) {
                var propertyValue = property.GetValue(this.Configuration)?.ToString();
                if (string.IsNullOrEmpty(propertyValue)) {
                    this.Unload();
                    throw new Exception($"{property.Name} is not set. Please set it in the configuration file.");
                }
            }

            // Initialize MariaDB connection
            string connectionString = Configuration.GetConnectionString();
            DbConnection = new MySqlConnection(connectionString);
            DbConnection.Open();

            Task.Run(PeriodicRetry);
        }

        public override async Task OnConnected() {
            await InsertServerLog("Connected");
        }

        public override async Task OnDisconnected() {
            await InsertServerLog("Disconnected");
        }

        public override async Task OnPlayerConnected(RunnerPlayer player) {
            await InsertPlayerConnectionLog(player, "Connected");
        }

        public override async Task OnPlayerDisconnected(RunnerPlayer player) {
            await InsertPlayerConnectionLog(player, "Disconnected");
        }

        public override async Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg) {
            if (msg.Length > 0) {
                await InsertChatLog(player, channel.ToString(), msg);
                return true;
            } else {
                return false;
            }
        }

        public override async Task OnPlayerReported(RunnerPlayer from, RunnerPlayer to, ReportReason reason, string additional) {
            await InsertPlayerReportLog(from, to, reason.ToString(), additional);
        }

        public async Task InsertServerLog(string connectionType) {
            using (MySqlCommand cmd = new MySqlCommand()) {
                cmd.Connection = DbConnection;
                cmd.CommandText = "INSERT INTO ServerAPILogs (timestamp, server, connection_type) VALUES (@timestamp, @server, @connectionType)";
                cmd.Parameters.AddWithValue("@timestamp", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@server", Server.ServerName);
                cmd.Parameters.AddWithValue("@connectionType", connectionType);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task InsertPlayerConnectionLog(RunnerPlayer player, string connectionType) {
            using (MySqlCommand cmd = new MySqlCommand()) {
                cmd.Connection = DbConnection;
                cmd.CommandText = "INSERT INTO PlayerConnectionLogs (steam_id, username, connection_type, timestamp, server_ip, server_name) VALUES (@steamId, @username, @connectionType, @timestamp, @serverIp, @serverName)";
                cmd.Parameters.AddWithValue("@steamId", player.SteamID.ToString());
                cmd.Parameters.AddWithValue("@username", player.Name);
                cmd.Parameters.AddWithValue("@connectionType", connectionType);
                cmd.Parameters.AddWithValue("@timestamp", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@serverIp", Server.GameIP.ToString());
                cmd.Parameters.AddWithValue("@serverName", Server.ServerName);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task InsertChatLog(RunnerPlayer player, string channel, string message) {
            using (MySqlCommand cmd = new MySqlCommand()) {
                cmd.Connection = DbConnection;
                cmd.CommandText = "INSERT INTO ChatLogs (steam_id, username, channel, message, timestamp, server_ip, server_name) VALUES (@steamId, @username, @channel, @message, @timestamp, @serverIp, @serverName)";
                cmd.Parameters.AddWithValue("@steamId", player.SteamID.ToString());
                cmd.Parameters.AddWithValue("@username", player.Name);
                cmd.Parameters.AddWithValue("@channel", channel);
                cmd.Parameters.AddWithValue("@message", message);
                cmd.Parameters.AddWithValue("@timestamp", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@serverIp", Server.GameIP.ToString());
                cmd.Parameters.AddWithValue("@serverName", Server.ServerName);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task InsertPlayerReportLog(RunnerPlayer from, RunnerPlayer to, string reasonType, string additional) {
            using (MySqlCommand cmd = new MySqlCommand()) {
                cmd.Connection = DbConnection;
                cmd.CommandText = "INSERT INTO PlayerReportLogs (reporting_steam_id, reporting_username, reported_steam_id, reported_username, reason_type, reason, status, resolution_approach, timestamp, server_ip, server_name) VALUES (@reportingSteamId, @reportingUsername, @reportedSteamId, @reportedUsername, @reasonType, @additional, 'Pending Action', '', @timestamp, @serverIp, @serverName)";
                cmd.Parameters.AddWithValue("@reportingSteamId", from.SteamID.ToString());
                cmd.Parameters.AddWithValue("@reportingUsername", from.Name);
                cmd.Parameters.AddWithValue("@reportedSteamId", to.SteamID.ToString());
                cmd.Parameters.AddWithValue("@reportedUsername", to.Name);
                cmd.Parameters.AddWithValue("@reasonType", reasonType);
                cmd.Parameters.AddWithValue("@additional", additional);
                cmd.Parameters.AddWithValue("@timestamp", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@serverIp", Server.GameIP.ToString());
                cmd.Parameters.AddWithValue("@serverName", Server.ServerName);
                await cmd.ExecuteNonQueryAsync();
            }

            if (Configuration.DiscordWebhookEnabled) {
                // Send to Discord here
                var reportID = GetLastInsertedID();
                var payload = $"Report ID: {reportID}\nReporting Player: {from.Name}\nReported Player: {to.Name}\nReason: {reasonType}\nAdditional Info: {additional}";
                Console.WriteLine($"writing payload {payload}");
                // Implement Discord webhook sending logic here
            }
        }

        private int GetLastInsertedID() {
            using (MySqlCommand cmd = new MySqlCommand()) {
                cmd.Connection = DbConnection;
                cmd.CommandText = "SELECT LAST_INSERT_ID()";
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public async Task RetryFailedLogs() {
            // Implement retry logic here for any failed logs
        }

        public async Task PeriodicRetry() {
            while (true) {
                await RetryFailedLogs();
                await Task.Delay(5 * 60 * 1000);
            }
        }

        public override void OnModuleUnloading() {
            DbConnection.Close();
        }
    }

    public class MariaDBLoggingConfiguration : ModuleConfiguration {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 3306;
        public string Username { get; set; } = "root";
        public string Password { get; set; } = "password";
        public string DatabaseName { get; set; } = "your_database_name";
        public string DiscordWebhook { get; set; } = string.Empty;
        public bool DiscordWebhookEnabled { get; set; } = false;

        public string GetConnectionString() {
            return $"Server={Host};Port={Port};Database={DatabaseName};User={Username};Password={Password};";
        }
    }
}