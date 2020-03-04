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

namespace Nucleus.Coop.Forms
{
    public partial class ScriptDownloader : BaseForm
    {
        protected string api = "https://hub.splitscreen.me/api/v1/";

        private readonly List<Handler> searchHandlers = new List<Handler>();

        private MainForm mainForm;

        private string lastSearch;

        public ScriptDownloader(MainForm mf)
        {
            InitializeComponent();

            mainForm = mf;

            list_Games.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            list_Games.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void btn_Search_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txt_Search.Text) && txt_Search.Text.Replace(" ", string.Empty).Length > 0)
            {
                lastSearch = txt_Search.Text;

                list_Games.Items.Clear();
                searchHandlers.Clear();

                string searchParam = Uri.EscapeDataString(txt_Search.Text);
                string rawHandlers = Get(api + "handlers/" + searchParam);
                txt_Search.Clear();

                if(rawHandlers == "{}")
                {
                    MessageBox.Show("No results found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txt_Search.Focus();
                    return;
                }

                ImageList imageList = new ImageList();
                imageList.ImageSize = new Size(35, 35);

                JObject jObject = JsonConvert.DeserializeObject(rawHandlers) as JObject;

                JArray handlers = jObject["Handlers"] as JArray;

                bool minResult = false;
                for (int i = 0; i < handlers.Count; i++)
                {
                    Handler handler = new Handler();

                    handler.Id = jObject["Handlers"][i]["_id"].ToString();
                    handler.Owner = jObject["Handlers"][i]["owner"].ToString();
                    handler.OwnerName = jObject["Handlers"][i]["ownerName"].ToString();
                    handler.Description = jObject["Handlers"][i]["description"].ToString();
                    handler.Title = jObject["Handlers"][i]["title"].ToString();
                    handler.GameName = jObject["Handlers"][i]["gameName"].ToString();
                    handler.GameDescription = jObject["Handlers"][i]["gameDescription"].ToString();
                    handler.GameCover = jObject["Handlers"][i]["gameCover"].ToString();
                    handler.GameId = jObject["Handlers"][i]["gameId"].ToString();
                    handler.GameUrl = jObject["Handlers"][i]["gameUrl"].ToString();
                    handler.CreatedAt = jObject["Handlers"][i]["createdAt"].ToString();
                    handler.UpdatedAt = jObject["Handlers"][i]["updatedAt"].ToString();
                    handler.Stars = jObject["Handlers"][i]["stars"].ToString();
                    handler.DownloadCount = jObject["Handlers"][i]["downloadCount"].ToString();
                    handler.Verified = jObject["Handlers"][i]["verified"].ToString();
                    handler.Private = jObject["Handlers"][i]["private"].ToString();
                    handler.CommentCount = jObject["Handlers"][i]["commentCount"].ToString();
                    handler.CurrentVersion = jObject["Handlers"][i]["currentVersion"].ToString();
                    handler.CurrentPackage = jObject["Handlers"][i]["currentPackage"].ToString();

                    if(chkBox_Verified.Checked && !Boolean.Parse(handler.Verified))
                    {
                        continue;
                    }

                    if (!minResult)
                        minResult = true;

                    searchHandlers.Add(handler);

                    string vSymb;
                    if(handler.Verified == "True")
                    {
                        vSymb = "ü";
                    }
                    else
                    {
                        vSymb = string.Empty;
                    }

                    string _cover = $@"https://images.igdb.com/igdb/image/upload/t_micro/{handler.GameCover}.jpg";
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

                    WebRequest request = WebRequest.Create(_cover);
                    WebResponse resp = request.GetResponse();
                    Stream respStream = resp.GetResponseStream();
                    Bitmap bmp = new Bitmap(respStream);
                    respStream.Dispose();

                    imageList.Images.Add(bmp);
                    list_Games.SmallImageList = imageList;

                    string[] handlerDisplayCols = { handler.OwnerName, vSymb, handler.DownloadCount, handler.Stars, handler.CreatedAt, handler.UpdatedAt, handler.Description };
                    list_Games.Items.Add(" " + handler.GameName).SubItems.AddRange(handlerDisplayCols);
                    list_Games.Items[i].ImageIndex = i;

                    list_Games.Items[i].SubItems[5].Text = list_Games.Items[i].SubItems[5].Text.Substring(0, list_Games.Items[i].SubItems[5].Text.IndexOf(' '));
                    if(int.Parse(handler.CurrentVersion) > 1)
                    {
                        list_Games.Items[i].SubItems[6].Text = list_Games.Items[i].SubItems[6].Text.Substring(0, list_Games.Items[i].SubItems[6].Text.IndexOf(' '));
                    }
                    else
                    {
                        list_Games.Items[i].SubItems[6].Text = string.Empty;
                    }

                    if(list_Games.Items[i].SubItems[7].Text.Length > 50)
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

                if (!minResult)
                {
                    MessageBox.Show("No results found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txt_Search.Focus();
                }
                else
                {
                    list_Games.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                    list_Games.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                }
            }
        }


        public string Get(string uri)
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
            list_Games.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            list_Games.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void list_Games_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            
        }

        private void btn_Info_Click(object sender, EventArgs e)
        {
            if(list_Games.SelectedItems.Count == 1)
            {
                int index = list_Games.Items.IndexOf(list_Games.SelectedItems[0]);
                HandlerInfo handlerInfo = new HandlerInfo(searchHandlers[index], mainForm);
                handlerInfo.ShowDialog();
            }
        }

        private void btn_Download_Click(object sender, EventArgs e)
        {
            if (list_Games.SelectedItems.Count == 1)
            {
                int index = list_Games.Items.IndexOf(list_Games.SelectedItems[0]);
                Handler handler = searchHandlers[index];

                Regex pattern = new Regex("[\\/:*?\"<>|]");
                string frmHandleTitle = pattern.Replace(handler.Title, "");
                if (File.Exists(Path.Combine(Gaming.GameManager.Instance.GetJsScriptsPath(), frmHandleTitle + ".js")))
                {
                    DialogResult dialogResult = MessageBox.Show("An existing script with the name " + (frmHandleTitle + ".js") + " already exists. Do you wish to overwrite it?", "Script already exists", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (dialogResult != DialogResult.Yes)
                    {
                        return;
                    }
                }

                DownloadPrompt downloadPrompt = new DownloadPrompt(handler, mainForm);
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
    }
}
