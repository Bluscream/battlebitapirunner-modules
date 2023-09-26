using BBRAPIModules;
using System.Collections.Generic;
using System.Diagnostics;

namespace Bluscream {
    [RequireModule(typeof(Bluscream.GeoApi))]
    [RequireModule(typeof(Commands.CommandHandler))]
    [Module("Example of using the GeoApi", "2.0.0")]
    public class GeoApiExample : BattleBitModule {
        [ModuleReference]
        public Commands.CommandHandler CommandHandler { get; set; }

        [ModuleReference]
#if DEBUG
        public Bluscream.GeoApi? GeoApi { get; set; }
#else
        public dynamic? GeoApi { get; set; }
#endif

        #region Events
        public override void OnModulesLoaded() {
            if (GeoApi is null) {
                System.Console.WriteLine($"GeoApi could not be found! Is it installed?");
            } else {
                this.CommandHandler.Register(this);
            }
        }
        public override System.Threading.Tasks.Task OnConnected() {
            if (GeoApi is not null) {
                var geoData = GeoApi._GetGeoData(this.Server.GameIP)?.Result;
                System.Console.WriteLine($"Connected to \"{this.Server.ServerName}\" in {geoData?.Country ?? "Unknown Country"}");
            }
            return System.Threading.Tasks.Task.CompletedTask;
        }
        public override System.Threading.Tasks.Task OnPlayerConnected(BBRAPIModules.RunnerPlayer player) {
            if (GeoApi is not null) {
                System.Threading.Tasks.Task.Delay(System.TimeSpan.FromSeconds(1)).Wait();
                var geoData = GeoApi.GetGeoData(player)?.Result;
                System.Console.WriteLine($"\"{player.Name}\" is coming from {geoData?.Country ?? "Unknown Country"}");
            }
            return System.Threading.Tasks.Task.CompletedTask;
        }
        public override System.Threading.Tasks.Task OnPlayerDisconnected(BBRAPIModules.RunnerPlayer player) {
            if (GeoApi is not null) {
                var geoData = GeoApi.GetGeoData(player)?.Result;
                System.Console.WriteLine($"\"{player.Name}\" is going back to {geoData?.Country ?? "Unknown Country"}");
            }
            return System.Threading.Tasks.Task.CompletedTask;
        }
        #endregion
        #region Commands
        [Commands.CommandCallback("playerinfo", Description = "Displays info about a player", AllowedRoles = BattleBitAPI.Common.Roles.Admin)]
        public void GetPlayerInfo(BBRAPIModules.RunnerPlayer commandSource, BBRAPIModules.RunnerPlayer? player = null) {
            if (GeoApi is null) { commandSource.Message("GeoApi not found, do you have it installed?"); return; }
            player = player ?? commandSource;
            var geoResponse = GeoApi?.GetGeoData(player)?.Result;
            if (geoResponse is null) { commandSource.Message($"Failed to get Geo Data for player \"{player.Name}\""); return; }
            var response = new System.Text.StringBuilder();
            response.AppendLine($"Name: \"{player.Name}\" ({player.Name.Length} chars)");
            if (!string.IsNullOrEmpty(player.IP.ToString())) response.Append($"IP: {player.IP}");
            if (geoResponse is not null) {
                if (geoResponse.Proxy == true) response.Append($" (Proxy/VPN)");
                if (geoResponse.Hosting == true) response.Append($" (Server)");
                if (!string.IsNullOrEmpty(geoResponse.Isp)) response.AppendLine($"\nISP: {geoResponse.Isp}");
                if (!string.IsNullOrEmpty(geoResponse.Country)) response.AppendLine($"Country: {geoResponse.Country}");
                if (!string.IsNullOrEmpty(geoResponse.RegionName)) response.AppendLine($"Region: {geoResponse.RegionName}");
                if (!string.IsNullOrEmpty(geoResponse.City)) response.AppendLine($"City: {geoResponse.City} ({geoResponse.Zip})");
                if (!string.IsNullOrEmpty(geoResponse.Timezone)) response.AppendLine($"Time: {System.TimeZoneInfo.ConvertTimeBySystemTimeZoneId(System.DateTime.UtcNow, geoResponse.Timezone).ToString("HH:mm")} ({geoResponse.Timezone})");
            }
            commandSource.Message(response.ToString());
        }
        [Commands.CommandCallback("playerlist", Description = "Lists players and their respective countries")]
        public void ListPlayers(BBRAPIModules.RunnerPlayer commandSource) {
            if (GeoApi is null) { commandSource.Message("GeoApi not found, do you have it installed?"); return; }
            var response = new System.Text.StringBuilder($"{GeoApi.Players.Count} Players:\n\n");
            foreach (var player in GeoApi.Players) {
                response.AppendLine($"\"{player.Key.Name}\" from {player.Value.Country}, {player.Value.Continent}");
            }
            commandSource.Message(response.ToString());
        }
        #endregion
    }
}