using Nucleus.Gaming;
using Nucleus.Gaming.Coop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

namespace Nucleus.Coop
{
    public partial class SearchDisksForm : BaseForm
    {
        public struct SearchDriveInfo
        {
            public DriveInfo drive;
            public string text;

            public override string ToString()
            {
                return text;
            }
        }

        private float progress;
        private float lastProgress;

        private List<string> pathsToSearch;

        private bool searching;
        private bool closed;
        private MainForm main;

        private readonly IniFile ini = new Gaming.IniFile(Path.Combine(Directory.GetCurrentDirectory(), "Settings.ini"));

        public SearchDisksForm(MainForm main)
        {
            
            this.main = main;
            InitializeComponent();

            DPIManager.AddForm(this);
            DPIManager.ForceUpdate();

            for (int x = 1; x <= 100; x++)
            {
                if (ini.IniReadValue("SearchPaths", x.ToString()) == "")
                {
                    break;
                }
                else
                {
                    disksBox.Items.Add(ini.IniReadValue("SearchPaths", x.ToString()), true);
                }
            }
        }

        protected override Size DefaultSize
        {
            get
            {
                return new Size(720, 500);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            closed = true;
        }

        private bool IsElevated
        {
            get
            {
                return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (btnSearch.Text == "Cancel")
            {
                closed = true;
                searching = false;
                btnSearch.Text = "Search";
                txt_Path.Text = "";
                txt_Stage.Text = "Cancelling";
                checkboxFoundGames.Items.Clear();
                Cancel();
                return;
            }

            if (searching || disksBox.CheckedItems.Count == 0)
            {
                return;
            }

            btnSearch.Text = "Cancel";
            btn_customPath.Enabled = false;
            btn_delPath.Enabled = false;
            disksBox.Enabled = false;
            searching = true;
            closed = false;
            txt_Path.Text = "";
            txt_Stage.Text = "";
            label1.Enabled = false;
            checkboxFoundGames.Items.Clear();

            pathsToSearch = new List<string>();

            for (int i = 0; i < disksBox.CheckedItems.Count; i++)
            {
                pathsToSearch.Add(disksBox.CheckedItems[i].ToString());
            }

            ThreadPool.QueueUserWorkItem(SearchDrive, null);
        }

        private void UpdateProgress(float toAdd)
        {
            progress += toAdd;

            float dif = progress - lastProgress;
            if (dif > 0.005f || toAdd == 0) // only update after .5% or if the user has just requested an update
            {
                lastProgress = progress;
                if (this.IsDisposed || closing)
                {
                    return;
                }

                Invoke(new Action(delegate
                {
                    if (this.IsDisposed || progressBar1.IsDisposed || closing)
                    {
                        return;
                    }
                    progressBar1.Value = Math.Min(100, (int)(progress * 100));
                }));
            }
        }

        private IEnumerable<string> GetFiles(string path)
        {
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(path);
            while (queue.Count > 0)
            {
                Application.DoEvents();
                if (closed)
                {
                    break;
                }

                path = queue.Dequeue();
                txt_Path.Text = path;
                try
                {
                    foreach (string subDir in Directory.GetDirectories(path))
                    {
                        if (closed)
                        {
                            break;
                        }
                        queue.Enqueue(subDir);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
                string[] files = null;
                try
                {
                    if (closed)
                    {
                        break;
                    }
                    files = Directory.GetFiles(path, "*.exe");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
                if (files != null)
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (closed)
                        {
                            break;
                        }
                        yield return files[i];
                    }
                }
            }
            if (closed)
            {
                txt_Path.Text = "";
                yield return null;
            }
        }

        private void Cancel()
        {
            searching = false;
            btnSearch.Enabled = true;
            btn_customPath.Enabled = true;
            btn_delPath.Enabled = true;
            btn_addSelection.Enabled = false;
            btn_selectAll.Enabled = false;
            btn_deselectAll.Enabled = false;
            checkboxFoundGames.Enabled = false;
            checkboxFoundGames.Items.Clear();
            disksBox.Enabled = true;
            progressBar1.Value = 0;
            progress = 0;
            lastProgress = 0;
            UpdateProgress(0);
            label2.Enabled = false;
            label1.Enabled = true;
            txt_Stage.Text = "Cancelled";
            txt_Path.Text = "";
        }

        private void SearchDrive(object state)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                for (int i = 0; i < pathsToSearch.Count; i++)
                {
                    txt_Stage.Text = i + 1 + " of " + pathsToSearch.Count;
                    string currentPath = pathsToSearch[i];

                    float totalDiskPc = 1 / (float)pathsToSearch.Count;
                    float thirdDiskPc = totalDiskPc / 3.0f;

                    // 1/3 done, we started the operation
                    UpdateProgress(thirdDiskPc);

                    List<string> result = new List<string>();
                    //if(IsElevated)
                    //{
                    //result = Directory.EnumerateFiles(currentPath, "*.exe", SearchOption.AllDirectories).ToList();
                    //result = Directory.GetFiles(currentPath, "*.exe", SearchOption.AllDirectories).ToList();
                    //}
                    //else
                    //{
                    //result = GetFiles(currentPath).ToList();
                    //}


                    //result = GetFiles(currentPath, "*.exe").ToList();
                    result = GetFiles(currentPath).ToList();


                    float increment = thirdDiskPc / result.Count;

                    foreach (string exeFilePath in result)
                    {
                        if (closed)
                        {
                            return;
                        }

                        //UpdateProgress(increment);
                        
                        if (GameManager.Instance.User.Games.Any(c => c.ExePath.ToLower() == exeFilePath.ToLower()))
                        {
                            continue;
                        }

                        if (GameManager.Instance.AnyGame(Path.GetFileName(exeFilePath).ToLower()))
                        {
                            if (exeFilePath.Contains("$Recycle.Bin") ||
                                exeFilePath.Contains(@"\Instance"))
                            {
                                // noope
                                continue;
                            }

                            GenericGameInfo uinfo = GameManager.Instance.GetGame(exeFilePath);

                            if (uinfo != null)
                            {
#if RELEASE
                    if (uinfo.Game.Debug)
                    {
                        continue;
                    }
#endif
                                //LogManager.Log("> Found new game {0} on drive {1}", uinfo.Game.GameName, info.drive.Name);
                                Invoke(new Action(delegate
                                {
                                    bool exists = false;
                                    foreach (var item in checkboxFoundGames.Items)
                                    {
                                        if (item.ToString() == uinfo.GameName + " | " + exeFilePath)
                                        {
                                            exists = true;
                                        }
                                    }
                                    if (!exists)
                                    {
                                        checkboxFoundGames.Items.Add(uinfo.GameName + " | " + exeFilePath, true);
                                        checkboxFoundGames.Refresh();
                                    }
                                }));
                            }
                        }
                    }

                    if (closed)
                    {
                        return;
                    }
                }

                searching = false;
                btnSearch.Text = "Search";


                watch.Stop();

                var elapsedMs = watch.ElapsedMilliseconds / 1000;

                if (checkboxFoundGames.Items.Count == 0)
                {
                    btn_customPath.Enabled = true;
                    btn_delPath.Enabled = true;
                    disksBox.Enabled = true;

                    MessageBox.Show("Operation completed in " + elapsedMs + "s. No new games found.");
                    progressBar1.Value = 0;
                    return;
                }
                
                progress = 1;
                UpdateProgress(0);
                btn_addSelection.Enabled = true;
                btn_selectAll.Enabled = true;
                btn_deselectAll.Enabled = true;
                checkboxFoundGames.Enabled = true;
                btnSearch.Enabled = false;
                label2.Enabled = true;
                label1.Enabled = false;
                txt_Stage.Text = "Done";
                txt_Path.Text = "";
                Refresh();
                Invalidate();
                MessageBox.Show("Search has completed, operation took " + elapsedMs + "s. Select the games you wish to add from the right-hand side.", "Search finished");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                MessageBox.Show(ex.Message);
            }
        }

        private bool closing;
        private void SearchDisksForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            closing = true;
        }

