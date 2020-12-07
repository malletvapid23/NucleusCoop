using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace Nucleus.Coop.Forms
{
    public partial class HandlerInfo : BaseForm
    {
        protected string api = "https://hub.splitscreen.me/api/v1/";

        private readonly Handler Handler;
        private MainForm mainForm;

        public HandlerInfo(Handler handler, MainForm mf)
        {
            InitializeComponent();

            Handler = handler;

            txt_GameName.Text = Handler.GameName;
            txt_GameDesc.Text = Handler.GameDescription;
            txt_Version.Text = Handler.CurrentVersion;
            txt_Down.Text = Handler.DownloadCount;
            txt_Likes.Text = Handler.Stars;
            if(Handler.Verified == "True")
            {
                txt_Verified.Text = "Yes";
            }
            else
            {
                txt_Verified.Text = "No";
            }

            txt_Created.Text = Handler.CreatedAt;
            if(int.Parse(Handler.CurrentVersion) > 1)
            {
                txt_Updated.Text = Handler.UpdatedAt;
            }
            else
            {
                txt_Updated.Text = "N/A";
            }
            
            txt_AuthDesc.Text = Handler.Description;

            Bitmap bmp = new Bitmap(Properties.Resources.no_image);
            string _cover = $@"https://images.igdb.com/igdb/image/upload/t_cover_small/{Handler.GameCover}.jpg";

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                    | SecurityProtocolType.Tls11
                    | SecurityProtocolType.Tls12
                    | SecurityProtocolType.Ssl3;
            ServicePointManager.ServerCertificateValidationCallback = (snder, cert, chain, error) => true;
            ServicePointManager.DefaultConnectionLimit = 9999;

            try
            {
                WebRequest request = WebRequest.Create(_cover);
                //request.Timeout = 10;
                WebResponse resp = request.GetResponse();
                Stream respStream = resp.GetResponseStream();
                bmp = new Bitmap(respStream);
                respStream.Dispose();
            }
            catch (Exception) { }
                
            pic_GameCover.Image = bmp;
            

            string rawComments = Get(api + "comments/" + Handler.Id);
            if (rawComments != "{}")
            {
                JObject jObject = JsonConvert.DeserializeObject(rawComments) as JObject;

                JArray comments = jObject["Comments"] as JArray;
                for (int i = 0; i < comments.Count; i++)
                {
                    string id = jObject["Comments"][i]["_id"].ToString();
                    string owner = jObject["Comments"][i]["owner"].ToString();
                    string ownerName = jObject["Comments"][i]["ownerName"].ToString();
                    string content = jObject["Comments"][i]["content"].ToString();
                    string handlerId = jObject["Comments"][i]["handlerId"].ToString();
                    string createdAt = jObject["Comments"][i]["createdAt"].ToString();
                    
                    txt_Comm.SelectionFont = new Font(txt_Comm.Font, FontStyle.Bold);
                    txt_Comm.AppendText(string.Format("{0} {1}", createdAt, ownerName));
                    txt_Comm.SelectionFont = new Font(txt_Comm.Font, FontStyle.Regular);
                    txt_Comm.AppendText(string.Format(": {0}", content));
                    txt_Comm.AppendText(Environment.NewLine);
                    txt_Comm.AppendText(Environment.NewLine);
                }
            }
        }

        public string Get(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            //request.Timeout = 10;

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

        private void btn_Download_Click(object sender, EventArgs e)
        {
            DownloadPrompt downloadPrompt = new DownloadPrompt(Handler, mainForm, null);
            downloadPrompt.ShowDialog();
        }

        private void LinkLabel_MoreInfo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(Handler.GameUrl);
        }

        private void txt_AuthDesc_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }

        private void txt_Comm_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }
    }
}
