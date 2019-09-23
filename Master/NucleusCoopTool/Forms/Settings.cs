﻿using Nucleus.Gaming.Windows.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Nucleus.Gaming;
using System.Text.RegularExpressions;
using SlimDX.DirectInput;

namespace Nucleus.Coop
{
    public partial class Form : BaseForm
    {
        private readonly IniFile ini = new Gaming.IniFile(Path.Combine(Directory.GetCurrentDirectory(), "Settings.ini"));

        private MainForm mainForm = null;
        private PositionsControl positionsControl;

        public int KillProcess_HotkeyID = 1;
        public int TopMost_HotkeyID = 2;
        public int StopSession_HotkeyID = 3;

        private TextBox[] controllerGuids;
        private TextBox[] controllerNicks;

        private DirectInput dinput;

        public Form(MainForm mf, PositionsControl pc)
        {
            InitializeComponent();

            controllerGuids = new TextBox[]{ controllerOneGuid, controllerTwoGuid, controllerThreeGuid, controllerFourGuid, controllerFiveGuid, controllerSixGuid, controllerSevenGuid, controllerEightGuid, controllerNineGuid, controllerTenGuid, controllerElevenGuid, controllerTwelveGuid, controllerThirteenGuid, controllerFourteenGuid, controllerFifteenGuid, controllerSixteenGuid };
            controllerNicks = new TextBox[]{ controllerOneNick, controllerTwoNick, controllerThreeNick, controllerFourNick, controllerFiveNick, controllerSixNick, controllerSevenNick, controllerEightNick, controllerNineNick, controllerTenNick, controllerElevenNick, controllerTwelveNick, controllerThirteenNick, controllerFourteenNick, controllerFifteenNick, controllerSixteenNick };

            mainForm = mf as MainForm;
            positionsControl = pc;
            
            if(ini.IniReadValue("Hotkeys", "Close").Contains('+'))
            {
                string[] closeHk = ini.IniReadValue("Hotkeys", "Close").Split('+');
                if((closeHk[0] == "Ctrl" || closeHk[0] == "Alt" || closeHk[0] == "Shift") && closeHk[1].Length == 1 && Regex.IsMatch(closeHk[1], @"^[a-zA-Z0-9]+$"))
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

            GetControllers();

            if (ini.IniReadValue("Misc", "UseNicksInGame") != "")
            {
                useNicksCheck.Checked = Boolean.Parse(ini.IniReadValue("Misc", "UseNicksInGame"));
            }

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
            IList<DeviceInstance> devices = dinput.GetDevices(DeviceClass.GameController, DeviceEnumerationFlags.AttachedOnly);
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

                ini.IniWriteValue("Misc", "UseNicksInGame", useNicksCheck.Checked.ToString());

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
            MessageBox.Show("NucleusCoop Mod - " + mainForm.version + "\n\nCredits\n---------------------------------------------------------------------\nOriginal NucleusCoop Project: Lucas Assis (lucasassislar)\nMod: ZeroFox\n\nThis mod brings further enhancements to NucleusCoop, such as:\n- HUGE increase to the amount of compabitle games\n- Much more customization (via game scripts)\n- 6 and 8 player support\n- Quality of life improvements\n- Bug fixes\n- And more!\n\nFull mod changelog in Mod-Readme.txt\n\nAll this wouldn't have been possible without Lucas. Thank you Lucas <3. Make split-screen great again!\n\nSpecial thanks to: Talos91, Ilyaki and the Splitscreen Dreams discord.", "Credits",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }
    }
}
