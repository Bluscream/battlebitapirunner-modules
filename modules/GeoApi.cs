using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Net;
using System.Net.Http;
using BattleBitAPI.Common;
using BBRAPIModules;

using Commands;
using System.Globalization;
using System.IO;
using Bluscream;

namespace Bluscream {
    public class ModuleInfo {
        public bool Loaded { get; set; }
        public bool Enabled { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public Version Version { get; set; }
        public string Author { get; set; }
        public Uri? WebsiteUrl { get; set; }
        public Uri? UpdateUrl { get; set; }
        public Uri? SupportUrl { get; set; }
        public ModuleInfo() { }
        public ModuleInfo(string name, string description, Version version, string author, Uri websiteUrl, Uri updateUrl, Uri supportUrl) {
            Name = name;
            Description = description;
            Version = version;
            Author = author;
            WebsiteUrl = websiteUrl;
            UpdateUrl = updateUrl;
            SupportUrl = supportUrl;
        }
        public ModuleInfo(string name, string description, Version version, string author, string websiteUrl, string updateUrl, string supportUrl) :
            this(name, description, version, author, new Uri(websiteUrl), new Uri(updateUrl), new Uri(supportUrl)) { }
        public ModuleInfo(string name, string description, string version, string author, string websiteUrl, string updateUrl, string supportUrl) :
            this(name, description, new Version(version), author, new Uri(websiteUrl), new Uri(updateUrl), new Uri(supportUrl)) { }
    }
    [RequireModule(typeof(CommandHandler))]
    [Module("IP and geolocation data provider API for other modules", "2.0.0")]
    public class GeoApi : BattleBitModule {
        public static ModuleInfo ModuleInfo = new() {
            Name = "GeoApi",
            Description = "IP and geolocation data provider API for other modules",
            Version = new Version(2, 0, 0),
            Author = "Bluscream",
            WebsiteUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/"),
            UpdateUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/raw/master/modules/GeoApi.cs"),
            SupportUrl = new Uri("https://github.com/Bluscream/battlebitapirunner-modules/issues/new?title=GeoApi")
        };
        [ModuleReference]
        public CommandHandler CommandHandler { get; set; }

        public IpApiConfiguration Configuration { get; set; }
        // public GeoApiCommandsConfiguration CommandsConfiguration { get; set; }
        internal static HttpClient httpClient = new HttpClient();
        private bool GettingGeoData = false;

