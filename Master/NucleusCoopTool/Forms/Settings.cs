using Nucleus.Gaming.Windows.Interop;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Nucleus.Gaming;
using System.Text.RegularExpressions;
using SharpDX.DirectInput;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Nucleus.Gaming.Coop.InputManagement;
using System.Management;
using NAudio.CoreAudioApi;

namespace Nucleus.Coop
{
    public partial class Settings : BaseForm
    {
        private readonly IniFile ini = new Gaming.IniFile(Path.Combine(Directory.GetCurrentDirectory(), "Settings.ini"));

        private MainForm mainForm = null;
        private PositionsControl positionsControl;

        public int KillProcess_HotkeyID = 1;
        public int TopMost_HotkeyID = 2;
        public int StopSession_HotkeyID = 3;

        private TextBox[] controllerGuids;
        private TextBox[] controllerNicks;

        //private List<string> audioDevices;
        private IDictionary<string, string> audioDevices;

        private DirectInput dinput;

        public Settings(MainForm mf, PositionsControl pc)
        {
            InitializeComponent();

            DPIManager.AddForm(this);
            DPIManager.ForceUpdate();

            Invalidate();

            controllerGuids = new TextBox[] { controllerOneGuid, controllerTwoGuid, controllerThreeGuid, controllerFourGuid, controllerFiveGuid, controllerSixGuid, controllerSevenGuid, controllerEightGuid, controllerNineGuid, controllerTenGuid, controllerElevenGuid, controllerTwelveGuid, controllerThirteenGuid, controllerFourteenGuid, controllerFifteenGuid, controllerSixteenGuid };
            controllerNicks = new TextBox[] { controllerOneNick, controllerTwoNick, controllerThreeNick, controllerFourNick, controllerFiveNick, controllerSixNick, controllerSevenNick, controllerEightNick, controllerNineNick, controllerTenNick, controllerElevenNick, controllerTwelveNick, controllerThirteenNick, controllerFourteenNick, controllerFifteenNick, controllerSixteenNick };

            mainForm = mf as MainForm;
            positionsControl = pc;

            RefreshCmbNetwork();
            if (ini.IniReadValue("Misc", "Network") != "")
            {
                cmb_Network.Text = ini.IniReadValue("Misc", "Network");
            }
            else
            {
                cmb_Network.SelectedIndex = 0;
            }

            if (ini.IniReadValue("Misc", "SteamLang") != "")
            {
                cmb_Lang.Text = ini.IniReadValue("Misc", "SteamLang");
            }
            else
            {
                cmb_Lang.SelectedIndex = 0;
            }

            //Hotkeys
            if (ini.IniReadValue("Hotkeys", "Close").Contains('+'))
            {
                string[] closeHk = ini.IniReadValue("Hotkeys", "Close").Split('+');
                if ((closeHk[0] == "Ctrl" || closeHk[0] == "Alt" || closeHk[0] == "Shift") && closeHk[1].Length == 1 && Regex.IsMatch(closeHk[1], @"^[a-zA-Z0-9]+$"))
                {
                    settingsCloseCmb.SelectedItem = closeHk[0];
                    settingsCloseHKTxt.Text = closeHk[1];
                }
            }
            else
            {
                ini.IniWriteValue("Hotkeys", "Close", "");
            }

            if (ini.IniReadValue("Hotkeys", "Stop").Contains('+'))
            {
                string[] stopHk = ini.IniReadValue("Hotkeys", "Stop").Split('+');
                if ((stopHk[0] == "Ctrl" || stopHk[0] == "Alt" || stopHk[0] == "Shift") && stopHk[1].Length == 1 && Regex.IsMatch(stopHk[1], @"^[a-zA-Z0-9]+$"))
                {
                    settingsStopCmb.SelectedItem = stopHk[0];
                    settingsStopTxt.Text = stopHk[1];
                }
            }
            else
            {
                ini.IniWriteValue("Hotkeys", "Stop", "");
            }

            if (ini.IniReadValue("Hotkeys", "TopMost").Contains('+'))
            {
                string[] topHk = ini.IniReadValue("Hotkeys", "TopMost").Split('+');
                if ((topHk[0] == "Ctrl" || topHk[0] == "Alt" || topHk[0] == "Shift") && topHk[1].Length == 1 && Regex.IsMatch(topHk[1], @"^[a-zA-Z0-9]+$"))
                {
                    settingsTopCmb.SelectedItem = topHk[0];
                    settingsTopTxt.Text = topHk[1];
                }
            }

            //Controll
            GetControllers();

            if (ini.IniReadValue("ControllerMapping", "Keyboard") != "")
            {
                keyboardNick.Text = ini.IniReadValue("ControllerMapping", "Keyboard");
            }

            //Custom Layout
            //if (ini.IniReadValue("CustomLayout", "Enabled") != "")
            //{
            //    mutexLogCheck.Checked = Boolean.Parse(ini.IniReadValue("CustomLayout", "Enabled"));
            //}
            if (ini.IniReadValue("CustomLayout", "HorizontalLines") != "")
            {
                numHorDiv.Value = int.Parse(ini.IniReadValue("CustomLayout", "HorizontalLines"));
            }
            if (ini.IniReadValue("CustomLayout", "VerticalLines") != "")
            {
                numVerDiv.Value = int.Parse(ini.IniReadValue("CustomLayout", "VerticalLines"));
            }
            if (ini.IniReadValue("CustomLayout", "MaxPlayers") != "")
            {
                numMaxPlyrs.Value = int.Parse(ini.IniReadValue("CustomLayout", "MaxPlayers"));
            }

            //Misc
            if (ini.IniReadValue("Misc", "UseNicksInGame") != "")
            {
                useNicksCheck.Checked = Boolean.Parse(ini.IniReadValue("Misc", "UseNicksInGame"));
            }

            if (ini.IniReadValue("Misc", "DebugLog") != "")
            {
                debugLogCheck.Checked = Boolean.Parse(ini.IniReadValue("Misc", "DebugLog"));
            }

            if (ini.IniReadValue("Misc", "ShowStatus") != "")
            {
                statusCheck.Checked = Boolean.Parse(ini.IniReadValue("Misc", "ShowStatus"));
            }

            if (ini.IniReadValue("Misc", "KeepAccounts") != "")
            {
                keepAccountsCheck.Checked = Boolean.Parse(ini.IniReadValue("Misc", "KeepAccounts"));
            }

            if (ini.IniReadValue("Misc", "NucleusAccountPassword") != "")
            {
                nucUserPassTxt.Text = ini.IniReadValue("Misc", "NucleusAccountPassword");
            }

            if (ini.IniReadValue("Audio", "Custom") == "0")
            {
                audioDefaultSettingsRadio.Checked = true;
                audioCustomSettingsBox.Enabled = false;
            }
            else
            {
                audioCustomSettingsRadio.Checked = true;
            }

            //if (ini.IniReadValue("Misc", "VibrateOpen") != "")
            //{
            //    check_Vibrate.Checked = Boolean.Parse(ini.IniReadValue("Misc", "VibrateOpen"));
            //}

            RefreshAudioList();
        }

