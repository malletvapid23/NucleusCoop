namespace Nucleus.Coop.Forms
{
    partial class ScriptDownloader
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ScriptDownloader));
            this.btn_Next = new System.Windows.Forms.Button();
            this.btn_Prev = new System.Windows.Forms.Button();
            this.btn_ViewAll = new System.Windows.Forms.Button();
            this.lbl_Status = new System.Windows.Forms.Label();
            this.chkBox_Verified = new System.Windows.Forms.CheckBox();
            this.btn_Info = new System.Windows.Forms.Button();
            this.list_Games = new System.Windows.Forms.ListView();
            this.gameName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ownerName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.verified = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.downloadCount = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.stars = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.createdAt = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.updatedAt = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.description = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btn_Search = new System.Windows.Forms.Button();
            this.btn_Close = new System.Windows.Forms.Button();
            this.btn_Download = new System.Windows.Forms.Button();
            this.txt_Search = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cmb_NumResults = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cmb_Sort = new System.Windows.Forms.ComboBox();
            this.id = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btn_Extract = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btn_Next
            // 
            this.btn_Next.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btn_Next.Enabled = false;
            this.btn_Next.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Next.ForeColor = System.Drawing.Color.White;
            this.btn_Next.Location = new System.Drawing.Point(557, 387);
            this.btn_Next.Name = "btn_Next";
            this.btn_Next.Size = new System.Drawing.Size(184, 35);
            this.btn_Next.TabIndex = 37;
            this.btn_Next.Text = "Next Page";
            this.btn_Next.UseVisualStyleBackColor = true;
            this.btn_Next.Click += new System.EventHandler(this.btn_Next_Click);
            // 
            // btn_Prev
            // 
            this.btn_Prev.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btn_Prev.Enabled = false;
            this.btn_Prev.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Prev.ForeColor = System.Drawing.Color.White;
            this.btn_Prev.Location = new System.Drawing.Point(82, 387);
            this.btn_Prev.Name = "btn_Prev";
            this.btn_Prev.Size = new System.Drawing.Size(184, 35);
            this.btn_Prev.TabIndex = 36;
            this.btn_Prev.Text = "Previous Page";
            this.btn_Prev.UseVisualStyleBackColor = true;
            this.btn_Prev.Click += new System.EventHandler(this.btn_Prev_Click);
            // 
            // btn_ViewAll
            // 
            this.btn_ViewAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_ViewAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_ViewAll.ForeColor = System.Drawing.Color.White;
            this.btn_ViewAll.Location = new System.Drawing.Point(690, 6);
            this.btn_ViewAll.Name = "btn_ViewAll";
            this.btn_ViewAll.Size = new System.Drawing.Size(120, 35);
            this.btn_ViewAll.TabIndex = 35;
            this.btn_ViewAll.Text = "View All";
            this.btn_ViewAll.UseVisualStyleBackColor = true;
            this.btn_ViewAll.Click += new System.EventHandler(this.btn_ViewAll_Click);
            // 
            // lbl_Status
            // 
            this.lbl_Status.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lbl_Status.Location = new System.Drawing.Point(514, 54);
            this.lbl_Status.Name = "lbl_Status";
            this.lbl_Status.Size = new System.Drawing.Size(295, 24);
            this.lbl_Status.TabIndex = 34;
            this.lbl_Status.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // chkBox_Verified
            // 
            this.chkBox_Verified.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkBox_Verified.AutoSize = true;
            this.chkBox_Verified.Location = new System.Drawing.Point(439, 12);
            this.chkBox_Verified.Name = "chkBox_Verified";
            this.chkBox_Verified.Size = new System.Drawing.Size(119, 25);
            this.chkBox_Verified.TabIndex = 33;
            this.chkBox_Verified.Text = "Only Verified";
            this.chkBox_Verified.UseVisualStyleBackColor = true;
            this.chkBox_Verified.Click += new System.EventHandler(this.chkBox_Verified_Click);
            // 
            // btn_Info
            // 
            this.btn_Info.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btn_Info.Enabled = false;
            this.btn_Info.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Info.ForeColor = System.Drawing.Color.White;
            this.btn_Info.Location = new System.Drawing.Point(420, 387);
            this.btn_Info.Name = "btn_Info";
            this.btn_Info.Size = new System.Drawing.Size(120, 35);
            this.btn_Info.TabIndex = 32;
            this.btn_Info.Text = "More Info...";
            this.btn_Info.UseVisualStyleBackColor = true;
            this.btn_Info.Click += new System.EventHandler(this.btn_Info_Click);
            // 
            // list_Games
            // 
            this.list_Games.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.list_Games.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.gameName,
            this.ownerName,
            this.verified,
            this.downloadCount,
            this.stars,
            this.createdAt,
            this.updatedAt,
            this.description,
            this.id});
            this.list_Games.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.list_Games.FullRowSelect = true;
            this.list_Games.HideSelection = false;
            this.list_Games.Location = new System.Drawing.Point(12, 88);
            this.list_Games.MultiSelect = false;
            this.list_Games.Name = "list_Games";
            this.list_Games.Size = new System.Drawing.Size(798, 293);
            this.list_Games.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.list_Games.TabIndex = 31;
            this.list_Games.UseCompatibleStateImageBehavior = false;
            this.list_Games.View = System.Windows.Forms.View.Details;
            this.list_Games.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.list_Games_ColumnClick);
            this.list_Games.SelectedIndexChanged += new System.EventHandler(this.list_Games_SelectedIndexChanged);
            // 
            // gameName
            // 
            this.gameName.Text = "Game";
            this.gameName.Width = 177;
            // 
            // ownerName
            // 
            this.ownerName.Text = "Uploader";
            this.ownerName.Width = 120;
            // 
            // verified
            // 
            this.verified.Text = "Verified";
            this.verified.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.verified.Width = 76;
            // 
            // downloadCount
            // 
            this.downloadCount.Text = "Downloads";
            this.downloadCount.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.downloadCount.Width = 77;
            // 
            // stars
            // 
            this.stars.Text = "Likes";
            this.stars.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // createdAt
            // 
            this.createdAt.Text = "Uploaded";
            this.createdAt.Width = 110;
            // 
            // updatedAt
            // 
            this.updatedAt.Text = "Last Updated";
            this.updatedAt.Width = 138;
            // 
            // description
            // 
            this.description.Text = "Uploader Description";
            // 
            // btn_Search
            // 
            this.btn_Search.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_Search.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Search.ForeColor = System.Drawing.Color.White;
            this.btn_Search.Location = new System.Drawing.Point(564, 6);
            this.btn_Search.Name = "btn_Search";
            this.btn_Search.Size = new System.Drawing.Size(120, 35);
            this.btn_Search.TabIndex = 30;
            this.btn_Search.Text = "Search";
            this.btn_Search.UseVisualStyleBackColor = true;
            this.btn_Search.Click += new System.EventHandler(this.btn_Search_Click);
            // 
            // btn_Close
            // 
            this.btn_Close.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btn_Close.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Close.ForeColor = System.Drawing.Color.White;
            this.btn_Close.Location = new System.Drawing.Point(450, 439);
            this.btn_Close.Name = "btn_Close";
            this.btn_Close.Size = new System.Drawing.Size(120, 35);
            this.btn_Close.TabIndex = 29;
            this.btn_Close.Text = "Close";
            this.btn_Close.UseVisualStyleBackColor = true;
            this.btn_Close.Click += new System.EventHandler(this.btn_Close_Click);
            // 
            // btn_Download
            // 
            this.btn_Download.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btn_Download.Enabled = false;
            this.btn_Download.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Download.ForeColor = System.Drawing.Color.White;
            this.btn_Download.Location = new System.Drawing.Point(283, 387);
            this.btn_Download.Name = "btn_Download";
            this.btn_Download.Size = new System.Drawing.Size(120, 35);
            this.btn_Download.TabIndex = 28;
            this.btn_Download.Text = "Download";
            this.btn_Download.UseVisualStyleBackColor = true;
            this.btn_Download.Click += new System.EventHandler(this.btn_Download_Click);
            // 
            // txt_Search
            // 
            this.txt_Search.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txt_Search.Location = new System.Drawing.Point(72, 10);
            this.txt_Search.Name = "txt_Search";
            this.txt_Search.Size = new System.Drawing.Size(361, 29);
            this.txt_Search.TabIndex = 26;
            this.txt_Search.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txt_Search_KeyDown);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Location = new System.Drawing.Point(12, 13);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(54, 21);
            this.label4.TabIndex = 25;
            this.label4.Text = "Game:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmb_NumResults
            // 
            this.cmb_NumResults.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmb_NumResults.FormattingEnabled = true;
            this.cmb_NumResults.Items.AddRange(new object[] {
            "25",
            "50",
            "100",
            "250",
            "All"});
            this.cmb_NumResults.Location = new System.Drawing.Point(423, 53);
            this.cmb_NumResults.Name = "cmb_NumResults";
            this.cmb_NumResults.Size = new System.Drawing.Size(85, 29);
            this.cmb_NumResults.TabIndex = 38;
            this.cmb_NumResults.SelectedIndexChanged += new System.EventHandler(this.cmb_NumResults_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(291, 56);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(126, 21);
            this.label1.TabIndex = 39;
            this.label1.Text = "Results Per Page:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(13, 56);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(63, 21);
            this.label2.TabIndex = 41;
            this.label2.Text = "Sort By:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmb_Sort
            // 
            this.cmb_Sort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmb_Sort.FormattingEnabled = true;
            this.cmb_Sort.Items.AddRange(new object[] {
            "Alphabetical",
            "Most Downloaded",
            "Most Likes",
            "Most Recently Uploaded",
            "Most Recently Updated"});
            this.cmb_Sort.Location = new System.Drawing.Point(82, 53);
            this.cmb_Sort.Name = "cmb_Sort";
            this.cmb_Sort.Size = new System.Drawing.Size(203, 29);
            this.cmb_Sort.TabIndex = 40;
            this.cmb_Sort.SelectedIndexChanged += new System.EventHandler(this.cmb_Sort_SelectedIndexChanged);
            // 
            // id
            // 
            this.id.Text = "id";
            this.id.Width = 0;
            // 
            // btn_Extract
            // 
            this.btn_Extract.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btn_Extract.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Extract.ForeColor = System.Drawing.Color.White;
            this.btn_Extract.Location = new System.Drawing.Point(252, 439);
            this.btn_Extract.Name = "btn_Extract";
            this.btn_Extract.Size = new System.Drawing.Size(184, 35);
            this.btn_Extract.TabIndex = 42;
            this.btn_Extract.Text = "Extract Existing";
            this.btn_Extract.UseVisualStyleBackColor = true;
            this.btn_Extract.Click += new System.EventHandler(this.btn_Extract_Click);
            // 
            // ScriptDownloader
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(822, 486);
            this.Controls.Add(this.btn_Extract);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cmb_Sort);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmb_NumResults);
            this.Controls.Add(this.btn_Next);
            this.Controls.Add(this.btn_Prev);
            this.Controls.Add(this.btn_ViewAll);
            this.Controls.Add(this.lbl_Status);
            this.Controls.Add(this.chkBox_Verified);
            this.Controls.Add(this.btn_Info);
            this.Controls.Add(this.list_Games);
            this.Controls.Add(this.btn_Search);
            this.Controls.Add(this.btn_Close);
            this.Controls.Add(this.btn_Download);
            this.Controls.Add(this.txt_Search);
            this.Controls.Add(this.label4);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(838, 479);
            this.Name = "ScriptDownloader";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Download Game Scripts";
            this.Resize += new System.EventHandler(this.ScriptDownloader_Resize);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txt_Search;
        private System.Windows.Forms.Button btn_Download;
        private System.Windows.Forms.Button btn_Close;
        private System.Windows.Forms.Button btn_Search;
        private System.Windows.Forms.ListView list_Games;
        private System.Windows.Forms.ColumnHeader gameName;
        private System.Windows.Forms.ColumnHeader ownerName;
        private System.Windows.Forms.ColumnHeader verified;
        private System.Windows.Forms.ColumnHeader downloadCount;
        private System.Windows.Forms.ColumnHeader createdAt;
        private System.Windows.Forms.ColumnHeader updatedAt;
        private System.Windows.Forms.Button btn_Info;
        private System.Windows.Forms.ColumnHeader stars;
        private System.Windows.Forms.CheckBox chkBox_Verified;
        private System.Windows.Forms.ColumnHeader description;
        private System.Windows.Forms.Label lbl_Status;
        private System.Windows.Forms.Button btn_ViewAll;
        private System.Windows.Forms.Button btn_Prev;
        private System.Windows.Forms.Button btn_Next;
        private System.Windows.Forms.ComboBox cmb_NumResults;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmb_Sort;
        private System.Windows.Forms.ColumnHeader id;
        private System.Windows.Forms.Button btn_Extract;
    }
}