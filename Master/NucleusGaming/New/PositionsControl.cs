using Nucleus.Gaming;
using Nucleus.Gaming.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SlimDX.DirectInput;
using SlimDX.XInput;
using Nucleus.Gaming.Coop;
using System.IO;

namespace Nucleus.Coop
{
    public class PositionsControl : UserInputControl
    {
        private bool canProceed;

        //private bool keyboardPlayer = false;

        // array of users's screens
        private UserScreen[] screens;

        // the factor to scale all screens to fit them inside the edit area
        private float scale;

        // the total bounds of all the connected monitors together
        private Rectangle totalBounds;

        private Font playerFont;
        private Font playerCustomFont;
        private Font smallTextFont;
        private Font playerTextFont;
        private Font extraSmallTextFont;

        private RectangleF screensArea;
        private RectangleF playersArea;
        private int playerSize;

        private bool dragging = false;
        private int draggingIndex = -1;
        private Point draggingOffset;
        private Point mousePos;
        private int draggingScreen = -1;
        private Rectangle draggingScreenRec;
        private Rectangle draggingScreenBounds;

        private Image gamepadImg;
        private Image genericImg;
        private Image keyboardImg;

        public bool isDisconnected;
        private int dinputPressed = -1;


        private readonly IniFile ini = new Gaming.IniFile(Path.Combine(Directory.GetCurrentDirectory(), "Settings.ini"));

        public override bool CanProceed
        {
            get { return canProceed; }
        }
        public override string Title
        {
            get { return "Position Players"; }
        }
        public override bool CanPlay
        {
            get { return false; }
        }

        // dinput
        private DirectInput dinput;
        //private List<Joystick> dinputJoysticks;

        // xinput
        private List<Controller> xinputControllers;

        private Timer gamepadTimer;
        private Timer gamepadPollTimer;

        private int gamePadPressed = -1;

        private int testDinputPlayers = -1;// 16;
        private int testXinputPlayers = -1;// 16;

        public PositionsControl()
        {
            Initialize();
        }

        private void Initialize()
        {
            this.BackColor = Color.FromArgb(40, 40, 40);

            dinput = new DirectInput();
            //dinputJoysticks = new List<Joystick>();
            xinputControllers = new List<Controller>();
            for (int i = 0; i < 4; i++)
            {
                xinputControllers.Add(new Controller((UserIndex)i));
            }

            gamepadTimer = new Timer();
            gamepadTimer.Interval = 100;
            gamepadTimer.Tick += GamepadTimer_Tick;

            gamepadPollTimer = new Timer();
            gamepadPollTimer.Interval = 200;
            gamepadPollTimer.Tick += GamepadPollTimer_Tick;

            playerFont = new Font("Segoe UI", 40);
            playerCustomFont = new Font("Segoe UI", 16);
            playerTextFont = new Font("Segoe UI", 18);
            smallTextFont = new Font("Segoe UI", 12);
            extraSmallTextFont = new Font("Segoe UI", 10);
            if (ini.IniReadValue("Advanced", "Font") != "")
            {
                float fontSize = 12F;
                fontSize = float.Parse(ini.IniReadValue("Advanced", "Font"));
                playerFont = new Font("Segoe UI", fontSize);
                playerCustomFont = new Font("Segoe UI", fontSize);
                playerTextFont = new Font("Segoe UI", fontSize);
                smallTextFont = new Font("Segoe UI", fontSize);
                extraSmallTextFont = new Font("Segoe UI", fontSize);
            }


            gamepadImg = Resources.gamepad;
            genericImg = Resources.generic;
            keyboardImg = Resources.keyboard;

            RemoveFlicker();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (dinput != null)
            {
                dinput.Dispose();
                dinput = null;
            }
        }

        public override void Ended()
        {
            base.Ended();

            gamepadTimer.Enabled = false;
            gamepadPollTimer.Enabled = false;
        }


        private void GamepadPollTimer_Tick(object sender, EventArgs e)
        {
            gamePadPressed = -1;
            try
            {
                List<PlayerInfo> data = profile.PlayerData;
                foreach (PlayerInfo player in data)
                {
                    if(!player.IsKeyboardPlayer)
                    {
                        PollGamepad(player);
                    }
                }
            }
            catch (Exception ex)
            {
                UpdatePlayers();
                Refresh();
                gamePadPressed = -1;
            }

        }

        private void PollGamepad(PlayerInfo player)
        {

            gamePadPressed = -1;

            try
            {
                if (player.DInputJoystick.Acquire().IsFailure)
                {
                    return;
                }

                if (player.DInputJoystick.Poll().IsFailure)
                {
                    return;
                }


                JoystickState state = player.DInputJoystick.GetCurrentState();

                bool[] buttonsPressed = state.GetButtons();

                bool btnPressed = false;
                for (int b = 0; b < buttonsPressed.Length; b++)
                {
                    if (buttonsPressed[b])
                    {
                        btnPressed = true;
                        if(player.IsDInput)
                        {
                            dinputPressed = player.GamepadId;
                        }
                        gamePadPressed = player.GamepadId;
                        Refresh();
                        break;
                    }
                }
                if (player.IsDInput && !btnPressed && dinputPressed == player.GamepadId)
                {
                    dinputPressed = -1;
                    this.Invalidate();
                }
            }
            catch(DirectInputException e)
            {
                UpdatePlayers();
                Refresh();
                gamePadPressed = -1;
            }
        }


