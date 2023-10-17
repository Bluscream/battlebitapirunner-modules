using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

using BBRAPIModules;
using static Bluscream.Response;

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
        public virtual Enums.MapSize? MapSize { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("Gamemode")]
        public virtual Enums.Gamemode? Gamemode { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("Region")]
        public virtual Enums.Region? Region { get; set; }

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
        public virtual Enums.DayNight? DayNight { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("IsOfficial")]
        public virtual bool? IsOfficial { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("HasPassword")]
        public virtual bool? HasPassword { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("AntiCheat")]
        public virtual Enums.AntiCheat? AntiCheat { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("Build")]
        public virtual Enums.Build? Build { get; set; }

        public class Enums {
            public enum AntiCheat { Unknown, Eac };
            public enum Build { Unknown, Production216, Production218Hotfix, Production218SecHotfix };
            public enum DayNight { Unknown, Day, Night };
            public enum Gamemode { Unknown, CaptureTheFlag, Conq, Domi, Eli, Frontline, Infconq, Rush, Tdm, VoxelFortify };
            public enum MapSize { Unknown, Big, Medium, Small, Tiny, Ultra };
            public enum Region { Unknown, AmericaCentral, AsiaCentral, AustraliaCentral, BrazilCentral, EuropeCentral, JapanCentral };
        }
        public static List<Response> FromJson(string json) => JsonSerializer.Deserialize<List<Response>>(json, Bluscream.Converter.Settings);
    }

    public static class Serialize {
        public static string ToJson(this List<Response> self) => JsonSerializer.Serialize(self, Converter.Settings);
    }


    internal class AntiCheatConverter : JsonConverter<Enums.AntiCheat> {
        public override bool CanConvert(Type t) => t == typeof(Enums.AntiCheat);

        public override Enums.AntiCheat Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var value = reader.GetString();
            if (value == "EAC") {
                return Enums.AntiCheat.Eac;
            }
            throw new Exception("Cannot unmarshal type AntiCheat");
        }

        public override void Write(Utf8JsonWriter writer, Enums.AntiCheat value, JsonSerializerOptions options) {
            if (value == Enums.AntiCheat.Eac) {
                JsonSerializer.Serialize(writer, "EAC", options);
                return;
            }
            throw new Exception("Cannot marshal type AntiCheat");
        }

        public static readonly AntiCheatConverter Singleton = new AntiCheatConverter();
    }
    internal class BuildConverter : JsonConverter<Enums.Build> {
        public override bool CanConvert(Type t) => t == typeof(Enums.Build);

        public override Enums.Build Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var value = reader.GetString();
            switch (value) {
                case "Production 2.1.6":
                    return Enums.Build.Production216;
                case "Production 2.1.8 Sec-Hotfix":
                    return Enums.Build.Production218SecHotfix;
                case "Production 2.1.8 hotfix":
                    return Enums.Build.Production218Hotfix; // TODO READ FROM DATA
            }
            throw new Exception("Cannot unmarshal type Build");
        }

        public override void Write(Utf8JsonWriter writer, Enums.Build value, JsonSerializerOptions options) {
            switch (value) {
                case Enums.Build.Production216:
                    JsonSerializer.Serialize(writer, "Production 2.1.6", options);
                    return;
                case Enums.Build.Production218SecHotfix:
                    JsonSerializer.Serialize(writer, "Production 2.1.8 Sec-Hotfix", options);
                    return;
                case Enums.Build.Production218Hotfix:
                    JsonSerializer.Serialize(writer, "Production 2.1.8 hotfix", options);
                    return;
            }
            throw new Exception("Cannot marshal type Build");
        }

        public static readonly BuildConverter Singleton = new BuildConverter();
    }
    internal class DayNightConverter : JsonConverter<Enums.DayNight> {
        public override bool CanConvert(Type t) => t == typeof(Enums.DayNight);

        public override Enums.DayNight Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var value = reader.GetString();
            switch (value) {
                case "Day":
                    return Enums.DayNight.Day;
                case "Night":
                    return Enums.DayNight.Night;
            }
            throw new Exception("Cannot unmarshal type DayNight");
        }

        public override void Write(Utf8JsonWriter writer, Enums.DayNight value, JsonSerializerOptions options) {
            switch (value) {
                case Enums.DayNight.Day:
                    JsonSerializer.Serialize(writer, "Day", options);
                    return;
                case Enums.DayNight.Night:
                    JsonSerializer.Serialize(writer, "Night", options);
                    return;
            }
            throw new Exception("Cannot marshal type DayNight");
        }

        public static readonly DayNightConverter Singleton = new DayNightConverter();
    }
    internal class GamemodeConverter : JsonConverter<Enums.Gamemode> {
        public override bool CanConvert(Type t) => t == typeof(Enums.Gamemode);

        public override Enums.Gamemode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var value = reader.GetString();
            switch (value) {
                case "CONQ":
                    return Enums.Gamemode.Conq;
                case "CaptureTheFlag":
                    return Enums.Gamemode.CaptureTheFlag;
                case "DOMI":
                    return Enums.Gamemode.Domi;
                case "ELI":
                    return Enums.Gamemode.Eli;
                case "FRONTLINE":
                    return Enums.Gamemode.Frontline;
                case "INFCONQ":
                    return Enums.Gamemode.Infconq;
                case "RUSH":
                    return Enums.Gamemode.Rush;
                case "TDM":
                    return Enums.Gamemode.Tdm;
                case "VoxelFortify":
                    return Enums.Gamemode.VoxelFortify;
            }
            throw new Exception("Cannot unmarshal type Gamemode");
        }

        public override void Write(Utf8JsonWriter writer, Enums.Gamemode value, JsonSerializerOptions options) {
            switch (value) {
                case Enums.Gamemode.Conq:
                    JsonSerializer.Serialize(writer, "CONQ", options);
                    return;
                case Enums.Gamemode.CaptureTheFlag:
                    JsonSerializer.Serialize(writer, "CaptureTheFlag", options);
                    return;
                case Enums.Gamemode.Domi:
                    JsonSerializer.Serialize(writer, "DOMI", options);
                    return;
                case Enums.Gamemode.Eli:
                    JsonSerializer.Serialize(writer, "ELI", options);
                    return;
                case Enums.Gamemode.Frontline:
                    JsonSerializer.Serialize(writer, "FRONTLINE", options);
                    return;
                case Enums.Gamemode.Infconq:
                    JsonSerializer.Serialize(writer, "INFCONQ", options);
                    return;
                case Enums.Gamemode.Rush:
                    JsonSerializer.Serialize(writer, "RUSH", options);
                    return;
                case Enums.Gamemode.Tdm:
                    JsonSerializer.Serialize(writer, "TDM", options);
                    return;
                case Enums.Gamemode.VoxelFortify:
                    JsonSerializer.Serialize(writer, "VoxelFortify", options);
                    return;
            }
            throw new Exception("Cannot marshal type Gamemode");
        }

        public static readonly GamemodeConverter Singleton = new GamemodeConverter();
    }
    internal class MapSizeConverter : JsonConverter<Enums.MapSize> {
        public override bool CanConvert(Type t) => t == typeof(Enums.MapSize);

        public override Enums.MapSize Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var value = reader.GetString();
            switch (value) {
                case "Big":
                    return Enums.MapSize.Big;
                case "Medium":
                    return Enums.MapSize.Medium;
                case "Small":
                    return Enums.MapSize.Small;
                case "Tiny":
                    return Enums.MapSize.Tiny;
                case "Ultra":
                    return Enums.MapSize.Ultra;
            }
            throw new Exception("Cannot unmarshal type MapSize");
        }

        public override void Write(Utf8JsonWriter writer, Enums.MapSize value, JsonSerializerOptions options) {
            switch (value) {
                case Enums.MapSize.Big:
                    JsonSerializer.Serialize(writer, "Big", options);
                    return;
                case Enums.MapSize.Medium:
                    JsonSerializer.Serialize(writer, "Medium", options);
                    return;
                case Enums.MapSize.Small:
                    JsonSerializer.Serialize(writer, "Small", options);
                    return;
                case Enums.MapSize.Tiny:
                    JsonSerializer.Serialize(writer, "Tiny", options);
                    return;
                case Enums.MapSize.Ultra:
                    JsonSerializer.Serialize(writer, "Ultra", options);
                    return;
            }
            throw new Exception("Cannot marshal type MapSize");
        }

        public static readonly MapSizeConverter Singleton = new MapSizeConverter();
    }
    internal class RegionConverter : JsonConverter<Enums.Region> {
        public override bool CanConvert(Type t) => t == typeof(Enums.Region);

        public override Enums.Region Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var value = reader.GetString();
            switch (value) {
                case "America_Central":
                    return Enums.Region.AmericaCentral;
                case "Asia_Central":
                    return Enums.Region.AsiaCentral;
                case "Australia_Central":
                    return Enums.Region.AustraliaCentral;
                case "Brazil_Central":
                    return Enums.Region.BrazilCentral;
                case "Europe_Central":
                    return Enums.Region.EuropeCentral;
                case "Japan_Central":
                    return Enums.Region.JapanCentral;
            }
            throw new Exception("Cannot unmarshal type Region");
        }

        public override void Write(Utf8JsonWriter writer, Enums.Region value, JsonSerializerOptions options) {
            switch (value) {
                case Enums.Region.AmericaCentral:
                    JsonSerializer.Serialize(writer, "America_Central", options);
                    return;
                case Enums.Region.AsiaCentral:
                    JsonSerializer.Serialize(writer, "Asia_Central", options);
                    return;
                case Enums.Region.AustraliaCentral:
                    JsonSerializer.Serialize(writer, "Australia_Central", options);
                    return;
                case Enums.Region.BrazilCentral:
                    JsonSerializer.Serialize(writer, "Brazil_Central", options);
                    return;
                case Enums.Region.EuropeCentral:
                    JsonSerializer.Serialize(writer, "Europe_Central", options);
                    return;
                case Enums.Region.JapanCentral:
                    JsonSerializer.Serialize(writer, "Japan_Central", options);
                    return;
            }
            throw new Exception("Cannot marshal type Region");
        }

        public static readonly RegionConverter Singleton = new RegionConverter();
    }

}
