﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using Nucleus.Gaming.Interop;
using Newtonsoft.Json;
using System.Threading;
using Ionic.Zip;
using Nucleus.Gaming.Properties;
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

        private Dictionary<string, IGameInfo> games;
        private Dictionary<string, IGameInfo> gameInfos;
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
        public Dictionary<string, IGameInfo> Games { get { return games; } }
        public Dictionary<string, IGameInfo> GameInfos { get { return gameInfos; } }

        public static GameManager Instance { get { return instance; } }

        public UserProfile User
        {
            get { return user; }
            set { user = value; }
        }

        public GameManager()
        {
            instance = this;
            games = new Dictionary<string, IGameInfo>();
            gameInfos = new Dictionary<string, IGameInfo>();

            string appData = GetAppDataPath();
            Directory.CreateDirectory(appData);

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
            return Games.Values.Any(c => c.ExecutableName == gameExe);
        }

        /// <summary>
        /// Tries to find the game
        /// </summary>
        /// <param name="exePath"></param>
        /// <returns></returns>
        public IGameInfo GetGame(string exePath)
        {
            string lower = exePath.ToLower();
            string fileName = Path.GetFileName(exePath).ToLower();
            string dir = Path.GetDirectoryName(exePath);

            var possibilities = Games.Values.Where(c => c.ExecutableName == fileName);

            foreach (IGameInfo game in possibilities)
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
        /// Tries adding a game to the collection with the provided IGameInfo
        /// </summary>
        /// <param name="exePath"></param>
        /// <returns></returns>
        public UserGameInfo TryAddGame(string exePath, IGameInfo game)
        {
            string lower = exePath.ToLower();

            // search for the same exe on the user profile
            if (GameManager.Instance.User.Games.Any(c => c.ExePath.ToLower() == lower))
            {
                return null;
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

            var possibilities = Games.Values.Where(c => c.ExecutableName == fileName);

            foreach (IGameInfo game in possibilities)
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
        public string ExtractSteamEmu(string outputFolder = null)
        {
            string steamEmu;

            if (string.IsNullOrEmpty(outputFolder))
            {
                string app = GetAppDataPath();
                steamEmu = Path.Combine(app, "SteamEmu");
            }
            else
            {
                steamEmu = outputFolder;
            }

            try
            {
                if (!Directory.Exists(steamEmu))
                {
                    LogManager.Log("Extracting SmartSteamEmu");

                    Directory.CreateDirectory(steamEmu);
                    using (MemoryStream stream = new MemoryStream(Resources.SmartSteamEmu))
                    {
                        using (ZipFile zip1 = ZipFile.Read(stream))
                        {
                            foreach (ZipEntry e in zip1)
                            {
                                e.Extract(steamEmu, ExtractExistingFileAction.OverwriteSilently);
                            }
                        }
                    }
                }
            }
            catch
            {
                LogManager.Log("Extraction of SmartSteamEmu failed");
                return string.Empty;
            }

            return steamEmu;
        }

        public void WaitSave()
        {
            while (IsSaving)
            {
            }
        }

        #region Initialize

        private string GetAppDataPath()
        {
#if ALPHA
            string local = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            return Path.Combine(local, "Data");
#else
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Nucleus Coop");
#endif
        }

        private string GetJsGamesPath()
        {
            string local = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            return Path.Combine(local, "games");
        }

        protected string GetUserProfilePath()
        {
            return Path.Combine(GetAppDataPath(), "userprofile.json");
        }

        public int Compare(UserGameInfo x, UserGameInfo y)
        {
            return x.Game.GameName.CompareTo(y.Game.GameName);
        }

        public UserGameInfo AddGame(IGameInfo game, string exePath)
        {
            UserGameInfo gInfo = new UserGameInfo();
            gInfo.InitializeDefault(game, exePath);
            user.Games.Add(gInfo);

            SaveUserProfile();

            return gInfo;
        }

        public void BeginBackup(IGameInfo game)
        {
            string appData = GetAppDataPath();
            string gamePath = Path.Combine(appData, game.GUID);
            Directory.CreateDirectory(gamePath);

            backupFiles = new List<BackupFile>();
        }

        public IGameHandler MakeHandler(IGameInfo game)
        {
            return (IGameHandler)Activator.CreateInstance(game.HandlerType);
        }

        public string GempTempFolder(IGameInfo game)
        {
            string appData = GetAppDataPath();
            return Path.Combine(appData, game.GUID);
        }

        public BackupFile BackupFile(IGameInfo game, string path)
        {
            string appData = GetAppDataPath();
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

        public void ExecuteBackup(IGameInfo game)
        {
            // we didnt backup anything
            if (backupFiles == null)
            {
                return;
            }

            string appData = GetAppDataPath();
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
            string jsfolder = GetJsGamesPath();
            DirectoryInfo jsFolder = new DirectoryInfo(jsfolder);
            FileInfo[] files = jsFolder.GetFiles("*.js");
            for (int i = 0; i < files.Length; i++)
            {
                FileInfo f = files[i];
                using (Stream str = f.OpenRead())
                {
                    GenericGameInfo info = new GenericGameInfo(str);
                    LogManager.Log("Found game info: " + info.GameName);

                    games.Add(info.GUID, info);
                    gameInfos.Add(info.ExecutableName, info);
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
            error = ((IGameHandler)state).Play();
        }
    }
}