        public IReadOnlyDictionary<RunnerPlayer, IpApi.Response> Players { get { return _Players; } }
        private Dictionary<RunnerPlayer, IpApi.Response> _Players { get; set; } = new Dictionary<RunnerPlayer, IpApi.Response>();
        #region Methods
        public static void Log(object _msg, string source = "GeoApi") {
            var msg = _msg.ToString();
            if (string.IsNullOrWhiteSpace(msg)) return;
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {source} > {msg.Trim()}");
        }
        private async Task AddAllGeoData(RunnerServer? server = null) {
            server = server ?? this.Server;
            foreach (var player in server.AllPlayers) {
                await AddGeoData(player);
            }
        }
        private async Task RemoveAllGeoData(RunnerServer? server = null, TimeSpan? delay = null) {
            server = server ?? this.Server;
            if (delay is not null && delay != TimeSpan.Zero) await Task.Delay(delay.Value); // Todo: Make configurable
            foreach (var player in server.AllPlayers) {
                await RemoveGeoData(player);
            }
        }
        private async Task AddGeoData(RunnerPlayer player) {
            if (Players.ContainsKey(player)) return;
            IpApi.Response? geoData = await _GetGeoData(player);
            if (geoData is null) return;
            _Players.Add(player, geoData);
        }
        private async Task RemoveGeoData(RunnerPlayer player, TimeSpan? delay = null) {
            if (delay is not null && delay != TimeSpan.Zero) await Task.Delay(delay.Value); // Todo: Make configurable
            if (_Players.ContainsKey(player))
                _Players.Remove(player);
        }
        #endregion
        #region Api
        public async Task<IpApi.Response>? GetGeoData(RunnerPlayer player) {
            if (!Players.ContainsKey(player)) {
                Log($"For some reason we dont have GeoData for {player.str()}, getting it now...");
                await AddGeoData(player);
            }
            return Players[player];
        }
        public async Task<IpApi.Response?> _GetGeoData(RunnerPlayer player) => await _GetGeoData(player.IP);
        public async Task<IpApi.Response?> _GetGeoData(IPAddress ip) {
            // if (GettingGeoData) return null;
            GettingGeoData = true;
            var url = Configuration.IpApiUrl.Replace("{ip}", ip.ToString());
            HttpResponseMessage httpResponse;
            try { httpResponse = await GeoApi.httpClient.GetAsync(url); } catch (Exception ex) {
                Log($"Failed to get geo data for {ip}: {ex.Message}");
                return null;
            }
            var json = await httpResponse.Content.ReadAsStringAsync();
            var response = IpApi.Response.FromJson(json);
            GettingGeoData = false;
            return response;
        }
        #endregion
        #region Events
        public override void OnModulesLoaded() {
            this.CommandHandler.Register(this);
        }
        public override Task OnConnected() {
            AddAllGeoData(this.Server).Wait();
            return Task.CompletedTask;
        }
        public override Task OnDisconnected() {
            RemoveAllGeoData(this.Server, Configuration.RemoveDelay).Wait();
            return Task.CompletedTask;
        }
        public override Task OnPlayerConnected(RunnerPlayer player) {
            AddGeoData(player).Wait();
            return Task.CompletedTask;
        }
        public override Task OnPlayerDisconnected(RunnerPlayer player) {
            RemoveGeoData(player, Configuration.RemoveDelay).Wait();
            return Task.CompletedTask;
        }
        #endregion
        #region Commands
        [CommandCallback("playerinfo", Description = "Displays info about a player", AllowedRoles = Roles.Admin )]
        public void GetPlayerInfo(RunnerPlayer commandSource, RunnerPlayer? player = null) {
            player = player ?? commandSource;
            var geoResponse = GetGeoData(player)?.Result;
            if (geoResponse is null) { commandSource.Message($"Failed to get Geo Data for player {player.str()}"); return; }
            var response = new StringBuilder();
            response.AppendLine($"Name: {player.str()} ({player.Name.Length} chars)");
            if (!string.IsNullOrEmpty(player.IP.ToString())) response.Append($"IP: {player.IP}");
            if (geoResponse is not null) {
                if (geoResponse.Proxy == true) response.Append($" (Proxy/VPN)");
                if (geoResponse.Hosting == true) response.Append($" (Server)");
                if (!string.IsNullOrEmpty(geoResponse.Isp)) response.AppendLine($"\nISP: {geoResponse.Isp}");
                if (!string.IsNullOrEmpty(geoResponse.Country)) response.AppendLine($"Country: {geoResponse.Country}");
                if (!string.IsNullOrEmpty(geoResponse.RegionName)) response.AppendLine($"Region: {geoResponse.RegionName}");
                if (!string.IsNullOrEmpty(geoResponse.City)) response.AppendLine($"City: {geoResponse.City} ({geoResponse.Zip})");
                if (!string.IsNullOrEmpty(geoResponse.Timezone)) response.AppendLine($"Time: {TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, geoResponse.Timezone).ToString("HH:mm")} ({geoResponse.Timezone})");
            }
            commandSource.Message(response.ToString());
        }
        [CommandCallback("playerlist", Description = "Lists players and their respective countries")]
        public void ListPlayers(RunnerPlayer commandSource) {
            var response = new StringBuilder($"{Players.Count} Players:\n\n");
            foreach (var (player, geoData) in Players) {
                response.AppendLine($"{player.str()} from {geoData.Country}, {geoData.Continent}");
            }
            commandSource.Message(response.ToString());
        }
        #endregion
    }
    //public class GeoApiCommandsConfiguration : ModuleConfiguration {
    //    public CommandConfiguration playerinfo { get; set; } = new CommandConfiguration() { AllowedRoles = new() { "Admin" } };
    //    public CommandConfiguration players { get; set; } = new CommandConfiguration() { AllowedRoles = new () { "All" } };
    //}
    public class IpApiConfiguration : ModuleConfiguration {
        public string IpApiUrl { get; set; } = "http://ip-api.com/json/{ip}?fields=status,message,continent,continentCode,country,countryCode,region,regionName,city,district,zip,lat,lon,timezone,offset,currency,isp,org,as,asname,reverse,mobile,proxy,hosting,query";
        public TimeSpan RemoveDelay { get; set; } = TimeSpan.FromMinutes(1);
    }
    #region extensions
    public static class Extensions {
        public static string str(this RunnerPlayer player) => $"\"{player.Name}\"";
    }
    #endregion
}
#region json
namespace IpApi {
    public partial class Response {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("continent")]
        public string? Continent { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("continentCode")]
        public string? ContinentCode { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("countryCode")]
        public string? CountryCode { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("regionName")]
        public string? RegionName { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("district")]
        public string? District { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("zip")]
        public string? Zip { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("lat")]
        public double? Lat { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("lon")]
        public double? Lon { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("offset")]
        public long? Offset { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("isp")]
        public string? Isp { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("org")]
        public string? Org { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("as")]
        public string? As { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("asname")]
        public string? Asname { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("reverse")]
        public string? Reverse { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("mobile")]
        public bool? Mobile { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("proxy")]
        public bool? Proxy { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("hosting")]
        public bool? Hosting { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("query")]
        public string? Query { get; set; }
    }