        private void GamepadTimer_Tick(object sender, EventArgs e)
        {
            gamePadPressed = -1;
            List<PlayerInfo> data = profile.PlayerData;
            //List<string> instanceIds = new List<string>();
            bool changed = false;

            GenericGameInfo g = game.Game;

            if (g.Hook.DInputEnabled || g.Hook.XInputReroute)
            {
                IList<DeviceInstance> devices = dinput.GetDevices(DeviceClass.GameController /*SlimDX.DirectInput.DeviceType.Gamepad*/, DeviceEnumerationFlags.AttachedOnly);

                // first search for disconnected gamepads
                for (int j = 0; j < data.Count; j++)
                {
                    PlayerInfo p = data[j];
                    if (!p.IsDInput || p.IsFake)
                    {
                        continue;
                    }

                    //if (!p.DInputJoystick.Acquire().IsFailure)
                    //{
                    //    if (!p.DInputJoystick.Poll().IsFailure)
                    //    {
                    //        JoystickState state = p.DInputJoystick.GetCurrentState();
                    //    }
                    //}

                    bool foundGamepad = false;
                    for (int i = 0; i < devices.Count; i++)
                    {
                        DeviceInstance device = devices[i];
                        if (device.InstanceGuid == p.GamepadGuid && i == data[j].GamepadId)
                        {
                            foundGamepad = true;
                            break;
                        }
                    }

                    if (!foundGamepad)
                    {
                        data[j].DInputJoystick.Unacquire();
                        changed = true;
                        data.RemoveAt(j);
                        j--;
                        isDisconnected = true;
                    }
                }

                for (int i = 0; i < devices.Count; i++)
                {
                    DeviceInstance device = devices[i];
                    bool already = false;

                    // see if this gamepad is already on a player
                    for (int j = 0; j < data.Count; j++)
                    {
                        PlayerInfo p = data[j];
                        if (p.GamepadGuid == device.InstanceGuid)
                        {
                            already = true;
                            break;
                        }
                    }


                    if (already)
                    {
                        continue;
                    }

                    changed = true;

                    // new gamepad
                    PlayerInfo player = new PlayerInfo();
                    player.DInputJoystick = new Joystick(dinput, device.InstanceGuid);
                    if (player.DInputJoystick.Properties.InterfacePath.ToUpper().Contains("IG_") && !g.Hook.XInputReroute)
                    {
                        continue;
                    }
                    player.GamepadProductGuid = device.ProductGuid;
                    player.GamepadGuid = device.InstanceGuid;
                    //instanceIds.Add(device.InstanceGuid.ToString());
                    player.GamepadName = device.InstanceName;
                    player.IsDInput = true;
                    player.GamepadId = i;
                    string hid = player.DInputJoystick.Properties.InterfacePath;
                    int start = hid.IndexOf("hid#");
                    int end = hid.LastIndexOf("#{");
                    string fhid = hid.Substring(start, end - start).Replace('#', '\\').ToUpper();
                    player.HIDDeviceID = fhid;
                    if (ini.IniReadValue("ControllerMapping", fhid) != "")
                    {
                        player.Nickname = ini.IniReadValue("ControllerMapping", fhid);
                    }
                    player.DInputJoystick.Acquire();
                    //data.Insert(0, player);
                    data.Add(player);
                }

            }

            if (g.Hook.XInputEnabled && !g.Hook.XInputReroute)
            {
                // XInput is only really enabled inside Nucleus Coop when
                // we have 4 or less players, else we need to force DirectInput to grab everything

                

                for (int j = 0; j < data.Count; j++)
                {
                    PlayerInfo p = data[j];
                    if (p.IsXInput && !p.IsFake)
                    {
                        Controller c = xinputControllers[p.GamepadId];
                        if (!c.IsConnected)
                        {
                            data[j].DInputJoystick.Unacquire();
                            changed = true;
                            data.RemoveAt(j);
                            j--;
                            isDisconnected = true;
                        }
                    }
                }

                for (int i = 0; i < 4; i++)
                {
                    Controller c = xinputControllers[i];
                    bool already = false;

                    if (c.IsConnected)
                    {
                        // see if this gamepad is already on a player
                        for (int j = 0; j < data.Count; j++)
                        {
                            PlayerInfo p = data[j];
                            if (p.IsXInput && p.GamepadId == i)
                            {
                                State s = c.GetState();
                                int newmask = (int)s.Gamepad.Buttons;
                                if (p.GamepadMask != newmask)
                                {
                                    changed = true;
                                    p.GamepadMask = newmask;
                                }
                                
                                
                                already = true;
                                break;
                            }
                        }
                        if (already)
                        {
                            continue;
                        }

                        changed = true;

                        PlayerInfo player = new PlayerInfo();
                        IList<DeviceInstance> devices = dinput.GetDevices(SlimDX.DirectInput.DeviceType.Gamepad, DeviceEnumerationFlags.AttachedOnly);
                        for (int x = 0; x < devices.Count; x++)
                        {
                            DeviceInstance device = devices[x];
                            //if(!instanceIds.Contains(device.InstanceGuid.ToString()))
                            if(x == i)
                            {
                                //instanceIds.Add(device.InstanceGuid.ToString());
                                player.GamepadGuid = device.InstanceGuid;
                                player.GamepadProductGuid = device.ProductGuid;
                                player.GamepadName = device.InstanceName;
                                player.DInputJoystick = new Joystick(dinput, device.InstanceGuid);
                                string hid = player.DInputJoystick.Properties.InterfacePath;
                                int start = hid.IndexOf("hid#");
                                int end = hid.LastIndexOf("#{");
                                string fhid = hid.Substring(start, end - start).Replace('#', '\\').ToUpper();
                                player.HIDDeviceID = fhid;
                                if(ini.IniReadValue("ControllerMapping", fhid) != "")
                                {
                                    player.Nickname = ini.IniReadValue("ControllerMapping", fhid);
                                }
                                player.DInputJoystick.Acquire();

                                break;
                            }
                            
                        }

                        // new gamepad
                        player.IsXInput = true;
                        player.GamepadId = i;
                        data.Add(player);
                    }
                }
            }

            if (changed)
            {
                UpdatePlayers();
                Refresh();
            }

        }

        private void AddPlayer(int i, float playerWidth, float playerHeight, float offset)
        {
            Rectangle r = RectangleUtil.Float(50 + ((playerWidth + offset) * i), 100, playerWidth, playerHeight);
            PlayerInfo player = new PlayerInfo();
            player.EditBounds = r;
            profile.PlayerData.Add(player);
        }

