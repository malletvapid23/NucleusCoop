﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using SlimDX.DirectInput;
using Nucleus.Gaming.Coop;

namespace Nucleus.Gaming.Coop
{
    public class PlayerInfo
    {
        private Rectangle sourceEditBounds;
        private Rectangle editBounds;
        private Rectangle monitorBounds;
        private int screenIndex = -1;
        private object tag;

        private ProcessData processData;
        private bool assigned;

        public UserScreen Owner;

        public int PlayerID;
        public bool SteamEmu;
        public bool GotLauncher;
        public bool GotGame;

        public bool IsKeyboardPlayer;
        public bool IsXInput;
        public bool IsDInput;
        public bool IsFake;

        public Guid GamepadProductGuid;
        public Guid GamepadGuid;
        public int GamepadId;
        public string GamepadName;
        public int GamepadMask;
        public Joystick DInputJoystick;

        public string HIDDeviceID;
        public string Nickname;
        public string InstanceId;

        // Serialized

        /// <summary>
        /// The bounds of this player's game screen
        /// </summary>
        public Rectangle MonitorBounds
        {
            get { return monitorBounds; }
            set { monitorBounds = value; }
        }


        // Runtime

        public Rectangle SourceEditBounds
        {
            get { return sourceEditBounds; }
            set { sourceEditBounds = value; }
        }

        /// <summary>
        /// A temporary rectangle to show the user
        /// where the game screen is going to be located
        /// </summary>
        public Rectangle EditBounds
        {
            get { return editBounds; }
            set { editBounds = value; }
        }

        /// <summary>
        /// The index of this player
        /// </summary>
        public int ScreenIndex
        {
            get { return screenIndex; }
            set { screenIndex = value; }
        }

        /// <summary>
        /// A custom tag object for handlers to store data in
        /// </summary>
        public object Tag
        {
            get { return tag; }
            set { tag = value; }
        }

        /// <summary>
        /// Information about the game's process, null if its not running
        /// </summary>
        public ProcessData ProcessData
        {
            get { return processData; }
            set { processData = value; }
        }

        

        
    }
}