    public partial class Response {
        public static Response FromJson(string json) => JsonUtils.FromJson<Response>(json);
    }
    public static class Serialize {
        public static string ToJson(this Response self) => JsonUtils.ToJson(self);
    }
}
#endregion
#region json
namespace Bluscream {
    public static class JsonUtils {
        public static T FromJson<T>(string jsonText) => JsonSerializer.Deserialize<T>(jsonText, Converter.Settings);
        public static T FromJsonFile<T>(FileInfo file) => FromJson<T>(File.ReadAllText(file.FullName));
        public static string ToJson<T>(this T self) => JsonSerializer.Serialize(self, Converter.Settings);
        public static void ToFile<T>(this T self, FileInfo file) => File.WriteAllText(file.FullName, ToJson(self));
    }
    public static class Converter {
        public static readonly JsonSerializerOptions Settings = new(JsonSerializerDefaults.General) {
            Converters =
            {
        new DateOnlyConverter(),
        new TimeOnlyConverter(),
        IsoDateTimeOffsetConverter.Singleton
    },
        };
    }
    public class ParseStringConverter : JsonConverter<long> {
        public override bool CanConvert(Type t) => t == typeof(long);

        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var value = reader.GetString();
            long l;
            if (Int64.TryParse(value, out l)) {
                return l;
            }
            throw new Exception("Cannot unmarshal type long");
        }

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options) {
            JsonSerializer.Serialize(writer, value.ToString(), options);
            return;
        }

        public static readonly ParseStringConverter Singleton = new ParseStringConverter();
    }
    public class DateOnlyConverter : JsonConverter<DateOnly> {
        private readonly string serializationFormat;
        public DateOnlyConverter() : this(null) { }

        public DateOnlyConverter(string? serializationFormat) {
            this.serializationFormat = serializationFormat ?? "yyyy-MM-dd";
        }

        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var value = reader.GetString();
            return DateOnly.Parse(value!);
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(serializationFormat));
    }
    public class TimeOnlyConverter : JsonConverter<TimeOnly> {
        private readonly string serializationFormat;

        public TimeOnlyConverter() : this(null) { }

        public TimeOnlyConverter(string? serializationFormat) {
            this.serializationFormat = serializationFormat ?? "HH:mm:ss.fff";
        }

        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var value = reader.GetString();
            return TimeOnly.Parse(value!);
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(serializationFormat));
    }
    public class IsoDateTimeOffsetConverter : JsonConverter<DateTimeOffset> {
        public override bool CanConvert(Type t) => t == typeof(DateTimeOffset);

        private const string DefaultDateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";

        private DateTimeStyles _dateTimeStyles = DateTimeStyles.RoundtripKind;
        private string? _dateTimeFormat;
        private CultureInfo? _culture;

        public DateTimeStyles DateTimeStyles {
            get => _dateTimeStyles;
            set => _dateTimeStyles = value;
        }

        public string? DateTimeFormat {
            get => _dateTimeFormat ?? string.Empty;
            set => _dateTimeFormat = (string.IsNullOrEmpty(value)) ? null : value;
        }

        public CultureInfo Culture {
            get => _culture ?? CultureInfo.CurrentCulture;
            set => _culture = value;
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) {
            string text;


            if ((_dateTimeStyles & DateTimeStyles.AdjustToUniversal) == DateTimeStyles.AdjustToUniversal
                || (_dateTimeStyles & DateTimeStyles.AssumeUniversal) == DateTimeStyles.AssumeUniversal) {
                value = value.ToUniversalTime();
            }

            text = value.ToString(_dateTimeFormat ?? DefaultDateTimeFormat, Culture);

            writer.WriteStringValue(text);
        }

        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            string? dateText = reader.GetString();

            if (string.IsNullOrEmpty(dateText) == false) {
                if (!string.IsNullOrEmpty(_dateTimeFormat)) {
                    return DateTimeOffset.ParseExact(dateText, _dateTimeFormat, Culture, _dateTimeStyles);
                } else {
                    return DateTimeOffset.Parse(dateText, Culture, _dateTimeStyles);
                }
            } else {
                return default(DateTimeOffset);
            }
        }

        public static readonly IsoDateTimeOffsetConverter Singleton = new IsoDateTimeOffsetConverter();
    }
    public class IPAddressConverter : JsonConverter<IPAddress> {
        public override IPAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            return IPAddress.Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options) {
            writer.WriteStringValue(value.ToString());
        }
    }
    public class IPEndPointConverter : JsonConverter<IPEndPoint> {
        public override IPEndPoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var ipEndPointString = reader.GetString();
            var endPointParts = ipEndPointString.Split(':');
            var ip = IPAddress.Parse(endPointParts[0]);
            var port = int.Parse(endPointParts[1]);
            return new IPEndPoint(ip, port);
        }

        public override void Write(Utf8JsonWriter writer, IPEndPoint value, JsonSerializerOptions options) {
            writer.WriteStringValue(value.ToString());
        }
    }
}
#endregion
