namespace Nucleus.Coop
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.settingsBtn = new System.Windows.Forms.Button();
            this.gameContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.nullToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.scriptNotesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.detailsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openScriptToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openDataFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openOrigExePathToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.changeIconToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.btn_gameOptions = new System.Windows.Forms.Button();
            this.sideInfoLbl = new System.Windows.Forms.Label();
            this.gameNameControl = new Nucleus.Coop.Controls.GameNameControl();
            this.btn_Next = new System.Windows.Forms.Button();
            this.btnAutoSearch = new System.Windows.Forms.Button();
            this.btnBack = new System.Windows.Forms.Button();
            this.btnSearch = new System.Windows.Forms.Button();
            this.btn_Play = new System.Windows.Forms.Button();
            this.label_StepTitle = new System.Windows.Forms.Label();
            this.list_Games = new Nucleus.Gaming.ControlListBox();
            this.StepPanel = new System.Windows.Forms.Panel();
            this.scriptAuthorLbl = new System.Windows.Forms.Label();
            this.scriptAuthorTxt = new System.Windows.Forms.TextBox();
            this.lblVersion = new System.Windows.Forms.Label();
            this.gameContextMenuStrip.SuspendLayout();
            this.StepPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // settingsBtn
            // 
            this.settingsBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.settingsBtn.BackColor = System.Drawing.Color.Transparent;
            this.settingsBtn.BackgroundImage = global::Nucleus.Coop.Properties.Resources.setting1;
            this.settingsBtn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.settingsBtn.ForeColor = System.Drawing.Color.Transparent;
            this.settingsBtn.Location = new System.Drawing.Point(1006, 12);
            this.settingsBtn.Name = "settingsBtn";
            this.settingsBtn.Size = new System.Drawing.Size(40, 40);
            this.settingsBtn.TabIndex = 16;
            this.toolTip1.SetToolTip(this.settingsBtn, "Settings");
            this.settingsBtn.UseVisualStyleBackColor = false;
            this.settingsBtn.Click += new System.EventHandler(this.SettingsBtn_Click);
            // 
            // gameContextMenuStrip
            // 
            this.gameContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.nullToolStripMenuItem,
            this.scriptNotesToolStripMenuItem,
            this.toolStripMenuItem1,
            this.detailsToolStripMenuItem,
            this.openScriptToolStripMenuItem,
            this.openDataFolderToolStripMenuItem,
            this.openOrigExePathToolStripMenuItem,
            this.toolStripMenuItem2,
            this.changeIconToolStripMenuItem,
            this.deleteToolStripMenuItem});
            this.gameContextMenuStrip.Name = "gameContextMenuStrip";
            this.gameContextMenuStrip.Size = new System.Drawing.Size(186, 192);
            this.gameContextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.GameContextMenuStrip_Opening);
            // 
            // nullToolStripMenuItem
            // 
            this.nullToolStripMenuItem.Name = "nullToolStripMenuItem";
            this.nullToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.nullToolStripMenuItem.Text = "null";
            // 
            // scriptNotesToolStripMenuItem
            // 
            this.scriptNotesToolStripMenuItem.Name = "scriptNotesToolStripMenuItem";
            this.scriptNotesToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.scriptNotesToolStripMenuItem.Text = "Script Author Notes";
            this.scriptNotesToolStripMenuItem.Visible = false;
            this.scriptNotesToolStripMenuItem.Click += new System.EventHandler(this.ScriptNotesToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(182, 6);
            // 
            // detailsToolStripMenuItem
            // 
            this.detailsToolStripMenuItem.Name = "detailsToolStripMenuItem";
            this.detailsToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.detailsToolStripMenuItem.Text = "Details";
            this.detailsToolStripMenuItem.Click += new System.EventHandler(this.DetailsToolStripMenuItem_Click);
            // 
            // openScriptToolStripMenuItem
            // 
            this.openScriptToolStripMenuItem.Name = "openScriptToolStripMenuItem";
            this.openScriptToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.openScriptToolStripMenuItem.Text = "Open Script";
            this.openScriptToolStripMenuItem.Click += new System.EventHandler(this.OpenScriptToolStripMenuItem_Click);
            // 
            // openDataFolderToolStripMenuItem
            // 
            this.openDataFolderToolStripMenuItem.Name = "openDataFolderToolStripMenuItem";
            this.openDataFolderToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.openDataFolderToolStripMenuItem.Text = "Open Content Folder";
            this.openDataFolderToolStripMenuItem.Click += new System.EventHandler(this.OpenDataFolderToolStripMenuItem_Click);
            // 
            // openOrigExePathToolStripMenuItem
            // 
            this.openOrigExePathToolStripMenuItem.Name = "openOrigExePathToolStripMenuItem";
            this.openOrigExePathToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.openOrigExePathToolStripMenuItem.Text = "Open Orig Exe Path";
            this.openOrigExePathToolStripMenuItem.Click += new System.EventHandler(this.OpenOrigExePathToolStripMenuItem_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(182, 6);
            // 
            // changeIconToolStripMenuItem
            // 
            this.changeIconToolStripMenuItem.Name = "changeIconToolStripMenuItem";
            this.changeIconToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.changeIconToolStripMenuItem.Text = "Change Icon";
            this.changeIconToolStripMenuItem.Click += new System.EventHandler(this.ChangeIconToolStripMenuItem_Click);
            // 
            // deleteToolStripMenuItem
            // 
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.deleteToolStripMenuItem.Text = "Delete";
            this.deleteToolStripMenuItem.Click += new System.EventHandler(this.DeleteToolStripMenuItem_Click);
            // 
            // btn_gameOptions
            // 
            this.btn_gameOptions.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_gameOptions.ForeColor = System.Drawing.Color.Black;
            this.btn_gameOptions.Location = new System.Drawing.Point(384, 13);
            this.btn_gameOptions.Name = "btn_gameOptions";
            this.btn_gameOptions.Size = new System.Drawing.Size(92, 45);
            this.btn_gameOptions.TabIndex = 21;
            this.btn_gameOptions.Text = "Game\r\nOptions";
            this.btn_gameOptions.UseVisualStyleBackColor = true;
            this.btn_gameOptions.Visible = false;
            this.btn_gameOptions.Click += new System.EventHandler(this.Button1_Click);
            // 
            // sideInfoLbl
            // 
            this.sideInfoLbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.sideInfoLbl.AutoSize = true;
            this.sideInfoLbl.Location = new System.Drawing.Point(845, 10);
            this.sideInfoLbl.Name = "sideInfoLbl";
            this.sideInfoLbl.Size = new System.Drawing.Size(0, 21);
            this.sideInfoLbl.TabIndex = 14;
            this.sideInfoLbl.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // gameNameControl
            // 
            this.gameNameControl.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.gameNameControl.GameInfo = null;
            this.gameNameControl.Location = new System.Drawing.Point(280, 12);
            this.gameNameControl.Name = "gameNameControl";
            this.gameNameControl.Size = new System.Drawing.Size(98, 46);
            this.gameNameControl.TabIndex = 13;
            // 
            // btn_Next
            // 
            this.btn_Next.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_Next.Enabled = false;
            this.btn_Next.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.btn_Next.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Next.Location = new System.Drawing.Point(910, 60);
            this.btn_Next.Name = "btn_Next";
            this.btn_Next.Size = new System.Drawing.Size(33, 35);
            this.btn_Next.TabIndex = 11;
            this.btn_Next.Text = ">";
            this.btn_Next.TextAlign = System.Drawing.ContentAlignment.TopRight;
            this.btn_Next.UseVisualStyleBackColor = true;
            this.btn_Next.Click += new System.EventHandler(this.btnNext_Click);
            // 
            // btnAutoSearch
            // 
            this.btnAutoSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAutoSearch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAutoSearch.Location = new System.Drawing.Point(146, 654);
            this.btnAutoSearch.Name = "btnAutoSearch";
            this.btnAutoSearch.Size = new System.Drawing.Size(128, 35);
            this.btnAutoSearch.TabIndex = 10;
            this.btnAutoSearch.Text = "Auto Search";
            this.btnAutoSearch.UseVisualStyleBackColor = true;
            this.btnAutoSearch.Click += new System.EventHandler(this.btnAutoSearch_Click);
            // 
            // btnBack
            // 
            this.btnBack.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBack.Enabled = false;
            this.btnBack.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBack.Location = new System.Drawing.Point(871, 60);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(33, 35);
            this.btnBack.TabIndex = 9;
            this.btnBack.Text = "<";
            this.btnBack.TextAlign = System.Drawing.ContentAlignment.TopRight;
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Click += new System.EventHandler(this.arrow_Back_Click);
            // 
            // btnSearch
            // 
            this.btnSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnSearch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSearch.Location = new System.Drawing.Point(12, 654);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(128, 35);
            this.btnSearch.TabIndex = 7;
            this.btnSearch.Text = "Search Game";
            this.btnSearch.UseVisualStyleBackColor = true;
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            // 
            // btn_Play
            // 
            this.btn_Play.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_Play.Enabled = false;
            this.btn_Play.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.btn_Play.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Play.Location = new System.Drawing.Point(949, 60);
            this.btn_Play.Name = "btn_Play";
            this.btn_Play.Size = new System.Drawing.Size(97, 35);
            this.btn_Play.TabIndex = 4;
            this.btn_Play.Text = "P L A Y";
            this.btn_Play.UseVisualStyleBackColor = true;
            this.btn_Play.Click += new System.EventHandler(this.btn_Play_Click);
            // 
            // label_StepTitle
            // 
            this.label_StepTitle.AutoSize = true;
            this.label_StepTitle.Location = new System.Drawing.Point(276, 70);
            this.label_StepTitle.Name = "label_StepTitle";
            this.label_StepTitle.Size = new System.Drawing.Size(127, 21);
            this.label_StepTitle.TabIndex = 3;
            this.label_StepTitle.Text = "Nothing selected";
            // 
            // list_Games
            // 
            this.list_Games.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.list_Games.AutoScroll = true;
            this.list_Games.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.list_Games.Border = 1;
            this.list_Games.ContextMenuStrip = this.gameContextMenuStrip;
            this.list_Games.Location = new System.Drawing.Point(12, 12);
            this.list_Games.Name = "list_Games";
            this.list_Games.Offset = new System.Drawing.Size(0, 2);
            this.list_Games.Size = new System.Drawing.Size(262, 636);
            this.list_Games.TabIndex = 2;
            this.list_Games.SelectedChanged += new System.Action<object, System.Windows.Forms.Control>(this.list_Games_SelectedChanged);
            // 
            // StepPanel
            // 
            this.StepPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.StepPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.StepPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.StepPanel.Controls.Add(this.scriptAuthorLbl);
            this.StepPanel.Controls.Add(this.scriptAuthorTxt);
            this.StepPanel.Controls.Add(this.lblVersion);
            this.StepPanel.Location = new System.Drawing.Point(280, 101);
            this.StepPanel.Name = "StepPanel";
            this.StepPanel.Size = new System.Drawing.Size(766, 588);
            this.StepPanel.TabIndex = 0;
            // 
            // scriptAuthorLbl
            // 
            this.scriptAuthorLbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.scriptAuthorLbl.AutoSize = true;
            this.scriptAuthorLbl.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.scriptAuthorLbl.Location = new System.Drawing.Point(12, 478);
            this.scriptAuthorLbl.Name = "scriptAuthorLbl";
            this.scriptAuthorLbl.Size = new System.Drawing.Size(151, 20);
            this.scriptAuthorLbl.TabIndex = 14;
            this.scriptAuthorLbl.Text = "Script Author\'s Notes:";
            this.scriptAuthorLbl.Visible = false;
            // 
            // scriptAuthorTxt
            // 
            this.scriptAuthorTxt.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.scriptAuthorTxt.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.scriptAuthorTxt.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.scriptAuthorTxt.ForeColor = System.Drawing.Color.White;
            this.scriptAuthorTxt.Location = new System.Drawing.Point(16, 502);
            this.scriptAuthorTxt.Multiline = true;
            this.scriptAuthorTxt.Name = "scriptAuthorTxt";
            this.scriptAuthorTxt.ReadOnly = true;
            this.scriptAuthorTxt.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.scriptAuthorTxt.Size = new System.Drawing.Size(733, 79);
            this.scriptAuthorTxt.TabIndex = 13;
            this.scriptAuthorTxt.Visible = false;
            // 
            // lblVersion
            // 
            this.lblVersion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblVersion.AutoSize = true;
            this.lblVersion.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblVersion.Location = new System.Drawing.Point(695, 567);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(71, 21);
            this.lblVersion.TabIndex = 12;
            this.lblVersion.Text = "ALPHA 8";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1058, 701);
            this.Controls.Add(this.btn_gameOptions);
            this.Controls.Add(this.settingsBtn);
            this.Controls.Add(this.sideInfoLbl);
            this.Controls.Add(this.gameNameControl);
            this.Controls.Add(this.btn_Next);
            this.Controls.Add(this.btnAutoSearch);
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.btnSearch);
            this.Controls.Add(this.btn_Play);
            this.Controls.Add(this.label_StepTitle);
            this.Controls.Add(this.list_Games);
            this.Controls.Add(this.StepPanel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(940, 460);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Nucleus Coop (Alpha 8 Mod)";
            this.gameContextMenuStrip.ResumeLayout(false);
            this.StepPanel.ResumeLayout(false);
            this.StepPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel StepPanel;
        private Gaming.ControlListBox list_Games;
        private System.Windows.Forms.Label label_StepTitle;
        private System.Windows.Forms.Button btn_Play;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.Button btnAutoSearch;
        private System.Windows.Forms.Button btn_Next;
        private System.Windows.Forms.Label lblVersion;
        private Controls.GameNameControl gameNameControl;
        private System.Windows.Forms.Label sideInfoLbl;
        private System.Windows.Forms.Button settingsBtn;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.ContextMenuStrip gameContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem nullToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem detailsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openScriptToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openDataFolderToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem changeIconToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem scriptNotesToolStripMenuItem;
        private System.Windows.Forms.Button btn_gameOptions;
        private System.Windows.Forms.Label scriptAuthorLbl;
        private System.Windows.Forms.TextBox scriptAuthorTxt;
        private System.Windows.Forms.ToolStripMenuItem openOrigExePathToolStripMenuItem;
    }
}