using System;
using System.Windows.Forms;
using vatsys;

namespace PluginManager.Plugin
{
    public partial class PluginWindow : BaseForm
    {
        public PluginWindow()
        {
            InitializeComponent();

            BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
            ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
        }

        private void ButtonUpdate_Click(object sender, EventArgs e)
        {
            Plugin.StartUpdaterApp("Update");
        }

        private void OnLoad(object sender, EventArgs e)
        {
            comboBoxPlugin.Items.Clear();

            foreach (var plugin in Plugin.Plugins)
            {
                comboBoxPlugin.Items.Add(plugin.Name);
            }
        }

        private void ButtonInstall_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(comboBoxPlugin.Text)) return;

            Plugin.StartUpdaterApp(comboBoxPlugin.Text);
        }
    }
}