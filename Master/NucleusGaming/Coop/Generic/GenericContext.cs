using Microsoft.Win32;
using Nucleus.Gaming.Coop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Nucleus.Gaming
{
    // This class is a holder for the GenericGameInfo class. It doesn't implement the IGenericGameInfo
    // because some of the elements are implemented differently to work with the JS engine
    // Comments can be found on the original class if no specific feature is implemented here
    public class GenericContext
    {
        public GameHookInfo Hook = new GameHookInfo();
        public double HandlerInterval;
        public bool Debug;
        public string Error;
        public int Interval;
        public bool SymlinkExe;
        public bool SupportsKeyboard;
        public string[] ExecutableContext;
        public string ExecutableName;
        public string SteamID;
        public string GUID;
        public string GameName;
        public int MaxPlayers;
        public int MaxPlayersOneMonitor;
        public SaveType SaveType;
        public SearchType searchType;
        public string SavePath;
        public string StartArguments;
        public string BinariesFolder;
        public string WorkingFolder;
        public bool NeedsSteamEmulation;
        public string[] KillMutex;
        public string KillMutexType;
        public int KillMutexDelay;
        public bool KeepSymLinkOnExit;
        public string LauncherExe;
        public string LauncherTitle;
        public int PlayerID;
        public bool IsFullscreen;
        public UserInfo User = new UserInfo();
        public DPIHandling DPIHandling = DPIHandling.True;
        public bool FakeFocus;
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
        public bool UseX360ce;
        public string HookFocusInstances;
        //public bool UseAlpha8CustomDll;
        public bool bHasKeyboardPlayer;
        public bool KeepAspectRatio;
        public bool HideDesktop;
        //public int FakeFocusInterval;
        public bool ResetWindows;
        public bool UseGoldberg;
        public string OrigSteamDllPath;
        public bool GoldbergNeedSteamInterface;
        public bool XboxOneControllerFix;
        public bool UseForceBindIP;
        public string[] XInputPlusDll;
        public string[] CopyCustomUtils;
        public int PlayersPerInstance;
        public bool UseDevReorder;
        //public string[] HexEditExe;

        public Type HandlerType
        {
            get { return typeof(GenericGameHandler); }
        }

        public Dictionary<string, object> Options
        {
            get { return profile.Options; }
        }

        public int Width
        {
            get
            {
                switch (DPIHandling)
                {
                    case DPIHandling.Scaled:
                        return (int)((pInfo.MonitorBounds.Width * DPIManager.Scale) + 0.5);
                    case DPIHandling.InvScaled:
                        return (int)((pInfo.MonitorBounds.Width * (1 / DPIManager.Scale)) + 0.5);
                    case DPIHandling.True:
                    default:
                        return pInfo.MonitorBounds.Width;
                }
            }
        }
        public int Height
        {
            get
            {
                switch (DPIHandling)
                {
                    case DPIHandling.Scaled:
                        return (int)((pInfo.MonitorBounds.Height * DPIManager.Scale) + 0.5);
                    case DPIHandling.InvScaled:
                        return (int)((pInfo.MonitorBounds.Height * (1 / DPIManager.Scale)) + 0.5);
                    case DPIHandling.True:
                    default:
                        return pInfo.MonitorBounds.Height;
                }
            }
        }

        [Dynamic(AutoHandles = true)]
        public string ExePath;
        [Dynamic(AutoHandles = true)]
        public string RootInstallFolder;
        [Dynamic(AutoHandles = true)]
        public string RootFolder;

        private GameProfile profile;
        private PlayerInfo pInfo;
        private GenericGameHandler parent;
        public GenericContext(GameProfile prof, PlayerInfo info, GenericGameHandler handler, bool hasKeyboard)
        {
            profile = prof;
            pInfo = info;
            parent = handler;

            bHasKeyboardPlayer = hasKeyboard;
        }

        //public void HexEditExe(string[] values)
        //{
        //    //if (gen.HexEditExe?.Length > 0)
        //    //{
        //    using (StreamWriter writer = new StreamWriter("important.txt", true))
        //    {
        //        writer.WriteLine("ExePath: " + ExePath + " Folder.InstancedGameFolder: " + parent.GetFolder(Folder.InstancedGameFolder) + " parent.exePath: " + parent.exePath + " getdirname: " + Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
        //    }
        //    foreach (string asciiValues in values)
        //    {
        //        if (File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\Data\\45770\\Instance0", "deadrising2otr-ORIG.exe")))
        //        {
        //            File.Delete(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\Data\\45770\\Instance0", "deadrising2otr-ORIG.exe"));
        //        }

        //        File.Move(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\Data\\45770\\Instance0", "deadrising2otr.exe"), Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\Data\\45770\\Instance0", "deadrising2otr-ORIG.exe"));
        //        string[] splitValues = asciiValues.Split('|');
        //        if (splitValues.Length > 1)
        //        {
        //            PatchFile(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\Data\\45770\\Instance0", "deadrising2otr-ORIG.exe"), Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\Data\\45770\\Instance0", "deadrising2otr.exe"), splitValues[0], splitValues[1]);
        //        }
        //    }
        //    //}
        //}

        public bool HasKeyboardPlayer()
        {
            return bHasKeyboardPlayer;
        }

        public string GetFolder(Folder folder)
        {
            return parent.GetFolder(folder);
        }

        public string x360ceGamepadGuid
        {
            get
            {
                return "IG_" + pInfo.GamepadGuid.ToString().Replace("-", string.Empty);
            }
        }

        public string GamepadGuid
        {
            get
            {
                return pInfo.GamepadGuid.ToString();
            }
        }

        public int OrigAspectRatio
        {
            get
            {
                return profile.Screens[pInfo.PlayerID].display.Width / profile.Screens[pInfo.PlayerID].display.Height;
            }
        }

        public int OrigWidth
        {
            get
            {
                return pInfo.ProcessData.HWnd.Size.Width;
            }
        }

        public int OrigHeight
        {
            get
            {
                return profile.Screens[pInfo.PlayerID].display.Height;
            }
        }

        public string Nickname
        {
            get
            {
                return pInfo.Nickname;
            }
        }

        public void WriteTextFile(string path, string[] lines)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            File.WriteAllLines(path, lines);
        }

        public int FindLineNumberInTextFile(string path, string searchValue, SearchType type)
        {
            string[] lines = File.ReadAllLines(path);
            if (!File.Exists(path))
            {
                return -1;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                switch (type)
                {
                    case SearchType.Contains:
                        {
                            if (lines[i].Contains(searchValue))
                            {
                                return i + 1;
                            }
                        }
                        break;
                    case SearchType.Full:
                        {
                            if (string.Equals(lines[i],searchValue))
                            {
                                return i + 1;
                            }
                        }
                        break;
                    case SearchType.StartsWith:
                        {
                            if (lines[i].StartsWith(searchValue))
                            {
                                return i + 1;
                            }
                        }
                        break;
                }
            }

            return -1;
        }

        public void RemoveLineInTextFile(string path, int lineNum)
        {
            string[] lines = File.ReadAllLines(path);
            if (lineNum < 0 || lineNum > lines.Length)
            {
                return;
            }
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else
            {
                return;
            }

            lines = lines.Where(w => w != lines[lineNum - 1]).ToArray();

            File.WriteAllLines(path, lines);
        }

        public void RemoveLineInTextFile(string path, int lineNum, string encoder)
        {
            string[] lines = File.ReadAllLines(path);
            if (lineNum < 0 || lineNum > lines.Length)
            {
                return;
            }
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else
            {
                return;
            }

            lines = lines.Where(w => w != lines[lineNum - 1]).ToArray();

            File.WriteAllLines(path, lines, Encoding.GetEncoding(encoder));
        }

        public void RemoveLineInTextFile(string path, string searchValue, SearchType type)
        {
            string[] lines = File.ReadAllLines(path);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else
            {
                return;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                switch (type)
                {
                    case SearchType.Contains:
                        {
                            if (lines[i].Contains(searchValue))
                            {
                                lines = lines.Where(w => w != lines[i]).ToArray();
                            }
                        }
                        break;
                    case SearchType.Full:
                        {
                            if (string.Equals(lines[i], searchValue))
                            {
                                lines = lines.Where(w => w != lines[i]).ToArray();
                            }
                        }
                        break;
                    case SearchType.StartsWith:
                        {
                            if (lines[i].StartsWith(searchValue))
                            {
                                lines = lines.Where(w => w != lines[i]).ToArray();
                            }
                        }
                        break;
                }
                //if (lines[i].Contains(searchValue))
                //{
                //    lines.Where(w => w != lines[i]).ToArray();
                //}
            }
            File.WriteAllLines(path, lines);
        }

        public void RemoveLineInTextFile(string path, string searchValue, SearchType type, string encoder)
        {
            string[] lines = File.ReadAllLines(path);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else
            {
                return;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                switch (type)
                {
                    case SearchType.Contains:
                        {
                            if (lines[i].Contains(searchValue))
                            {
                                lines = lines.Where(w => w != lines[i]).ToArray();
                            }
                        }
                        break;
                    case SearchType.Full:
                        {
                            if (string.Equals(lines[i], searchValue))
                            {
                                lines = lines.Where(w => w != lines[i]).ToArray();
                            }
                        }
                        break;
                    case SearchType.StartsWith:
                        {
                            if (lines[i].StartsWith(searchValue))
                            {
                                lines = lines.Where(w => w != lines[i]).ToArray();
                            }
                        }
                        break;
                }
                //if (lines[i].Contains(searchValue))
                //{
                //    lines.Where(w => w != lines[i]).ToArray();
                //}
            }
            File.WriteAllLines(path, lines, Encoding.GetEncoding(encoder));
        }

        public void ReplaceLinesInTextFile(string path, string[] newLines)
        {
            string[] lines = File.ReadAllLines(path);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else
            {
                return;
            }

            foreach (string line in newLines)
            {
                string[] splitString = line.Split('|');
                string lineNum = splitString[0];
                string newValue = splitString[1];

                if (int.Parse(lineNum) > 0 && int.Parse(lineNum) < lines.Length)
                {
                    lines[int.Parse(lineNum) - 1] = newValue;
                }
            }

            File.WriteAllLines(path, lines);
        }

        public void ReplaceLinesInTextFile(string path, string[] newLines, string encoder)
        {
            string[] lines = File.ReadAllLines(path);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else
            {
                return;
            }

            foreach (string line in newLines)
            {
                string[] splitString = line.Split('|');
                string lineNum = splitString[0];
                string newValue = splitString[1];
                if (int.Parse(lineNum) > 0 && int.Parse(lineNum) < lines.Length)
                {
                    lines[int.Parse(lineNum) - 1] = newValue;
                }
            }

            File.WriteAllLines(path, lines, Encoding.GetEncoding(encoder));
        }

        public void ReplacePartialLinesInTextFile(string path, string[] newLines)
        {
            string[] lines = File.ReadAllLines(path);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else
            {
                return;
            }

            foreach (string item in newLines)
            {
                string[] data = item.Split('|');
                string lineNum = data[0];
                string regexPtrn = data[1];
                string newValue = data[2];

                if (int.Parse(lineNum) > 0 && int.Parse(lineNum) < lines.Length)
                {
                    lines[int.Parse(lineNum) - 1] = Regex.Replace(lines[int.Parse(lineNum) - 1], regexPtrn, newValue);
                }
            }

            File.WriteAllLines(path, lines);
        }

        public void ReplacePartialLinesInTextFile(string path, string[] newLines, string encoder)
        {
            string[] lines = File.ReadAllLines(path);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else
            {
                return;
            }

            foreach (string item in newLines)
            {
                string[] data = item.Split('|');
                string lineNum = data[0];
                string regexPtrn = data[1];
                string newValue = data[2];

                if (int.Parse(lineNum) > 0 && int.Parse(lineNum) < lines.Length)
                {
                    lines[int.Parse(lineNum) - 1] = Regex.Replace(lines[int.Parse(lineNum) - 1], regexPtrn, newValue);
                }
            }

            File.WriteAllLines(path, lines, Encoding.GetEncoding(encoder));
        }

        public SaveInfo NewSaveInfo(string section, string key, string value)
        {
            return new SaveInfo(section, key, value);
        }

        public void ModifySaveFile(string installSavePath, string saveFullPath, SaveType type, params SaveInfo[] info)
        {
            // this needs to be dynamic someday
            switch (type)
            {
                case SaveType.CFG:
                    {
                        SourceCfgFile cfg = new SourceCfgFile(installSavePath);
                        for (int j = 0; j < info.Length; j++)
                        {
                            SaveInfo save = info[j];
                            if (save is CfgSaveInfo)
                            {
                                CfgSaveInfo option = (CfgSaveInfo)save;
                                cfg.ChangeProperty(option.Section, option.Key, option.Value);
                            }
                        }
                        cfg.Save(saveFullPath);
                    }
                    break;
                case SaveType.INI:
                    {
                        if (!installSavePath.Equals(saveFullPath))
                        {
                            File.Copy(installSavePath, saveFullPath);
                        }
                        IniFile file = new IniFile(saveFullPath);
                        for (int j = 0; j < info.Length; j++)
                        {
                            SaveInfo save = info[j];
                            if (save is IniSaveInfo)
                            {
                                IniSaveInfo ini = (IniSaveInfo)save;
                                file.IniWriteValue(ini.Section, ini.Key, ini.Value);
                            }
                        }
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void PatchFile(string originalFile, string patchedFile, byte[] patchFind, byte[] patchReplace)
        {
            // Read file bytes.
            byte[] fileContent = File.ReadAllBytes(originalFile);

            int patchCount = 0;
            // Detect and patch file.
            for (int p = 0; p < fileContent.Length; p++)
            {
                if (p + patchFind.Length > fileContent.Length)
                    continue;
                var toContinue = false;
                for (int i = 0; i < patchFind.Length; i++)
                {
                    if (patchFind[i] != fileContent[p + i])
                    {
                        toContinue = true;
                        break;
                    }
                }
                if (toContinue) continue;

                patchCount++;
                if (patchCount > 1)
                {
                    LogManager.Log("PatchFind pattern is not unique in " + originalFile);
                }
                else
                {
                    for (int w = 0; w < patchReplace.Length; w++)
                    {
                        fileContent[p + w] = patchReplace[w];
                    }
                }
            }

            if (patchCount == 0)
            {
                LogManager.Log("PatchFind pattern was not found in " + originalFile);
            }

            // Save it to another location.
            File.WriteAllBytes(patchedFile, fileContent);
        }

        public void PatchFile(string originalFile, string patchedFile, string spatchFind, string spatchReplace)
        {
            // Read file bytes.
            byte[] fileContent = File.ReadAllBytes(originalFile);

            byte[] patchFind = Encoding.ASCII.GetBytes(spatchFind);
            byte[] patchReplace = Encoding.ASCII.GetBytes(spatchReplace);

            int patchCount = 0;
            // Detect and patch file.
            for (int p = 0; p < fileContent.Length; p++)
            {
                if (p + patchFind.Length > fileContent.Length)
                    continue;
                var toContinue = false;
                for (int i = 0; i < patchFind.Length; i++)
                {
                    if (patchFind[i] != fileContent[p + i])
                    {
                        toContinue = true;
                        break;
                    }
                }
                if (toContinue) continue;

                patchCount++;
                if (patchCount > 1)
                {
                    LogManager.Log("PatchFind pattern is not unique in " + originalFile);
                }
                else
                {
                    for (int w = 0; w < patchReplace.Length; w++)
                    {
                        fileContent[p + w] = patchReplace[w];
                    }
                }
            }

            if (patchCount == 0)
            {
                LogManager.Log("PatchFind pattern was not found in " + originalFile);
            }

            // Save it to another location.
            File.WriteAllBytes(patchedFile, fileContent);
        }

        //XPath syntax
        //https://www.w3schools.com/xml/xpath_syntax.asp
        public void ChangeXmlAttributeValue(string path, string xpath, string attributeName, string attributeValue)
        {
            path = Environment.ExpandEnvironmentVariables(path);

            var doc = new XmlDocument();
            doc.Load(path);
            var nodes = doc.SelectNodes(xpath);
            foreach (XmlNode node in nodes)
            {
                node.Attributes[attributeName].Value = attributeValue;
            }
            doc.Save(path);
        }

        public void ChangeXmlNodeValue(string path, string xpath, string nodeValue)
        {
            path = Environment.ExpandEnvironmentVariables(path);

            var doc = new XmlDocument();
            doc.Load(path);
            var nodes = doc.SelectNodes(xpath);
            foreach (XmlNode node in nodes)
            {
                node.Value = nodeValue;
            }
            doc.Save(path);
        }

        public void ChangeXmlNodeInnerTextValue(string path, string xpath, string innerTextValue)
        {
            path = Environment.ExpandEnvironmentVariables(path);

            var doc = new XmlDocument();
            doc.Load(path);
            var nodes = doc.SelectNodes(xpath);
            foreach (XmlNode node in nodes)
            {
                node.InnerText = innerTextValue;
            }
            doc.Save(path);
        }

        public void CreateRegKey(string baseKey, string sKey, string subKey)
        {
            if (baseKey == "HKEY_LOCAL_MACHINE" || baseKey == "HKEY_CURRENT_USER")
            {
                if (baseKey == "HKEY_LOCAL_MACHINE")
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey(sKey, true);
                    key.CreateSubKey(subKey);
                    key.Close();
                }
                else
                {
                    RegistryKey key = Registry.CurrentUser.OpenSubKey(sKey, true);
                    key.CreateSubKey(subKey);
                    key.Close();
                }
            }
        }

        public void DeleteRegKey(string baseKey, string sKey, string subKey)
        {
            if (baseKey == "HKEY_LOCAL_MACHINE" || baseKey == "HKEY_CURRENT_USER")
            {
                if (baseKey == "HKEY_LOCAL_MACHINE")
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey(sKey, true);
                    key.DeleteSubKey(subKey);
                    key.Close();
                }
                else
                {
                    RegistryKey key = Registry.CurrentUser.OpenSubKey(sKey, true);
                    key.DeleteSubKey(subKey);
                    key.Close();
                }
            }
        }

        public void EditRegKey(string baseKey, string sKey, string subKey, object value, RegType regType)
        {

            //Binary = 3,
            //DWord = 4,
            //ExpandString = 2,
            //MultiString = 7,
            //None = -1,
            //QWord = 11,
            //String = 1,
            //Unknown = 0

            if (regType == RegType.Binary)
            {
                string val = value.ToString();
                byte[] bytes = Encoding.UTF8.GetBytes(val);
                if (baseKey == "HKEY_LOCAL_MACHINE" || baseKey == "HKEY_CURRENT_USER")
                {
                    if (baseKey == "HKEY_LOCAL_MACHINE")
                    {
                        RegistryKey key = Registry.LocalMachine.OpenSubKey(sKey, true);
                        key.SetValue(subKey, bytes, (RegistryValueKind)(int)regType);
                        key.Close();
                    }
                    else
                    {
                        RegistryKey key = Registry.CurrentUser.OpenSubKey(sKey, true);
                        key.SetValue(subKey, bytes, (RegistryValueKind)(int)regType);
                        key.Close();
                    }

                }
            }
            else
            {
                if (baseKey == "HKEY_LOCAL_MACHINE" || baseKey == "HKEY_CURRENT_USER")
                {
                    if (baseKey == "HKEY_LOCAL_MACHINE")
                    {
                        RegistryKey key = Registry.LocalMachine.OpenSubKey(sKey, true);
                        key.SetValue(subKey, value, (RegistryValueKind)(int)regType);
                        key.Close();
                    }
                    else
                    {
                        RegistryKey key = Registry.CurrentUser.OpenSubKey(sKey, true);
                        key.SetValue(subKey, value, (RegistryValueKind)(int)regType);
                        key.Close();
                    }

                }
            }
        }
    }
}
