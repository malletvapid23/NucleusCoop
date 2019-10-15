using Nucleus.Gaming;
using Nucleus.Gaming.Coop;
using Nucleus.Gaming.Generic.Step;
using Nucleus.Gaming.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Nucleus.Gaming.Windows.Interop;
using WindowScrape.Constants;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Win32;
using System.Text;

namespace Nucleus.Coop
{
    /// <summary>
    /// Central UI class to the Nucleus Coop application
    /// </summary>
    public partial class MainForm : BaseForm
    {
        public string version = "v0.9.7.1 ALPHA";

        private Settings settingsForm = null;

        private int currentStepIndex;
        private bool formClosing;
        private ContentManager content;
        private IGameHandler handler;

        private GameManager gameManager;
        private Dictionary<UserGameInfo, GameControl> controls;

        private SearchDisksForm form;

        private GameControl currentControl;
        private UserGameInfo currentGameInfo;
        private GenericGameInfo currentGame;
        private GameProfile currentProfile;
        private bool noGamesPresent;
        private List<UserInputControl> stepsList;
        private UserInputControl currentStep;

        private PositionsControl positionsControl;
        private PlayerOptionsControl optionsControl;
        private JSUserInputControl jsControl;

        private int KillProcess_HotkeyID = 1;
        private int TopMost_HotkeyID = 2;
        private int StopSession_HotkeyID = 3;

        private readonly IniFile ini = new Gaming.IniFile(Path.Combine(Directory.GetCurrentDirectory(), "Settings.ini"));

        private Thread handlerThread;

        private bool TopMostToggle = true;

        private enum ShowWindowEnum
        {
            Hide = 0,
            ShowNormal = 1, ShowMinimized = 2, ShowMaximized = 3,
            Maximize = 3, ShowNormalNoActivate = 4, Show = 5,
            Minimize = 6, ShowMinNoActivate = 7, ShowNoActivate = 8,
            Restore = 9, ShowDefault = 10, ForceMinimized = 11
        };

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);

        public MainForm()
        {
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                MessageBox.Show("Nucleus Coop is already running.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                this.Close();
                return;
            }

            InitializeComponent();

            if (ini.IniReadValue("Advanced", "Font") != "")
            {
                btn_gameOptions.Font = new Font("Segoe UI", float.Parse((ini.IniReadValue("Advanced", "Font")))-1.75f);
            }
                
            sideInfoLbl.Text = "Modded by ZeroFox" + "\n" + version;

            positionsControl = new PositionsControl();
            Settings settingsForm = new Settings(this, positionsControl);

            positionsControl.Paint += PositionsControl_Paint;

            settingsForm.RegHotkeys(this);

            controls = new Dictionary<UserGameInfo, GameControl>();
            gameManager = new GameManager();

            optionsControl = new PlayerOptionsControl();
            jsControl = new JSUserInputControl();

            positionsControl.OnCanPlayUpdated += StepCanPlay;
            optionsControl.OnCanPlayUpdated += StepCanPlay;
            jsControl.OnCanPlayUpdated += StepCanPlay;

            // selects the list of games, so the buttons look equal
            list_Games.Select();

            //MessageBox.Show("list games: " + list_Games.Height + "\nform: " + this.Height + "\nbtnsearhc y coord: " + btnSearch.Location.Y + "\nsteppanel: " + StepPanel.Height);

            //list_Games.AutoScroll = false;
            //int vertScrollWidth = SystemInformation.VerticalScrollBarWidth;
            //list_Games.Padding = new Padding(0, 0, vertScrollWidth, 0);
        }

        private void PositionsControl_Paint(object sender, PaintEventArgs e)
        {
            if (positionsControl.isDisconnected)
            {
                DPIManager.ForceUpdate();
                positionsControl.isDisconnected = false;
            }
        }

        protected override Size DefaultSize
        {
            get
            {
                return new Size(1070, 740);
            }
        }

        //protected override void OnGotFocus(EventArgs e)
        //{
        //    base.OnGotFocus(e);
        //    //this.TopMost = true;
        //    this.BringToFront();