        private void UpdateScreens()
        {
            if (screens == null)
            {
                screens = ScreensUtil.AllScreens();
                totalBounds = RectangleUtil.Union(screens);
            }
            else
            {
                UserScreen[] newScreens = ScreensUtil.AllScreens();
                Rectangle newBounds = RectangleUtil.Union(newScreens);
                if (newBounds.Equals(totalBounds))
                {
                    return;
                }

                // screens got updated, need to reflect in our window
                screens = newScreens;
                totalBounds = newBounds;

                // remove all players screens
                List<PlayerInfo> playerData = profile.PlayerData;
                if (playerData != null)
                {
                    for (int i = 0; i < playerData.Count; i++)
                    {
                        PlayerInfo player = playerData[i];
                        player.EditBounds = GetDefaultBounds(draggingIndex);
                        player.ScreenIndex = -1;
                    }
                }
            }

            screensArea = new RectangleF(10, 50 + Height * 0.2f + 10, Width - 20, Height * 0.5f);
            if (totalBounds.Width > totalBounds.Height)
            {
                // horizontal monitor setup
                scale = screensArea.Width / (float)totalBounds.Width;
                if (totalBounds.Height * scale > screensArea.Height)
                {
                    scale = screensArea.Height / (float)totalBounds.Height;
                }
            }
            else
            {
                // vertical monitor setup
                scale = screensArea.Height / (float)totalBounds.Height;
                if (totalBounds.Width * scale > screensArea.Width)
                {
                    scale = screensArea.Width / (float)totalBounds.Width;
                }
            }

            Rectangle scaledBounds = RectangleUtil.Scale(totalBounds, scale);
            scaledBounds.X = (int)screensArea.X;
            scaledBounds.Y = (int)screensArea.Y;
            //scaledBounds = RectangleUtil.Center(scaledBounds, RectangleUtil.Float(0, this.Height * 0.25f, this.Width, this.Height * 0.7f));

            int minY = 0;
            for (int i = 0; i < screens.Length; i++)
            {
                UserScreen screen = screens[i];

                Rectangle bounds = RectangleUtil.Scale(screen.MonitorBounds, scale);
                Rectangle uiBounds = new Rectangle(bounds.X, bounds.Y + scaledBounds.Y, bounds.Width, bounds.Height);
                screen.UIBounds = uiBounds;

                minY = Math.Min(minY, uiBounds.X);
            }

            // remove negative monitors
            minY = -minY;
            for (int i = 0; i < screens.Length; i++)
            {
                UserScreen screen = screens[i];

                Rectangle uiBounds = screen.UIBounds;
                uiBounds.X += minY + scaledBounds.X;
                screen.UIBounds = uiBounds;
                screen.SwapTypeBounds = RectangleUtil.Float(uiBounds.X, uiBounds.Y, uiBounds.Width * 0.1f, uiBounds.Width * 0.1f);
            }
        }
        public override void Initialize(UserGameInfo game, GameProfile profile)
        {
            base.Initialize(game, profile);

            gamepadTimer.Enabled = true;
            gamepadPollTimer.Enabled = true;
            UpdatePlayers();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            totalBounds = Rectangle.Empty;
            UpdatePlayers();
            Invalidate();
        }

        private void UpdatePlayers()
        {
            if (profile == null)
            {
                return;
            }

            List<PlayerInfo> playerData = profile.PlayerData;
            canProceed = playerData.Count(c => c.ScreenIndex != -1) >= 1;
            if (playerData.Count == 0)
            {
                if (game.Game.SupportsKeyboard)
                {
                    // add keyboard data
                    PlayerInfo kbPlayer = new PlayerInfo();
                    kbPlayer.IsKeyboardPlayer = true;
                    kbPlayer.GamepadId = 99;
                    playerData.Add(kbPlayer);
                    //keyboardPlayer = true;
                }

                // make fake data if needed
                if (testDinputPlayers != -1)
                {
                    for (int i = 0; i < testDinputPlayers; i++)
                    {
                        // new gamepad
                        PlayerInfo player = new PlayerInfo();
                        player.GamepadGuid = new Guid();
                        player.GamepadName = "Player";
                        player.IsDInput = true;
                        player.IsFake = true;
                        playerData.Add(player);
                    }
                }

                if (testXinputPlayers != -1)
                {
                    for (int i = 0; i < testXinputPlayers;i++)
                    {
                        PlayerInfo player = new PlayerInfo();
                        player.GamepadGuid = new Guid();
                        player.GamepadName = "XPlayer";
                        player.IsXInput = true;
                        player.GamepadId = i;
                        player.IsFake = true;
                        playerData.Add(player);
                    }
                }
            }
            UpdateScreens();

            float playersWidth = Width * 0.65f;

            float playerCount = playerData.Count;
            float playerWidth = playersWidth * 0.9f;
            float playerHeight = Height * 0.2f;
            playersArea = new RectangleF(10, 50, playersWidth, playerHeight);

            float playersAreaArea = playersArea.Width * playersArea.Height;
            float maxArea = playersAreaArea / playerCount;
            playerSize = (int)Math.Round(Math.Sqrt(maxArea) - 0.5); // force the round down
            // see if the size can fit it or we need to make some further adjustments
            int horizontal = (int)Math.Round((playersWidth / playerSize) - 0.5);
            int vertical = (int)Math.Round((playerHeight / playerSize) - 0.5);
            int total = vertical * horizontal;
            if (total < playerCount)
            {
                int newVertical = vertical + 1;
                playerSize = (int)Math.Round((playerHeight / newVertical) - 0.5);
            }

            for (int i = 0; i < playerData.Count; i++)
            {
                PlayerInfo info = playerData[i];

                if (info.ScreenIndex == -1)
                {
                    info.EditBounds = GetDefaultBounds(i);
                    info.SourceEditBounds = info.EditBounds;
                }
            }

            CanPlayUpdated(canProceed, false);
            Invalidate();
        }

