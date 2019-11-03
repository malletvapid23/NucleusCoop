﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using System.Threading;
//using Ionic.Zip;
using Nucleus.Gaming.Properties;
using Nucleus.Gaming.Coop;
using System.Windows.Forms;

namespace Nucleus.Gaming
{
    /// <summary>
    /// Manages games information, so we can know what games are supported 
    /// and how to support it
    /// </summary>
    public class GameManager
    {
        private static GameManager instance;

        private Dictionary<string, GenericGameInfo> games;
        private Dictionary<string, GenericGameInfo> gameInfos;
        private UserProfile user;
        private List<BackupFile> backupFiles;
        private string error;
        private bool isSaving;

        /// object instance so we can thread-safe save the user profile
        private object saving = new object();

        public string Error { get { return error; } }

        public bool IsSaving { get { return isSaving; } }

        /// <summary>
        /// A dictionary containing GameInfos. The key is the game's guid
        /// </summary>
        public Dictionary<string, GenericGameInfo> Games { get { return games; } }
        public Dictionary<string, GenericGameInfo> GameInfos { get { return gameInfos; } }

        public static GameManager Instance { get { return instance; } }

        public UserProfile User
        {
            get { return user; }
            set { user = value; }
        }

        public GameManager()
        {
            instance = this;
            games = new Dictionary<string, GenericGameInfo>();
            gameInfos = new Dictionary<string, GenericGameInfo>();

            string appData = GetAppContentPath();
            Directory.CreateDirectory(appData);

            string gameJs = GetJsScriptsPath();
            Directory.CreateDirectory(gameJs);

            Initialize();
            LoadUser();
        }

        /// <summary>
        /// Tests if there's any game with the named exe
        /// </summary>
        /// <param name="gameExe"></param>
        /// <returns></returns>
        public bool AnyGame(string gameExe)
        {
            return Games.Values.Any(c => c.ExecutableName.ToLower() == gameExe);
        }

        /// <summary>
        /// Tries to find the game
        /// </summary>
        /// <param name="exePath"></param>
        /// <returns></returns>
        public GenericGameInfo GetGame(string exePath)
        {
            string lower = exePath.ToLower();
            string fileName = Path.GetFileName(exePath).ToLower();
            string dir = Path.GetDirectoryName(exePath);

            var possibilities = Games.Values.Where(c => string.Equals(c.ExecutableName.ToLower(), fileName, StringComparison.OrdinalIgnoreCase));

            foreach (GenericGameInfo game in possibilities)
            {
                // check if the Context matches
                string[] context = game.ExecutableContext;
                bool notAdd = false;
                if (context != null)
                {
                    for (int j = 0; j < context.Length; j++)
                    {
                        string con = Path.Combine(dir, context[j]);
                        if (!File.Exists(con) &&
                            !Directory.Exists(con))
                        {
                            notAdd = true;
                            break;
                        }
                    }
                }

                if (notAdd)
                {
                    continue;
                }

                // search for the same exe on the user profile
                return game;
            }

            return null;
        }

        /// <summary>
        /// Returns all the possible games with the exe path
        /// </summary>
        /// <param name="exePath"></param>
        /// <returns></returns>
        public List<GenericGameInfo> GetGames(string exePath)
        {
            string lower = exePath.ToLower();
            string fileName = Path.GetFileName(exePath).ToLower();
            string dir = Path.GetDirectoryName(exePath);

            var possibilities = Games.Values.Where(c => c.ExecutableName.ToLower() == fileName);
            List<GenericGameInfo> games = new List<GenericGameInfo>();

            foreach (GenericGameInfo game in possibilities)
            {
                // check if the Context matches
                string[] context = game.ExecutableContext;
                bool notAdd = false;
                if (context != null)
                {
                    for (int j = 0; j < context.Length; j++)
                    {
                        string con = Path.Combine(dir, context[j]);
                        if (!File.Exists(con) &&
                            !Directory.Exists(con))
                        {
                            notAdd = true;
                            break;
                        }
                    }
                }

                if (notAdd)
                {
                    continue;
                }

                // search for the same exe on the user profile
                games.Add(game);
            }

            return games;
        }

