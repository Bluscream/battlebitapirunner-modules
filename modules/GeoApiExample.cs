using BBRAPIModules;
using Commands;
using System;
using System.Net;

namespace Bluscream {
    [RequireModule(typeof(Bluscream.BluscreamLib))]
    [RequireModule(typeof(Bluscream.GeoApi))]
    [RequireModule(typeof(Commands.CommandHandler))]
    [Module("Example of using the GeoApi", "2.0.2")]
    public class GeoApiExample : BattleBitModule {
        public static ModuleInfo ModuleInfo = new() {
            Name = "GeoApiExample",
            Description = "Example usage of the GeoApi module",
            Version = new Version(2, 0, 2),
            Author = "Bluscream",
            WebsiteUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/"),
            UpdateUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/raw/master/modules/GeoApiExample.cs"),
            SupportUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/issues/new?title=GeoApiExample")
        };

        #region References
        [ModuleReference]
        public Commands.CommandHandler CommandHandler { get; set; }

        [ModuleReference]
#if DEBUG
        public Permissions.PlayerPermissions? PlayerPermissions { get; set; }
#else
        public dynamic? PlayerPermissions { get; set; }
#endif

        [ModuleReference]
        public Bluscream.GeoApi? GeoApi { get; set; }
        #endregion

        #region Methods
        private static void Log(object _msg, string source = "GeoApiExample") => BluscreamLib.Log(_msg, source);
        #endregion

        #region Events
        public override void OnModulesLoaded() {
            if (GeoApi is null) {
                Log($"GeoApi could not be found! Is it installed?");
            } else {
                this.CommandHandler.Register(this);
                GeoApi.OnDataReceived += GeoApi_OnDataReceived;
            }
        }

        private void GeoApi_OnDataReceived(IPAddress ip, IpApi.Response geoData) {
        }

        public override System.Threading.Tasks.Task OnConnected() {
            if (GeoApi is not null) {
                var geoData = this.Server.GetGeoData()?.Result;
                var str = geoData?.Country is null ? $" in {geoData?.Country}" : string.Empty;
                Log($"Connected to \"{this.Server.ServerName}\"{str}");
            }
            return System.Threading.Tasks.Task.CompletedTask;
        }
        public override System.Threading.Tasks.Task OnPlayerConnected(BBRAPIModules.RunnerPlayer player) {
            if (GeoApi is not null) {
                System.Threading.Tasks.Task.Delay(System.TimeSpan.FromSeconds(1)).Wait();
                var geoData = player.GetGeoData()?.Result;
                Log($"\"{player.Name}\" is coming from {geoData?.Country ?? "Unknown Country"}");
            }
            return System.Threading.Tasks.Task.CompletedTask;
        }
        public override System.Threading.Tasks.Task OnPlayerDisconnected(BBRAPIModules.RunnerPlayer player) {
            if (GeoApi is not null) {
                var geoData = player.GetGeoData()?.Result;
                Log($"\"{player.Name}\" is going back to {geoData?.Country ?? "Unknown Country"}");
            }
            return System.Threading.Tasks.Task.CompletedTask;
        }
        #endregion

        #region Commands
        [CommandCallback("playerinfo", Description = "Displays info about a player", ConsoleCommand = true, Permissions = new[] { "commands.playerinfo" })]
        public void GetPlayerInfoChatCommand(BBRAPIModules.RunnerPlayer commandSource, BBRAPIModules.RunnerPlayer? player = null) {
            if (GeoApi is null) { commandSource.Message("GeoApi not found, do you have it installed?"); return; }
            player = player ?? commandSource;
            var geoResponse = GeoApi?.GetData(player)?.Result;
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
        #endregion

    }
}