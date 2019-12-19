using Nucleus.Gaming.Coop.BasicTypes;
using Nucleus.Gaming.Coop.InputManagement;
using Nucleus.Gaming.Coop.InputManagement.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Nucleus.Gaming.Coop
{
	public class Window
	{
		//Window handle
		public readonly IntPtr hWnd;

		//Some games use an invisible window called DIEmWin. WM_INPUT needs to be sent to this hWnd instead of the visible game hWnd or it is ignored.
		public IntPtr DIEmWin_hWnd = IntPtr.Zero;

		public readonly int pid;//Process ID

		public IntPtr MouseAttached { get; set; } = new IntPtr(0);
		public IntVector2 MousePosition { get; } = new IntVector2();//This is a reference type. Relative to game window
		public (bool l, bool m, bool r, bool x1, bool x2) MouseState { get; set; } = (false, false, false, false, false);
		public byte WASD_State { get; set; } = 0;

		public IntPtr KeyboardAttached { get; set; } = new IntPtr(0);
		public readonly BitArray keysDown = new BitArray(0xFF);

		public int ControllerIndex { get; set; } = 0;//0 = none, 1234 = 1234

		public RECT Bounds { get; private set; }
		public int Width => Bounds.Right - Bounds.Left;
		public int Height => Bounds.Bottom - Bounds.Top;

		public HookPipe HookPipe { get; set; }

		#region Mouse Cursor
		/* How drawing the fake mouse cursor works:
		 * A transparent window (PointerForm) is created over the game window.
		 * When the mouse is moved, UpdateCursorPosition will tell the window to paint over the old cursor (wiping it) and draw the new one.
		 * If the mouse moves out of bounds of PointerForm, the centre of the window is moved to the mouse position.
		 * (Depends if Hook mouse visibility is enabled) In HooksCPP, SetCursor(NULL or not NULL) and ShowCursor(TRUE/FALSE) is monitored to show/hide cursor.
		 * When this is detected, it sends a message via a named pipe to set CursorVisibility, which which will wipe/draw the cursor.
		 * 
		 */

		private class PointerForm : Form
		{
			IntPtr hicon;

			//Screen coords (relative to primary monitor)
			public int screenX;
			public int screenY;

			public bool visible = true;

			private int oldScreenX;
			private int oldScreenY;

			IntPtr hWnd;

			System.Reflection.MethodInfo paintBkgMethod;

			System.Drawing.Graphics g = null;
			IntPtr h;
			bool hasDrawn = false;//We need to paint the entire window once at the start.

			const int windowWidth = 1300;//Minimum width
			const int windowHeight = 800;

			private readonly IntPtr pointerHandle;


			public PointerForm(IntPtr hWnd, int gameWindowWidth, int gameWindowHeight, int gameWindowX, int gameWindowY) : base()
			{
				this.hWnd = hWnd;
				this.pointerHandle = this.Handle;
				//CheckForIllegalCrossThreadCalls = false;

				Width = Math.Max(windowWidth, gameWindowWidth + 100);
				Height = Math.Max(windowHeight, gameWindowHeight + 100);
				Logger.WriteLine($"Cursor window width,height = {Width},{Height}");
				FormBorderStyle = FormBorderStyle.None;
				Text = "";
				StartPosition = FormStartPosition.Manual;
				Location = new System.Drawing.Point(gameWindowX + gameWindowWidth / 2, gameWindowY + gameWindowHeight / 2);
				TopMost = true;
				BackColor = System.Drawing.Color.Green;
				TransparencyKey = BackColor;
				ShowInTaskbar = false;
				//WinApi.SetWindowLongPtr(pointerHandle, (-8), hWnd);//Sets owner. Always draws above owner.

				hicon = Cursors.Arrow.Handle;

				paintBkgMethod = typeof(Control).GetMethod("PaintBackground", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new Type[] { typeof(PaintEventArgs), typeof(System.Drawing.Rectangle) }, null);
			}


			const int cursorWidthHeight = 100;

			protected override void OnPaintBackground(PaintEventArgs e)
			{
				if (!hasDrawn)
				{
					hasDrawn = true;
					base.OnPaintBackground(e);
					return;
				}

				//This only wipes a small area, which reduces CPU usage.
				paintBkgMethod.Invoke(this, new object[]{e,
					new System.Drawing.Rectangle(oldScreenX - Location.X, oldScreenY - Location.Y, cursorWidthHeight, cursorWidthHeight) });

				if (g == null)
				{
					//g = System.Drawing.Graphics.FromHwnd(pointerHandle);
					g = CreateGraphics();
					h = g.GetHdc();
				}

				if (visible)
					WinApi.DrawIcon(h, screenX - Location.X, screenY - Location.Y, hicon);//Coordinates are relative to Location of PointerForm

				oldScreenX = screenX;
				oldScreenY = screenY;

				//Greatly reduces CPU usage, doesn't lock any input. Insignificant delay. Mouse cursor is only used in menus, not first person.
				System.Threading.Thread.Sleep(1);
			}

			public void InvalidateMouse()
			{
				//Causes this region to be re-drawn
				//Invalidate(new System.Drawing.Rectangle(oldScreenX - Location.X, oldScreenY - Location.Y, cursorWidthHeight, cursorWidthHeight)); (Seems to work inconsistently)
				Invalidate();
			}

			//Wipes the entire window
			public void RepaintAll()
			{
				try
				{
					base.OnPaintBackground(new PaintEventArgs(g, Bounds));
				}
				catch (Exception e)
				{
					Debug.WriteLine($"Error in RepaintAll: {e}");
				}
			}
		}

		public bool NeedsCursorToBeCreatedOnMainMessageLoop { get; set; } = false;
		private PointerForm pointerForm = null;
		public bool CursorVisibility
		{
			get => pointerForm.visible;
			set
			{
				if (pointerForm != null)
				{
					pointerForm.visible = value;
				}
			}
		}

		public void CreateCursor()
		{
			pointerForm = new PointerForm(hWnd, Width, Height, Bounds.Left, Bounds.Top);
			pointerForm.Show();
		}

		bool hasRepaintedSinceLastInvisible = false;

		public void UpdateCursorPosition()
		{
			if (pointerForm != null)//If Draw Mouse is selected
			{
				if (pointerForm.visible)
				{
					hasRepaintedSinceLastInvisible = false;
					var p = new System.Drawing.Point(MousePosition.x, MousePosition.y);
					WinApi.ClientToScreen(hWnd, ref p);//p is screen coords

					pointerForm.screenX = p.X;
					pointerForm.screenY = p.Y;

					const int padding = 32;
					if (p.X <= pointerForm.Location.X + padding || p.Y <= pointerForm.Location.Y + padding ||
						p.X >= pointerForm.Location.X + pointerForm.Width - padding || p.Y >= pointerForm.Location.Y + pointerForm.Height - padding)
					{
						pointerForm.RepaintAll();
						pointerForm.Location = new System.Drawing.Point(p.X - pointerForm.Width / 2, p.Y - pointerForm.Height / 2);
					}

					//NucleusCoop brings the game window the the top, this brings the mouse top of top
					WinApi.BringWindowToTop(pointerForm.Handle);

					pointerForm.InvalidateMouse();
				}
				else if (!hasRepaintedSinceLastInvisible)
				{
					//If the cursor should be invisible, make sure the last cursor has been wiped.
					pointerForm.RepaintAll();
					hasRepaintedSinceLastInvisible = true;
				}
			}
		}

		public void KillCursor()
		{
			pointerForm?.Hide();
			pointerForm?.Dispose();
			pointerForm = null;
		}
		#endregion

		public Window(IntPtr hWnd)
		{
			this.hWnd = hWnd;
			WinApi.GetWindowThreadProcessId(hWnd, out this.pid);
			UpdateBounds();

			//Logger.WriteLine($"Bounds for hWnd={hWnd}: Left={Bounds.Left}, Right={Bounds.Right}, Top={Bounds.Top}, Bottom={Bounds.Bottom}, WIDTH={Width}, HEIGHT={Height}");
		}

		public void UpdateBounds()
		{
			//TODO: on another thread?
			WinApi.GetClientRect(hWnd, out var bounds);
			Bounds = bounds;
		}
	}
}
