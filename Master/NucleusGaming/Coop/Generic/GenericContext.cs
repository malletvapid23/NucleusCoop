﻿using Microsoft.Win32;
using Nucleus.Gaming.Coop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
        public string[] CustomUserGeneralValues;
        public string[] CustomUserPlayerValues;
        public string[] CustomUserInstanceValues;

        private List<string> regKeyPaths = new List<string>();



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
        [Dynamic(AutoHandles = true)]
        public string OrigRootFolder;

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

        public bool IsKeyboardPlayer
        {
            get
            {
                return pInfo.IsKeyboardPlayer;
            }
        }

        public float OrigAspectRatioDecimal
        {
            get
            {
                return (float)profile.Screens[pInfo.PlayerID].display.Width / profile.Screens[pInfo.PlayerID].display.Height;
            }
        }

        public string OrigAspectRatio
        {
            get
            {
                int width = profile.Screens[pInfo.PlayerID].display.Width;
                int height = profile.Screens[pInfo.PlayerID].display.Height;
                var gcd = GCD(width, height);
                return string.Format("{0}:{1}", width / gcd, height / gcd);
            }
        }

        public float AspectRatioDecimal
        {
            get
            {
                return (float)Width / Height;
            }
        }

        public string AspectRatio
        {
            get
            {
                var gcd = GCD(Width, Height);
                return string.Format("{0}:{1}", Width / gcd, Height / gcd);
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

        public string LocalIP
        {
            get
            {
                //string localIP;
                //using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                //{
                //    socket.Connect("8.8.8.8", 65530);
                //    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                //    localIP = endPoint.Address.ToString();
                //}

                //var dadada = GetBestInterface(BitConverter.ToUInt32(IPAddress.Parse("8.8.8.8").GetAddressBytes(), 0), out uint interfaceIndex);
                //IPAddress xxxd = NetworkInterface.GetAllNetworkInterfaces()
                //                .Where(netInterface => netInterface.GetIPProperties().GetIPv4Properties().Index == BitConverter.ToInt32(BitConverter.GetBytes(interfaceIndex), 0)).First().GetIPProperties().UnicastAddresses.Where(ipAdd => ipAdd.Address.AddressFamily == AddressFamily.InterNetwork).First().Address;

                //return xxxd.ToString();

                return parent.GetLocalIP();
            }
        }

        public string EnvironmentPlayer
        {
            get
            {
                return $@"C:\Users\{Environment.UserName}\NucleusCoop\{Nickname}\";
            }
        }

        public string EnvironmentRoot
        {
            get
            {
                return $@"C:\Users\{Environment.UserName}\NucleusCoop\";
            }
        }

        public string UserProfileConfigPath
        {
            //get
            //{
            //    //return parent.UserProfileConfigPath;
            //}
            //set { }

            get; set;
        }

        public string UserProfileSavePath
        {
            //get
            //{
            //    //return parent.UserProfileSavePath;
            //}
            //set { }

            get; set;
        }

        public byte[] ConvertToBytes(float num)
        {
            return BitConverter.GetBytes(num);
        }

        public byte[] ConvertToBytes(int num)
        {
            return BitConverter.GetBytes(num);
        }

        public byte[] ConvertToBytes(string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }

        public void RunAdditionalFiles(string[] filePaths, bool changeWorkingDir, int secondsToPauseInbetween)
        {
            for (int fileIndex = 0; fileIndex<filePaths.Length; fileIndex++)
            {
                string fileName = filePaths[fileIndex];
                if(fileName.Contains('|'))
                {
                    string[] fileNameSplit = fileName.Split('|');
                    fileName = fileNameSplit[1];

                    if(fileNameSplit[0].ToLower() != "all")
                    {
                        if (int.Parse(fileNameSplit[0]) != (pInfo.PlayerID - 1))
                        {
                            continue;
                        }
                    }
}
                else
                {
                    if(pInfo.PlayerID > 0)
                    {
                        continue;
                    }
                }
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = fileName;
                psi.UseShellExecute = false;
                if(changeWorkingDir)
                {
                    psi.WorkingDirectory = GameManager.Instance.GetAppContentPath() + "\\AdditionalFiles";
                }
                else
                {
                    psi.WorkingDirectory = Path.GetDirectoryName(fileName);
                }

                Process.Start(psi);

                if(fileIndex<(filePaths.Length - 1) && secondsToPauseInbetween > 0)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(secondsToPauseInbetween));
                }
            }
        }

        public void RunAdditionalFiles(string[] filePaths, bool changeWorkingDir, int secondsToPauseInbetween, bool runAsAdmin)
        {
            for (int fileIndex = 0; fileIndex < filePaths.Length; fileIndex++)
            {
                string fileName = filePaths[fileIndex];
                if (fileName.Contains('|'))
                {
                    string[] fileNameSplit = fileName.Split('|');
                    fileName = fileNameSplit[1];

                    if (fileNameSplit[0].ToLower() != "all")
                    {
                        if (int.Parse(fileNameSplit[0]) != (pInfo.PlayerID - 1))
                        {
                            continue;
                        }
                    }
                }
                else
                {
                    if (pInfo.PlayerID > 0)
                    {
                        continue;
                    }
                }
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = fileName;
                psi.UseShellExecute = false;
                if (changeWorkingDir)
                {
                    psi.WorkingDirectory = GameManager.Instance.GetAppContentPath() + "\\AdditionalFiles";
                }
                else
                {
                    psi.WorkingDirectory = Path.GetDirectoryName(fileName);
                }
                if(runAsAdmin)
                {
                    psi.UseShellExecute = true;
                    psi.Verb = "runas";
                }

                Process.Start(psi);

                if (fileIndex < (filePaths.Length - 1) && secondsToPauseInbetween > 0)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(secondsToPauseInbetween));
                }
            }
        }

        static int GCD(int a, int b)
        {
            return b == 0 ? Math.Abs(a) : GCD(b, a % b);
        }

        public void ModifiedDate(string file, int year, int month, int day, int hour, int minute, int second)
        {
            DateTime dt = new DateTime(year, month, day, hour, minute, second);
            File.SetLastWriteTime(file, dt);
        }

        public void ModifiedDate(string file, int year, int month, int day)
        {
            DateTime dt = new DateTime(year, month, day, 0, 0, 0);
            File.SetLastWriteTime(file, dt);
        }

        public void CreatedDate(string file, int year, int month, int day, int hour, int minute, int second)
        {
            DateTime dt = new DateTime(year, month, day, hour, minute, second);
            File.SetCreationTime(file, dt);
        }

        public void CreatedDate(string file, int year, int month, int day)
        {
            DateTime dt = new DateTime(year, month, day, 0, 0, 0);
            File.SetCreationTime(file, dt);
        }

        public string[] FindFiles(string rootFolder, string fileName)
        {
            string[] files = Directory.GetFiles(rootFolder, fileName, SearchOption.TopDirectoryOnly);
            return files;
        }

        public string[] FindFiles(string rootFolder, string fileName, bool searchAll)
        {
            SearchOption searchOp = searchAll ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            string[] files = Directory.GetFiles(rootFolder, fileName, searchOp);
            return files;
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
            string[] lines = File.ReadAllLines(path, Encoding.GetEncoding(encoder));
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
            string[] lines = File.ReadAllLines(path, Encoding.GetEncoding(encoder));
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
            string[] lines = File.ReadAllLines(path, Encoding.GetEncoding(encoder));
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
            string[] lines = File.ReadAllLines(path, Encoding.GetEncoding(encoder));
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

        void ExportRegistry(string strKey, string filepath)
        {
            try
            {
                using (Process proc = new Process())
                {
                    proc.StartInfo.FileName = "reg.exe";
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;
                    proc.StartInfo.CreateNoWindow = true;
                    proc.StartInfo.Arguments = "export \"" + strKey + "\" \"" + filepath + "\" /y";
                    proc.Start();
                    string stdout = proc.StandardOutput.ReadToEnd();
                    string stderr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                // handle exception
            }
        }

        public void DeleteRegKey(string baseKey, string sKey, string subKey)
        {
            if (baseKey != "HKEY_LOCAL_MACHINE" && baseKey != "HKEY_CURRENT_USER" && baseKey != "HKEY_USERS")
            {
                return;
            }

            RegistryKey key = null;
            switch (baseKey)
            {
                case "HKEY_LOCAL_MACHINE":
                    key = Registry.LocalMachine.OpenSubKey(sKey, true);
                    break;
                case "HKEY_CURRENT_USER":
                    key = Registry.CurrentUser.OpenSubKey(sKey, true);
                    break;
                case "HKEY_USERS":
                    key = Registry.Users.OpenSubKey(sKey, true);
                    break;
            }

            string fullKeyPath = baseKey + "\\" + sKey;
            if (!regKeyPaths.Contains(fullKeyPath) && key != null)
            {
                regKeyPaths.Add(fullKeyPath);
                ExportRegistry(baseKey + "\\" + sKey, Directory.GetCurrentDirectory() + "\\utils\\backup\\" + sKey.Substring(sKey.LastIndexOf('\\') + 1) + ".reg");
            }

            key.DeleteSubKey(subKey);
            key.Close();
        }

        public string ReadRegKey(string baseKey, string sKey, string subKey)
        {
            if (baseKey != "HKEY_LOCAL_MACHINE" && baseKey != "HKEY_CURRENT_USER" && baseKey != "HKEY_USERS")
            {
                return string.Empty;
            }

            RegistryKey key = null;
            switch (baseKey)
            {
                case "HKEY_LOCAL_MACHINE":
                    key = Registry.LocalMachine.OpenSubKey(sKey);
                    break;
                case "HKEY_CURRENT_USER":
                    key = Registry.CurrentUser.OpenSubKey(sKey);
                    break;
                case "HKEY_USERS":
                    key = Registry.Users.OpenSubKey(sKey);
                    break;
            }

            if(key != null)
            {
                return key.GetValue(subKey).ToString();
            }
            else
            {
                return null;
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

            if ((baseKey != "HKEY_LOCAL_MACHINE" && baseKey != "HKEY_CURRENT_USER" && baseKey != "HKEY_USERS") || value == null)
            {
                return;
            }

            string val = value.ToString();

            RegistryKey key = null;
            switch (baseKey)
            {
                case "HKEY_LOCAL_MACHINE":
                    key = Registry.LocalMachine.OpenSubKey(sKey, true);
                    break;
                case "HKEY_CURRENT_USER":
                    key = Registry.CurrentUser.OpenSubKey(sKey, true);
                    break;
                case "HKEY_USERS":
                    key = Registry.Users.OpenSubKey(sKey, true);
                    break;
            }

            string fullKeyPath = baseKey + "\\" + sKey;
            if (!regKeyPaths.Contains(fullKeyPath) && key != null)
            {
                regKeyPaths.Add(fullKeyPath);
                ExportRegistry(baseKey + "\\" + sKey, Directory.GetCurrentDirectory() + "\\utils\\backup\\" + sKey.Substring(sKey.LastIndexOf('\\') + 1) + ".reg");
            }

            if(key == null)
            {
                switch (baseKey)
                {
                    case "HKEY_LOCAL_MACHINE":
                        key = Registry.LocalMachine.CreateSubKey(sKey, true);
                        break;
                    case "HKEY_CURRENT_USER":
                        key = Registry.CurrentUser.CreateSubKey(sKey, true);
                        break;
                    case "HKEY_USERS":
                        key = Registry.Users.CreateSubKey(sKey, true);
                        break;
                }
            }

            if (regType == RegType.Binary)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(val);
                key.SetValue(subKey, bytes, (RegistryValueKind)(int)regType);
            }
            else
            {
                key.SetValue(subKey, value, (RegistryValueKind)(int)regType);
            }
            key.Close();
        }

        public void KillProcessesMatchingWindowName(string name)
        {
			foreach (var p in System.Diagnostics.Process.GetProcesses().Where(x => x.MainWindowTitle.ToLower().Contains(name.ToLower())).ToArray())
				p.Kill();
        }

        public void KillProcessesMatchingProcessName(string name)
        {
	        foreach (var p in System.Diagnostics.Process.GetProcesses().Where(x => x.ProcessName.ToLower().Contains(name.ToLower())).ToArray())
		        p.Kill();
        }
	}
}