        //    System.Diagnostics.Debug.WriteLine("Got Focus");
        //}
        //private void CheckForManualExit()
        //{
        //    while (true)
        //    {
        //        //you need to use Invoke because the new thread can't access the UI elements directly
        //        MethodInvoker mi = delegate () { if (handler == null) { GoToStep(0); this.Controls.Clear(); this.InitializeComponent(); RefreshGames(); t.Abort(); } };
        //        this.Invoke(mi);
        //        Thread.Sleep(500);
        //    }
        //}

        //protected override void OnDeactivate(EventArgs e)
        //{
        //    if (t != null && t.IsAlive)
        //        t.Abort();
        //}

        //protected override void OnActivated(EventArgs e)
        //{

        //    if (btn_Play.Text == "S T O P")
        //    {
        //        if (handler == null)
        //        {
        //            ////MessageBox.Show("handle not null and has ended");
        //            ////SetBtnToPlay();
        //            //if (handlerThread != null)
        //            //{
        //            //    handlerThread.Abort();
        //            //    handlerThread = null;
        //            //}
        //            ////list_Games_SelectedChanged(null, null);
        //            //RefreshGames();
        //            //Invoke(new Action(SetBtnToPlay));
        //            //btn_Play.Enabled = false;
        //            GoToStep(0);
        //            this.Controls.Clear();
        //            this.InitializeComponent();
        //            RefreshGames();
        //        }
        //        else
        //        {
        //            //btn_Play.Enabled = false;
        //            t = new System.Threading.Thread(CheckForManualExit);
        //            t.Start();
        //        }
        //    }
        //}

