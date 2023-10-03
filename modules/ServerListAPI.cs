using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

using BBRAPIModules;

namespace Bluscream {

    public class ServerListAPI : BattleBitModule {

    }

    public partial class Response {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("Name")]
        public virtual string Name { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("Map")]
        public virtual string Map { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("MapSize")]
        public virtual MapSize? MapSize { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("Gamemode")]
        public virtual Gamemode? Gamemode { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("Region")]
        public virtual Region? Region { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("Players")]
        public virtual long? Players { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("QueuePlayers")]
        public virtual long? QueuePlayers { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("MaxPlayers")]
        public virtual long? MaxPlayers { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("Hz")]
        public virtual long? Hz { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("DayNight")]
        public virtual DayNight? DayNight { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("IsOfficial")]
        public virtual bool? IsOfficial { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("HasPassword")]
        public virtual bool? HasPassword { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("AntiCheat")]
        public virtual AntiCheat? AntiCheat { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("Build")]
        public virtual Build? Build { get; set; }
    }

    public enum AntiCheat { Unknown, Eac };
    public enum Build { Unknown, Production216, Production218Hotfix, Production218SecHotfix };
    public enum DayNight { Unknown, Day, Night };
    public enum Gamemode { Unknown, CaptureTheFlag, Conq, Domi, Eli, Frontline, Infconq, Rush, Tdm, VoxelFortify };
    public enum MapSize { Unknown, Big, Medium, Small, Tiny, Ultra };
    public enum Region { Unknown, AmericaCentral, AsiaCentral, AustraliaCentral, BrazilCentral, EuropeCentral, JapanCentral };

    public partial class Response {
        public static List<Response> FromJson(string json) => JsonSerializer.Deserialize<List<Response>>(json, Bluscream.Converter.Settings);
    }

    public static class Serialize {
        public static string ToJson(this List<Response> self) => JsonSerializer.Serialize(self, Converter.Settings);
    }


    internal class AntiCheatConverter : JsonConverter<AntiCheat> {
        public override bool CanConvert(Type t) => t == typeof(AntiCheat);

        public override AntiCheat Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var value = reader.GetString();
            if (value == "EAC") {
                return AntiCheat.Eac;
            }
            throw new Exception("Cannot unmarshal type AntiCheat");
        }

        public override void Write(Utf8JsonWriter writer, AntiCheat value, JsonSerializerOptions options) {
            if (value == AntiCheat.Eac) {
                JsonSerializer.Serialize(writer, "EAC", options);
                return;
            }
            throw new Exception("Cannot marshal type AntiCheat");
        }

        public static readonly AntiCheatConverter Singleton = new AntiCheatConverter();
    }
    internal class BuildConverter : JsonConverter<Build> {
        public override bool CanConvert(Type t) => t == typeof(Build);

        public override Build Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var value = reader.GetString();
            switch (value) {
                case "Production 2.1.6":
                    return Build.Production216;
                case "Production 2.1.8 Sec-Hotfix":
                    return Build.Production218SecHotfix;
                case "Production 2.1.8 hotfix":
                    return Build.Production218Hotfix;
            }
            throw new Exception("Cannot unmarshal type Build");
        }

        public override void Write(Utf8JsonWriter writer, Build value, JsonSerializerOptions options) {
            switch (value) {
                case Build.Production216:
                    JsonSerializer.Serialize(writer, "Production 2.1.6", options);
                    return;
                case Build.Production218SecHotfix:
                    JsonSerializer.Serialize(writer, "Production 2.1.8 Sec-Hotfix", options);
                    return;
                case Build.Production218Hotfix:
                    JsonSerializer.Serialize(writer, "Production 2.1.8 hotfix", options);
                    return;
            }
            throw new Exception("Cannot marshal type Build");
        }

        public static readonly BuildConverter Singleton = new BuildConverter();
    }
    internal class DayNightConverter : JsonConverter<DayNight> {
        public override bool CanConvert(Type t) => t == typeof(DayNight);

        public override DayNight Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var value = reader.GetString();
            switch (value) {
                case "Day":
                    return DayNight.Day;
                case "Night":
                    return DayNight.Night;
            }
            throw new Exception("Cannot unmarshal type DayNight");
        }

        public override void Write(Utf8JsonWriter writer, DayNight value, JsonSerializerOptions options) {
            switch (value) {
                case DayNight.Day:
                    JsonSerializer.Serialize(writer, "Day", options);
                    return;
                case DayNight.Night:
                    JsonSerializer.Serialize(writer, "Night", options);
                    return;
            }
            throw new Exception("Cannot marshal type DayNight");
        }

        public static readonly DayNightConverter Singleton = new DayNightConverter();
    }
    internal class GamemodeConverter : JsonConverter<Gamemode> {
        public override bool CanConvert(Type t) => t == typeof(Gamemode);

        public override Gamemode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var value = reader.GetString();
            switch (value) {
                case "CONQ":
                    return Gamemode.Conq;
                case "CaptureTheFlag":
                    return Gamemode.CaptureTheFlag;
                case "DOMI":
                    return Gamemode.Domi;
                case "ELI":
                    return Gamemode.Eli;
                case "FRONTLINE":
                    return Gamemode.Frontline;
                case "INFCONQ":
                    return Gamemode.Infconq;
                case "RUSH":
                    return Gamemode.Rush;
                case "TDM":
                    return Gamemode.Tdm;
                case "VoxelFortify":
                    return Gamemode.VoxelFortify;
            }
            throw new Exception("Cannot unmarshal type Gamemode");
        }

        public override void Write(Utf8JsonWriter writer, Gamemode value, JsonSerializerOptions options) {
            switch (value) {
                case Gamemode.Conq:
                    JsonSerializer.Serialize(writer, "CONQ", options);
                    return;
                case Gamemode.CaptureTheFlag:
                    JsonSerializer.Serialize(writer, "CaptureTheFlag", options);
                    return;
                case Gamemode.Domi:
                    JsonSerializer.Serialize(writer, "DOMI", options);
                    return;
                case Gamemode.Eli:
                    JsonSerializer.Serialize(writer, "ELI", options);
                    return;
                case Gamemode.Frontline:
                    JsonSerializer.Serialize(writer, "FRONTLINE", options);
                    return;
                case Gamemode.Infconq:
                    JsonSerializer.Serialize(writer, "INFCONQ", options);
                    return;
                case Gamemode.Rush:
                    JsonSerializer.Serialize(writer, "RUSH", options);
                    return;
                case Gamemode.Tdm:
                    JsonSerializer.Serialize(writer, "TDM", options);
                    return;
                case Gamemode.VoxelFortify:
                    JsonSerializer.Serialize(writer, "VoxelFortify", options);
                    return;
            }
            throw new Exception("Cannot marshal type Gamemode");
        }

        public static readonly GamemodeConverter Singleton = new GamemodeConverter();
    }
    internal class MapSizeConverter : JsonConverter<MapSize> {
        public override bool CanConvert(Type t) => t == typeof(MapSize);

        public override MapSize Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var value = reader.GetString();
            switch (value) {
                case "Big":
                    return MapSize.Big;
                case "Medium":
                    return MapSize.Medium;
                case "Small":
                    return MapSize.Small;
                case "Tiny":
                    return MapSize.Tiny;
                case "Ultra":
                    return MapSize.Ultra;
            }
            throw new Exception("Cannot unmarshal type MapSize");
        }

        public override void Write(Utf8JsonWriter writer, MapSize value, JsonSerializerOptions options) {
            switch (value) {
                case MapSize.Big:
                    JsonSerializer.Serialize(writer, "Big", options);
                    return;
                case MapSize.Medium:
                    JsonSerializer.Serialize(writer, "Medium", options);
                    return;
                case MapSize.Small:
                    JsonSerializer.Serialize(writer, "Small", options);
                    return;
                case MapSize.Tiny:
                    JsonSerializer.Serialize(writer, "Tiny", options);
                    return;
                case MapSize.Ultra:
                    JsonSerializer.Serialize(writer, "Ultra", options);
                    return;
            }
            throw new Exception("Cannot marshal type MapSize");
        }

        public static readonly MapSizeConverter Singleton = new MapSizeConverter();
    }
    internal class RegionConverter : JsonConverter<Region> {
        public override bool CanConvert(Type t) => t == typeof(Region);

        public override Region Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var value = reader.GetString();
            switch (value) {
                case "America_Central":
                    return Region.AmericaCentral;
                case "Asia_Central":
                    return Region.AsiaCentral;
                case "Australia_Central":
                    return Region.AustraliaCentral;
                case "Brazil_Central":
                    return Region.BrazilCentral;
                case "Europe_Central":
                    return Region.EuropeCentral;
                case "Japan_Central":
                    return Region.JapanCentral;
            }
            throw new Exception("Cannot unmarshal type Region");
        }

        public override void Write(Utf8JsonWriter writer, Region value, JsonSerializerOptions options) {
            switch (value) {
                case Region.AmericaCentral:
                    JsonSerializer.Serialize(writer, "America_Central", options);
                    return;
                case Region.AsiaCentral:
                    JsonSerializer.Serialize(writer, "Asia_Central", options);
                    return;
                case Region.AustraliaCentral:
                    JsonSerializer.Serialize(writer, "Australia_Central", options);
                    return;
                case Region.BrazilCentral:
                    JsonSerializer.Serialize(writer, "Brazil_Central", options);
                    return;
                case Region.EuropeCentral:
                    JsonSerializer.Serialize(writer, "Europe_Central", options);
                    return;
                case Region.JapanCentral:
                    JsonSerializer.Serialize(writer, "Japan_Central", options);
                    return;
            }
            throw new Exception("Cannot marshal type Region");
        }

        public static readonly RegionConverter Singleton = new RegionConverter();
    }

}