        public Settings()
        {
            InitializeComponent();
            DPIManager.AddForm(this);
            DPIManager.ForceUpdate();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            DPIManager.ForceUpdate();
        }

        private void GetControllers()
        {
            foreach (TextBox tbox in controllerGuids)
            {
                tbox.Clear();
            }

            foreach (TextBox tbox in controllerNicks)
            {
                tbox.Clear();
            }


            dinput = new DirectInput();
            IList<DeviceInstance> devices = dinput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
            int gcDevicesCnt = devices.Count;
            for (int i = 0; i < devices.Count; i++)
            {
                DeviceInstance device = devices[i];
                Joystick gamePad = new Joystick(dinput, device.InstanceGuid);
                string hid = gamePad.Properties.InterfacePath;
                int start = hid.IndexOf("hid#");
                int end = hid.LastIndexOf("#{");
                string fhid = hid.Substring(start, end - start).Replace('#', '\\').ToUpper();
                controllerGuids[i].Text = fhid;
                if (ini.IniReadValue("ControllerMapping", fhid) != "")
                {
                    controllerNicks[i].Text = ini.IniReadValue("ControllerMapping", fhid);
                }
                gamePad.Dispose();
            }
            dinput.Dispose();
            
            
            foreach (var device in RawInputManager.GetDeviceList().Where(x => x.deviceInfo.dwType <= 1))
            {

                //string hid = device.deviceInfo.hid.ToString();
                //int start = hid.IndexOf("hid#");
                //int end = hid.LastIndexOf("#{");
                //string fhid = hid.Substring(start, end - start).Replace('#', '\\').ToUpper();

                //controllerGuids[gcDevicesCnt].Text = fhid;
                string did = string.Empty;
                if(device.deviceInfo.dwType == 0)
                {
                    did = "T" + device.deviceInfo.dwType + "PID" + device.deviceInfo.hid.dwProductId + "VID" + device.deviceInfo.hid.dwVendorId + "VN" + device.deviceInfo.hid.dwVersionNumber;
                    controllerGuids[gcDevicesCnt].Text = did;
                    gcDevicesCnt++;
                }
                

                if (ini.IniReadValue("ControllerMapping", did) != "")
                {
                    controllerNicks[gcDevicesCnt].Text = ini.IniReadValue("ControllerMapping", did);
                }
            }
        }

