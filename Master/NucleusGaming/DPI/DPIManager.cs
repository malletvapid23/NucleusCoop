﻿using Nucleus.Gaming.Windows;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace Nucleus.Gaming
{
    public static class DPIManager
    {
        public static float Scale = 1f;
        private static List<IDynamicSized> components = new List<IDynamicSized>();

        private static readonly IniFile ini = new Gaming.IniFile(Path.Combine(Directory.GetCurrentDirectory(), "Settings.ini"));

        public static Font Font;

        public static void PreInitialize()
        {
            if(ini.IniReadValue("Advanced","Scale") != "")
            {
                Scale = float.Parse(ini.IniReadValue("Advanced", "Scale"));
            }
            else
            {
                Scale = User32Util.GetDPIScalingFactor();
            }
        }

        private static void UpdateFont()
        {
            if (Font != null)
            {
                Font.Dispose();
            }

            int fontSize = (int)(12 / DPIManager.Scale);
            //if (Scale > 1)
            //{
            //    fontSize = (int)(12 / (0.7 * DPIManager.Scale));
            //}
            if (ini.IniReadValue("Advanced", "Font") != "")
            {
                fontSize = Int32.Parse(ini.IniReadValue("Advanced", "Font"));
            }

            Font = new Font("Segoe UI", fontSize, GraphicsUnit.Point);
        }

        public static void AddForm(Form form)
        {
            Version os = WindowsVersionInfo.Version;

            if (os.Major > 6 || os.Major == 6 && os.Minor >= 3)
            {
                // if we are on Windows 8.1 or higher we can have
                // custom DPI by window
                form.LocationChanged += AppForm_LocationChanged;
            }
            UpdateFont();
        }

        private static void AppForm_LocationChanged(object sender, EventArgs e)
        {
            Form form = (Form)sender;
            //uint val = Convert.ToUInt32(User32Util.GetDPIScalingFactor() * 96.0f); //User32Util.GetDpiForWindow(form.Handle);
            uint val = User32Util.GetDpiForWindow(form.Handle);
            //uint val = 96;
            float newScale = val / 96.0f;
            if (ini.IniReadValue("Advanced", "Scale") != "")
            {
                newScale = Scale;
            }
            float dif = Math.Abs(newScale - Scale);

            //MessageBox.Show("val: " + val + " \nnewScale: " + newScale + "\nUser32Util.GetDPIScalingFactor(): " + User32Util.GetDPIScalingFactor() + "\nUser32Util.GetDpiForWindow(form.Handle): " + User32Util.GetDpiForWindow(form.Handle));
            if (dif > 0.001f)
            {
                // DPI changed
                Scale = newScale;
                UpdateFont();

                // update all components
                form.Invoke((Action)delegate ()
                {
                    UpdateAll();
                });
            }
        }

        public static void ForceUpdate()
        {
            UpdateAll();
        }

        private static void UpdateAll()
        {
            for (int i = 0; i < components.Count; i++)
            {
                IDynamicSized comp = components[i];
                comp.UpdateSize(Scale);
            }
        }

        public static int Adjust(float value, float scale)
        {
            return (int)(value / scale);
        }

        public static void Register(IDynamicSized component)
        {
            components.Add(component);
        }

        public static void Update(IDynamicSized component)
        {
            component.UpdateSize(Scale);
        }

        public static void Unregister(IDynamicSized component)
        {
            components.Remove(component);
        }
    }
}