        /// <summary>
        /// Tries adding a game to the collection with the provided IGameInfo
        /// </summary>
        /// <param name="exePath"></param>
        /// <returns></returns>
        public UserGameInfo TryAddGame(string exePath, GenericGameInfo game)
        {
            string lower = exePath.ToLower();

            // search for the same exe on the user profile
            if (GameManager.Instance.User.Games.Any(c => c.ExePath.ToLower() == lower))
            {
                DialogResult dialogResult = MessageBox.Show("This game's executable is already in your library. Do you wish to add it anyway?\n\nExecutable Path: " + exePath, "Already exists", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dialogResult == DialogResult.No)
                {
                    return null;
                }               
            }

            LogManager.Log("Found game: {0}, full path: {1}", game.GameName, exePath);
            UserGameInfo uinfo = new UserGameInfo();
            uinfo.InitializeDefault(game, exePath);
            GameManager.Instance.User.Games.Add(uinfo);
            GameManager.Instance.SaveUserProfile();

            return uinfo;
        }

        /// <summary>
        /// Tries adding a game to the collection with the provided executable path
        /// </summary>
        /// <param name="exePath"></param>
        /// <returns></returns>
        public UserGameInfo TryAddGame(string exePath)
        {
            string lower = exePath.ToLower();
            string fileName = Path.GetFileName(exePath).ToLower();
            string dir = Path.GetDirectoryName(exePath);

            var possibilities = Games.Values.Where(c => c.ExecutableName.ToLower() == fileName);

            foreach (GenericGameInfo game in possibilities)
            {
                // check if the Context matches
                string[] context = game.ExecutableContext;
                bool notAdd = false;
                if (context != null)
                {
                    for (int j = 0; j < context.Length; j++)
                    {
                        string con = Path.Combine(dir, context[j]);
                        if (!File.Exists(con) &&
                            !Directory.Exists(con))
                        {
                            notAdd = true;
                            break;
                        }
                    }
                }

                if (notAdd)
                {
                    continue;
                }

                // search for the same exe on the user profile
                if (GameManager.Instance.User.Games.Any(c => c.ExePath.ToLower() == lower))
                {
                    continue;
                }

#if RELEASE
                if (game.Debug)
                {
                    continue;
                }
#endif

                LogManager.Log("Found game: {0}, full path: {1}", game.GameName, exePath);
                UserGameInfo uinfo = new UserGameInfo();
                uinfo.InitializeDefault(game, exePath);
                GameManager.Instance.User.Games.Add(uinfo);
                GameManager.Instance.SaveUserProfile();

                return uinfo;
            }

            return null;
        }

        /// <summary>
        /// Extracts the SmartSteamEmu and returns the folder its on
        /// </summary>
        /// <returns></returns>
        //public string ExtractSteamEmu(string outputFolder = null)
        //{
        //    string steamEmu;

        //    if (string.IsNullOrEmpty(outputFolder))
        //    {
        //        string app = GetAppDataPath();
        //        steamEmu = Path.Combine(app, "SteamEmu");
        //    }
        //    else
        //    {
        //        steamEmu = outputFolder;
        //    }

        //    try
        //    {
        //        //if (!Directory.Exists(steamEmu))
        //        //{
        //        LogManager.Log("Extracting SmartSteamEmu");

        //        Directory.CreateDirectory(steamEmu);
        //        //using (MemoryStream stream = new MemoryStream(Resources.SmartSteamEmu))
        //        //{
        //        using (ZipFile zip1 = ZipFile.Read(Path.Combine(GetUtilsPath(), "SmartSteamEmu\\SmartSteamEmu.zip")))
        //        {
        //            foreach (ZipEntry e in zip1)
        //            {
        //                e.Extract(steamEmu, ExtractExistingFileAction.OverwriteSilently);
        //            }
        //        }
        //        //}
        //        //}
        //    }
        //    catch
        //    {
        //        LogManager.Log("Extraction of SmartSteamEmu failed");
        //        return string.Empty;
        //    }

