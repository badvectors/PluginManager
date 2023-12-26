namespace UpdaterPlugin.Plugin
{
    partial class UpdaterWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.buttonUpdate = new System.Windows.Forms.Button();
            this.buttonInstall = new System.Windows.Forms.Button();
            this.comboBoxPlugin = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // buttonUpdate
            // 
            this.buttonUpdate.Location = new System.Drawing.Point(12, 41);
            this.buttonUpdate.Name = "buttonUpdate";
            this.buttonUpdate.Size = new System.Drawing.Size(352, 26);
            this.buttonUpdate.TabIndex = 0;
            this.buttonUpdate.Text = "Update Existing";
            this.buttonUpdate.UseVisualStyleBackColor = true;
            this.buttonUpdate.Click += new System.EventHandler(this.ButtonUpdate_Click);
            // 
            // buttonInstall
            // 
            this.buttonInstall.Location = new System.Drawing.Point(277, 10);
            this.buttonInstall.Name = "buttonInstall";
            this.buttonInstall.Size = new System.Drawing.Size(87, 26);
            this.buttonInstall.TabIndex = 1;
            this.buttonInstall.Text = "Install";
            this.buttonInstall.UseVisualStyleBackColor = true;
            this.buttonInstall.Click += new System.EventHandler(this.ButtonInstall_Click);
            // 
            // comboBoxPlugin
            // 
            this.comboBoxPlugin.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPlugin.FormattingEnabled = true;
            this.comboBoxPlugin.Location = new System.Drawing.Point(12, 10);
            this.comboBoxPlugin.Name = "comboBoxPlugin";
            this.comboBoxPlugin.Size = new System.Drawing.Size(259, 25);
            this.comboBoxPlugin.TabIndex = 2;
            // 
            // UpdaterWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.Disable;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(376, 82);
            this.Controls.Add(this.comboBoxPlugin);
            this.Controls.Add(this.buttonInstall);
            this.Controls.Add(this.buttonUpdate);
            this.ForeColor = System.Drawing.SystemColors.InfoText;
            this.HasMinimizeButton = false;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximumSize = new System.Drawing.Size(380, 110);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(380, 110);
            this.Name = "UpdaterWindow";
            this.Resizeable = false;
            this.Text = "Plugin Manager";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.OnLoad);
            this.ResumeLayout(false);

        }

        #endregion
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.Button buttonUpdate;
        private System.Windows.Forms.Button buttonInstall;
        private System.Windows.Forms.ComboBox comboBoxPlugin;
    }
}