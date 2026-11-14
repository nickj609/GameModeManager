// Included libraries
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using CounterStrikeSharp.API.Core;
using MaxMind.Db;
using Microsoft.Extensions.Logging;

// Declare namespace
namespace GameModeManager.Panel
{
    // Define class
    // The panel's "Player countries": each player's country from their IP address, looked up here, on the game
    // server, in DB-IP's free "IP to Country Lite" database (CC BY 4.0, https://db-ip.com). Only the two-letter country
    // code goes to the panel with the roster; the IP address never leaves the server and isn't kept. The database is
    // downloaded next to the plugin (data/dbip-country-lite.mmdb) and refreshed when it's a month old, off the game thread.
    public class PanelCountries : IPluginDependency<Plugin, Config>
    {
        // Define class dependencies
        private ILogger<PanelCountries> _logger;
        private Config _config = new();

        // Define class constructor
        public PanelCountries(ILogger<PanelCountries> logger)
        {
            _logger = logger;
        }

        // Define class properties
        private const string FileName = "dbip-country-lite.mmdb";
        private static readonly TimeSpan MaxAge = TimeSpan.FromDays(32);
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(2) };
        private volatile Reader? _reader;
        private string _folder = "";
        private readonly Dictionary<ulong, string?> _bySteamId = new();   // Game thread only: one lookup per player per map

        // Load config
        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        // Find or fetch the database, off the game thread
        public void OnLoad(Plugin plugin)
        {
            _folder = Path.Combine(plugin.ModuleDirectory, "data");
            if (!_config.Panel.Enabled || !_config.Panel.Countries)
                return;
            Task.Run(PrepareAsync);
        }

        public void OnMapStart(string mapName) => _bySteamId.Clear();

        // On the game thread: the player's country code (ISO 3166-1 alpha-2, "US"), or null when off, unknown or a LAN address
        public string? Of(CCSPlayerController player)
        {
            if (!_config.Panel.Countries || _reader is not Reader reader || player.AuthorizedSteamID == null)
                return null;
            ulong id = player.AuthorizedSteamID.SteamId64;
            if (_bySteamId.TryGetValue(id, out string? known))
                return known;
            string? code = null;
            string address = player.IpAddress ?? "";
            int colon = address.LastIndexOf(':');
            if (colon > 0 && address.IndexOf(':') == colon)
                address = address[..colon];   // "1.2.3.4:27005" -> "1.2.3.4"
            if (IPAddress.TryParse(address, out IPAddress? ip) && Public(ip))
            {
                try
                {
                    var found = reader.Find<Dictionary<string, object>>(ip);
                    if (found != null && found.TryGetValue("country", out object? country) && country is Dictionary<string, object> c
                        && c.TryGetValue("iso_code", out object? iso) && iso is string { Length: 2 } two)
                        code = two.ToUpperInvariant();
                }
                catch (Exception error)
                {
                    _logger.LogDebug("[Panel] Country lookup failed: {Error}", error.Message);
                }
            }
            _bySteamId[id] = code;
            return code;
        }

        // Define class methods
        private static bool Public(IPAddress ip)
        {
            if (IPAddress.IsLoopback(ip) || ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal)
                return false;
            if (ip.AddressFamily != AddressFamily.InterNetwork)
                return true;
            byte[] b = ip.GetAddressBytes();
            return !(b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168) || (b[0] == 100 && b[1] >= 64 && b[1] <= 127) || b[0] == 127);
        }

        private async Task PrepareAsync()
        {
            try
            {
                Directory.CreateDirectory(_folder);
                string path = Path.Combine(_folder, FileName);
                bool fresh = File.Exists(path) && DateTime.UtcNow - File.GetLastWriteTimeUtc(path) < MaxAge;
                if (!fresh && !await DownloadAsync(path) && !File.Exists(path))
                    return;
                _reader = new Reader(path, FileAccessMode.Memory);
                _logger.LogInformation("[Panel] Country lookups ready (DB-IP, {Date:yyyy-MM})", File.GetLastWriteTimeUtc(path));
            }
            catch (Exception error)
            {
                _logger.LogWarning("[Panel] Country lookups are off: {Error}", error.Message);
            }
        }

        // DB-IP publishes each month's file as dbip-country-lite-YYYY-MM.mmdb.gz; early in a month it may not be out yet
        private async Task<bool> DownloadAsync(string path)
        {
            foreach (DateTime month in new[] { DateTime.UtcNow, DateTime.UtcNow.AddMonths(-1) })
            {
                string url = $"https://download.db-ip.com/free/dbip-country-lite-{month:yyyy-MM}.mmdb.gz";
                string temp = path + ".download";
                try
                {
                    using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    if (!response.IsSuccessStatusCode)
                        continue;
                    await using (var gz = new GZipStream(await response.Content.ReadAsStreamAsync(), CompressionMode.Decompress))
                    await using (var file = File.Create(temp))
                        await gz.CopyToAsync(file);
                    using (new Reader(temp, FileAccessMode.Memory)) { }   // Readable before it replaces the old one
                    File.Move(temp, path, overwrite: true);
                    _logger.LogInformation("[Panel] Downloaded DB-IP's country database ({Month:yyyy-MM})", month);
                    return true;
                }
                catch (Exception error)
                {
                    _logger.LogWarning("[Panel] Couldn't download DB-IP's country database from {Url}: {Error}", url, error.Message);
                    try { File.Delete(temp); } catch { }
                }
            }
            return false;
        }
    }
}
