using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UpdaterPlugin.Common;

namespace UpdaterPlugin.Updater
{
    internal class Program
    {
        private static readonly string VatsysProcessName = $@"vatSys";
        private static readonly string PluginsUrl = "https://raw.githubusercontent.com/badvectors/UpdaterPlugin/master/Plugins.json";

        private static readonly HttpClient HttpClient = new HttpClient();

        public static List<PluginInfo> Plugins { get; set; } = new List<PluginInfo>();
        public static List<KeyValuePair<string, PluginInfo>> FoundPlugins { get; set; } = new List<KeyValuePair<string, PluginInfo>>();
        public static Serilog.Core.Logger Logger { get; set; }
        public static string BaseDirectory => Directories.First();

        private static List<string> Directories { get; set; } = new List<string>();

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;

            if (args.Length == 0)
            {
                args = new string[] { "Update", @"C:\Program Files (x86)\vatSys", @"C:\Users\ajdun\OneDrive\Documents\vatSys Files\" };
            }

            Logger = new LoggerConfiguration()
                        //.WriteTo.Console()
                        .WriteTo.File($"{args[1]}pluginmanager_log.txt", rollingInterval: RollingInterval.Day)
                        .MinimumLevel.Debug()
                        .CreateLogger();

            foreach (var arg in args)
            {
                Logger.Information($"Start argument: {arg}");
                Console.WriteLine($"Start argument: {arg}");
                if (!Directory.Exists(arg)) continue;
                Directories.Add(arg);
            }

            // Check and exit if a version already running.
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                Environment.Exit(0);
            }

            Logger.Information($"Base directory: {BaseDirectory}");

            Console.WriteLine($"Base directory: {BaseDirectory}");

            Logger.Information($"Updater app started.");

            DoArt();

            // Get plugins.
            GetPlugins().GetAwaiter().GetResult();

            // Find existing plugins.
            FindExisting().GetAwaiter().GetResult();

            // Request go from use.
            Console.WriteLine();
            Console.Write("Press any key to continue. ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("WARNING: vatSys will be restarted.");
            Console.ForegroundColor = ConsoleColor.White;
            Console.ReadKey();
            Console.WriteLine();

            // Get any running vatSys processes.
            var vatsysProcesses = Process.GetProcessesByName(VatsysProcessName);

            // Kill all running vatSys processes.
            if (vatsysProcesses.Length > 0)
            {
                foreach (var vatsysProcess in vatsysProcesses)
                    vatsysProcess.Kill();
            }

            // Interpret the start args.
            if (args.Length == 3)
            {
                if (args[0] == "Update") RunUpdates().GetAwaiter().GetResult();
                else RunInstalls(args[0]).GetAwaiter().GetResult();
            }

            var vatsys = Path.Combine(BaseDirectory, "bin", "vatSys.exe");

            Console.WriteLine("Completed!");

            if (File.Exists(vatsys))
            {
                Console.WriteLine("Restarting vatSys now.");

                Process.Start(vatsys);
            }

            Environment.Exit(0);
        }

        private static async Task GetPlugins()
        {
            Console.Write("Getting available plugins:");

            Logger.Information($"Getting list of plugins.");

            var response = await HttpClient.GetAsync(PluginsUrl);

            if (!response.IsSuccessStatusCode)
            {
                Logger.Error("Could not get list of plugins.");
                Environment.Exit(0);
                return;
            }

            var stringContent = await response.Content.ReadAsStringAsync();

            var plugins = JsonConvert.DeserializeObject<PluginInfo[]>(stringContent);

            foreach (var plugin in plugins)
            {
                Logger.Information($"Adding plugin {plugin.Name}.");
                Plugins.Add(new PluginInfo(plugin.Name, plugin.DllName, plugin.Description));
            }

            Console.WriteLine($" found {plugins.Count()}.");
        }

        private static async Task FindExisting()
        {
            await Console.Out.WriteAsync("Searching for existing plugins:");

            foreach (var directory in Directories)
            {
                await ProcessDirectory(directory);
            }

            await Console.Out.WriteLineAsync($" found {FoundPlugins.Count()}.");
        }

        private static async Task RunInstalls(string name)
        {
            var plugin = Plugins.FirstOrDefault(x => x.Name == name);

            if (plugin == null)
            {
                Console.Write($"Could not find a plugin with name: {name}.");

                Logger.Error($"Could not find a plugin with name: {name}.");

                return;
            }

            if (FoundPlugins.Any(x => x.Key.EndsWith(plugin.DllName)))
            {
                Console.Write($"Plugin was already installed.");

                Logger.Error($"Plugin was already installed.");

                return;
            }

            var directory = $@"{BaseDirectory}\bin\Plugins\{plugin.Name.Split('/')[1]}";

            if (Directory.Exists(directory))
            {
                Console.Write("Did not install because target directory already existed.");

                Logger.Error("Did not install because target directory already existed.");

                return;
            }

            Directory.CreateDirectory(directory);

            await Install(plugin, directory);
        }

        private static async Task RunUpdates()
        {
            foreach (var plugin in FoundPlugins)
            {
                await ProcessPlugin(plugin.Key, plugin.Value); 
            }

            Logger.Information("Completed updating existing plugins.");
        }

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

            Console.WriteLine($"Processing '{pluginInfo.Name}'.");

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
                await Console.Out.WriteLineAsync("Plugin up-to-date.");
                Logger.Information("Plugin up-to-date.");
                return;
            }

            await Update(pluginInfo, localDirectory);
        }

        public static async Task Install(PluginInfo pluginInfo, string localDirectory)
        {
            // download update
            await Download(pluginInfo.DownladUrl, localDirectory);

            // unzip
            Extract(localDirectory);

            // delete download
            if (!File.Exists(Path.Combine(localDirectory, "Plugin.zip"))) return;

            Console.WriteLine("Cleaning up.");

            Logger.Information("Removing download.");

            File.Delete(Path.Combine(localDirectory, "Plugin.zip"));

            Logger.Information("Completed install.");
        }

        public static async Task Update(PluginInfo pluginInfo, string localDirectory)
        {
            Logger.Information("Updating plugin.");

            Console.WriteLine($"Updating plugin.");

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
                Logger.Error($"Could not remove existing version: {ex.Message}.");
                return;
            }

            await Install(pluginInfo, localDirectory); 
        }

        public static async Task Download(string downloadUrl, string localDirectory)
        {
            Logger.Information($"Downloading plugin from: {downloadUrl}.");

            Console.WriteLine($"Downloading plugin from: {downloadUrl}.");

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

            Console.WriteLine("Extracting plugin.");

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