        //    return steamEmu;
        //}

        public void WaitSave()
        {
            while (IsSaving)
            {
            }
        }

        #region Initialize

        public string GetAppContentPath()
        {
#if ALPHA
            string local = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            return Path.Combine(local, "content");
#else
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Nucleus Coop");
#endif
        }

        public string GetJsScriptsPath()
        {
            string local = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            return Path.Combine(local, "scripts");
        }

        public string GetUtilsPath()
        {
            string local = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            return Path.Combine(local, "utils");
        }

        public string GetUserProfilePath()
        {
            return Path.Combine(GetAppContentPath(), "userprofile.json");
        }

        public int Compare(UserGameInfo x, UserGameInfo y)
        {
            if (x.Game == null || y.Game == null)
            {
                return 0;
            }
            return x.Game.GameName.CompareTo(y.Game.GameName);
        }

        public UserGameInfo AddGame(GenericGameInfo game, string exePath)
        {
            UserGameInfo gInfo = new UserGameInfo();
            gInfo.InitializeDefault(game, exePath);
            user.Games.Add(gInfo);

            SaveUserProfile();

            return gInfo;
        }

        public void BeginBackup(GenericGameInfo game)
        {
            string appData = GetAppContentPath();
            string gamePath = Path.Combine(appData, game.GUID);
            Directory.CreateDirectory(gamePath);

            backupFiles = new List<BackupFile>();
        }

        public IGameHandler MakeHandler(GenericGameInfo game)
        {
            return (IGameHandler)Activator.CreateInstance(game.HandlerType);
        }

        public string GempTempFolder(GenericGameInfo game)
        {
            string appData = GetAppContentPath();
            return Path.Combine(appData, game.GUID);
        }

        public BackupFile BackupFile(GenericGameInfo game, string path)
        {
            string appData = GetAppContentPath();
            string gamePath = Path.Combine(appData, game.GUID);
            string destination = Path.Combine(gamePath, Path.GetFileName(path));

            if (!File.Exists(path))
            {
                if (File.Exists(destination))
                {
                    // we fucked up and the backup exists? maybe, so restore
                    File.Copy(destination, path);
                }
            }
            else
            {
                if (File.Exists(destination))
                {
                    File.Delete(destination);
                }
                File.Copy(path, destination);
            }

            BackupFile bkp = new BackupFile(path, destination);
            backupFiles.Add(bkp);

            return bkp;
        }

        public void ExecuteBackup(GenericGameInfo game)
        {
            // we didnt backup anything
            if (backupFiles == null)
            {
                return;
            }

            string appData = GetAppContentPath();
            string gamePath = Path.Combine(appData, game.GUID);

            for (int i = 0; i < backupFiles.Count; i++)
            {
                BackupFile bkp = backupFiles[i];
                if (File.Exists(bkp.BackupPath))
                {
                    File.Delete(bkp.Source);
                    File.Move(bkp.BackupPath, bkp.Source);
                }
            }
        }

        public void ReorderUserProfile()
        {
            lock (user.Games)
            {
                user.Games.Sort(Compare);
            }
        }

        public void SaveUserProfile()
        {
            lock (user.Games)
            {
                user.Games.Sort(Compare);
            }

            string userProfile = GetUserProfilePath();
            asyncSaveUser(userProfile);
        }

