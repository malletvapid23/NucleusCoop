using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using ListViewSorter;

namespace Nucleus.Coop.Forms
{
    public partial class ScriptDownloader : BaseForm
    {
        protected string api = "https://hub.splitscreen.me/api/v1/";

        private readonly List<Handler> searchHandlers = new List<Handler>();

        private MainForm mainForm;

        private string lastSearch;

        private ListViewColumnSorter lvwColumnSorter;

        private bool grabAll = false;

        private JArray handlers;

        private int entriesPerPage;

        private int entryIndex = 0;

        private int sortColumn = 0;

        private SortOrder sortOrder = SortOrder.Ascending;

        public ScriptDownloader(MainForm mf)
        {
            InitializeComponent();

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                   | SecurityProtocolType.Tls11
                   | SecurityProtocolType.Tls12
                   | SecurityProtocolType.Ssl3;
            ServicePointManager.ServerCertificateValidationCallback = (snder, cert, chain, error) => true;

            lvwColumnSorter = new ListViewColumnSorter();
            list_Games.ListViewItemSorter = lvwColumnSorter;

            cmb_Sort.SelectedIndex = 0;
            cmb_NumResults.SelectedIndex = 1;
            entriesPerPage = Convert.ToInt32(cmb_NumResults.SelectedItem);

            mainForm = mf;

            //list_Games.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            //list_Games.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void btn_Search_Click(object sender, EventArgs e)
        {
            if(txt_Search.Text.Contains("\\") || txt_Search.Text.Contains("/"))
            {
                MessageBox.Show("Search cannot contain the characters \"/\" or \"\\\".", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if(txt_Search.Text.StartsWith("*") || txt_Search.Text == ".")
            {
                MessageBox.Show("Illegal search query, please try something else.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if ((!string.IsNullOrEmpty(txt_Search.Text) && txt_Search.Text.Replace(" ", string.Empty).Length > 0) || grabAll)
            {
                btn_Prev.Enabled = false;
                btn_Next.Enabled = false;

                lastSearch = txt_Search.Text;

                //list_Games.Items.Clear();
                //searchHandlers.Clear();
                lbl_Status.Text = "";

                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                       | SecurityProtocolType.Tls11
                       | SecurityProtocolType.Tls12
                       | SecurityProtocolType.Ssl3;
                ServicePointManager.ServerCertificateValidationCallback = (snder, cert, chain, error) => true;

                string searchParam = Uri.EscapeDataString(txt_Search.Text);
                string rawHandlers = null;
                if(grabAll)
                {
                    rawHandlers = Get(api + "allhandlers");
                    grabAll = false;
                }
                else
                {
                    rawHandlers = Get(api + "handlers/" + searchParam);
                }
                
                txt_Search.Clear();

                if(rawHandlers == null)
                {
                    return;
                }
                else if(rawHandlers == "{}")
                {
                    MessageBox.Show("No results found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txt_Search.Focus();
                    return;
                }

                JObject jObject = JsonConvert.DeserializeObject(rawHandlers) as JObject;

                JArray array = jObject["Handlers"] as JArray;

                switch(cmb_Sort.SelectedIndex)
                {
                    case 0: // Alphabetical
                        handlers = new JArray(array.OrderBy(obj => (string)obj["gameName"]));
                        sortColumn = 0;
                        sortOrder = SortOrder.Ascending;
                        break;
                    case 1: // Most Downloaded
                        handlers = new JArray(array.OrderByDescending(obj => (int)obj["downloadCount"]));
                        sortColumn = 3;
                        sortOrder = SortOrder.Descending;
                        break;
                    case 2: // Most Likes
                        handlers = new JArray(array.OrderByDescending(obj => (int)obj["stars"]));
                        sortColumn = 4;
                        sortOrder = SortOrder.Descending;
                        break;
                    case 3: // Most Recently Uploaded
                        handlers = new JArray(array.OrderByDescending(obj => (DateTime)obj["createdAt"]));
                        sortColumn = 5;
                        sortOrder = SortOrder.Descending;
                        break;
                    case 4: // Most Recently Updated
                        handlers = new JArray(array.OrderByDescending(obj => (DateTime)obj["updatedAt"]));
                        sortColumn = 6;
                        sortOrder = SortOrder.Descending;
                        break;
                }

                entryIndex = 0;
                FetchHandlers(entryIndex);

                if(handlers.Count > entriesPerPage)
                {
                    btn_Next.Enabled = true;
                }
            }
        }

        private void FetchHandlers(int startIndex)
        {
            ImageList imageList = new ImageList();
            imageList.ImageSize = new Size(35, 35);

            list_Games.Items.Clear();
            searchHandlers.Clear();
            btn_Download.Enabled = false;
            btn_Info.Enabled = false;

            list_Games.BeginUpdate();
            Cursor.Current = Cursors.WaitCursor;

            int entriesToView = entriesPerPage;
            if((startIndex + entriesPerPage) > handlers.Count)
            {
                entriesToView = handlers.Count - startIndex;
            }

            for (int i = 0; i < entriesToView; i++)
            {
                lbl_Status.Text = string.Format("Fetching {0}/{1} handlers", (i+1), entriesToView);
                Handler handler = new Handler();

                handler.Id = handlers[i+startIndex]["_id"].ToString();
                handler.Owner = handlers[i+startIndex]["owner"].ToString();
                handler.OwnerName = handlers[i+startIndex]["ownerName"].ToString();
                handler.Description = handlers[i+startIndex]["description"].ToString();
                handler.Title = handlers[i+startIndex]["title"].ToString();
                handler.GameName = handlers[i+startIndex]["gameName"].ToString();
                handler.GameDescription = handlers[i+startIndex]["gameDescription"].ToString();
                handler.GameCover = handlers[i+startIndex]["gameCover"].ToString();
                handler.GameId = handlers[i+startIndex]["gameId"].ToString();
                handler.GameUrl = handlers[i+startIndex]["gameUrl"].ToString();
                handler.CreatedAt = handlers[i+startIndex]["createdAt"].ToString();
                handler.UpdatedAt = handlers[i+startIndex]["updatedAt"].ToString();
                handler.Stars = handlers[i+startIndex]["stars"].ToString();
                handler.DownloadCount = handlers[i+startIndex]["downloadCount"].ToString();
                handler.Verified = handlers[i+startIndex]["verified"].ToString();
                handler.Private = handlers[i+startIndex]["private"].ToString();
                handler.CommentCount = handlers[i+startIndex]["commentCount"].ToString();
                handler.CurrentVersion = handlers[i+startIndex]["currentVersion"].ToString();
                handler.CurrentPackage = handlers[i+startIndex]["currentPackage"].ToString();

                if (chkBox_Verified.Checked && !Boolean.Parse(handler.Verified))
                {
                    continue;
                }

                searchHandlers.Add(handler);

                string vSymb;
                if (handler.Verified == "True")
                {
                    vSymb = "ü";
                }
                else
                {
                    vSymb = string.Empty;
                }

                Bitmap bmp = new Bitmap(Properties.Resources.no_image);
                string _cover = $@"https://images.igdb.com/igdb/image/upload/t_micro/{handler.GameCover}.jpg";

                try
                {
                    WebRequest request = WebRequest.Create(_cover);
                    WebResponse resp = request.GetResponse();
                    Stream respStream = resp.GetResponseStream();
                    bmp = new Bitmap(respStream);
                    respStream.Dispose();
                }
                catch (Exception) { }

                imageList.Images.Add(bmp);
                list_Games.SmallImageList = imageList;

                string[] handlerDisplayCols = { handler.OwnerName, vSymb, handler.DownloadCount, handler.Stars, handler.CreatedAt, handler.UpdatedAt, handler.Description, handler.Id };
                list_Games.Items.Add(" " + handler.GameName).SubItems.AddRange(handlerDisplayCols);
                list_Games.Items[i].ImageIndex = i;

                if(list_Games.Items[i].SubItems[5].Text.Contains(" "))
                {
                    list_Games.Items[i].SubItems[5].Text = list_Games.Items[i].SubItems[5].Text.Substring(0, list_Games.Items[i].SubItems[5].Text.IndexOf(' '));
                }

                if (int.Parse(handler.CurrentVersion) > 1)
                {
                    if (list_Games.Items[i].SubItems[6].Text.Contains(" "))
                    {
                        list_Games.Items[i].SubItems[6].Text = list_Games.Items[i].SubItems[6].Text.Substring(0, list_Games.Items[i].SubItems[6].Text.IndexOf(' '));
                    }  
                }
                else
                {
                    list_Games.Items[i].SubItems[6].Text = string.Empty;
                }

                if (list_Games.Items[i].SubItems[7].Text.Length > 50)
                {
                    list_Games.Items[i].SubItems[7].Text = list_Games.Items[i].SubItems[7].Text.Substring(0, 50) + "...";
                }

                foreach (ListViewItem lvi in list_Games.Items)
                {
                    lvi.UseItemStyleForSubItems = false;
                }

                list_Games.Items[i].SubItems[2].Font = new Font(new FontFamily("Wingdings"), 10, FontStyle.Bold);
                list_Games.Items[i].SubItems[2].ForeColor = Color.Green;


            }

            list_Games.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            list_Games.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            list_Games.Columns[8].Width = 0;

            lvwColumnSorter.SortColumn = sortColumn;
            lvwColumnSorter.Order = sortOrder;
            list_Games.SetSortIcon(0, lvwColumnSorter.Order);

            list_Games.Sort();

            list_Games.EndUpdate();

            if(startIndex + entriesToView == handlers.Count)
                btn_Next.Enabled = false;
            else
                btn_Next.Enabled = true;
            if (startIndex == 0)
                btn_Prev.Enabled = false;
            else
                btn_Prev.Enabled = true;

            Cursor.Current = Cursors.Default;

            lbl_Status.Text = string.Format("Viewing results {0}-{1}. Total: {2}", (startIndex + 1), entriesToView + startIndex, handlers.Count);
        }

        public string Get(string uri)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                //request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(string.Format("{0}: {1}", ex.ToString(), ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private void btn_Close_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void txt_Search_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btn_Search_Click(this, new EventArgs());
            }
        }

        private void ScriptDownloader_Resize(object sender, EventArgs e)
        {
            list_Games.BeginUpdate();
            list_Games.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            list_Games.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            list_Games.Columns[8].Width = 0;
            list_Games.EndUpdate();
        }

        private void list_Games_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ListView myListView = (ListView)sender;

            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == lvwColumnSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (lvwColumnSorter.Order == SortOrder.Ascending)
                {
                    lvwColumnSorter.Order = SortOrder.Descending;
                }
                else
                {
                    lvwColumnSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Set the column number that is to be sorted; default to descending.
                lvwColumnSorter.SortColumn = e.Column;
                if(e.Column == 0)
                {
                    lvwColumnSorter.Order = SortOrder.Ascending;
                }
                else
                {
                    lvwColumnSorter.Order = SortOrder.Descending;
                }
                
            }

            // Perform the sort with these new sort options.
            myListView.Sort();
            myListView.SetSortIcon(e.Column, lvwColumnSorter.Order);
            myListView.EnsureVisible(0);
        }

        private void btn_Info_Click(object sender, EventArgs e)
        {
            if(list_Games.SelectedItems.Count == 1)
            {
                //int index = list_Games.Items.IndexOf(list_Games.SelectedItems[0]);

                Handler handler = null;
                foreach (Handler hndl in searchHandlers)
                {
                    if (list_Games.SelectedItems[0].SubItems[8].Text == hndl.Id)
                    {
                        handler = hndl;
                        break;
                    }
                }

                if (handler == null)
                {
                    MessageBox.Show("Error fetching handler", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                HandlerInfo handlerInfo = new HandlerInfo(handler, mainForm);
                handlerInfo.ShowDialog();
            }
        }

        private void btn_Download_Click(object sender, EventArgs e)
        {
            if (list_Games.SelectedItems.Count == 1)
            {
                //int index = list_Games.Items.IndexOf(list_Games.SelectedItems[0]);
                //Handler handler = searchHandlers[index];
                Handler handler = null;
                foreach (Handler hndl in searchHandlers)
                {
                    if (list_Games.SelectedItems[0].SubItems[8].Text == hndl.Id)
                    {
                        handler = hndl;
                        break;
                    }
                }

                if(handler == null)
                {
                    MessageBox.Show("Error fetching handler", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                //Regex pattern = new Regex("[\\/:*?\"<>|]");
                //string frmHandleTitle = pattern.Replace(handler.Title, ""); 
                //if (File.Exists(Path.Combine(Gaming.GameManager.Instance.GetJsScriptsPath(), frmHandleTitle + ".js")))
                //{
                //    DialogResult dialogResult = MessageBox.Show("An existing script with the name " + (frmHandleTitle + ".js") + " already exists. Do you wish to overwrite it?", "Script already exists", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                //    if (dialogResult != DialogResult.Yes)
                //    {
                //        return;
                //    }
                //}

                DownloadPrompt downloadPrompt = new DownloadPrompt(handler, mainForm, null);
                downloadPrompt.ShowDialog();
            }
        }

        private void chkBox_Verified_Click(object sender, EventArgs e)
        {
            if (chkBox_Verified.Checked)
            {
                if (list_Games.Items.Count > 0)
                {
                    foreach (ListViewItem game in list_Games.Items)
                    {
                        if (game.SubItems[2].Text != "ü")
                        {
                            game.Remove();
                        }
                    }
                }
            }
            else
            {
                txt_Search.Text = lastSearch;
                btn_Search.PerformClick();
            }
        }

        private void btn_ViewAll_Click(object sender, EventArgs e)
        {
            grabAll = true;
            btn_Search.PerformClick();
        }

        private void btn_Prev_Click(object sender, EventArgs e)
        {
            entryIndex -= entriesPerPage;
            if (entryIndex < 0)
                entryIndex = 0;

            chkBox_Verified.Checked = false;
            FetchHandlers(entryIndex);
        }
            

        private void btn_Next_Click(object sender, EventArgs e)
        {
            entryIndex += entriesPerPage;
            if (entryIndex > handlers.Count)
                entryIndex = handlers.Count - entriesPerPage;

            chkBox_Verified.Checked = false;
            FetchHandlers(entryIndex);
        }

        private void cmb_NumResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(cmb_NumResults.SelectedItem.ToString() == "All")
            {
                entriesPerPage = 9999;
            }
            else
            {
                entriesPerPage = Convert.ToInt32(cmb_NumResults.SelectedItem);
            }

            if (handlers != null && handlers.Count > 0)
            {
                chkBox_Verified.Checked = false;
                FetchHandlers(entryIndex);
            }
        }

        private void cmb_Sort_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(list_Games.Items.Count > 0)
            {
                switch (cmb_Sort.SelectedIndex)
                {
                    case 0: // Alphabetical
                        sortColumn = 0;
                        sortOrder = SortOrder.Ascending;
                        break;
                    case 1: // Most Downloaded
                        sortColumn = 3;
                        sortOrder = SortOrder.Descending;
                        break;
                    case 2: // Most Likes
                        sortColumn = 4;
                        sortOrder = SortOrder.Descending;
                        break;
                    case 3: // Most Recently Uploaded
                        sortColumn = 5;
                        sortOrder = SortOrder.Descending;
                        break;
                    case 4: // Most Recently Updated
                        sortColumn = 6;
                        sortOrder = SortOrder.Descending;
                        break;
                }

                list_Games.EnsureVisible(0);

                lvwColumnSorter.SortColumn = sortColumn;
                lvwColumnSorter.Order = sortOrder;
                list_Games.SetSortIcon(sortColumn, lvwColumnSorter.Order);

                list_Games.Sort();
            }
        }

        private void list_Games_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(list_Games.SelectedItems.Count == 1)
            {
                btn_Download.Enabled = true;
                btn_Info.Enabled = true;
            }
        }

        private void btn_Extract_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Select a game handler to extract",
                DefaultExt = "nc",
                InitialDirectory = Gaming.GameManager.Instance.GetJsScriptsPath(),
                Filter = "nc files (*.nc)|*.nc"
            };

            DialogResult result = ofd.ShowDialog();
            if (result == DialogResult.OK)
            {
                DownloadPrompt downloadPrompt = new DownloadPrompt(null, mainForm, ofd.FileName);
                downloadPrompt.ShowDialog();
            }
        }
    }
}
