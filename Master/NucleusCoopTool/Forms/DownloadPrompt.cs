using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Ionic.Zip;

namespace Nucleus.Coop.Forms
{
    public partial class DownloadPrompt : Form
    {

        private Handler Handler;
        private string zipFile;
        private string scriptFolder = Gaming.GameManager.Instance.GetJsScriptsPath();
        private bool zipExtractFinished;
        private int numEntries;
        private int entriesDone = 0;

        private MainForm mainForm;

        public DownloadPrompt(Handler handler, MainForm mf)
        {
            InitializeComponent();

            Handler = handler;
            mainForm = mf;

            zipFile = string.Format("handler-{0}-v{1}.nc", Handler.Id, Handler.CurrentVersion);
            lbl_Handler.Text = zipFile;

            BeginDownload();
        }

        private void BeginDownload()
        {
            using (WebClient wc = new WebClient())
            {
                wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                wc.DownloadFileAsync(
                    // Param1 = Link of file
                    new System.Uri($@"https://hub.splitscreen.me/cdn/storage/packages/{Handler.CurrentPackage}/original/handler-{Handler.Id}-v{Handler.CurrentVersion}.nc?download=true"),
                    // Param2 = Path to save
                    Path.Combine(scriptFolder, zipFile)
                ); ;
                wc.DownloadFileCompleted += new AsyncCompletedEventHandler(wc_DownloadFileCompleted);
            }
        }

        void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            prog_DownloadBar.Value = e.ProgressPercentage;
            lbl_ProgPerc.Text = e.ProgressPercentage + "%";
        }

        void wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            label1.Text = "Extracting:";

            ZipFile zip = new ZipFile(Path.Combine(scriptFolder, zipFile));//"handler-wFsXDjoAkHpL4XZm8-v2.nc"));
            zip.ExtractProgress += ExtractProgress;
            numEntries = zip.Entries.Count;
            Regex pattern = new Regex("[\\/:*?\"<>|]");
            string frmHandleTitle = pattern.Replace(Handler.Title, "");

            //zip.ExtractAll(scriptFolder, ExtractExistingFileAction.OverwriteSilently);
            List<string> handlerFolders = new List<string>();

            bool isHFolderMain = false;
            foreach (ZipEntry ze in zip)
            {
                if (ze.IsDirectory)
                {
                    int count = 0;
                    foreach (char c in ze.FileName)
                        if (c == '/') count++;
                    if(count == 1)
                    {
                        handlerFolders.Add(ze.FileName.TrimEnd('/'));
                        if(ze.FileName.TrimEnd('/') == frmHandleTitle)
                        {
                            isHFolderMain = true;
                            if(Directory.Exists(Path.Combine(scriptFolder, frmHandleTitle)))
                            {
                                Directory.Delete(Path.Combine(scriptFolder, frmHandleTitle), true);
                            }
                        }
                    }
                    ze.Extract(scriptFolder, ExtractExistingFileAction.OverwriteSilently);
                }
                else
                {
                    ze.Extract(scriptFolder, ExtractExistingFileAction.OverwriteSilently);
                }
            }

            if (File.Exists(Path.Combine(scriptFolder, frmHandleTitle + ".js")))
            {
                File.Delete(Path.Combine(scriptFolder, frmHandleTitle + ".js"));
            }
            File.Move(Path.Combine(scriptFolder, "handler.js"), Path.Combine(scriptFolder, frmHandleTitle + ".js"));

            if(handlerFolders.Count > 0)
            {
                string gameFolder = Path.Combine(scriptFolder, frmHandleTitle);

                if (Directory.Exists(gameFolder) && !isHFolderMain)
                {
                    Directory.Delete(gameFolder, true);
                }

                if (!Directory.Exists(gameFolder))
                {
                    Directory.CreateDirectory(gameFolder);
                }


                foreach (string hFolder in handlerFolders)
                {
                    string newFolder = Path.Combine(scriptFolder, hFolder);

                    if (newFolder != gameFolder)
                    {
                        foreach (string dir in Directory.GetDirectories(newFolder, "*", SearchOption.AllDirectories))
                        {
                            Directory.CreateDirectory(Path.Combine(gameFolder, dir.Substring(newFolder.Length + 1)));
                        }

                        foreach (string file_name in Directory.GetFiles(newFolder, "*", SearchOption.AllDirectories))
                        {
                            File.Move(file_name, Path.Combine(gameFolder, file_name.Substring(newFolder.Length + 1)));
                        }

                        Directory.Delete(newFolder, true);
                    }
                }
            }

            while (!zipExtractFinished)
            {
                Application.DoEvents();
            }

            zip.Dispose();
            lbl_Handler.Text = "";
            label1.Text = "Finished!";

            File.Delete(Path.Combine(scriptFolder, zipFile));

            string exeName = null;
            DialogResult dialogResult = MessageBox.Show("Downloading and extraction of " + Handler.GameName + " script is complete. Would you like to add this game to Nucleus now? You will need to select the game executable to add it.", "Download finished! Add to Nucleus?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialogResult == DialogResult.Yes)
            {
                foreach (var line in File.ReadAllLines(Path.Combine(scriptFolder, frmHandleTitle + ".js")))
                {
                    if(line.ToLower().StartsWith("game.executablename"))
                    {
                        int start = line.IndexOf("\"");
                        int end = line.LastIndexOf("\"");
                        exeName = line.Substring(start + 1, (end - start) - 1);
                    }
                }

                Gaming.GameManager.Instance.AddScript(frmHandleTitle);
                mainForm.SearchGame(exeName);
            }

            Close();
        }

        private void ExtractProgress(object sender, ExtractProgressEventArgs e)
        {
            if (e.EventType == ZipProgressEventType.Extracting_AfterExtractEntry)
            {
                entriesDone++;
                prog_DownloadBar.Value = ((entriesDone / numEntries) * 100);
                lbl_ProgPerc.Text = ((entriesDone / numEntries) * 100) + "%";
            }

            if (e.EventType == ZipProgressEventType.Extracting_AfterExtractAll || (e.EventType == ZipProgressEventType.Extracting_AfterExtractEntry && entriesDone == numEntries))
            {
                zipExtractFinished = true;
            }
            else if (e.EventType == ZipProgressEventType.Extracting_BeforeExtractEntry)
            {
                lbl_Handler.Text = e.CurrentEntry.FileName;
            }
        }
    }
}