        private bool GetFreeSpace(int screenIndex, out Rectangle? editorBounds, out Rectangle? monitorBounds)
        {
            editorBounds = null;
            monitorBounds = null;

            var players = profile.PlayerData;
            UserScreen screen = screens[screenIndex];
            Rectangle bounds = screen.MonitorBounds;
            Rectangle ebounds = screen.UIBounds;

            switch (screen.Type)
            {
                case UserScreenType.FullScreen:
                    for (int i = 0; i < players.Count; i++)
                    {
                        PlayerInfo p = players[i];
                        if (p.ScreenIndex == screenIndex)
                        {
                            return false;
                        }
                    }

                    monitorBounds = screen.MonitorBounds;
                    editorBounds = screen.UIBounds;
                    return true;
                case UserScreenType.DualHorizontal:
                    {
                        int playersUsing = 0;
                        Rectangle areaUsed = new Rectangle();

                        for (int i = 0; i < players.Count; i++)
                        {
                            PlayerInfo p = players[i];
                            if (p.ScreenIndex == screenIndex)
                            {
                                playersUsing++;
                                areaUsed = p.MonitorBounds;
                            }
                        }

                        if (playersUsing == 2)
                        {
                            return false;
                        }

                        int half = (int)(bounds.Height / 2.0f);
                        for (int i = 0; i < 2; i++)
                        {
                            Rectangle area = new Rectangle(bounds.X, bounds.Y + (half * i), bounds.Width, half);
                            if (!areaUsed.Contains(area))
                            {
                                monitorBounds = area;

                                int halfe = (int)(ebounds.Height / 2.0f);
                                editorBounds = new Rectangle(ebounds.X, ebounds.Y + (halfe * i), ebounds.Width, halfe);
                                return true;
                            }
                        }
                    }
                    break;
                case UserScreenType.DualVertical:
                    {
                        int playersUsing = 0;
                        Rectangle areaUsed = new Rectangle();

                        for (int i = 0; i < players.Count; i++)
                        {
                            PlayerInfo p = players[i];
                            if (p.ScreenIndex == screenIndex)
                            {
                                playersUsing++;
                                areaUsed = p.MonitorBounds;
                            }
                        }

                        if (playersUsing == 2)
                        {
                            return false;
                        }

                        int half = (int)(bounds.Width / 2.0f);
                        for (int i = 0; i < 2; i++)
                        {
                            Rectangle area = new Rectangle(bounds.X + (half * i), bounds.Y, half, bounds.Height);
                            if (!areaUsed.Contains(area))
                            {
                                monitorBounds = area;
                                int halfe = (int)(ebounds.Width / 2.0f);
                                editorBounds = new Rectangle(ebounds.X + (halfe * i), ebounds.Y, halfe, ebounds.Height);
                                return true;
                            }
                        }
                    }
                    break;
                case UserScreenType.FourPlayers:
                    {
                        int playersUsing = 0;
                        for (int i = 0; i < players.Count; i++)
                        {
                            PlayerInfo p = players[i];
                            if (p.ScreenIndex == screenIndex)
                            {
                                playersUsing++;
                            }
                        }

                        if (playersUsing == 4)
                        {
                            return false;
                        }

                        int halfw = (int)(bounds.Width / 2.0f);
                        int halfh = (int)(bounds.Height / 2.0f);

                        for (int x = 0; x < 2; x++)
                        {
                            for (int y = 0; y < 2; y++)
                            {
                                Rectangle area = new Rectangle(bounds.X + (halfw * x), bounds.Y + (halfh * y), halfw, halfh);

                                bool goNext = false;
                                // check if there's any player with the area's x,y coord
                                for (int i = 0; i < players.Count; i++)
                                {
                                    PlayerInfo p = players[i];
                                    if (p.ScreenIndex == screenIndex)
                                    {
                                        //if (p.MonitorBounds.X == area.X &&
                                        //    p.MonitorBounds.Y == area.Y)
                                        if (p.MonitorBounds.IntersectsWith(area))
                                        {
                                            goNext = true;
                                            break;
                                        }
                                    }
                                }

                                if (goNext)
                                {
                                    continue;
                                }
                                monitorBounds = area;
                                int halfwe = (int)(ebounds.Width / 2.0f);
                                int halfhe = (int)(ebounds.Height / 2.0f);
                                editorBounds = new Rectangle(ebounds.X + (halfwe * x), ebounds.Y + (halfhe * y), halfwe, halfhe);
                                return true;
                            }
                        }
                    }
                    break;
                case UserScreenType.SixPlayers:
                    {
                        int playersUsing = 0;
                        for (int i = 0; i < players.Count; i++)
                        {
                            PlayerInfo p = players[i];
                            if (p.ScreenIndex == screenIndex)
                            {
                                playersUsing++;
                            }
                        }

                        if (playersUsing == 6)
                        {
                            return false;
                        }

                        int halfw = (int)(bounds.Width / 3.0f);
                        int halfh = (int)(bounds.Height / 2.0f);

                        for (int x = 0; x < 3; x++)
                        {
                            for (int y = 0; y < 2; y++)
                            {
                                Rectangle area = new Rectangle(bounds.X + (halfw * x), bounds.Y + (halfh * y), halfw, halfh);

                                bool goNext = false;
                                // check if there's any player with the area's x,y coord
                                for (int i = 0; i < players.Count; i++)
                                {
                                    PlayerInfo p = players[i];
                                    if (p.ScreenIndex == screenIndex)
                                    {
                                        //if (p.MonitorBounds.X == area.X &&
                                        //    p.MonitorBounds.Y == area.Y)
                                        if (p.MonitorBounds.IntersectsWith(area))
                                        {
                                            goNext = true;
                                            break;
                                        }
                                    }
                                }

                                if (goNext)
                                {
                                    continue;
                                }
                                monitorBounds = area;
                                int halfwe = (int)(ebounds.Width / 3.0f);
                                int halfhe = (int)(ebounds.Height / 2.0f);
                                editorBounds = new Rectangle(ebounds.X + (halfwe * x), ebounds.Y + (halfhe * y), halfwe, halfhe);
                                return true;
                            }
                        }
                    }
                    break;
                case UserScreenType.EightPlayers:
                    {
                        int playersUsing = 0;
                        for (int i = 0; i < players.Count; i++)
                        {
                            PlayerInfo p = players[i];
                            if (p.ScreenIndex == screenIndex)
                            {
                                playersUsing++;
                            }
                        }

                        if (playersUsing == 8)
                        {
                            return false;
                        }

                        int halfw = (int)(bounds.Width / 4.0f);
                        int halfh = (int)(bounds.Height / 2.0f);

                        for (int x = 0; x < 4; x++)
                        {
                            for (int y = 0; y < 2; y++)
                            {
                                Rectangle area = new Rectangle(bounds.X + (halfw * x), bounds.Y + (halfh * y), halfw, halfh);

                                bool goNext = false;
                                // check if there's any player with the area's x,y coord
                                for (int i = 0; i < players.Count; i++)
                                {
                                    PlayerInfo p = players[i];
                                    if (p.ScreenIndex == screenIndex)
                                    {
                                        //if (p.MonitorBounds.X == area.X &&
                                        //    p.MonitorBounds.Y == area.Y)
                                        if (p.MonitorBounds.IntersectsWith(area))
                                        {
                                            goNext = true;
                                            break;
                                        }
                                    }
                                }

                                if (goNext)
                                {
                                    continue;
                                }
                                monitorBounds = area;
                                int halfwe = (int)(ebounds.Width / 4.0f);
                                int halfhe = (int)(ebounds.Height / 2.0f);
                                editorBounds = new Rectangle(ebounds.X + (halfwe * x), ebounds.Y + (halfhe * y), halfwe, halfhe);
                                return true;
                            }
                        }
                    }
                    break;
                case UserScreenType.SixteenPlayers:
                    {
                        int playersUsing = 0;
                        for (int i = 0; i < players.Count; i++)
                        {
                            PlayerInfo p = players[i];
                            if (p.ScreenIndex == screenIndex)
                            {
                                playersUsing++;
                            }
                        }

                        if (playersUsing == 16)
                        {
                            return false;
                        }

                        int halfw = (int)(bounds.Width / 4.0f);
                        int halfh = (int)(bounds.Height / 4.0f);

                        for (int x = 0; x < 4; x++)
                        {
                            for (int y = 0; y < 4; y++)
                            {
                                Rectangle area = new Rectangle(bounds.X + (halfw * x), bounds.Y + (halfh * y), halfw, halfh);

                                bool goNext = false;
                                // check if there's any player with the area's x,y coord
                                for (int i = 0; i < players.Count; i++)
                                {
                                    PlayerInfo p = players[i];
                                    if (p.ScreenIndex == screenIndex)
                                    {
                                        if (p.MonitorBounds.IntersectsWith(area))
                                        {
                                            goNext = true;
                                            break;
                                        }
                                    }
                                }

                                if (goNext)
                                {
                                    continue;
                                }
                                monitorBounds = area;
                                int halfwe = (int)(ebounds.Width / 4.0f);
                                int halfhe = (int)(ebounds.Height / 4.0f);
                                editorBounds = new Rectangle(ebounds.X + (halfwe * x), ebounds.Y + (halfhe * y), halfwe, halfhe);
                                return true;
                            }
                        }
                    }
                    break;
                case UserScreenType.Custom:
                    {
                        int horLines = int.Parse(ini.IniReadValue("CustomLayout", "HorizontalLines"));
                        int verLines = int.Parse(ini.IniReadValue("CustomLayout", "VerticalLines"));
                        int maxPlayers = int.Parse(ini.IniReadValue("CustomLayout", "MaxPlayers"));

                        //if(horLines==0)
                        //{
                            horLines++;
                        //}
                        //if(verLines==0)
                        //{
                            verLines++;
                        //}

                        int playersUsing = 0;
                        for (int i = 0; i < players.Count; i++)
                        {
                            PlayerInfo p = players[i];
                            if (p.ScreenIndex == screenIndex)
                            {
                                playersUsing++;
                            }
                        }

                        if (playersUsing == maxPlayers)
                        {
                            return false;
                        }

                        int halfw = (int)(bounds.Width / (float)verLines);
                        int halfh = (int)(bounds.Height / (float)horLines);


                        for (int x = 0; x < verLines; x++)
                        {
                            for (int y = 0; y < horLines; y++)
                            {
                                Rectangle area = new Rectangle(bounds.X + (halfw * x), bounds.Y + (halfh * y), halfw, halfh);

                                bool goNext = false;
                                // check if there's any player with the area's x,y coord
                                for (int i = 0; i < players.Count; i++)
                                {
                                    PlayerInfo p = players[i];
                                    if (p.ScreenIndex == screenIndex)
                                    {
                                        if (p.MonitorBounds.IntersectsWith(area))
                                        {
                                            goNext = true;
                                            break;
                                        }
                                    }
                                }

                                if (goNext)
                                {
                                    continue;
                                }
                                monitorBounds = area;
                                int halfwe = (int)(ebounds.Width / (float)verLines);
                                int halfhe = (int)(ebounds.Height / (float)horLines);
                                editorBounds = new Rectangle(ebounds.X + (halfwe * x), ebounds.Y + (halfhe * y), halfwe, halfhe);
                                return true;
                            }
                        }
                    }
                    break;
            }
            return false;
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            if (dragging)
            {
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                // first count how many gamepads we have
                bool changed = false;
                for (int i = 0; i < screens.Length; i++)
                {
                    UserScreen screen = screens[i];
                    if (screen.UIBounds.Contains(e.Location) && !screen.SwapTypeBounds.Contains(e.Location))
                    {
                        List<PlayerInfo> playerData = profile.PlayerData;

                        // add all possible players!
                        for (int j = 0; j < playerData.Count; j++)
                        {
                            PlayerInfo player = playerData[j];

                            Rectangle? editor;
                            Rectangle? monitor;
                            bool hasFreeSpace = GetFreeSpace(i, out editor, out monitor);

                            if (hasFreeSpace)
                            {
                                if (player.ScreenIndex == -1)
                                {
                                    player.Owner = screens[i];
                                    player.ScreenIndex = i;
                                    player.MonitorBounds = monitor.Value;
                                    player.EditBounds = editor.Value;
                                    changed = true;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        break;
                    }
                }

                if (changed)
                {
                    UpdatePlayers();
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            var players = profile.PlayerData;

            if (dragging)
            {
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                for (int i = 0; i < screens.Length; i++)
                {
                    UserScreen screen = screens[i];
                    if (screen.SwapTypeBounds.Contains(e.Location))
                    {
                        if (screen.Type == UserScreenType.Custom)
                        {
                            screen.Type = 0;
                        }
                        else
                        {
                            screen.Type++;
                        }

                        // invalidate all players inside screen
                        for (int j = 0; j < players.Count; j++)
                        {
                            // return to default position
                            PlayerInfo p = players[j];
                            if (p.ScreenIndex == i)
                            {
                                p.EditBounds = GetDefaultBounds(j);
                                p.ScreenIndex = -1;
                            }
                        }

                        Invalidate();
                        return;
                    }
                }

                for (int i = 0; i < players.Count; i++)
                {
                    Rectangle r = players[i].EditBounds;
                    if (r.Contains(e.Location))
                    {
                        dragging = true;
                        draggingIndex = i;
                        draggingOffset = new Point(r.X - e.X, r.Y - e.Y);
                        Rectangle newBounds = GetDefaultBounds(draggingIndex);
                        profile.PlayerData[draggingIndex].EditBounds = newBounds;

                        if (draggingOffset.X < -newBounds.Width ||
                            draggingOffset.Y < -newBounds.Height)
                        {
                            draggingOffset = new Point(0, 0);
                        }

                        break;
                    }
                }
            }
            else if (e.Button == MouseButtons.Right || e.Button == MouseButtons.Middle)
            {
                for (int i = 0; i < screens.Length; i++)
                {
                    UserScreen screen = screens[i];
                    if (screen.SwapTypeBounds.Contains(e.Location))
                    {
                        if (screen.Type == UserScreenType.FullScreen)
                        {
                            screen.Type = UserScreenType.Custom;
                        }
                        else
                        {
                            screen.Type--;
                        }

                        // invalidate all players inside screen
                        for (int j = 0; j < players.Count; j++)
                        {
                            // return to default position
                            PlayerInfo p = players[j];
                            if (p.ScreenIndex == i)
                            {
                                p.EditBounds = GetDefaultBounds(j);
                                p.ScreenIndex = -1;
                            }
                        }

                        Invalidate();
                        return;
                    }
                }

                // if over a player on a screen, change the type
                for (int i = 0; i < players.Count; i++)
                {

                    PlayerInfo p = players[i];
                    Rectangle r = p.EditBounds;
                    if (r.Contains(e.Location))
                    {
                        
                        if (p.ScreenIndex != -1)
                        {


                            UserScreen screen = screens[p.ScreenIndex];

                            int verLines = 2;
                            int horLines = 2;
                            switch (screen.Type)
                            {
                                case UserScreenType.FourPlayers:
                                    {
                                        verLines = 2;
                                        horLines = 2;
                                    }
                                    break;
                                case UserScreenType.SixPlayers:
                                    {
                                        verLines = 3;
                                        horLines = 2;
                                    }
                                    break;
                                case UserScreenType.EightPlayers:
                                    {
                                        verLines = 4;
                                        horLines = 2;
                                    }
                                    break;
                                case UserScreenType.SixteenPlayers:
                                    {
                                        verLines = 4;
                                        horLines = 4;
                                    }
                                    break;
                                case UserScreenType.Custom:
                                    {
                                        horLines = int.Parse(ini.IniReadValue("CustomLayout", "HorizontalLines")) + 1;
                                        verLines = int.Parse(ini.IniReadValue("CustomLayout", "VerticalLines")) + 1;
                                    }
                                    break;
                            }

                            //int halfWidth = screen.MonitorBounds.Width / 2;
                            //int halfHeight = screen.MonitorBounds.Height / 2;
                            int halfWidth = screen.MonitorBounds.Width / verLines;
                            int halfHeight = screen.MonitorBounds.Height / horLines;



                            Rectangle bounds = p.MonitorBounds;
                            if ((int)screen.Type >= 3)
                            {
                                // check if the size is 1/4th of screen
                                if (bounds.Width == halfWidth && bounds.Height == halfHeight)
                                {
                                    bool hasLeftRightSpace = true;
                                    bool hasTopBottomSpace = true;

                                    // check if we have something left/right or top/bottom
                                    for (int j = 0; j < players.Count; j++)
                                    {
                                        if (i == j)
                                        {
                                            continue;
                                        }

                                        PlayerInfo other = players[j];
                                        if (other.ScreenIndex != p.ScreenIndex)
                                        {
                                            continue;
                                        }

                                        if (other.MonitorBounds.Y == p.MonitorBounds.Y)
                                        {
                                            hasLeftRightSpace = false;
                                        }
                                        if (other.MonitorBounds.X == p.MonitorBounds.X)
                                        {
                                            hasTopBottomSpace = false;
                                        }

                                        if (other.MonitorBounds.X == screen.MonitorBounds.X + halfWidth &&
                                            other.MonitorBounds.Height == screen.MonitorBounds.Height)
                                        {
                                            hasLeftRightSpace = false;
                                        }
                                        if (other.MonitorBounds.X == screen.MonitorBounds.X &&
                                            other.MonitorBounds.Width == screen.MonitorBounds.Width)
                                        {
                                            hasTopBottomSpace = false;
                                        }
                                    }

                                    if (hasLeftRightSpace)
                                    {
                                        Rectangle edit = p.EditBounds;
                                        if (bounds.X == screen.MonitorBounds.X + bounds.Width)
                                        {
                                            bounds.X -= bounds.Width;
                                            edit.X -= edit.Width;
                                        }

                                        bounds.Width *= verLines;
                                        edit.Width *= verLines;

                                        p.EditBounds = edit;
                                        p.MonitorBounds = bounds;

                                        Invalidate();
                                    }
                                    else if (hasTopBottomSpace)
                                    {
                                        Rectangle edit = p.EditBounds;
                                        if (bounds.Y == screen.MonitorBounds.Y + bounds.Height)
                                        {
                                            bounds.Y -= bounds.Height;
                                            edit.Y -= edit.Height;
                                        }

                                        bounds.Height *= horLines;
                                        edit.Height *= horLines;

                                        p.EditBounds = edit;
                                        p.MonitorBounds = bounds;
                                        Invalidate();
                                    }
                                }
                                else
                                {
                                    //bounds.Width = screen.MonitorBounds.Width / 2;
                                    //bounds.Height = screen.MonitorBounds.Height / 2;
                                    bounds.Width = screen.MonitorBounds.Width / verLines;
                                    bounds.Height = screen.MonitorBounds.Height / horLines;
                                    p.MonitorBounds = bounds;

                                    Rectangle edit = p.EditBounds;
                                    //edit.Width = screen.UIBounds.Width / 2;
                                    //edit.Height = screen.UIBounds.Height / 2;
                                    edit.Width = screen.UIBounds.Width / verLines;
                                    edit.Height = screen.UIBounds.Height / horLines;
                                    p.EditBounds = edit;

                                    Invalidate();
                                }
                            }
                        }
                    }
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            mousePos = e.Location;

            if (dragging)
            {
                var players = profile.PlayerData;

                PlayerInfo player = players[draggingIndex];
                Rectangle p = player.EditBounds;
                if (draggingScreen == -1)
                {
                    for (int i = 0; i < screens.Length; i++)
                    {
                        UserScreen screen = screens[i];
                        Rectangle s = screen.UIBounds;
                        float pc = RectangleUtil.PcInside(p, s);

                        // bigger than 60% = major part inside this screen
                        if (pc > 0.6f)
                        {
                            float offset = s.Width * 0.05f;

                            // check if there's space available on this screen
                            var playas = profile.PlayerData;
                            Rectangle? editor;
                            Rectangle? monitor;
                            GetFreeSpace(i, out editor, out monitor);

                            if (editor != null)
                            {
                                draggingScreenRec = editor.Value;
                                draggingScreenBounds = monitor.Value;
                                draggingScreen = i;
                            }
                            break;
                        }
                    }
                }
                else
                {
                    Rectangle s = screens[draggingScreen].UIBounds;
                    float pc = RectangleUtil.PcInside(p, s);
                    if (pc < 0.6f)
                    {
                        draggingScreen = -1;
                    }
                }

                p = new Rectangle(mousePos.X + draggingOffset.X, mousePos.Y + draggingOffset.Y, p.Width, p.Height);
                players[draggingIndex].EditBounds = p;

                Invalidate();
            }
        }


        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button == MouseButtons.Left)
            {
                if (dragging)
                {
                    PlayerInfo p = profile.PlayerData[draggingIndex];
                    dragging = false;

                    if (draggingScreen != -1)
                    {
                        p.Owner = screens[draggingScreen];
                        p.ScreenIndex = draggingScreen;
                        p.MonitorBounds = draggingScreenBounds;
                        p.EditBounds = draggingScreenRec;

                        draggingScreen = -1;
                    }
                    else
                    {
                        // return to default position
                        p.Owner = null;
                        p.EditBounds = GetDefaultBounds(draggingIndex);
                        p.ScreenIndex = -1;
                    }

                    UpdatePlayers(); // force a player update                    

                    Invalidate();
                }
            }
        }

        private Rectangle GetDefaultBounds(int index)
        {
            float lineWidth = index * playerSize;
            float line = (float)Math.Round(((lineWidth + playerSize) / (double)playersArea.Width) - 0.5);
            int perLine = (int)Math.Round((playersArea.Width / (double)playerSize) - 0.5);

            float x = playersArea.X + (index * playerSize) - (perLine * playerSize * line);
            float y = playersArea.Y + (playerSize * line);

            return new Rectangle((int)x, (int)y, playerSize, playerSize);
        }

        public void RefreshAll()
        {
            Refresh();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            
#if DEBUG
            //g.FillRectangle(Brushes.Green, playersArea);
            //g.FillRectangle(Brushes.CornflowerBlue, screensArea);
            //g.FillRectangle(Brushes.CornflowerBlue, new RectangleF(0, 0, Width, Height));
#endif

            UpdateScreens();

            for (int i = 0; i < screens.Length; i++)
            {
                UserScreen s = screens[i];
                g.DrawRectangle(Pens.White, s.UIBounds);
                g.DrawRectangle(Pens.White, s.SwapTypeBounds);

                switch (s.Type)
                {
                    case UserScreenType.FullScreen:
                        g.DrawImage(Resources.fullscreen, s.SwapTypeBounds);
                        break;
                    case UserScreenType.DualHorizontal:
                        g.DrawImage(Resources.horizontal, s.SwapTypeBounds);
                        break;
                    case UserScreenType.DualVertical:
                        g.DrawImage(Resources.vertical, s.SwapTypeBounds);
                        break;
                    case UserScreenType.FourPlayers:
                        g.DrawImage(Resources._4players, s.SwapTypeBounds);
                        break;
                    case UserScreenType.SixPlayers:
                        g.DrawImage(Resources._6players, s.SwapTypeBounds);
                        break;
                    case UserScreenType.EightPlayers:
                        g.DrawImage(Resources._8players, s.SwapTypeBounds);
                        break;
                    case UserScreenType.SixteenPlayers:
                        g.DrawImage(Resources._16players, s.SwapTypeBounds);
                        break;
                    case UserScreenType.Custom:
                        g.DrawImage(Resources.customLayout, s.SwapTypeBounds);
                        break;
                }
            }

            var players = profile.PlayerData;
            if (players.Count == 0)
            {
                g.DrawString("No Gamepads connected", playerTextFont, Brushes.Red, new PointF(20, 40));
            }
            else
            {
                
                for (int i = 0; i < players.Count; i++)
                {
                    
                    PlayerInfo info = players[i];
                    //MessageBox.Show("Game name: " + ggi.GameName + "\nDInputEnabled: " + ggi.Hook.DInputEnabled + "\nDInputForceDisable: " + ggi.Hook.DInputForceDisable + "\nXInputReroute: " + ggi.Hook.XInputReroute + "\nPlayers count: " + players.Count + "\n\nController Name: " + info.GamepadName + "\nHID Device ID: " + info.HIDDeviceID + "\nInstance GUID: " + info.GamepadGuid + "\nSlot: " + i + "\nIsXInput: " + info.IsXInput + "\nIsDInput: " + info.IsDInput + "\nIsKeyboardPlayer: " + info.IsKeyboardPlayer + "\nRaw hid: " + info.DInputJoystick.Properties.InterfacePath);
                    Rectangle s = info.EditBounds;
                    g.ResetClip();
                    g.Clip = new Region(new RectangleF(s.X, s.Y, s.Width + 1, s.Height + 1));

                    Rectangle gamepadRect = RectangleUtil.ScaleAndCenter(gamepadImg.Size, s);

                    string str = (i + 1).ToString();
                    SizeF size = g.MeasureString(str, playerFont);
                    PointF loc = RectangleUtil.Center(size, s);
                    if(gamePadPressed == info.GamepadId)
                    {
                        g.FillRectangle(Brushes.Green, gamepadRect);
                        gamePadPressed = -1;
                    }
                    if (info.IsXInput)
                    {

                        loc.Y -= gamepadRect.Height * 0.1f;
                        
                        GamepadButtonFlags flags = (GamepadButtonFlags)info.GamepadMask;
                        //g.DrawString(flags.ToString(), smallTextFont, Brushes.White, new PointF(loc.X, loc.Y + gamepadRect.Height * 0.01f));

                        if(ini.IniReadValue("ControllerMapping",info.HIDDeviceID) != "")
                        {
                            str = ini.IniReadValue("ControllerMapping", info.HIDDeviceID);
                            size = g.MeasureString(str, playerCustomFont);
                            loc = RectangleUtil.Center(size, s);
                            loc.Y -= 10;
                            g.DrawString(str, playerCustomFont, Brushes.White, loc);
                        }
                        else
                        {
                            g.DrawString((info.GamepadId + 1).ToString(), playerFont, Brushes.White, loc);
                        }
                        g.DrawImage(gamepadImg, gamepadRect);
                    }
                    else if (info.IsKeyboardPlayer)
                    {
                        g.DrawImage(keyboardImg, gamepadRect);
                    }
                    else
                    {
                        loc.Y -= gamepadRect.Height * 0.2f;
                        if (ini.IniReadValue("ControllerMapping", info.HIDDeviceID) != "")
                        {
                            str = ini.IniReadValue("ControllerMapping", info.HIDDeviceID);
                            size = g.MeasureString(str, playerCustomFont);
                            loc = RectangleUtil.Center(size, s);
                            loc.Y -= 10;
                            g.DrawString(str, playerCustomFont, Brushes.White, loc);
                        }
                        else
                        {
                            size = g.MeasureString(str, playerTextFont);
                            loc = RectangleUtil.Center(size, s);
                            loc.Y -= 12;
                            g.DrawString((info.GamepadId + 1).ToString()/*info.GamepadName*/, playerTextFont, Brushes.White, loc);
                        }
                        g.DrawImage(genericImg, gamepadRect);
                    }

                    if (info.ScreenIndex != -1)
                    {
                        UserScreen screen = screens[info.ScreenIndex];
                        if ((s.Height + s.Y) - (screen.UIBounds.Height + screen.UIBounds.Y) == -1)
                        {
                            s.Height += 1;
                        }
                        g.Clip = new Region(new RectangleF(s.X, s.Y, s.Width + 1, s.Height + 1));
                        g.DrawRectangle(Pens.Green, s);
                    }
                }
            }
            g.ResetClip();

            if (dragging && draggingScreen != -1)
            {
                g.DrawRectangle(Pens.Red, draggingScreenRec);
            }

            g.DrawString("Gamepads", playerTextFont, Brushes.White, new PointF(10, 10));

            SizeF dragEachGamepadSize;
            string dragEachGamepad = "Drag each gamepad to a screen\nClick top-left corner to change layout\nRight click player to change size";
            dragEachGamepad = StringUtil.WrapString(Width /** 0.3f*/, dragEachGamepad, g, extraSmallTextFont, out dragEachGamepadSize);
            g.DrawString(dragEachGamepad, extraSmallTextFont, Brushes.White, new PointF(Width - dragEachGamepadSize.Width, 4/*playersArea.Y - 50*/));

            //string bottomText = "";
            //GenericGameInfo ggi = game.Game;
            //if (ggi.Description?.Length > 0)
            //{
            //    bottomText = "Script Author Notes: " + ggi.Description;
            //    SizeF bottomTextSize;
            //    //string bottomText = ggi.Description; //"Click on screen's top-left corner to change players on that screen. (4-player only) Right click player to change size";
            //    bottomText = StringUtil.WrapString(Width - 20, bottomText, g, smallTextFont, out bottomTextSize);
            //    g.DrawString(bottomText, smallTextFont, Brushes.White, new PointF(10, (Height * 0.95f)));

            //    // make text smaller
            //    //int charSize = TextRenderer.MeasureText("g", title.Font).Width;
            //    //int toRemove = (int)((reservedSpaceLabel - labelSize.Width) / (float)charSize);
            //    //toRemove = Math.Max(toRemove + 3, 7);
            //    //title.Text = TitleText.Remove(TitleText.Length - toRemove, toRemove) + "...";
            //}

            
            
        }

        //private void InitializeComponent()
        //{
        //    this.SuspendLayout();
        //    // 
        //    // PositionsControl
        //    // 
        //    this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        //    this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        //    this.Name = "PositionsControl";
        //    this.ResumeLayout(false);

        //}
    }
}