        private void SettingsSaveBtn_Click(object sender, EventArgs e)
        {
            try
            {
                ini.IniWriteValue("Hotkeys", "Close", settingsCloseCmb.SelectedItem.ToString() + "+" + settingsCloseHKTxt.Text);
                ini.IniWriteValue("Hotkeys", "Stop", settingsStopCmb.SelectedItem.ToString() + "+" + settingsStopTxt.Text);
                ini.IniWriteValue("Hotkeys", "TopMost", settingsTopCmb.SelectedItem.ToString() + "+" + settingsTopTxt.Text);

                User32Interop.UnregisterHotKey(mainForm.Handle, KillProcess_HotkeyID);
                User32Interop.UnregisterHotKey(mainForm.Handle, TopMost_HotkeyID);
                User32Interop.UnregisterHotKey(mainForm.Handle, StopSession_HotkeyID);

                //RegHotkeys(KillProcess_HotkeyID, GetMod(settingsCloseCmb.SelectedItem.ToString()), (int)Enum.Parse(typeof(Keys), settingsCloseHKTxt.Text));
                //RegHotkeys(TopMost_HotkeyID, GetMod(settingsTopCmb.SelectedItem.ToString()), (int)Enum.Parse(typeof(Keys), settingsTopTxt.Text));
                //RegHotkeys(StopSession_HotkeyID, GetMod(settingsStopCmb.SelectedItem.ToString()), (int)Enum.Parse(typeof(Keys), settingsStopTxt.Text));

                User32Interop.RegisterHotKey(mainForm.Handle, KillProcess_HotkeyID, GetMod(ini.IniReadValue("Hotkeys", "Close").Split('+')[0].ToString()), (int)Enum.Parse(typeof(Keys), ini.IniReadValue("Hotkeys", "Close").Split('+')[1].ToString()));
                User32Interop.RegisterHotKey(mainForm.Handle, TopMost_HotkeyID, GetMod(ini.IniReadValue("Hotkeys", "TopMost").Split('+')[0].ToString()), (int)Enum.Parse(typeof(Keys), ini.IniReadValue("Hotkeys", "TopMost").Split('+')[1].ToString()));
                User32Interop.RegisterHotKey(mainForm.Handle, StopSession_HotkeyID, GetMod(ini.IniReadValue("Hotkeys", "Stop").Split('+')[0].ToString()), (int)Enum.Parse(typeof(Keys), ini.IniReadValue("Hotkeys", "Stop").Split('+')[1].ToString()));

                for(int i =0; i < controllerGuids.Length; i++)
                {
                    if (!string.IsNullOrEmpty(controllerGuids[i].Text)) //&& !string.IsNullOrEmpty(controllerNicks[i].Text))
                    {
                        ini.IniWriteValue("ControllerMapping", controllerGuids[i].Text, controllerNicks[i].Text);
                    }
                }
                if (positionsControl != null)
                {
                    positionsControl.Refresh();
                }

                if(!string.IsNullOrEmpty(keyboardNick.Text))
                {
                    ini.IniWriteValue("ControllerMapping", "Keyboard", keyboardNick.Text);
                }

                ini.IniWriteValue("Misc", "UseNicksInGame", useNicksCheck.Checked.ToString());
                ini.IniWriteValue("Misc", "DebugLog", debugLogCheck.Checked.ToString());
                ini.IniWriteValue("Misc", "Network", cmb_Network.SelectedItem.ToString());
                ini.IniWriteValue("Misc", "SteamLang", cmb_Lang.SelectedItem.ToString());
                ini.IniWriteValue("Misc", "ShowStatus", statusCheck.Checked.ToString());
                ini.IniWriteValue("Misc", "KeepAccounts", keepAccountsCheck.Checked.ToString());
                ini.IniWriteValue("Misc", "NucleusAccountPassword", nucUserPassTxt.Text);

                //ini.IniWriteValue("CustomLayout", "Enabled", enableCustomCheckbox.Checked.ToString());
                ini.IniWriteValue("CustomLayout", "HorizontalLines", numHorDiv.Value.ToString());
                ini.IniWriteValue("CustomLayout", "VerticalLines", numVerDiv.Value.ToString());
                ini.IniWriteValue("CustomLayout", "MaxPlayers", numMaxPlyrs.Value.ToString());

                if(audioDefaultSettingsRadio.Checked)
                {
                    ini.IniWriteValue("Audio", "Custom", 0.ToString());
                }
                else
                {
                    ini.IniWriteValue("Audio", "Custom", 1.ToString());
                }
                
                //ini.IniWriteValue("Misc", "VibrateOpen", check_Vibrate.Checked.ToString());

                foreach (Control ctrl in audioCustomSettingsBox.Controls)
                {
                    if (ctrl is ComboBox)
                    {
                        var cmb = (ComboBox)ctrl;
                        if(audioDevices?.Count > 0 && audioDevices.Keys.Contains(cmb.Text))
                        {
                            ini.IniWriteValue("Audio", cmb.Name, audioDevices[cmb.Text]);
                        }
                    }
                }

                MessageBox.Show("Settings saved succesfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.None);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, ex.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int GetMod(string modifier)
        {
            int mod = 0;
            switch(modifier)
            {
                case "Ctrl": // Ctrl
                    mod = 2;
                    break;
                case "Alt":
                    mod = 1;
                    break;
                case "Shift":
                    mod = 4;
                    break;
            }
            return mod;
        }

        private void SettingsCloseBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void SettingsCloseHKTxt_TextChanged(object sender, EventArgs e)
        {
            settingsCloseHKTxt.Text = string.Concat(settingsCloseHKTxt.Text.Where(char.IsLetterOrDigit));
        }

        public void RegHotkeys(MainForm form)
        {
            mainForm = form;

            try
            {
                User32Interop.RegisterHotKey(form.Handle, KillProcess_HotkeyID, GetMod(ini.IniReadValue("Hotkeys", "Close").Split('+')[0].ToString()), (int)Enum.Parse(typeof(Keys), ini.IniReadValue("Hotkeys", "Close").Split('+')[1].ToString()));
                User32Interop.RegisterHotKey(form.Handle, TopMost_HotkeyID, GetMod(ini.IniReadValue("Hotkeys", "TopMost").Split('+')[0].ToString()), (int)Enum.Parse(typeof(Keys), ini.IniReadValue("Hotkeys", "TopMost").Split('+')[1].ToString()));
                User32Interop.RegisterHotKey(form.Handle, StopSession_HotkeyID, GetMod(ini.IniReadValue("Hotkeys", "Stop").Split('+')[0].ToString()), (int)Enum.Parse(typeof(Keys), ini.IniReadValue("Hotkeys", "Stop").Split('+')[1].ToString()));
            }
            catch (Exception ex)
            {
                MessageBox.Show( "Error registering hotkeys " + ex.Message, ex.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        public void RegHotkeys(int id, int mod, int key)
        {
            User32Interop.RegisterHotKey(mainForm.Handle, id, mod, key);
        }

        private void SettingsCloseHKTxt_KeyPress(object sender, KeyPressEventArgs e)
        {
            settingsCloseHKTxt.Text = "";
            e.Handled = !char.IsLetter(e.KeyChar) && !char.IsControl(e.KeyChar)
             && !char.IsSeparator(e.KeyChar) && !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }

        private void SettingsStopTxt_KeyPress(object sender, KeyPressEventArgs e)
        {
            settingsStopTxt.Text = "";
            e.Handled = !char.IsLetter(e.KeyChar) && !char.IsControl(e.KeyChar)
             && !char.IsSeparator(e.KeyChar) && !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }

        private void SettingsTopTxt_KeyPress(object sender, KeyPressEventArgs e)
        {
            settingsTopTxt.Text = "";
            e.Handled = !char.IsLetter(e.KeyChar) && !char.IsControl(e.KeyChar)
             && !char.IsSeparator(e.KeyChar) && !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }

        private void Btn_Refresh_Click(object sender, EventArgs e)
        {
            GetControllers();
        }

        private void Btn_credits_Click(object sender, EventArgs e)
        {
            MessageBox.Show("NucleusCoop Mod - " + mainForm.version + 
                "\n" +
                "\nCredits" +
                "\n---------------------------------------------------------------------" +
                "\nOriginal NucleusCoop Project: Lucas Assis (lucasassislar)" +
                "\nMod: ZeroFox" +
                "\nMultiple keyboards/mice & hooks: Ilyaki" +
                "\nWebsite & handler API: r-mach" +
                "\n" +
                "\nAdditional credits to all original developers of third party utilities Nucleus uses:" +
                "\nMr_Goldberg (Goldberg Emulator), syahmixp (SmartSteamEmu), EJocys (x360ce), 0dd14 Lab (Xinput Plus), r1ch (ForceBindIP), HaYDeN (Flawless Widescreen), briankendall (devreorder), VerGreeneyes (DirectXWrapper), wizark952 (dinput8 blocker), Nemirtinga (Epic Emulator)" +
                "\n" +
                "\nThis mod brings further enhancements to NucleusCoop, such as:" +
                "\n- Huge increase to the amount of compabitle games" +
                "\n- Much more customization (via game scripts)" +
                "\n- Support for any number of players" +
                "\n- Quality of life improvements" +
                "\n- Bug fixes\n- And so much more!" +
                "\n" +
                "\nFull mod changelog in Mod-Readme.txt" +
                "\n" +
                "\nAll this wouldn't have been possible without Lucas. Thank you Lucas <3" +
                "\n" +
                "\nSpecial thanks to: Talos91, PoundlandBacon and the rest of the Splitscreen Dreams discord community.", "Credits",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }

        private void NumHorDiv_ValueChanged(object sender, EventArgs e)
        {
            tabPage4.Invalidate();
            tabControl2.TabPages[1].Invalidate();
            //if (numHorDiv.Value > 0 && numVerDiv.Value > 0)
            //{
            numMaxPlyrs.Value = (numHorDiv.Value + 1) * (numVerDiv.Value + 1);
            //}
            //else
            //{
            //    numMaxPlyrs.Value = 1;
            //}
        }

        private void NumVerDiv_ValueChanged(object sender, EventArgs e)
        {
            tabPage4.Invalidate();
            tabControl2.TabPages[1].Invalidate();
            //if (numHorDiv.Value > 0 && numVerDiv.Value > 0)
            //{
            numMaxPlyrs.Value = (numHorDiv.Value + 1) * (numVerDiv.Value + 1);
            //}
            //else
            //{
            //    numMaxPlyrs.Value = 1;
            //}
        }

        private void tabPage4_Paint(object sender, PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics gs = e.Graphics;

            Pen p = new Pen(new SolidBrush(Color.Black));

            Rectangle outline = new Rectangle(370, 45, 360, 240);
            gs.DrawRectangle(p, outline);

            int[] hlines = new int[(int)numHorDiv.Value];
            int[] vlines = new int[(int)numVerDiv.Value];

            for (int i = 0; i < (int)numHorDiv.Value; i++)
            {
                int divisions = (int)numHorDiv.Value + 1;

                //370-380
                int y = (240 / divisions);
                if (i == 0)
                {
                    hlines[i] = y + 45;
                }
                else
                {
                    hlines[i] = y + hlines[i - 1];
                }
                gs.DrawLine(p, 370, hlines[i], 370 + 360, hlines[i]);
            }

            for (int i = 0; i < (int)numVerDiv.Value; i++)
            {

                int divisions = (int)numVerDiv.Value + 1;

                //45-460
                int x = (360 / divisions);
                if (i == 0)
                {
                    vlines[i] = x + 370;
                }
                else
                {
                    vlines[i] = x + vlines[i - 1];
                }
                gs.DrawLine(p, vlines[i], 45, vlines[i], 45 + 240);
            }

            p.Dispose();
            gs.Dispose();
        }

        private void cmb_Network_DropDown(object sender, EventArgs e)
        {
            RefreshCmbNetwork();
        }

        private void RefreshCmbNetwork()
        {
            cmb_Network.Items.Clear();

            cmb_Network.Items.Add("Automatic");

            var ni = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface item in ni)
            {
                if (item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            cmb_Network.Items.Add(item.Name);
                        }
                    }
                }
            }
        }

        private void cmb_Network_DropDownClosed(object sender, EventArgs e)
        {
            if(cmb_Network.SelectedItem == null)
            {
                cmb_Network.SelectedIndex = 0;
            }
        }

        private void keepAccountsCheck_Click(object sender, EventArgs e)
        {
            if (!keepAccountsCheck.Checked)
            {
                DialogResult res = MessageBox.Show("Warning: by disabling this feature, the next time you run a game that uses different user accounts, all Nucleus-made user accounts (and their files, saves) will be deleted. Do you wish to proceed?", "Confirmation", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                if (res != DialogResult.OK)
                {
                    keepAccountsCheck.Checked = true;
                }
            }
        }

        private void audioCustomSettingsRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (audioCustomSettingsRadio.Checked)
            {
                audioCustomSettingsBox.Enabled = true;
            }
            else
            {
                audioCustomSettingsBox.Enabled = false;
            }
        }

        private void audioBox_DropDown(object sender, EventArgs e)
        {
            //var cmb = (ComboBox)sender;
            //cmb.Items.Clear();
            //cmb.Items.Add("Default");
            //cmb.SelectedItem = "Default";


        }

        private void tabControl2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(tabControl2.SelectedTab.Text == "Audio")
            {
                RefreshAudioList();
                var enumerator = new MMDeviceEnumerator();
                MMDevice audioDefault = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                audioDefaultDevice.Text = "Default: " + audioDefault.FriendlyName;
            }
        }



        private void RefreshAudioList()
        {
            audioDevices = new Dictionary<string, string>();
            audioDevices.Add("Default", "Default");

            //ManagementObjectSearcher objSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_SoundDevice");

            //ManagementObjectCollection objCollection = objSearcher.Get();

            //foreach (ManagementObject obj in objCollection)
            //{
            //    audioDevices.Add(obj["Caption"].ToString(), obj["DeviceID"].ToString());
            //}

            var enumerator = new MMDeviceEnumerator();
            foreach (var endpoint in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                audioDevices.Add(endpoint.FriendlyName, endpoint.ID);
            }          

            foreach (Control ctrl in audioCustomSettingsBox.Controls)
            {
                if (ctrl is ComboBox)
                {
                    var cmb = (ComboBox)ctrl;
                    string lastItem = cmb.Text;
                    cmb.Items.Clear();
                    cmb.Items.AddRange(audioDevices.Keys.ToArray());
                    if(cmb.Items.Contains(lastItem))
                    {
                        cmb.SelectedItem = lastItem;
                    }
                    else if (audioDevices.Values.Contains(ini.IniReadValue("Audio", cmb.Name)))
                    {
                        cmb.SelectedItem = audioDevices.FirstOrDefault(x => x.Value == ini.IniReadValue("Audio", cmb.Name)).Key;
                    }
                    else
                    {
                        cmb.SelectedItem = "Default";
                    }
                }
            }
        }

        private void audioRefresh_Click(object sender, EventArgs e)
        {
            RefreshAudioList();
        }
    }
}
