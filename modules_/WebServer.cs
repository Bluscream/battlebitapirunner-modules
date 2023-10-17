using BBRAPIModules;
using System;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Bluscream {
    public class WebServer {
        private HttpListener _listener;

        public WebServer(string prefix) {
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
        }

        public void Start() {
            _listener.Start();
            _listener.BeginGetContext(OnRequestReceived, null);
        }

        private void OnRequestReceived(IAsyncResult result) {
            var context = _listener.EndGetContext(result);
            _listener.BeginGetContext(OnRequestReceived, null);

            var responseString = "<html><body>Hello, World!</body></html>";
            var buffer = Encoding.UTF8.GetBytes(responseString);

            var response = context.Response;
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
    }

    [RequireModule(typeof(Commands.CommandHandler))]
    [Module("Uploads the currently loaded module list to a telemetry server.", "2.0.1")]
    public class WebServerModule : BattleBitModule {
        [ModuleReference]
        public Commands.CommandHandler CommandHandler { get; set; } = null!;
        public WebServer? WebServer;

        internal void Initialize() {
            if (WebServer is not null || !GlobalConfig.Enabled || !ServerConfig.Enabled) return;
            var url = $"{ServerConfig.HostProtocol}://{ServerConfig.HostAddress}:{ServerConfig.HostPort ?? GlobalConfig.HostPort.EvalToInt()}/";
            if (!Uri.TryCreate(url, new UriCreationOptions(), out var uri)) {
                this.Logger.Error($"Failed to parse {url.Quote()} as URL!"); return;
            }
            WebServer = new WebServer(uri.ToString());
            if (WebServer is null) {
                this.Logger.Error($"Failed to initialize webserver at {uri.ToString().Quote()}!"); return;
            }
            Task.Run(WebServer.Start);
        }

        public override void OnModulesLoaded() { this.CommandHandler.Register(this); }
        public override Task OnConnected() {
            return Task.CompletedTask;
        }

        public static GlobalConfiguration GlobalConfig { get; set; } = null!;
        public class GlobalConfiguration : ModuleConfiguration {
            public bool Enabled { get; set; } = true;
            public string HostPort { get; set; } = "Server.GamePort + 3";
        }
        public ServerConfiguration ServerConfig { get; set; } = null!;
        public class ServerConfiguration : ModuleConfiguration {
            public bool Enabled { get; set; } = true;
            public string HostProtocol { get; set; } = "http";
            public string HostAddress { get; set; } = "0.0.0.0";
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public short? HostPort { get; set; } = null;
        }
    }
}