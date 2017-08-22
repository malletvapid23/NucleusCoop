﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nucleus.Gaming.Coop
{
    public class GameProfile
    {
        private List<PlayerInfo> playerData;
        private List<UserScreen> screens;
        private Dictionary<string, object> options;

        public List<PlayerInfo> PlayerData
        {
            get { return playerData; }
        }

        /// <summary>
        /// A reference to the screens as they were
        /// when the user made the profile
        /// (so we can compare if a screen
        /// is missing or added)
        /// </summary>
        public List<UserScreen> Screens
        {
            get { return screens; }
        }

        /// <summary>
        /// Options set by the user
        /// </summary>
        public Dictionary<string, object> Options
        {
            get { return options; }
        }

        public GameProfile()
        {

        }

        public void InitializeDefault(GenericGameInfo game)
        {
            if (playerData == null)
            {
                playerData = new List<PlayerInfo>();
            }

            if (screens == null)
            {
                screens = new List<UserScreen>();
            }

            if (options == null)
            {
                options = new Dictionary<string, object>();

                foreach (var opt in game.Options)
                {
                    options.Add(opt.Key, opt.Value);
                }
            }
        }

        public static GameProfile CleanClone(GameProfile profile)
        {
            GameProfile nprof = new GameProfile();
            nprof.playerData = new List<PlayerInfo>();
            nprof.screens = profile.screens.ToList();

            List<PlayerInfo> source = profile.playerData;
            for (int i = 0; i < source.Count; i++)
            {
                PlayerInfo player = source[i];
                if (player.ScreenIndex != -1)
                {
                    // only add valid players to the clean version
                    nprof.playerData.Add(player);
                }
            }

            Dictionary<string, object> noptions = new Dictionary<string, object>();
            foreach (var opt in profile.Options)
            {
                noptions.Add(opt.Key, opt.Value);
            }
            nprof.options = noptions;

            return nprof;
        }
    }
}
