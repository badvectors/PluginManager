namespace UpdaterPlugin.Common
{
    public class PluginInfo
    {
        public PluginInfo() { }

        public PluginInfo(string name, string dllName, string description) 
        {
            Name = name;
            DllName = dllName;
            Description = description;
        }

        public string Name { get; set; }
        public string DllName { get; set; }
        public string Description { get; set; }
        public string VersionUrl => $"https://raw.githubusercontent.com/{Name}/master/Version.json";
        public string DownladUrl => $"https://github.com/{Name}/releases/download/latest/Plugin.zip";
    }
}
