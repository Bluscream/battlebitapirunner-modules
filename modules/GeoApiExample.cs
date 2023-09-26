using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BBRAPIModules;

namespace Bluscream {
    [RequireModule(typeof(Bluscream.GeoApi))]
    [Module("Example of using the GeoApi", "2.0.0")]
    public class GeoApiExample : BattleBitModule {

        [ModuleReference]
#if DEBUG
        public Bluscream.GeoApi? GeoApi { get; set; }
#else
        public dynamic? GeoApi { get; set; }
#endif

        public IReadOnlyDictionary<RunnerPlayer, IpApi.Response> Players { get { return _Players; } }
        private Dictionary<RunnerPlayer, IpApi.Response> _Players { get; set; } = new Dictionary<RunnerPlayer, IpApi.Response>();
        #region Events
        public override Task OnConnected() {
            if (GeoApi is not null) {
                var geoData = GeoApi._GetGeoData(this.Server.GameIP)?.Result;
                Console.WriteLine($"Connected to \"{this.Server.ServerName}\" in {geoData?.Country ?? "Unknown Country"}");
            }
            return Task.CompletedTask;
        }
        public override Task OnPlayerConnected(RunnerPlayer player) {
            if (GeoApi is not null) {
                Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                var geoData = GeoApi.GetGeoData(player)?.Result;
                Console.WriteLine($"\"{player.Name}\" is coming from {geoData?.Country ?? "Unknown Country"}");
            }
            return Task.CompletedTask;
        }
        public override Task OnPlayerDisconnected(RunnerPlayer player) {
            if (GeoApi is not null) {
                var geoData = GeoApi.GetGeoData(player)?.Result;
                Console.WriteLine($"\"{player.Name}\" is going back to {geoData?.Country ?? "Unknown Country"}");
            }
            return Task.CompletedTask;
        }
        #endregion
}