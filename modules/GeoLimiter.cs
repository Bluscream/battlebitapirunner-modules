using BBRAPIModules;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GeoLimiter;

[Module("Kick users based on their country of origin", "1.0.0")]
public class GeoLimiter : BattleBitModule
{
    public static GeoLimiterConfiguration Configuration { get; set; } = null!;
    public GeoLimiterServerConfiguration ServerConfiguration { get; set; } = null!;

    private static Geolocation[] database = null!;

    public override void OnModulesLoaded()
    {
        if (!File.Exists(Path.Combine(Configuration.DataDirectory, Configuration.DatabaseFileName)))
        {
            Console.WriteLine($"Could not find geolocation database at {Path.GetFullPath(Path.Combine(Configuration.DataDirectory, Configuration.DatabaseFileName))}");
            this.Unload();
            return;
        }

        if (database != null)
        {
            return;
        }

        database = File.ReadAllLines(Path.Combine(Configuration.DataDirectory, Configuration.DatabaseFileName))
            .Select(x => x.Split(','))
            .Select(x => new Geolocation() { FromIP = uint.Parse(x[0].Trim('"')), ToIP = uint.Parse(x[1].Trim('"')), CountryCode = x[2].Trim('"'), CountryName = x[3].Trim('"') })
            .ToArray();
    }

    public override Task OnPlayerConnected(RunnerPlayer player)
    {
        byte[] ipBytes = player.IP.GetAddressBytes();
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(ipBytes);
        }

        int ipNumber = BitConverter.ToInt32(ipBytes, 0);
        Console.WriteLine($"IP address {player.IP} as integer: {ipNumber}");

        Geolocation? geolocation = database.FirstOrDefault(x => x.FromIP <= ipNumber && x.ToIP >= ipNumber);
        if (geolocation == null)
        {
            Console.WriteLine($"Could not find geolocation for IP {player.IP}");
            return Task.CompletedTask;
        }

        if (ServerConfiguration.AllowedCountries?.Contains(geolocation.CountryCode) == true)
        {
            return Task.CompletedTask;
        }

        if (ServerConfiguration.DisallowedCountries?.Contains(geolocation.CountryCode) == false)
        {
            return Task.CompletedTask;
        }

        Console.WriteLine($"Kicking player {player.SteamID} because country {geolocation.CountryCode} is disallowed");
        player.Kick(string.Format(ServerConfiguration.KickMessage, geolocation.CountryName));

        return Task.CompletedTask;
    }
}

public class GeoLimiterConfiguration : ModuleConfiguration
{
    public string DataDirectory { get; set; } = "./data/GeoLimiter";
    public string DatabaseFileName { get; set; } = "IP2LOCATION-LITE-DB1.CSV";
}

public class GeoLimiterServerConfiguration : ModuleConfiguration
{
    public string[]? AllowedCountries { get; set; } = new string[] { "DE", "AT", "CH" };
    public string[]? DisallowedCountries { get; set; } = new string[] { "US", "RU", "CN" };

    public string KickMessage { get; set; } = "Your country {0} is not allowed on this server.";
}

public class Geolocation
{
    public uint FromIP { get; set; }
    public uint ToIP { get; set; }
    public string CountryCode { get; set; } = null!;
    public string CountryName { get; set; } = null!;
}
