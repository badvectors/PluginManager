using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace UpdaterPlugin.Updater
{
    internal class Program
    {
        private static readonly string VatsysProcessName = $@"vatSys";
        private static readonly string VatsysDirectory = $@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\vatSys Files\";
        
        private static readonly HttpClient HttpClient = new HttpClient();

        public static List<PluginInfo> Plugins { get; set; } = new List<PluginInfo>();
        public static List<KeyValuePair<string, PluginInfo>> FoundPlugins { get; set; } = new List<KeyValuePair<string, PluginInfo>>();
        public static Serilog.Core.Logger Logger { get; set; }
        public static bool Running { get; set; }

        static void Main()
        {
            Logger = new LoggerConfiguration()
                        .WriteTo.Console()
                        .WriteTo.File("logs.txt", rollingInterval: RollingInterval.Day)
                        .MinimumLevel.Debug()
                        .CreateLogger();

            DoArt();

            Plugins.Add(new PluginInfo("badvectors/ATISPlugin", "ATISPlugin.dll"));

            try
            {
                // Check and exit if a version already running.
                if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
                {
                    Environment.Exit(0);
                }

                // Check to see that vatSys is running.
                CheckVatsys();

                Running = true;

                Logger.Information($"Updater app started.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not start updater: {ex.Message}");
            }

            while (Running) Thread.Sleep(TimeSpan.FromSeconds(3));
        }

        private static void CheckVatsys()
        {
            // Get the vatSys process.
            var vatsysProcess = Process.GetProcessesByName(VatsysProcessName);

            // If vatSys not running close.
            if (vatsysProcess.Length == 0)
            {
                Logger.Information("vatSys was not open. Closing app.");
                Environment.Exit(0);
            }

            Logger.Information("Waiting for vatSys to close.");

            // Monitor for vatSys to exit to update the plugins.
            ProcessMonitor.MonitorForExit(vatsysProcess[0]);
        }

        public static class ProcessMonitor
        {
            public static event EventHandler ProcessClosed;

            public static void MonitorForExit(Process process)
            {
                Thread thread = new Thread(() =>
                {
                    process.WaitForExit();
                    OnProcessClosed(EventArgs.Empty);
                });
                thread.Start();
            }
        }

        private static async void OnProcessClosed(EventArgs e)
        {
            Logger.Information("vatSys closed, checking for plugin updates.");
            
            if (Directory.Exists(VatsysDirectory))
            {
                await ProcessDirectory(VatsysDirectory);
            }

            foreach (var plugin in FoundPlugins)
            {
                await ProcessPlugin(plugin.Key, plugin.Value); 
            }

            // Now close this app.
            Logger.Information("Completed.");

            Running = false;
        }

        // Process all files in the directory passed in, recurse on any directories 
        // that are found, and process the files they contain.
        public static async Task ProcessDirectory(string targetDirectory)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                ProcessFile(fileName);

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                await ProcessDirectory(subdirectory);
        }

        // Insert logic for processing found files here.
        public static void ProcessFile(string path)
        {
            var pluginInfo = Plugins.FirstOrDefault(x => path.EndsWith(x.DllName));

            if (pluginInfo == null) return;

            Logger.Information($"Found '{pluginInfo.Name}' @ {path}.");

            FoundPlugins.Add(new KeyValuePair<string, PluginInfo>(path, pluginInfo));
        }

        public static async Task ProcessPlugin(string path, PluginInfo pluginInfo)
        {
            Logger.Information($"Processing '{pluginInfo.Name}' @ {path}.");

            var localDirectory = path.Substring(0, path.Length - pluginInfo.DllName.Length);

            var localVersionPath = Path.Combine(localDirectory, "Version.json");

            if (!File.Exists(localVersionPath))
            {
                Logger.Warning("No local version file found.");
                await Update(pluginInfo, localDirectory);
                return;
            }

            var localVersion = JsonConvert.DeserializeObject<Version>(File.ReadAllText(localVersionPath));

            Logger.Information($"Local version: {localVersion.Major}.{localVersion.Minor}.{localVersion.Build}.");

            var response = await HttpClient.GetAsync(pluginInfo.VersionUrl);

            if (!response.IsSuccessStatusCode)
            {
                Logger.Error("Remote version file was not found.");
                return;
            }

            var remoteVersionContent = await response.Content.ReadAsStringAsync();

            var remoteVersion = JsonConvert.DeserializeObject<Version>(remoteVersionContent);

            Logger.Information($"Remote version: {remoteVersion.Major}.{remoteVersion.Minor}.{remoteVersion.Build}.");

            if (localVersion >= remoteVersion)
            {
                Logger.Information("Plugin up-to-date.");
                return;
            }

            await Update(pluginInfo, localDirectory);
        }

        public static async Task Update(PluginInfo pluginInfo, string localDirectory)
        {
            Logger.Information("Updating plugin.");

            // delete contents
            if (!Directory.Exists(localDirectory)) return;

            Logger.Information("Removing existing version.");

            try
            {
                string[] fileEntries = Directory.GetFiles(localDirectory);
                foreach (string fileName in fileEntries)
                {
                    File.Delete(fileName);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not remove existing version: {ex.Message}");
                return;
            }

            // download update
            await Download(pluginInfo.DownladUrl, localDirectory);

            // unzip
            Extract(localDirectory);

            // delete download

            if (!File.Exists(Path.Combine(localDirectory, "Plugin.zip"))) return;

            Logger.Information("Removing download.");

            File.Delete(Path.Combine(localDirectory, "Plugin.zip"));
        }

        public static async Task Download(string downloadUrl, string localDirectory)
        {
            Logger.Information($"Downloading plugin from: {downloadUrl}");

            using (var response = await HttpClient.GetAsync(downloadUrl))
            {
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"Could not download plugin: {response.StatusCode}");
                    return;
                }

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var file = File.OpenWrite(Path.Combine(localDirectory, "Plugin.zip")))
                {
                    stream.CopyTo(file);
                }
            }

            Logger.Information("Download completed.");
        }

        public static void Extract(string localDirectory)
        {
            Logger.Information("Extracting plugin.");

            try
            {
                ZipFile.ExtractToDirectory(Path.Combine(localDirectory, "Plugin.zip"), localDirectory);
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not extract plugin: {ex.Message}");
                return;
            }

            Logger.Information("Extract completed.");
        }

        public static void DoArt()
        {
            Console.WriteLine("\r\n\r\n             _    _____             _____  _             _         _    _           _       _            \r\n            | |  / ____|           |  __ \\| |           (_)       | |  | |         | |     | |           \r\n __   ____ _| |_| (___  _   _ ___  | |__) | |_   _  __ _ _ _ __   | |  | |_ __   __| | __ _| |_ ___ _ __ \r\n \\ \\ / / _` | __|\\___ \\| | | / __| |  ___/| | | | |/ _` | | '_ \\  | |  | | '_ \\ / _` |/ _` | __/ _ \\ '__|\r\n  \\ V / (_| | |_ ____) | |_| \\__ \\ | |    | | |_| | (_| | | | | | | |__| | |_) | (_| | (_| | ||  __/ |   \r\n   \\_/ \\__,_|\\__|_____/ \\__, |___/ |_|    |_|\\__,_|\\__, |_|_| |_|  \\____/| .__/ \\__,_|\\__,_|\\__\\___|_|   \r\n                         __/ |                      __/ |                | |                             \r\n                        |___/                      |___/                 |_|                             \r\n\r\n");
        }
    }
}