        private void LoadUser()
        {
            string userProfile = GetUserProfilePath();

            if (File.Exists(userProfile))
            {
                try
                {
                    using (FileStream stream = new FileStream(userProfile, FileMode.Open))
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string json = reader.ReadToEnd();
                            user = JsonConvert.DeserializeObject<UserProfile>(json);

                            if (user.Games == null)
                            {
                                // json doesn't save empty lists, and user didn't add any game
                                user.InitializeDefault();
                            }
                            //else
                            //{
                            //    // delete invalid games
                            //    for (int i = 0; i < user.Games.Count; i++)
                            //    {
                            //        UserGameInfo gameInfo = user.Games[i];
                            //        if (gameInfo.Game == null)
                            //        {
                            //            LogManager.Log("Deleting invalid game " + user.Games[i].Game);
                            //            user.Games.RemoveAt(i);
                            //            i--;
                            //        }
                            //    }
                            //}
                        }
                    }
                }
                catch
                {
                    makeDefaultUserFile();
                }
            }
            else
            {
                makeDefaultUserFile();
            }
        }

        private void makeDefaultUserFile()
        {
            user = new UserProfile();
            user.InitializeDefault();

            string userProfile = GetUserProfilePath();
            string split = Path.GetDirectoryName(userProfile);
            if (!Directory.Exists(split))
            {
                Directory.CreateDirectory(split);
            }

            saveUser(userProfile);
        }

        private void asyncSaveUser(string path)
        {
            if (!IsSaving)
            {
                isSaving = true;
                LogManager.Log("> Saving user profile....");
                ThreadPool.QueueUserWorkItem(saveUser, path);
            }
        }

        private void saveUser(object p)
        {
            lock (saving)
            {
                try
                {
                    isSaving = true;
                    string path = (string)p;
                    using (FileStream stream = new FileStream(path, FileMode.Create))
                    {
                        using (StreamWriter writer = new StreamWriter(stream))
                        {
                            string json = JsonConvert.SerializeObject(user);
                            writer.Write(json);
                            stream.Flush();
                        }
                    }
                    LogManager.Log("Saved user profile");
                }
                catch { }
                isSaving = false;
            }
        }

        private void Initialize()
        {
            // Search for Javascript games-infos
            string jsfolder = GetJsScriptsPath();
            DirectoryInfo jsFolder = new DirectoryInfo(jsfolder);
            FileInfo[] files = jsFolder.GetFiles("*.js");
            for (int i = 0; i < files.Length; i++)
            {
                FileInfo f = files[i];
                try
                {
                    using (Stream str = f.OpenRead())
                    {
                        string ext = Path.GetFileNameWithoutExtension(f.Name);
                        string pathBlock = Path.Combine(f.Directory.FullName, ext);

                        GenericGameInfo info = new GenericGameInfo(f.Name, pathBlock, str);

                        LogManager.Log("Found game info: " + info.GameName);
                        games.Add(info.GUID, info);
                        //breaks anything? idk
                        gameInfos.Add(info.GUID, info);
                    }
                }
                catch
                {
                    MessageBox.Show(string.Format("There is an error in the game script {0}. The game this script is for will not appear in the list. If the issue has been fixed, please try re-adding the game.\n\nCommon errors include:\n- A syntax error (such as a \',\' \';\' or \']\' missing)\n- Another script has this GUID (must be unique!)\n- Code is not in the right place or format (for example: methods using Context must be within the Game.Play function)", f.Name), "Error in script", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                
            }

        }
        #endregion

        public void Play(IGameHandler handler)
        {
            // Start the Play method in another thread, so the
            // handler can update while it's still loading
            error = null;
            ThreadPool.QueueUserWorkItem(play, handler);
        }

        private void play(object state)
        {
#if RELEASE
            try
            {
                error = ((IGameHandler)state).Play();
            }
            catch (Exception ex)
            {
                error = ex.Message;
                try
                {
                    // try to save the exception
                    LogManager.Instance.LogExceptionFile(ex);
                }
                catch
                {
                    LogManager.Instance.LogExceptionFile(ex);
                    error = "We failed so hard we failed while trying to record the reason we failed initially. Sorry.";
                    return;
                }
            }
#else
            error = ((IGameHandler)state).Play();
#endif
        }
    }
}
