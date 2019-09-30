﻿using Jint;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;
using Nucleus.Gaming.Generic.Step;
using Nucleus.Gaming.Coop;

namespace Nucleus.Gaming
{
    public class GenericGameInfo
    {
        private Engine engine;
        private string js;
        
        public GameHookInfo Hook = new GameHookInfo();
        public List<GameOption> Options = new List<GameOption>();

        public SaveType SaveType;
        public string SavePath;

        public string[] DirSymlinkExclusions;
        public string[] FileSymlinkExclusions;
        public string[] FileSymlinkCopyInstead;
        public bool KeepSymLinkOnExit;

        public double HandlerInterval;
        public bool Debug;
        public bool SupportsPositioning;
        public bool SymlinkExe;
        public bool SymlinkGame;
        public bool HardcopyGame;

        public bool SupportsKeyboard;
        public bool KeyboardPlayerFirst;

        public string[] ExecutableContext;
        public string ExecutableName;
        public string SteamID;
        public string GUID;
        public string GameName;
        public int MaxPlayers;
        public int MaxPlayersOneMonitor;
        public int PauseBetweenStarts;
        public DPIHandling DPIHandling = DPIHandling.True;

        public string StartArguments;
        public string BinariesFolder;

        public bool FakeFocus;

        public void AddOption(string name, string desc, string key, object value, object defaultValue)
        {
            Options.Add(new GameOption(name, desc, key, value, defaultValue));
        }

        public void AddOption(string name, string desc, string key, object value)
        {
            Options.Add(new GameOption(name, desc, key, value));
        }

        /// <summary>
        /// The relative path to where the games starts in
        /// </summary>
        public string WorkingFolder;
        public bool NeedsSteamEmulation;
        public string[] KillMutex;
        public string KillMutexType;
        public int KillMutexDelay;
        public string LauncherExe;
        public string LauncherTitle;
        public Action Play;
        public Action SetupSse;
        public List<CustomStep> CustomSteps = new List<CustomStep>();
        public string JsFileName;
        public bool LockMouse;
        public string Folder;
        public bool HookFocus;
        public bool ForceWindowTitle;
        public int IdealProcessor;
        public string UseProcessor;
        public string ProcessorPriorityClass;
        public bool CMDLaunch;
        public string[] CMDOptions;
        public bool HasDynamicWindowTitle;
        public string[] SymlinkFiles;
        public bool HookInitDelay;
        public bool HookInit;
        public string[] CopyFiles;
        public bool SetWindowHook;
        public bool HideTaskbar;
        public bool PromptBetweenInstances;
        public bool HideCursor;
        public bool RenameNotKillMutex;
        public bool IdInWindowTitle;
        public bool ChangeExe;
        public string GamepadGuid;
        public bool UseX360ce;
        public string HookFocusInstances;
        public bool KeepAspectRatio;
        public bool HideDesktop;
        //public bool UseAlpha8CustomDll;
        //public int FakeFocusInterval;
        public bool ResetWindows;
        public bool PartialMutexSearch;
        public bool UseGoldberg;
        public string OrigSteamDllPath;
        public bool GoldbergNeedSteamInterface;
        public bool XboxOneControllerFix;
        public bool UseForceBindIP;
        public string XInputPlusDll;

        public Type HandlerType
        {
            get { return typeof(GenericGameHandler); }
        }

        public GenericGameInfo(string fileName, string folderPath, Stream str)
        {
            JsFileName = fileName;
            Folder = folderPath;

            StreamReader reader = new StreamReader(str);
            js = reader.ReadToEnd();

            Assembly assembly = typeof(GameOption).Assembly;

            engine = new Engine(cfg => cfg.AllowClr(assembly));
            
            engine.SetValue("Game", this);
            engine.Execute("var Nucleus = importNamespace('Nucleus.Gaming');");
            engine.Execute(js);
            engine.SetValue("Game", (object)null);
        }


        public CustomStep ShowOptionAsStep(string optionKey, bool required, string title)
        {
            GameOption option = Options.First(c => c.Key == optionKey);
            option.Hidden = true;

            CustomStep step = new CustomStep();
            step.Option = option;
            step.Required = required;
            step.Title = title;

            CustomSteps.Add(step);
            return step;
        }

        public void PrePlay(GenericContext context, GenericGameHandler handler, PlayerInfo player)
        {
            engine.SetValue("Context", context);
            engine.SetValue("Handler", handler);
            engine.SetValue("Player", player);
            engine.SetValue("Game", this);

            Play?.Invoke();
        }

        /// <summary>
        /// Clones this Game Info into a new Generic Context
        /// </summary>
        /// <returns></returns>
        public GenericContext CreateContext(GameProfile profile, PlayerInfo info, GenericGameHandler handler, bool hasKeyboardPlayer)
        {
            GenericContext context = new GenericContext(profile, info, handler, hasKeyboardPlayer);

            Type t = GetType();
            PropertyInfo[] props = t.GetProperties();
            FieldInfo[] fields = t.GetFields();

            Type c = context.GetType();
            PropertyInfo[] cprops = c.GetProperties();
            FieldInfo[] cfields = c.GetFields();

            for (int i = 0; i < props.Length; i++)
            {
                PropertyInfo p = props[i];
                PropertyInfo d = cprops.FirstOrDefault(k => k.Name == p.Name);
                if (d == null)
                {
                    continue;
                }

                if (p.PropertyType != d.PropertyType ||
                    !d.CanWrite)
                {
                    continue;
                }

                object value = p.GetValue(this, null);
                d.SetValue(context, value, null);
            }

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo source = fields[i];
                FieldInfo dest = cfields.FirstOrDefault(k => k.Name == source.Name);
                if (dest == null)
                {
                    continue;
                }

                if (source.FieldType != dest.FieldType)
                {
                    continue;
                }

                object value = source.GetValue(this);
                dest.SetValue(context, value);
            }

            return context;
        }

        public string GetSteamLanguage()
        {
            string result;
            if (Environment.Is64BitOperatingSystem)
                result = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Valve\Steam", "Language", "english");
            else
                result = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "Language", "english");

            return result;
        }
    }
}