        protected override void WndProc(ref Message m)
        {
            //int msg = m.Msg;
            //LogManager.Log(msg.ToString());
            if (m.Msg == 0x0312 && m.WParam.ToInt32() == KillProcess_HotkeyID)
            {
                //System.Diagnostics.Process.GetCurrentProcess().Kill();
                User32Util.ShowTaskBar();
                Close();
            }
            else if (m.Msg == 0x0312 && m.WParam.ToInt32() == TopMost_HotkeyID)
            {
                if (TopMostToggle && handler != null)
                {
                    try
                    {
                        //Process[] procs = Process.GetProcesses();
                        //foreach (Process proc in procs)
                        //{
                        //    if (proc.ProcessName.ToLower() == Path.GetFileNameWithoutExtension(currentGame.Hook.For gen.ExecutableName.ToLower()) || attachedIds.Contains(proc.Id) || proc.MainWindowTitle == gen.Hook.ForceFocusWindowName)
                        //    {
                        //    }
                        //}
                        Process[] procs = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(currentGame.ExecutableName.ToLower()));
                        if (procs.Length > 0)
                        {
                            for (int i = 0; i < procs.Length; i++)
                            {
                                IntPtr hWnd = procs[i].MainWindowHandle;
                                User32Interop.SetWindowPos(hWnd, new IntPtr(-2), 0, 0, 0, 0, (uint)(PositioningFlags.SWP_NOSIZE | PositioningFlags.SWP_NOMOVE));
                                ShowWindow(hWnd, ShowWindowEnum.Minimize);
                            }
                        }
                    }
                    catch { }
                    User32Util.ShowTaskBar();
                    //currentGame.LockMouse = false;
                    this.Activate();
                    this.BringToFront();
                    TopMostToggle = false;
                }
                else if (!TopMostToggle && handler != null)
                {
                    Process[] procs = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(currentGame.ExecutableName.ToLower()));
                    if (procs.Length > 0)
                    {
                        for (int i = 0; i < procs.Length; i++)
                        {
                            IntPtr hWnd = procs[i].MainWindowHandle;
                            ShowWindow(hWnd, ShowWindowEnum.Restore);
                            User32Interop.SetWindowPos(hWnd, new IntPtr(-1), 0, 0, 0, 0, (uint)(PositioningFlags.SWP_NOSIZE | PositioningFlags.SWP_NOMOVE));
                        }
                    }
                    User32Util.HideTaskbar();
                    //currentGame.LockMouse = true;
                    //this.Activate();
                    //this.BringToFront();
                    TopMostToggle = true;
                }
            }
            else if (m.Msg == 0x0312 && m.WParam.ToInt32() == StopSession_HotkeyID)
            {
                if (btn_Play.Text == "S T O P")
                {
                    WindowState = FormWindowState.Normal;
                    this.BringToFront();
                    btn_Play.PerformClick();
                }

            }
            base.WndProc(ref m);
        }

        public void RefreshGames()
        {
            lock (controls)
            {
                foreach (var con in controls)
                {
                    if (con.Value != null)
                    {
                        con.Value.Dispose();
                    }
                }
                this.list_Games.Controls.Clear();
                controls.Clear();

                List<UserGameInfo> games = gameManager.User.Games;
                for (int i = 0; i < games.Count; i++)
                {
                    UserGameInfo game = games[i];
                    NewUserGame(game);
                }

                if (games.Count == 0)
                {
                    noGamesPresent = true;
                    GameControl con = new GameControl(null, null);
                    con.Width = list_Games.Width;
                    con.Text = "No games";
                    this.list_Games.Controls.Add(con);
                }
            }

            DPIManager.ForceUpdate();
            GameManager.Instance.SaveUserProfile();
        }

        public void NewUserGame(UserGameInfo game)
        {
            if (game.Game == null || !game.IsGamePresent())
            {
                return;
            }

            if (noGamesPresent)
            {
                noGamesPresent = false;
                RefreshGames();
                return;
            }

            GameControl con = new GameControl(game.Game, game);
            con.Width = list_Games.Width;

            controls.Add(game, con);
            this.list_Games.Controls.Add(con);

            ThreadPool.QueueUserWorkItem(GetIcon, game);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            RefreshGames();

            DPIManager.ForceUpdate();
        }

        private void GetIcon(object state)
        {
            UserGameInfo game = (UserGameInfo)state;
            Bitmap bmp;
            string iconPath = ini.IniReadValue("GameIcons", game.Game.GameName);
            if (!string.IsNullOrEmpty(iconPath))
            {
                if (iconPath.EndsWith(".exe"))
                {
                    Icon icon = Shell32.GetIcon(iconPath, false);
                    bmp = icon.ToBitmap();
                    icon.Dispose();
                }
                else
                {
                    bmp = new Bitmap(iconPath);
                }
            }
            else
            {
                Icon icon = Shell32.GetIcon(game.ExePath, false);
                bmp = icon.ToBitmap();
                icon.Dispose();
            }

            game.Icon = bmp;

            lock (controls)
            {
                if (controls.ContainsKey(game))
                {
                    GameControl control = controls[game];
                    control.Invoke((Action)delegate ()
                    {
                        control.Image = game.Icon;
                    });
                }
            }
        }

        private void list_Games_SelectedChanged(object arg1, Control arg2)
        {
            currentControl = (GameControl)arg1;
            currentGameInfo = currentControl.UserGameInfo;
            if (currentGameInfo == null)
            {
                btn_gameOptions.Visible = false;
                return;
            }

            StepPanel.Visible = true;

            currentGame = currentGameInfo.Game;

            btn_Play.Enabled = false;

            stepsList = new List<UserInputControl>();
            stepsList.Add(positionsControl);
            stepsList.Add(optionsControl);
            for (int i = 0; i < currentGame.CustomSteps.Count; i++)
            {
                stepsList.Add(jsControl);
            }

            currentProfile = new GameProfile();
            currentProfile.InitializeDefault(currentGame);

            gameNameControl.GameInfo = currentGameInfo;

            btn_gameOptions.Left = 384;

            //btn_gameOptions.Location = new Point(384 + (gameNameControl.Width - 100), 39);
            btn_gameOptions.Left += gameNameControl.Width - 100;
            btn_gameOptions.Visible = true;

            if (content != null)
            {
                content.Dispose();
            }

            // contnet manager is shared withing the same game
            content = new ContentManager(currentGame);
            GoToStep(0);
        }

        private void EnablePlay()
        {
            btn_Play.Enabled = true;
        }

        private void StepCanPlay(UserControl obj, bool canProceed, bool autoProceed)
        {
            if (!canProceed)
            {
                btn_Next.Enabled = false;
                return;
            }

            if (currentStepIndex + 1 > stepsList.Count - 1)
            {
                EnablePlay();
                return;
            }

            if (autoProceed)
            {
                GoToStep(currentStepIndex + 1);
            }
            else
            {
                btn_Next.Enabled = true;
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            GoToStep(currentStepIndex + 1);
        }

        private void KillCurrentStep()
        {
            currentStep?.Ended();
            this.StepPanel.Controls.Clear();
        }

        private void GoToStep(int step)
        {
            btnBack.Enabled = step > 0;
            if (step >= stepsList.Count)
            {
                return;
            }

            if (step >= 2)
            {
                // Custom steps
                List<CustomStep> customSteps = currentGame.CustomSteps;
                int customStepIndex = step - 2;
                CustomStep customStep = customSteps[0];

                if (customStep.UpdateRequired != null)
                {
                    customStep.UpdateRequired();
                }

                if (customStep.Required)
                {
                    jsControl.CustomStep = customStep;
                    jsControl.Content = content;
                }
                else
                {
                    EnablePlay();
                    return;
                }
            }

            KillCurrentStep();

            currentStepIndex = step;
            currentStep = stepsList[step];
            currentStep.Size = StepPanel.Size;
            currentStep.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;

            currentStep.Initialize(currentGameInfo, currentProfile);

            btn_Next.Enabled = currentStep.CanProceed && step != stepsList.Count - 1;

            StepPanel.Controls.Add(currentStep);
            currentStep.Size = StepPanel.Size; // for some reason this line must exist or the PositionsControl get messed up

            label_StepTitle.Text = currentStep.Title;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            formClosing = true;
            //if (handler.FakeFocus != null)
            //{
            //    handler.FakeFocus.Abort();
            //}
            if (handler != null)
            {
                Log("OnFormClosed method calling Handler End function");
                handler.End();
            }
            User32Util.ShowTaskBar();
        }

        private void btn_Play_Click(object sender, EventArgs e)
        {
            if (btn_Play.Text == "S T O P")
            {
                try
                {
                    if (handler.FakeFocus != null)
                    {
                        handler.FakeFocus.Abort();
                    }
                    if (handler != null)
                    {
                        Log("Stop button clicked, calling Handler End function");
                        handler.End();
                    }

                }
                catch { }
                User32Util.ShowTaskBar();
                SetBtnToPlay();
                btn_Play.Enabled = false;
                //this.Controls.Clear();
                //this.InitializeComponent();
                RefreshGames();
                GoToStep(0);

                return;
            }

            btn_Play.Text = "S T O P";
            btnBack.Enabled = false;

            handler = gameManager.MakeHandler(currentGame);
            handler.Initialize(currentGameInfo, GameProfile.CleanClone(currentProfile));
            handler.Ended += handler_Ended;

            gameManager.Play(handler);
            if (handler.TimerInterval > 0)
            {
                handlerThread = new Thread(UpdateGameManager);
                handlerThread.Start();
            }

            if (currentGame.HideTaskbar)
            {
                User32Util.HideTaskbar();
            }

            if (currentGame.HideDesktop)
            {
                foreach (Screen screen in Screen.AllScreens)
                {
                    System.Windows.Forms.Form hform = new System.Windows.Forms.Form();
                    hform.BackColor = Color.Black;
                    hform.Location = new Point(0, 0);
                    hform.Size = screen.WorkingArea.Size;
                    this.Size = screen.WorkingArea.Size;
                    hform.FormBorderStyle = FormBorderStyle.None;
                    hform.StartPosition = FormStartPosition.Manual;
                    //hform.TopMost = true;
                    hform.Show();
                }
            }

            WindowState = FormWindowState.Minimized;
        }

        private void SetBtnToPlay()
        {
            //btn_Play.Visible = true;
            btn_Play.Text = "P L A Y";
        }

        private void handler_Ended()
        {
            User32Util.ShowTaskBar();
            handler = null;
            if (handlerThread != null)
            {
                handlerThread.Abort();
                handlerThread = null;
            }
            Invoke(new Action(SetBtnToPlay));
        }

        private void UpdateGameManager(object state)
        {
            for (; ; )
            {
                try
                {
                    if (gameManager == null || formClosing || handler == null)
                    {
                        break;
                    }

                    string error = gameManager.Error;
                    if (!string.IsNullOrEmpty(error))
                    {
                        MessageBox.Show(error, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        handler_Ended();
                        return;
                    }

                    handler.Update(handler.TimerInterval);
                    Thread.Sleep(TimeSpan.FromMilliseconds(handler.TimerInterval));
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch { }
            }
        }

        private void arrow_Back_Click(object sender, EventArgs e)
        {
            currentStepIndex--;
            if (currentStepIndex < 0)
            {
                currentStepIndex = 0;
                return;
            }
            GoToStep(currentStepIndex);
        }

        private void arrow_Next_Click(object sender, EventArgs e)
        {
            currentStepIndex = Math.Min(currentStepIndex++, stepsList.Count - 1);
            GoToStep(currentStepIndex);
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog open = new OpenFileDialog())
            {
                open.Filter = "Game Executable Files|*.exe";
                if (open.ShowDialog() == DialogResult.OK)
                {
                    string path = open.FileName;

                    List<GenericGameInfo> info = gameManager.GetGames(path);

                    if (info.Count > 1)
                    {
                        GameList list = new GameList(info);
                        DPIManager.ForceUpdate();

                        if (list.ShowDialog() == DialogResult.OK)
                        {
                            UserGameInfo game = gameManager.TryAddGame(path, list.Selected);

                            //if (game == null)
                            //{
                            //    MessageBox.Show("Game already in your library!");
                            //}
                            //else
                            //{

                            //if (GameManager.Instance.User.Games.Any(c => c.ExePath.ToLower() == path.ToLower()))
                            //{
                            //    DialogResult dialogResult = MessageBox.Show("This game's executable is already in your library. Do you wish to add it anyway?\n\nExecutable Path: " + path, "Already exists", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                            //    if (dialogResult == DialogResult.Yes)
                            //    {
                            if (game != null)
                            {
                                MessageBox.Show("Game accepted as " + game.Game.GameName);
                                RefreshGames();
                            }

                            //}
                            //}
                            //}
                        }
                    }
                    else if (info.Count == 1)
                    {
                        UserGameInfo game = gameManager.TryAddGame(path, info[0]);
                        MessageBox.Show("Game accepted as " + game.Game.GameName);
                        RefreshGames();
                    }
                    else
                    {
                        MessageBox.Show(string.Format("The executable '{0}' was not found in any game script's Game.ExecutableName field. Game has not been added.", Path.GetFileName(path)), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnAutoSearch_Click(object sender, EventArgs e)
        {
            if (form != null)
            {
                return;
            }

            form = new SearchDisksForm(this);
            //DPIManager.AddForm(form);

            form.FormClosed += Form_FormClosed;
            form.Show();
            SetUpForm(form);

            DPIManager.ForceUpdate();
        }

        private void Form_FormClosed(object sender, FormClosedEventArgs e)
        {
            User32Util.ShowTaskBar();
            form = null;
        }

        private void btnShowTaskbar_Click(object sender, EventArgs e)
        {
            User32Util.ShowTaskBar();
        }

        private void SettingsBtn_Click(object sender, EventArgs e)
        {
            settingsForm = new Settings(this, positionsControl);
            settingsForm.Show();
        }

        private void DetailsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GetDetails();
        }

        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteGame();
        }

        private void GetDetails()
        {
            string userProfile = gameManager.GetUserProfilePath();

            if (File.Exists(userProfile))
            {
                string jsonString = File.ReadAllText(userProfile);
                JObject jObject = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString) as JObject;

                JArray games = jObject["Games"] as JArray;
                for (int i = 0; i < games.Count; i++)
                {
                    string gameGuid = jObject["Games"][i]["GameGuid"].ToString();
                    string profiles = jObject["Games"][i]["Profiles"].ToString();
                    string exePath = jObject["Games"][i]["ExePath"].ToString();

                    if (gameGuid == currentGameInfo.GameGuid && exePath == currentGameInfo.ExePath)
                    {
                        MessageBox.Show(string.Format("Game Name: {0}\nSteam ID: {1}\nProfiles: {2}\nExe Path: {3}\nScript Filename: {4}\nNucleus Game Data Path: {5}", currentGameInfo.Game.GameName, currentGameInfo.Game.SteamID, profiles, exePath, currentGameInfo.Game.JsFileName, Path.Combine(gameManager.GetAppDataPath(), gameGuid)), "Game Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void DeleteGame()
        {
            string userProfile = gameManager.GetUserProfilePath();

            if (File.Exists(userProfile))
            {
                string jsonString = File.ReadAllText(userProfile);
                JObject jObject = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString) as JObject;

                JArray games = jObject["Games"] as JArray;
                for (int i = 0; i < games.Count; i++)
                {
                    string gameGuid = jObject["Games"][i]["GameGuid"].ToString();
                    string profiles = jObject["Games"][i]["Profiles"].ToString();
                    string exePath = jObject["Games"][i]["ExePath"].ToString();

                    if (gameGuid == currentGameInfo.GameGuid && exePath == currentGameInfo.ExePath)
                    {
                        DialogResult dialogResult = MessageBox.Show("Are you sure you want to delete " + currentGameInfo.Game.GameName + "?", "Confirm deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (dialogResult == DialogResult.Yes)
                        {
                            gameManager.User.Games.RemoveAt(i);
                            jObject["Games"][i].Remove();
                            string output = JsonConvert.SerializeObject(jObject, Formatting.Indented);
                            File.WriteAllText(userProfile, output);
                            //this.Controls.Clear();
                            //this.InitializeComponent();
                            RefreshGames();
                        }
                    }
                }
            }
        }

        private void GameContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Control selectedControl = FindControlAtCursor(this);

            if (selectedControl.GetType() == typeof(Label) || selectedControl.GetType() == typeof(PictureBox))
            {
                selectedControl = selectedControl.Parent;
            }

            foreach (Control c in selectedControl.Controls)
            {
                if (c is Label)
                {
                    if (c.Text == "No games")
                    {
                        gameContextMenuStrip.Items[0].Text = "No game selected...";
                        for (int i = 1; i < gameContextMenuStrip.Items.Count; i++)
                        {
                            gameContextMenuStrip.Items[i].Visible = false;
                        }
                        return;
                    }
                }
            }

            if (selectedControl.GetType() == typeof(GameControl) || selectedControl.GetType() == typeof(Button))
            {
                bool btnClick = false;
                if (selectedControl.GetType() == typeof(GameControl))
                {
                    currentControl = (GameControl)selectedControl;
                    currentGameInfo = currentControl.UserGameInfo;
                    gameContextMenuStrip.Items[0].Visible = true;
                    gameContextMenuStrip.Items[2].Visible = true;
                }
                else
                {
                    btnClick = true;
                    gameContextMenuStrip.Items[0].Visible = false;
                }

                gameContextMenuStrip.Items[1].Visible = false;

                if (string.IsNullOrEmpty(currentGameInfo.GameGuid) || currentGameInfo == null)
                {
                    gameContextMenuStrip.Items[0].Text = "No game selected...";
                    for (int i = 1; i < gameContextMenuStrip.Items.Count; i++)
                    {
                        gameContextMenuStrip.Items[i].Visible = false;
                    }
                }
                else
                {
                    gameContextMenuStrip.Items[0].Text = currentGameInfo.Game.GameName;
                    for (int i = 1; i < gameContextMenuStrip.Items.Count; i++)
                    {
                        //if(i > 1 || (i == 1 && currentGameInfo.Game.Description?.Length > 0))
                        //{
                        //    gameContextMenuStrip.Items[i].Visible = true;
                        //}
                        gameContextMenuStrip.Items[i].Visible = true;
                        if (i == 1 && currentGameInfo.Game.Description == null)
                        {
                            gameContextMenuStrip.Items[i].Visible = false;
                            if (btnClick)
                            {
                                gameContextMenuStrip.Items[2].Visible = false;
                                i++;
                            }
                        }
                    }
                }
            }
            else
            {
                gameContextMenuStrip.Items[0].Text = "No game selected...";
                for (int i = 1; i < gameContextMenuStrip.Items.Count; i++)
                {
                    gameContextMenuStrip.Items[i].Visible = false;
                }
            }
        }

        public static Control FindControlAtPoint(Control container, Point pos)
        {
            Control child;
            foreach (Control c in container.Controls)
            {
                if (c.Visible && c.Bounds.Contains(pos))
                {
                    child = FindControlAtPoint(c, new Point(pos.X - c.Left, pos.Y - c.Top));
                    if (child == null) return c;
                    else return child;
                }
            }
            return null;
        }

        public static Control FindControlAtCursor(MainForm form)
        {
            Point pos = Cursor.Position;
            if (form.Bounds.Contains(pos))
                return FindControlAtPoint(form, form.PointToClient(pos));
            return null;
        }

        private void OpenRawGameScript()
        {
            var nppDir = (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Notepad++", null, null);
            if (nppDir != null)
            {
                var nppExePath = Path.Combine(nppDir, "Notepad++.exe");
                var nppReadmePath = Path.Combine(nppDir, "readme.txt");
                var sb = new StringBuilder();
                sb.AppendFormat("\"{0}\" -n{1}", nppReadmePath);
                Process.Start(nppExePath, sb.ToString());
            }
            else
            {
                Process.Start("notepad.exe", Path.Combine(gameManager.GetJsGamesPath(), currentGameInfo.Game.JsFileName));
            }
        }

        private void OpenScriptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenRawGameScript();
        }

        private void OpenDataFolder()
        {
            string path = Path.Combine(gameManager.GetAppDataPath(), currentGameInfo.Game.GUID);
            if (Directory.Exists(path))
            {
                Process.Start(path);
            }
            else
            {
                MessageBox.Show("No data present for this game.", "No data found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenDataFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenDataFolder();
        }

        private void ChangeIconToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Open Image";
                dlg.Filter = "All Images Files (*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.tiff;*.tif;*.ico;*.exe)|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.tiff;*.tif;*.ico;*.exe" +
                            "|PNG Portable Network Graphics (*.png)|*.png" +
                            "|JPEG File Interchange Format (*.jpg *.jpeg *jfif)|*.jpg;*.jpeg;*.jfif" +
                            "|BMP Windows Bitmap (*.bmp)|*.bmp" +
                            "|TIF Tagged Imaged File Format (*.tif *.tiff)|*.tif;*.tiff" +
                            "|Icon (*.ico)|*.ico" +
                            "|Executable (*.exe)|*.exe";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    //PictureBox PictureBox1 = new PictureBox();

                    // Create a new Bitmap object from the picture file on disk,
                    // and assign that to the PictureBox.Image property
                    //PictureBox1.Image = new Bitmap(dlg.FileName);

                    if (dlg.FileName.EndsWith(".exe"))
                    {
                        Icon icon = Shell32.GetIcon(dlg.FileName, false);

                        Bitmap bmp = icon.ToBitmap();
                        icon.Dispose();
                        currentGameInfo.Icon = bmp;
                    }
                    else
                    {
                        currentGameInfo.Icon = new Bitmap(dlg.FileName);
                    }
                    ini.IniWriteValue("GameIcons", currentGameInfo.Game.GameName, dlg.FileName);

                    GetIcon(currentGameInfo);
                    //this.Controls.Clear();
                    //this.InitializeComponent();
                    RefreshGames();
                }
            }
        }

        private void Log(string logMessage)
        {
            if (ini.IniReadValue("Misc", "DebugLog") == "True")
            {
                using (StreamWriter writer = new StreamWriter("debug-log.txt", true))
                {
                    writer.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}]MAIN: {logMessage}");
                    writer.Close();
                }
            }
        }

        private void ScriptNotesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(currentGameInfo.Game.Description, "Script Author Notes", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Button btnSender = (Button)sender;
            Point ptLowerLeft = new Point(0, btnSender.Height);
            ptLowerLeft = btnSender.PointToScreen(ptLowerLeft);
            gameContextMenuStrip.Show(ptLowerLeft);
        }
    }
}
