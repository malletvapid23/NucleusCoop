using System.Drawing;

namespace Nucleus.Coop
{
    partial class SearchDisksForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SearchDisksForm));
            this.btn_addSelection = new System.Windows.Forms.Button();
            this.checkboxFoundGames = new System.Windows.Forms.CheckedListBox();
            this.btn_customPath = new System.Windows.Forms.Button();
            this.btnSearch = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.disksBox = new System.Windows.Forms.CheckedListBox();
            this.btn_delPath = new System.Windows.Forms.Button();
            this.btn_selectAll = new System.Windows.Forms.Button();
            this.btn_deselectAll = new System.Windows.Forms.Button();
            this.txt_Stage = new System.Windows.Forms.TextBox();
            this.txt_Path = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // btn_addSelection
            // 
            this.btn_addSelection.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_addSelection.Enabled = false;
            this.btn_addSelection.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_addSelection.Location = new System.Drawing.Point(358, 410);
            this.btn_addSelection.Name = "btn_addSelection";
            this.btn_addSelection.Size = new System.Drawing.Size(334, 43);
            this.btn_addSelection.TabIndex = 11;
            this.btn_addSelection.Text = "Add Selected Games";
            this.btn_addSelection.UseVisualStyleBackColor = true;
            this.btn_addSelection.Click += new System.EventHandler(this.Btn_addSelection_Click);
            // 
            // checkboxFoundGames
            // 
            this.checkboxFoundGames.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.checkboxFoundGames.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.checkboxFoundGames.CheckOnClick = true;
            this.checkboxFoundGames.Enabled = false;
            this.checkboxFoundGames.ForeColor = System.Drawing.Color.White;
            this.checkboxFoundGames.FormattingEnabled = true;
            this.checkboxFoundGames.HorizontalScrollbar = true;
            this.checkboxFoundGames.IntegralHeight = false;
            this.checkboxFoundGames.Location = new System.Drawing.Point(358, 33);
            this.checkboxFoundGames.Name = "checkboxFoundGames";
            this.checkboxFoundGames.Size = new System.Drawing.Size(337, 322);
            this.checkboxFoundGames.TabIndex = 10;
            // 
            // btn_customPath
            // 
            this.btn_customPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btn_customPath.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_customPath.Location = new System.Drawing.Point(12, 361);
            this.btn_customPath.Name = "btn_customPath";
            this.btn_customPath.Size = new System.Drawing.Size(164, 43);
            this.btn_customPath.TabIndex = 9;
            this.btn_customPath.Text = "Add a Path";
            this.btn_customPath.UseVisualStyleBackColor = true;
            this.btn_customPath.Click += new System.EventHandler(this.Btn_customPath_Click);
            // 
            // btnSearch
            // 
            this.btnSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnSearch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSearch.Location = new System.Drawing.Point(12, 410);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(336, 43);
            this.btnSearch.TabIndex = 8;
            this.btnSearch.Text = "Search";
            this.btnSearch.UseVisualStyleBackColor = true;
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar1.Location = new System.Drawing.Point(12, 526);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(680, 23);
            this.progressBar1.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Enabled = false;
            this.label2.Location = new System.Drawing.Point(354, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(106, 21);
            this.label2.TabIndex = 2;
            this.label2.Text = "Found Games";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(116, 21);
            this.label1.TabIndex = 1;
            this.label1.Text = "Paths to Search";
            // 
            // disksBox
            // 
            this.disksBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.disksBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.disksBox.CheckOnClick = true;
            this.disksBox.ForeColor = System.Drawing.Color.White;
            this.disksBox.FormattingEnabled = true;
            this.disksBox.HorizontalScrollbar = true;
            this.disksBox.IntegralHeight = false;
            this.disksBox.Location = new System.Drawing.Point(12, 33);
            this.disksBox.Name = "disksBox";
            this.disksBox.Size = new System.Drawing.Size(336, 322);
            this.disksBox.TabIndex = 0;
            // 
            // btn_delPath
            // 
            this.btn_delPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btn_delPath.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_delPath.Location = new System.Drawing.Point(184, 361);
            this.btn_delPath.Name = "btn_delPath";
            this.btn_delPath.Size = new System.Drawing.Size(164, 43);
            this.btn_delPath.TabIndex = 12;
            this.btn_delPath.Text = "Delete Selected";
            this.btn_delPath.UseVisualStyleBackColor = true;
            this.btn_delPath.Click += new System.EventHandler(this.Btn_delPath_Click);
            // 
            // btn_selectAll
            // 
            this.btn_selectAll.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_selectAll.Enabled = false;
            this.btn_selectAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_selectAll.Location = new System.Drawing.Point(358, 361);
            this.btn_selectAll.Name = "btn_selectAll";
            this.btn_selectAll.Size = new System.Drawing.Size(164, 43);
            this.btn_selectAll.TabIndex = 13;
            this.btn_selectAll.Text = "Select All";
            this.btn_selectAll.UseVisualStyleBackColor = true;
            this.btn_selectAll.Click += new System.EventHandler(this.Btn_selectAll_Click);
            // 
            // btn_deselectAll
            // 
            this.btn_deselectAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_deselectAll.Enabled = false;
            this.btn_deselectAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_deselectAll.Location = new System.Drawing.Point(528, 361);
            this.btn_deselectAll.Name = "btn_deselectAll";
            this.btn_deselectAll.Size = new System.Drawing.Size(164, 43);
            this.btn_deselectAll.TabIndex = 14;
            this.btn_deselectAll.Text = "Deselect All";
            this.btn_deselectAll.UseVisualStyleBackColor = true;
            this.btn_deselectAll.Click += new System.EventHandler(this.Btn_deselectAll_Click);
            // 
            // txt_Stage
            // 
            this.txt_Stage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.txt_Stage.BackColor = System.Drawing.SystemColors.GrayText;
            this.txt_Stage.Enabled = false;
            this.txt_Stage.ForeColor = System.Drawing.SystemColors.Window;
            this.txt_Stage.Location = new System.Drawing.Point(12, 491);
            this.txt_Stage.Name = "txt_Stage";
            this.txt_Stage.Size = new System.Drawing.Size(99, 29);
            this.txt_Stage.TabIndex = 15;
            this.txt_Stage.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txt_Path
            // 
            this.txt_Path.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txt_Path.BackColor = System.Drawing.SystemColors.GrayText;
            this.txt_Path.Enabled = false;
            this.txt_Path.ForeColor = System.Drawing.SystemColors.Window;
            this.txt_Path.Location = new System.Drawing.Point(117, 491);
            this.txt_Path.Name = "txt_Path";
            this.txt_Path.Size = new System.Drawing.Size(575, 29);
            this.txt_Path.TabIndex = 16;
            // 
            // SearchDisksForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(704, 561);
            this.Controls.Add(this.txt_Path);
            this.Controls.Add(this.txt_Stage);
            this.Controls.Add(this.btn_deselectAll);
            this.Controls.Add(this.btn_selectAll);
            this.Controls.Add(this.btn_delPath);
            this.Controls.Add(this.btn_addSelection);
            this.Controls.Add(this.checkboxFoundGames);
            this.Controls.Add(this.btn_customPath);
            this.Controls.Add(this.btnSearch);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.disksBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(720, 600);
            this.Name = "SearchDisksForm";
            this.Text = "Search for Games";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SearchDisksForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckedListBox disksBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.Button btn_customPath;
        private System.Windows.Forms.CheckedListBox checkboxFoundGames;
        private System.Windows.Forms.Button btn_addSelection;
        private System.Windows.Forms.Button btn_delPath;
        private System.Windows.Forms.Button btn_selectAll;
        private System.Windows.Forms.Button btn_deselectAll;
        private System.Windows.Forms.TextBox txt_Stage;
        private System.Windows.Forms.TextBox txt_Path;
    }
}