        private void Btn_customPath_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    disksBox.Items.Add(fbd.SelectedPath, true);
                    int freeIndex = 1;
                    for(int x = 1; x <= 100; x++)
                    {
                        if(ini.IniReadValue("SearchPaths",x.ToString()) == "")
                        {
                            freeIndex = x;
                            break;
                        }
                    }
                    ini.IniWriteValue("SearchPaths", freeIndex.ToString(), fbd.SelectedPath);
                }
            }
        }

        private void Btn_addSelection_Click(object sender, EventArgs e)
        {
            if (checkboxFoundGames.CheckedItems.Count == 0)
            {
                DialogResult dialogResult = MessageBox.Show("No games have been selected. Do you wish to add any?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dialogResult == DialogResult.Yes)
                {
                    return;
                }
            }

            if(checkboxFoundGames.CheckedItems.Count > 0)
            {
                List<string> gamesToAdd = new List<string>();
                int numAdded = 0;

                for (int i = 0; i < checkboxFoundGames.CheckedItems.Count; i++)
                {
                    string checkBoxFullname = checkboxFoundGames.CheckedItems[i].ToString();
                    string exePath = checkBoxFullname.Substring(checkBoxFullname.IndexOf(" | ") + " | ".Length);
                    gamesToAdd.Add(exePath);
                }

                foreach (string gameToAdd in gamesToAdd)
                {
                    UserGameInfo uinfo = GameManager.Instance.TryAddGame(gameToAdd);
                    if (uinfo != null)
                    {
                        main.NewUserGame(uinfo);
                        numAdded++;
                    }
                }
                MessageBox.Show(string.Format("{0}/{1} selected games added!", numAdded, checkboxFoundGames.CheckedItems.Count), "Games added");
                main.RefreshGames();
            }

            btnSearch.Enabled = true;
            btn_customPath.Enabled = true;
            btn_delPath.Enabled = true;
            btn_addSelection.Enabled = false;
            btn_selectAll.Enabled = false;
            btn_deselectAll.Enabled = false;
            checkboxFoundGames.Enabled = false;
            checkboxFoundGames.Items.Clear();
            disksBox.Enabled = true;
            progressBar1.Value = 0;
            progress = 0;
            lastProgress = 0;
            label2.Enabled = false;
            label1.Enabled = true;
            txt_Path.Text = "";
            txt_Stage.Text = "";
        }

        private void Btn_delPath_Click(object sender, EventArgs e)
        {
            if (disksBox.CheckedItems.Count == 0)
            {
                return;
            }

            DialogResult dialogResult = MessageBox.Show("Are you sure you want to delete the paths that are currently checked?", "Confirm deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dialogResult == DialogResult.Yes)
            {
                foreach (var item in disksBox.CheckedItems.OfType<string>().ToList())
                {
                    disksBox.Items.Remove(item);
                    for (int x = 1; x <= 100; x++)
                    {
                        if (ini.IniReadValue("SearchPaths", x.ToString()) == item)
                        {
                            ini.IniWriteValue("SearchPaths", x.ToString(), "");
                            break;
                        }
                    }
                }
            }
        }

        private void Btn_selectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkboxFoundGames.Items.Count; i++)
            {
                checkboxFoundGames.SetItemChecked(i, true);
            }
        }

        private void Btn_deselectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkboxFoundGames.Items.Count; i++)
            {
                checkboxFoundGames.SetItemChecked(i, false);
            }
        }
    }
}