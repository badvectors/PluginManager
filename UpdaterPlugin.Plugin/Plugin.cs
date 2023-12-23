using System;
using System.ComponentModel.Composition;
using vatsys;
using vatsys.Plugin;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace UpdaterPlugin.Plugin
{
    [Export(typeof(IPlugin))]
    public class Plugin : IPlugin
    {
        public string Name => "Plugin Updater";
        public static string DisplayName => "Plugin Updater";

        private static readonly Version _version = new Version(1, 1);
        private static readonly string _fileName = $@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\vatSys Files\Discord.json";
        private static readonly string _versionUrl = "https://raw.githubusercontent.com/badvectors/UpdaterPlugin/master/Version.json";

        private static readonly HttpClient _httpClient = new HttpClient();

        public Plugin()
        {
            _ = CheckVersion();
        }

        private static async Task CheckVersion()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(_versionUrl);

                var version = JsonConvert.DeserializeObject<Version>(response);

                if (version == _version) return;

                Errors.Add(new Exception("A new version of the plugin is available."), DisplayName);
            }
            catch { }
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
