﻿namespace x360ce.App.Controls
{
    partial class OptionsControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			this.TestingAndLoggingGroupBox = new System.Windows.Forms.GroupBox();
			this.CombineEnabledCheckBox = new System.Windows.Forms.CheckBox();
			this.XInputEnableCheckBox = new System.Windows.Forms.CheckBox();
			this.UseInitBeepCheckBox = new System.Windows.Forms.CheckBox();
			this.ConsoleCheckBox = new System.Windows.Forms.CheckBox();
			this.DebugModeCheckBox = new System.Windows.Forms.CheckBox();
			this.EnableLoggingCheckBox = new System.Windows.Forms.CheckBox();
			this.OperationGroupBox = new System.Windows.Forms.GroupBox();
			this.AllowOnlyOneCopyCheckBox = new System.Windows.Forms.CheckBox();
			this.InternetGroupBox = new System.Windows.Forms.GroupBox();
			this.InternetDatabaseUrlComboBox = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.InternetAutoloadCheckBox = new System.Windows.Forms.CheckBox();
			this.InternetCheckBox = new System.Windows.Forms.CheckBox();
			this.ProgramScanLocationsTabControl = new System.Windows.Forms.TabControl();
			this.GameScanLocationsTabPage = new System.Windows.Forms.TabPage();
			this.GameScanLocationsListBox = new System.Windows.Forms.ListBox();
			this.LocationsToolStrip = new System.Windows.Forms.ToolStrip();
			this.RefreshLocationsButton = new System.Windows.Forms.ToolStripButton();
			this.RemoveLocationButton = new System.Windows.Forms.ToolStripButton();
			this.AddLocationButton = new System.Windows.Forms.ToolStripButton();
			this.LocationFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.ConfigurationGroupBox = new System.Windows.Forms.GroupBox();
			this.ConfigurationVersionLabel = new System.Windows.Forms.Label();
			this.ConfigurationVersionTextBox = new System.Windows.Forms.TextBox();
			this.SaveSettingsButton = new System.Windows.Forms.Button();
			this.OpenSettingsFolderButton = new System.Windows.Forms.Button();
			this.DirectInputDevicesGroupBox = new System.Windows.Forms.GroupBox();
			this.ExcludeVirtualDevicesCheckBox = new System.Windows.Forms.CheckBox();
			this.ExcludeSupplementalDevicesCheckBox = new System.Windows.Forms.CheckBox();
			this.MinimizeToTrayCheckBox = new System.Windows.Forms.CheckBox();
			this.TestingAndLoggingGroupBox.SuspendLayout();
			this.OperationGroupBox.SuspendLayout();
			this.InternetGroupBox.SuspendLayout();
			this.ProgramScanLocationsTabControl.SuspendLayout();
			this.GameScanLocationsTabPage.SuspendLayout();
			this.LocationsToolStrip.SuspendLayout();
			this.ConfigurationGroupBox.SuspendLayout();
			this.DirectInputDevicesGroupBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// TestingAndLoggingGroupBox
			// 
			this.TestingAndLoggingGroupBox.Controls.Add(this.CombineEnabledCheckBox);
			this.TestingAndLoggingGroupBox.Controls.Add(this.XInputEnableCheckBox);
			this.TestingAndLoggingGroupBox.Controls.Add(this.UseInitBeepCheckBox);
			this.TestingAndLoggingGroupBox.Controls.Add(this.ConsoleCheckBox);
			this.TestingAndLoggingGroupBox.Controls.Add(this.DebugModeCheckBox);
			this.TestingAndLoggingGroupBox.Controls.Add(this.EnableLoggingCheckBox);
			this.TestingAndLoggingGroupBox.Location = new System.Drawing.Point(3, 86);
			this.TestingAndLoggingGroupBox.Name = "TestingAndLoggingGroupBox";
			this.TestingAndLoggingGroupBox.Size = new System.Drawing.Size(241, 136);
			this.TestingAndLoggingGroupBox.TabIndex = 30;
			this.TestingAndLoggingGroupBox.TabStop = false;
			this.TestingAndLoggingGroupBox.Text = "Testing and Logging";
			// 
			// CombineEnabledCheckBox
			// 
			this.CombineEnabledCheckBox.AutoSize = true;
			this.CombineEnabledCheckBox.Location = new System.Drawing.Point(124, 21);
			this.CombineEnabledCheckBox.Name = "CombineEnabledCheckBox";
			this.CombineEnabledCheckBox.Size = new System.Drawing.Size(111, 17);
			this.CombineEnabledCheckBox.TabIndex = 1;
			this.CombineEnabledCheckBox.Text = "Enable Combining";
			this.CombineEnabledCheckBox.UseVisualStyleBackColor = true;
			// 
			// XInputEnableCheckBox
			// 
			this.XInputEnableCheckBox.AutoSize = true;
			this.XInputEnableCheckBox.Checked = true;
			this.XInputEnableCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.XInputEnableCheckBox.Location = new System.Drawing.Point(7, 21);
			this.XInputEnableCheckBox.Name = "XInputEnableCheckBox";
			this.XInputEnableCheckBox.Size = new System.Drawing.Size(93, 17);
			this.XInputEnableCheckBox.TabIndex = 0;
			this.XInputEnableCheckBox.Text = "Enable XInput";
			this.XInputEnableCheckBox.UseVisualStyleBackColor = true;
			// 
			// UseInitBeepCheckBox
			// 
			this.UseInitBeepCheckBox.AutoSize = true;
			this.UseInitBeepCheckBox.Location = new System.Drawing.Point(6, 44);
			this.UseInitBeepCheckBox.Name = "UseInitBeepCheckBox";
			this.UseInitBeepCheckBox.Size = new System.Drawing.Size(90, 17);
			this.UseInitBeepCheckBox.TabIndex = 0;
			this.UseInitBeepCheckBox.Text = "Use Init Beep";
			this.UseInitBeepCheckBox.UseVisualStyleBackColor = true;
			// 
			// ConsoleCheckBox
			// 
			this.ConsoleCheckBox.AutoSize = true;
			this.ConsoleCheckBox.Location = new System.Drawing.Point(6, 90);
			this.ConsoleCheckBox.Name = "ConsoleCheckBox";
			this.ConsoleCheckBox.Size = new System.Drawing.Size(100, 17);
			this.ConsoleCheckBox.TabIndex = 0;
			this.ConsoleCheckBox.Text = "Enable Console";
			this.ConsoleCheckBox.UseVisualStyleBackColor = true;
			// 
			// DebugModeCheckBox
			// 
			this.DebugModeCheckBox.AutoSize = true;
			this.DebugModeCheckBox.Checked = true;
			this.DebugModeCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.DebugModeCheckBox.Location = new System.Drawing.Point(6, 113);
			this.DebugModeCheckBox.Name = "DebugModeCheckBox";
			this.DebugModeCheckBox.Size = new System.Drawing.Size(88, 17);
			this.DebugModeCheckBox.TabIndex = 0;
			this.DebugModeCheckBox.Text = "Debug Mode";
			this.DebugModeCheckBox.UseVisualStyleBackColor = true;
			// 
			// EnableLoggingCheckBox
			// 
			this.EnableLoggingCheckBox.AutoSize = true;
			this.EnableLoggingCheckBox.Location = new System.Drawing.Point(6, 67);
			this.EnableLoggingCheckBox.Name = "EnableLoggingCheckBox";
			this.EnableLoggingCheckBox.Size = new System.Drawing.Size(100, 17);
			this.EnableLoggingCheckBox.TabIndex = 0;
			this.EnableLoggingCheckBox.Text = "Enable Logging";
			this.EnableLoggingCheckBox.UseVisualStyleBackColor = true;
			// 
			// OperationGroupBox
			// 
			this.OperationGroupBox.Controls.Add(this.MinimizeToTrayCheckBox);
			this.OperationGroupBox.Controls.Add(this.AllowOnlyOneCopyCheckBox);
			this.OperationGroupBox.Location = new System.Drawing.Point(3, 3);
			this.OperationGroupBox.Name = "OperationGroupBox";
			this.OperationGroupBox.Size = new System.Drawing.Size(241, 77);
			this.OperationGroupBox.TabIndex = 31;
			this.OperationGroupBox.TabStop = false;
			this.OperationGroupBox.Text = "Operation";
			// 
			// AllowOnlyOneCopyCheckBox
			// 
			this.AllowOnlyOneCopyCheckBox.AutoSize = true;
			this.AllowOnlyOneCopyCheckBox.Location = new System.Drawing.Point(6, 29);
			this.AllowOnlyOneCopyCheckBox.Name = "AllowOnlyOneCopyCheckBox";
			this.AllowOnlyOneCopyCheckBox.Size = new System.Drawing.Size(230, 17);
			this.AllowOnlyOneCopyCheckBox.TabIndex = 1;
			this.AllowOnlyOneCopyCheckBox.Text = "Allow only one copy of Application at a time";
			this.AllowOnlyOneCopyCheckBox.UseVisualStyleBackColor = true;
			// 
			// InternetGroupBox
			// 
			this.InternetGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.InternetGroupBox.Controls.Add(this.InternetDatabaseUrlComboBox);
			this.InternetGroupBox.Controls.Add(this.label1);
			this.InternetGroupBox.Controls.Add(this.InternetAutoloadCheckBox);
			this.InternetGroupBox.Controls.Add(this.InternetCheckBox);
			this.InternetGroupBox.Location = new System.Drawing.Point(250, 3);
			this.InternetGroupBox.Name = "InternetGroupBox";
			this.InternetGroupBox.Size = new System.Drawing.Size(390, 109);
			this.InternetGroupBox.TabIndex = 32;
			this.InternetGroupBox.TabStop = false;
			this.InternetGroupBox.Text = "Internet";
			// 
			// InternetDatabaseUrlComboBox
			// 
			this.InternetDatabaseUrlComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.InternetDatabaseUrlComboBox.FormattingEnabled = true;
			this.InternetDatabaseUrlComboBox.Items.AddRange(new object[] {
            "http://www.x360ce.com/webservices/x360ce.asmx",
            "http://localhost:20360/webservices/x360ce.asmx"});
			this.InternetDatabaseUrlComboBox.Location = new System.Drawing.Point(6, 80);
			this.InternetDatabaseUrlComboBox.Name = "InternetDatabaseUrlComboBox";
			this.InternetDatabaseUrlComboBox.Size = new System.Drawing.Size(378, 21);
			this.InternetDatabaseUrlComboBox.TabIndex = 14;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 64);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(97, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Web Service URL:";
			// 
			// InternetAutoloadCheckBox
			// 
			this.InternetAutoloadCheckBox.AutoSize = true;
			this.InternetAutoloadCheckBox.Location = new System.Drawing.Point(6, 44);
			this.InternetAutoloadCheckBox.Name = "InternetAutoloadCheckBox";
			this.InternetAutoloadCheckBox.Size = new System.Drawing.Size(215, 17);
			this.InternetAutoloadCheckBox.TabIndex = 1;
			this.InternetAutoloadCheckBox.Text = "Auto Load Settings When Tab Selected";
			this.InternetAutoloadCheckBox.UseVisualStyleBackColor = true;
			// 
			// InternetCheckBox
			// 
			this.InternetCheckBox.AutoSize = true;
			this.InternetCheckBox.Location = new System.Drawing.Point(6, 21);
			this.InternetCheckBox.Name = "InternetCheckBox";
			this.InternetCheckBox.Size = new System.Drawing.Size(232, 17);
			this.InternetCheckBox.TabIndex = 1;
			this.InternetCheckBox.Text = "Enable Internet Settings Database Features";
			this.InternetCheckBox.UseVisualStyleBackColor = true;
			// 
			// ProgramScanLocationsTabControl
			// 
			this.ProgramScanLocationsTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ProgramScanLocationsTabControl.Controls.Add(this.GameScanLocationsTabPage);
			this.ProgramScanLocationsTabControl.ItemSize = new System.Drawing.Size(116, 24);
			this.ProgramScanLocationsTabControl.Location = new System.Drawing.Point(250, 118);
			this.ProgramScanLocationsTabControl.Name = "ProgramScanLocationsTabControl";
			this.ProgramScanLocationsTabControl.SelectedIndex = 0;
			this.ProgramScanLocationsTabControl.Size = new System.Drawing.Size(391, 136);
			this.ProgramScanLocationsTabControl.TabIndex = 34;
			// 
			// GameScanLocationsTabPage
			// 
			this.GameScanLocationsTabPage.Controls.Add(this.GameScanLocationsListBox);
			this.GameScanLocationsTabPage.Controls.Add(this.LocationsToolStrip);
			this.GameScanLocationsTabPage.Location = new System.Drawing.Point(4, 28);
			this.GameScanLocationsTabPage.Name = "GameScanLocationsTabPage";
			this.GameScanLocationsTabPage.Size = new System.Drawing.Size(383, 104);
			this.GameScanLocationsTabPage.TabIndex = 0;
			this.GameScanLocationsTabPage.Text = "Game Scan Locations";
			this.GameScanLocationsTabPage.UseVisualStyleBackColor = true;
			// 
			// GameScanLocationsListBox
			// 
			this.GameScanLocationsListBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.GameScanLocationsListBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.GameScanLocationsListBox.FormattingEnabled = true;
			this.GameScanLocationsListBox.Location = new System.Drawing.Point(0, 25);
			this.GameScanLocationsListBox.Name = "GameScanLocationsListBox";
			this.GameScanLocationsListBox.Size = new System.Drawing.Size(383, 79);
			this.GameScanLocationsListBox.Sorted = true;
			this.GameScanLocationsListBox.TabIndex = 0;
			this.GameScanLocationsListBox.SelectedIndexChanged += new System.EventHandler(this.ProgramScanLocationsListBox_SelectedIndexChanged);
			// 
			// LocationsToolStrip
			// 
			this.LocationsToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.LocationsToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.RefreshLocationsButton,
            this.RemoveLocationButton,
            this.AddLocationButton});
			this.LocationsToolStrip.Location = new System.Drawing.Point(0, 0);
			this.LocationsToolStrip.Name = "LocationsToolStrip";
			this.LocationsToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.LocationsToolStrip.Size = new System.Drawing.Size(383, 25);
			this.LocationsToolStrip.TabIndex = 2;
			this.LocationsToolStrip.Text = "MySettingsToolStrip";
			// 
			// RefreshLocationsButton
			// 
			this.RefreshLocationsButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.RefreshLocationsButton.Image = global::x360ce.App.Properties.Resources.refresh_16x16;
			this.RefreshLocationsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RefreshLocationsButton.Name = "RefreshLocationsButton";
			this.RefreshLocationsButton.Size = new System.Drawing.Size(66, 22);
			this.RefreshLocationsButton.Text = "Refresh";
			this.RefreshLocationsButton.Click += new System.EventHandler(this.RefreshLocationsButton_Click);
			// 
			// RemoveLocationButton
			// 
			this.RemoveLocationButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.RemoveLocationButton.Image = global::x360ce.App.Properties.Resources.remove_16x16;
			this.RemoveLocationButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RemoveLocationButton.Name = "RemoveLocationButton";
			this.RemoveLocationButton.Size = new System.Drawing.Size(70, 22);
			this.RemoveLocationButton.Text = "Remove";
			this.RemoveLocationButton.Click += new System.EventHandler(this.RemoveLocationButton_Click);
			// 
			// AddLocationButton
			// 
			this.AddLocationButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.AddLocationButton.Image = global::x360ce.App.Properties.Resources.add_16x16;
			this.AddLocationButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.AddLocationButton.Name = "AddLocationButton";
			this.AddLocationButton.Size = new System.Drawing.Size(58, 22);
			this.AddLocationButton.Text = "&Add...";
			this.AddLocationButton.Click += new System.EventHandler(this.AddLocationButton_Click);
			// 
			// ConfigurationGroupBox
			// 
			this.ConfigurationGroupBox.Controls.Add(this.ConfigurationVersionLabel);
			this.ConfigurationGroupBox.Controls.Add(this.ConfigurationVersionTextBox);
			this.ConfigurationGroupBox.Location = new System.Drawing.Point(3, 319);
			this.ConfigurationGroupBox.Name = "ConfigurationGroupBox";
			this.ConfigurationGroupBox.Size = new System.Drawing.Size(241, 47);
			this.ConfigurationGroupBox.TabIndex = 31;
			this.ConfigurationGroupBox.TabStop = false;
			this.ConfigurationGroupBox.Text = "Configuration";
			// 
			// ConfigurationVersionLabel
			// 
			this.ConfigurationVersionLabel.AutoSize = true;
			this.ConfigurationVersionLabel.Location = new System.Drawing.Point(4, 22);
			this.ConfigurationVersionLabel.Name = "ConfigurationVersionLabel";
			this.ConfigurationVersionLabel.Size = new System.Drawing.Size(45, 13);
			this.ConfigurationVersionLabel.TabIndex = 0;
			this.ConfigurationVersionLabel.Text = "Version:";
			// 
			// ConfigurationVersionTextBox
			// 
			this.ConfigurationVersionTextBox.Enabled = false;
			this.ConfigurationVersionTextBox.Location = new System.Drawing.Point(76, 19);
			this.ConfigurationVersionTextBox.Name = "ConfigurationVersionTextBox";
			this.ConfigurationVersionTextBox.Size = new System.Drawing.Size(51, 20);
			this.ConfigurationVersionTextBox.TabIndex = 0;
			// 
			// SaveSettingsButton
			// 
			this.SaveSettingsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.SaveSettingsButton.Location = new System.Drawing.Point(566, 354);
			this.SaveSettingsButton.Name = "SaveSettingsButton";
			this.SaveSettingsButton.Size = new System.Drawing.Size(75, 23);
			this.SaveSettingsButton.TabIndex = 68;
			this.SaveSettingsButton.Text = "&Save";
			this.SaveSettingsButton.UseVisualStyleBackColor = true;
			this.SaveSettingsButton.Click += new System.EventHandler(this.SaveSettingsButton_Click);
			// 
			// OpenSettingsFolderButton
			// 
			this.OpenSettingsFolderButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OpenSettingsFolderButton.Image = global::x360ce.App.Properties.Resources.folder_16x16;
			this.OpenSettingsFolderButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.OpenSettingsFolderButton.Location = new System.Drawing.Point(397, 354);
			this.OpenSettingsFolderButton.Name = "OpenSettingsFolderButton";
			this.OpenSettingsFolderButton.Size = new System.Drawing.Size(163, 23);
			this.OpenSettingsFolderButton.TabIndex = 69;
			this.OpenSettingsFolderButton.Text = "Open Settings Folder...";
			this.OpenSettingsFolderButton.UseVisualStyleBackColor = true;
			this.OpenSettingsFolderButton.Click += new System.EventHandler(this.OpenSettingsFolderButton_Click);
			// 
			// DirectInputDevicesGroupBox
			// 
			this.DirectInputDevicesGroupBox.Controls.Add(this.ExcludeVirtualDevicesCheckBox);
			this.DirectInputDevicesGroupBox.Controls.Add(this.ExcludeSupplementalDevicesCheckBox);
			this.DirectInputDevicesGroupBox.Location = new System.Drawing.Point(3, 228);
			this.DirectInputDevicesGroupBox.Name = "DirectInputDevicesGroupBox";
			this.DirectInputDevicesGroupBox.Size = new System.Drawing.Size(241, 69);
			this.DirectInputDevicesGroupBox.TabIndex = 31;
			this.DirectInputDevicesGroupBox.TabStop = false;
			this.DirectInputDevicesGroupBox.Text = "Direct Input Devices";
			// 
			// ExcludeVirtualDevicesCheckBox
			// 
			this.ExcludeVirtualDevicesCheckBox.AutoSize = true;
			this.ExcludeVirtualDevicesCheckBox.Location = new System.Drawing.Point(6, 42);
			this.ExcludeVirtualDevicesCheckBox.Name = "ExcludeVirtualDevicesCheckBox";
			this.ExcludeVirtualDevicesCheckBox.Size = new System.Drawing.Size(138, 17);
			this.ExcludeVirtualDevicesCheckBox.TabIndex = 0;
			this.ExcludeVirtualDevicesCheckBox.Text = "Exclude Virtual Devices";
			this.ExcludeVirtualDevicesCheckBox.UseVisualStyleBackColor = true;
			// 
			// ExcludeSupplementalDevicesCheckBox
			// 
			this.ExcludeSupplementalDevicesCheckBox.AutoSize = true;
			this.ExcludeSupplementalDevicesCheckBox.Location = new System.Drawing.Point(6, 19);
			this.ExcludeSupplementalDevicesCheckBox.Name = "ExcludeSupplementalDevicesCheckBox";
			this.ExcludeSupplementalDevicesCheckBox.Size = new System.Drawing.Size(173, 17);
			this.ExcludeSupplementalDevicesCheckBox.TabIndex = 0;
			this.ExcludeSupplementalDevicesCheckBox.Text = "Exclude Supplemental Devices";
			this.ExcludeSupplementalDevicesCheckBox.UseVisualStyleBackColor = true;
			// 
			// MinimizeToTrayCheckBox
			// 
			this.MinimizeToTrayCheckBox.AutoSize = true;
			this.MinimizeToTrayCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.MinimizeToTrayCheckBox.Location = new System.Drawing.Point(6, 52);
			this.MinimizeToTrayCheckBox.Name = "MinimizeToTrayCheckBox";
			this.MinimizeToTrayCheckBox.Size = new System.Drawing.Size(108, 18);
			this.MinimizeToTrayCheckBox.TabIndex = 93;
			this.MinimizeToTrayCheckBox.Text = "Minimize to Tray";
			this.MinimizeToTrayCheckBox.CheckedChanged += new System.EventHandler(this.MinimizeToTrayCheckBox_CheckedChanged);
			// 
			// OptionsControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.OpenSettingsFolderButton);
			this.Controls.Add(this.SaveSettingsButton);
			this.Controls.Add(this.TestingAndLoggingGroupBox);
			this.Controls.Add(this.DirectInputDevicesGroupBox);
			this.Controls.Add(this.ConfigurationGroupBox);
			this.Controls.Add(this.OperationGroupBox);
			this.Controls.Add(this.InternetGroupBox);
			this.Controls.Add(this.ProgramScanLocationsTabControl);
			this.Name = "OptionsControl";
			this.Size = new System.Drawing.Size(644, 380);
			this.TestingAndLoggingGroupBox.ResumeLayout(false);
			this.TestingAndLoggingGroupBox.PerformLayout();
			this.OperationGroupBox.ResumeLayout(false);
			this.OperationGroupBox.PerformLayout();
			this.InternetGroupBox.ResumeLayout(false);
			this.InternetGroupBox.PerformLayout();
			this.ProgramScanLocationsTabControl.ResumeLayout(false);
			this.GameScanLocationsTabPage.ResumeLayout(false);
			this.GameScanLocationsTabPage.PerformLayout();
			this.LocationsToolStrip.ResumeLayout(false);
			this.LocationsToolStrip.PerformLayout();
			this.ConfigurationGroupBox.ResumeLayout(false);
			this.ConfigurationGroupBox.PerformLayout();
			this.DirectInputDevicesGroupBox.ResumeLayout(false);
			this.DirectInputDevicesGroupBox.PerformLayout();
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox TestingAndLoggingGroupBox;
        private System.Windows.Forms.CheckBox XInputEnableCheckBox;
        private System.Windows.Forms.CheckBox UseInitBeepCheckBox;
        private System.Windows.Forms.CheckBox ConsoleCheckBox;
        private System.Windows.Forms.CheckBox DebugModeCheckBox;
        private System.Windows.Forms.CheckBox EnableLoggingCheckBox;
        private System.Windows.Forms.GroupBox OperationGroupBox;
        public System.Windows.Forms.CheckBox AllowOnlyOneCopyCheckBox;
        private System.Windows.Forms.GroupBox InternetGroupBox;
        private System.Windows.Forms.Label label1;
        public System.Windows.Forms.CheckBox InternetAutoloadCheckBox;
		public System.Windows.Forms.CheckBox InternetCheckBox;
        private System.Windows.Forms.TabControl ProgramScanLocationsTabControl;
		private System.Windows.Forms.TabPage GameScanLocationsTabPage;
		private System.Windows.Forms.FolderBrowserDialog LocationFolderBrowserDialog;
        public System.Windows.Forms.ListBox GameScanLocationsListBox;
        public System.Windows.Forms.ComboBox InternetDatabaseUrlComboBox;
        private System.Windows.Forms.GroupBox ConfigurationGroupBox;
        private System.Windows.Forms.Label ConfigurationVersionLabel;
        private System.Windows.Forms.TextBox ConfigurationVersionTextBox;
        private System.Windows.Forms.ToolStrip LocationsToolStrip;
        private System.Windows.Forms.ToolStripButton RefreshLocationsButton;
        private System.Windows.Forms.ToolStripButton AddLocationButton;
        private System.Windows.Forms.ToolStripButton RemoveLocationButton;
		private System.Windows.Forms.Button SaveSettingsButton;
		private System.Windows.Forms.Button OpenSettingsFolderButton;
        private System.Windows.Forms.CheckBox CombineEnabledCheckBox;
		private System.Windows.Forms.GroupBox DirectInputDevicesGroupBox;
		private System.Windows.Forms.CheckBox ExcludeSupplementalDevicesCheckBox;
		private System.Windows.Forms.CheckBox ExcludeVirtualDevicesCheckBox;
		internal System.Windows.Forms.CheckBox MinimizeToTrayCheckBox;
	}
}
