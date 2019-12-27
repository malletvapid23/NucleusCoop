using Jint.Parser.Ast;
using Nucleus.Gaming.Coop.BasicTypes;
using Nucleus.Gaming.Coop.InputManagement.Enums;
using Nucleus.Gaming.Coop.InputManagement.Logging;
using Nucleus.Gaming.Coop.InputManagement.Structs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Nucleus.Gaming.Coop.InputManagement
{
	class RawInputProcessor
	{
		#region Temporary options
		//TODO: remove temporary options
		//private static bool sendNormalKeyboardInput = true;
		//private static bool sendRawKeyboardInput = false;
		//private static bool forwardRawMouseInput = false;
		//private static bool sendScrollWheel = false;
		//private static bool sendNormalMouse = true;

		private static int toggleLockInputKey = 0x23;//End
		private bool inputLocked = false;

		#endregion
		
		//TODO: implement splitScreenRunning
		readonly Ref<bool> splitScreenRunning;

		private List<Window> Windows => RawInputManager.windows;

		public static GenericGameInfo CurrentGameInfo { get; set; } = null;
		public static GameProfile CurrentProfile { get; set; } = null;
		private List<PlayerInfo> PlayerInfos => CurrentProfile?.PlayerData;

		//leftMiddleRight: left=1, middle=2, right=3, xbutton1=4, xbutton2=5
		private readonly Dictionary<RawInputButtonFlags, (MouseEvents msg, uint wParam, ushort leftMiddleRight, bool isButtonDown, int VKey)> _buttonFlagToMouseEvents = new Dictionary<RawInputButtonFlags, (MouseEvents, uint, ushort, bool, int)>()
		{
			{ RawInputButtonFlags.RI_MOUSE_LEFT_BUTTON_DOWN,    (MouseEvents.WM_LBUTTONDOWN ,   0x0001,     1, true,    0x01) },
			{ RawInputButtonFlags.RI_MOUSE_LEFT_BUTTON_UP,      (MouseEvents.WM_LBUTTONUP,      0,          1, false,   0x01) },

			{ RawInputButtonFlags.RI_MOUSE_RIGHT_BUTTON_DOWN,   (MouseEvents.WM_RBUTTONDOWN,    0x0002,     2, true,    0x02) },
			{ RawInputButtonFlags.RI_MOUSE_RIGHT_BUTTON_UP,     (MouseEvents.WM_RBUTTONUP,      0,          2, false,   0x02) },

			{ RawInputButtonFlags.RI_MOUSE_MIDDLE_BUTTON_DOWN,  (MouseEvents.WM_MBUTTONDOWN,    0x0010,     3, true,    0x04) },
			{ RawInputButtonFlags.RI_MOUSE_MIDDLE_BUTTON_UP,    (MouseEvents.WM_MBUTTONUP,      0,          3, false,   0x04) },

			{ RawInputButtonFlags.RI_MOUSE_BUTTON_4_DOWN,       (MouseEvents.WM_XBUTTONDOWN,    0x0120,     4, true,    0x05) },// (0x0001 << 8) | 0x0020 = 0x0120
			{ RawInputButtonFlags.RI_MOUSE_BUTTON_4_UP,         (MouseEvents.WM_XBUTTONUP,      0,          4, false,   0x05) },

			{ RawInputButtonFlags.RI_MOUSE_BUTTON_5_DOWN,       (MouseEvents.WM_XBUTTONDOWN,    0x0240,     5, true,    0x06) },//(0x0002 << 8) | 0x0040 = 0x0240
			{ RawInputButtonFlags.RI_MOUSE_BUTTON_5_UP,         (MouseEvents.WM_XBUTTONUP,      0,          5, false,   0x06) }
		};

		public RawInputProcessor(Ref<bool> splitScreenRunning)
		{
			this.splitScreenRunning = splitScreenRunning;
		}

		public void WndProc(Message msg)
		{
			IntPtr hRawInput = msg.LParam;

			Process(hRawInput);
		}

		private void ProcessKeyboard(IntPtr hRawInput, RAWINPUT rawBuffer, Window window, IntPtr hWnd, uint keyboardMessage, bool keyUpOrDown)
		{
			//TODO: implement
			/*if (!Program.SplitScreenManager.IsRunningInSplitScreen)
			{
				return;
			}*/

			if (!keyUpOrDown)
			{
				return;
			}
						
			if (CurrentGameInfo.SendNormalKeyboardInput)
			{
				uint scanCode = rawBuffer.data.keyboard.MakeCode;
				ushort vKey = rawBuffer.data.keyboard.VKey;

				bool keyDown = keyboardMessage == (uint)KeyboardEvents.WM_KEYDOWN;

				//uint code = 0x000000000000001 | (scanCode << 16);//32-bit
				uint code = scanCode << 16;//32-bit

				BitArray keysDown = window.keysDown;
				bool stateChangedSinceLast = vKey < keysDown.Length && keyDown != keysDown[vKey];

				if (keyDown)
				{
					//bit 30 : The previous key state. The value is 1 if the key is down before the message is sent, or it is zero if the key is up.
					if (vKey < keysDown.Length && keysDown[vKey])
					{
						code |= 0x40000000;
					}
				}
				else
				{
					code |= 0xC0000000;//WM_KEYUP requires the bit 31 and 30 to be 1
					code |= 0x000000000000001;
				}

				code |= 1;

				if (vKey < keysDown.Length)
					keysDown[vKey] = keyDown;

				if ((CurrentGameInfo.HookGetKeyState || CurrentGameInfo.HookGetAsyncKeyState || CurrentGameInfo.HookGetKeyboardState) && stateChangedSinceLast)
				{
					window.HookPipe.WriteMessage(0x02, vKey, keyDown ? 1 : 0);
				}

				//This also (sometimes) makes GetKeyboardState work, as windows uses the message queue for GetKeyboardState
				WinApi.PostMessageA(hWnd, keyboardMessage, (IntPtr)vKey, (UIntPtr)code);
			}

			//Resend raw input to application. Works for some games only
			if (CurrentGameInfo.ForwardRawKeyboardInput)
			{
				WinApi.PostMessageA(window.hWnd, (uint)MessageTypes.WM_INPUT, (IntPtr)0x0000, hRawInput);

				if (window.DIEmWin_hWnd != IntPtr.Zero)
					WinApi.PostMessageA(window.DIEmWin_hWnd == IntPtr.Zero ? hWnd : window.DIEmWin_hWnd, (uint)MessageTypes.WM_INPUT, (IntPtr)0x0000, hRawInput);
			}
		}

		private void ProcessMouse(IntPtr hRawInput, RAWINPUT rawBuffer, Window window, IntPtr hWnd)
		{
			RAWMOUSE mouse = rawBuffer.data.mouse;
			IntPtr mouseHandle = rawBuffer.header.hDevice;
			
			//Resend raw input to application. Works for some games only
			if (CurrentGameInfo.ForwardRawMouseInput)
			{
				WinApi.PostMessageA(window.hWnd, (uint)MessageTypes.WM_INPUT, (IntPtr)0x0000, hRawInput);

				if (window.DIEmWin_hWnd != IntPtr.Zero)
					WinApi.PostMessageA(window.DIEmWin_hWnd == IntPtr.Zero ? hWnd : window.DIEmWin_hWnd, (uint)MessageTypes.WM_INPUT, (IntPtr)0x0000, hRawInput);
			}

			IntVector2 mouseVec = window.MousePosition;

			int deltaX = mouse.lLastX;
			int deltaY = mouse.lLastY;

			mouseVec.x = Math.Min(window.Width, Math.Max(mouseVec.x + deltaX, 0));
			mouseVec.y = Math.Min(window.Height, Math.Max(mouseVec.y + deltaY, 0));

			if (CurrentGameInfo.HookGetCursorPos)
			{
				window.HookPipe?.SendMousePosition(deltaX, deltaY, mouseVec.x, mouseVec.y);
			}

			long packedXY = mouseVec.y * 0x10000 + mouseVec.x;

			window.UpdateCursorPosition();

			//Mouse buttons.
			ushort f = mouse.usButtonFlags;
			if (f != 0)
			{
				foreach (var pair in _buttonFlagToMouseEvents)
				{
					if ((f & (ushort)pair.Key) > 0)
					{
						(MouseEvents msg, uint wParam, ushort leftMiddleRight, bool isButtonDown, int vKey) = pair.Value;
						//Logger.WriteLine(pair.Key);

						var state = window.MouseState;

						bool oldBtnState = false;
						if (leftMiddleRight == 1)
							oldBtnState = state.l;
						else if (leftMiddleRight == 2)
							oldBtnState = state.r;
						else if (leftMiddleRight == 3)
							oldBtnState = state.m;
						else if (leftMiddleRight == 4)
							oldBtnState = state.x1;
						else if (leftMiddleRight == 5)
							oldBtnState = state.x2;

						//TODO: needs option?
						if (oldBtnState != isButtonDown)
							WinApi.PostMessageA(hWnd, (uint)msg, (IntPtr)wParam, (IntPtr)packedXY);

						if ((CurrentGameInfo.HookGetAsyncKeyState || CurrentGameInfo.HookGetKeyState || CurrentGameInfo.HookGetKeyboardState) && (oldBtnState != isButtonDown))
							window.HookPipe.WriteMessage(0x02, vKey, isButtonDown ? 1 : 0);


						if (leftMiddleRight == 1)
						{
							state.l = isButtonDown;

							window.UpdateBounds();
						}
						else if (leftMiddleRight == 2)
						{
							state.r = isButtonDown;
						}
						else if (leftMiddleRight == 3)
						{
							state.m = isButtonDown;
						}
						else if (leftMiddleRight == 4)
						{
							state.x1 = isButtonDown;
						}
						else if (leftMiddleRight == 5)
						{
							state.x2 = isButtonDown;
						}

						window.MouseState = state;
					}
				}

				if (CurrentGameInfo.SendScrollWheel && (f & (ushort)RawInputButtonFlags.RI_MOUSE_WHEEL) > 0)
				{
					ushort delta = mouse.usButtonData;
					WinApi.PostMessageA(hWnd, (uint)MouseEvents.WM_MOUSEWHEEL, (IntPtr)((delta * 0x10000) + 0), (IntPtr)packedXY);
				}
			}

			if (CurrentGameInfo.SendNormalMouseInput)
			{
				ushort mouseMoveState = 0x0000;
				(bool l, bool m, bool r, bool x1, bool x2) = window.MouseState;
				if (l) mouseMoveState |= (ushort)WM_MOUSEMOVE_wParam.MK_LBUTTON;
				if (m) mouseMoveState |= (ushort)WM_MOUSEMOVE_wParam.MK_MBUTTON;
				if (r) mouseMoveState |= (ushort)WM_MOUSEMOVE_wParam.MK_RBUTTON;
				if (x1) mouseMoveState |= (ushort)WM_MOUSEMOVE_wParam.MK_XBUTTON1;
				if (x2) mouseMoveState |= (ushort)WM_MOUSEMOVE_wParam.MK_XBUTTON2;
				mouseMoveState |= 0b10000000;//Signature for USS 
				WinApi.PostMessageA(hWnd, (uint)MouseEvents.WM_MOUSEMOVE, (IntPtr)mouseMoveState, (IntPtr)packedXY);
			}
		}

		private void Process(IntPtr hRawInput)
		{
			uint pbDataSize = 0;
			
			int ret = WinApi.GetRawInputData(hRawInput, DataCommand.RID_INPUT, IntPtr.Zero, ref pbDataSize, Marshal.SizeOf(typeof(RAWINPUTHEADER)));

			if (ret == 0 && pbDataSize == WinApi.GetRawInputData(hRawInput, DataCommand.RID_INPUT, out RAWINPUT rawBuffer, ref pbDataSize, Marshal.SizeOf(typeof(RAWINPUTHEADER))))
			{
				var type = (HeaderDwType)rawBuffer.header.dwType;
				var hDevice = rawBuffer.header.hDevice;

				if (type == HeaderDwType.RIM_TYPEHID)
					return;


				//TODO: if not running split screen
				if (PlayerInfos != null)
				foreach (var toFlash in PlayerInfos.Where(x => x != null && (x.RawMouseDeviceHandle.Equals(hDevice) || x.RawKeyboardDeviceHandle.Equals(hDevice))))
				{
					toFlash.FlashIcon();
				}



				if (type == HeaderDwType.RIM_TYPEKEYBOARD)
				{
					uint keyboardMessage = rawBuffer.data.keyboard.Message;
					bool keyUpOrDown = keyboardMessage == (uint)KeyboardEvents.WM_KEYDOWN || keyboardMessage == (uint)KeyboardEvents.WM_KEYUP;

					if (keyboardMessage == (uint)KeyboardEvents.WM_KEYUP && (rawBuffer.data.keyboard.Flags | 1) != 0 && rawBuffer.data.keyboard.VKey == toggleLockInputKey)
					{
						Debug.WriteLine("Lock input key pressed");
						inputLocked = !inputLocked;
						if (inputLocked)
							LockInput.Lock();
						else
							LockInput.Unlock();
					}

					foreach (var window in Windows.Where(x => x.KeyboardAttached == hDevice))
					{
						ProcessKeyboard(hRawInput, rawBuffer, window, window.hWnd, keyboardMessage, keyUpOrDown);
					}
				}
				else if (type == HeaderDwType.RIM_TYPEMOUSE)
				{
					foreach (var window in Windows.Where(x => x.MouseAttached == hDevice))
					{
						if (window.NeedsCursorToBeCreatedOnMainMessageLoop)
						{
							//Cursor needs to be created on the MainForm message loop so it can be accessed in the loop.
							window.NeedsCursorToBeCreatedOnMainMessageLoop = false;
							window.CreateCursor();
						}

						ProcessMouse(hRawInput, rawBuffer, window, window.hWnd);
					}
				}
			}
		}
	}
}
