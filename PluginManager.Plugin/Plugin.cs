using System;
using System.ComponentModel.Composition;
using vatsys;
using vatsys.Plugin;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PluginManager.Common;
using System.Collections.Generic;
using System.Reflection;

namespace PluginManager.Plugin
{
    [Export(typeof(IPlugin))]
    public class Plugin : IPlugin
    {
        public string Name => "Plugin Manager";
        public static string DisplayName => "Plugin Manager";

        private static readonly Version Version = new Version(1, 1);
        private static readonly string VersionUrl = "https://raw.githubusercontent.com/badvectors/PluginManager/master/Version.json";
        private static readonly string PluginsUrl = "https://raw.githubusercontent.com/badvectors/PluginManager/master/Plugins.json";

        private static readonly string AppName = "PluginManager.Updater";
        private static readonly string AppFile = $"{AppName}.exe";

        private static readonly HttpClient HttpClient = new HttpClient();
        public static List<PluginInfo> Plugins { get; set; } = new List<PluginInfo>();

        private static CustomToolStripMenuItem UpdaterMenu;
        private static PluginWindow UpdaterWindow;

        public Plugin()
        {
            UpdaterMenu = new CustomToolStripMenuItem(CustomToolStripMenuItemWindowType.Main, CustomToolStripMenuItemCategory.Settings, new ToolStripMenuItem(DisplayName));
            UpdaterMenu.Item.Click += UpdaterMenu_Click;
            MMI.AddCustomMenuItem(UpdaterMenu);

            _ = GetPlugins();

            _ = CheckVersion();
        }

        private void UpdaterMenu_Click(object sender, EventArgs e)
        {
            ShowUpdaterWindow();
        }

        private static void ShowUpdaterWindow()
        {
            MMI.InvokeOnGUI((MethodInvoker)delegate ()
            {
                if (UpdaterWindow == null || UpdaterWindow.IsDisposed)
                {
                    UpdaterWindow = new PluginWindow();
                }
                else if (UpdaterWindow.Visible) return;

                UpdaterWindow.Show();
            });
        }

        private static async Task CheckVersion()
        {
            try
            {
                var response = await HttpClient.GetStringAsync(VersionUrl);

                var remoteVersion = JsonConvert.DeserializeObject<Version>(response);

                if (remoteVersion.Major == Version.Major && remoteVersion.Minor == Version.Minor) return;

                Errors.Add(new Exception("A new version of the plugin is available."), DisplayName);
            }
            catch { }
        }

        private static async Task GetPlugins()
        {
            var response = await HttpClient.GetAsync(PluginsUrl);

            if (!response.IsSuccessStatusCode)
            {
                return;
            }

            var stringContent = await response.Content.ReadAsStringAsync();

            var plugins = JsonConvert.DeserializeObject<PluginInfo[]>(stringContent);

            foreach (var plugin in plugins)
            {
                Plugins.Add(new PluginInfo(plugin.Name, plugin.DllName, plugin.Description));
            }
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public static void StartUpdaterApp(string command)
        {
            if (Process.GetProcessesByName(AppName).Any()) return;

            if (string.IsNullOrWhiteSpace(command)) return;

            var file = Path.Combine(AssemblyDirectory, AppFile);

            if (!new FileInfo(file).Exists) return;

            Process proc = new Process();
            proc.StartInfo.FileName = file;
            proc.StartInfo.Arguments = $"\"{command}\" \"{Helpers.GetProgramFolder()}\" \"{Helpers.GetFilesFolder()}";
            proc.StartInfo.UseShellExecute = true;
            proc.StartInfo.Verb = "runas";
            proc.Start();
        }

        public void OnFDRUpdate(FDP2.FDR updated)
        {
            return;
        }

        public void OnRadarTrackUpdate(RDP.RadarTrack updated)
        {
            return;
        }
    }
}
