// More or less a copy of the Controller class from SharpDX, except without the limit on 4 controllers 
// since we are going to be using OpenXinput

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Mathematics.Interop;
using SharpDX.Win32;
using SharpDX.XInput;

namespace Nucleus.Gaming.Coop
{
	public class OpenXinputController
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct XINPUT_VIBRATION
		{
			public ushort wLeftMotorSpeed;
			public ushort wRightMotorSpeed;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct XINPUT_GAMEPAD
		{
			public ushort wButtons;
			public byte bLeftTrigger;
			public byte bRightTrigger;
			public short sThumbLX;
			public short sThumbLY;
			public short sThumbRX;
			public short sThumbRY;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct XINPUT_STATE
		{
			public uint dwPacketNumber;
			public XINPUT_GAMEPAD Gamepad;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct DeviceInfo_t
		{
			public uint status;
			public IntPtr hDevice;
			public IntPtr hGuideWait;
			public byte dwUserIndex;
			[MarshalAs(UnmanagedType.LPWStr)] public string lpDevicePath;
			public uint dwDevicePathSize;
			[MarshalAs(UnmanagedType.LPWStr)] public string field_18;
			public uint field_1C;
			public ushort wType;
			public ushort field_22;
			public XINPUT_STATE DeviceState;
			public XINPUT_VIBRATION DeviceVibration;
			public ushort field_38;
			public ushort field_3A;
			public ushort field_3C;
			public ushort field_3E;
			public ushort field_40;
			public ushort field_42;
			public ushort field_44;
			public ushort field_46;
			public ushort field_48;
			public ushort field_4A;
			public ushort field_4C;
			public ushort vendorId;
			public ushort productId;
			public byte inputId;
			public byte bTriggers;
			public ushort wButtons;
			public ushort LeftStickVirtualKey;
			public ushort RightStickVirtualKey;
			public ushort field_5A;
			public ushort field_5C;
			public ushort field_5E;
		}

		public static class Native
		{
			[DllImport("openxinput1_3.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "XInputGetKeystroke")]
			public static extern int XInputGetKeystroke(int dwUserIndex, int dwReserved, out Keystroke keystrokeRef);

			[DllImport("openxinput1_3.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "XInputGetBatteryInformation")]
			public static extern int XInputGetBatteryInformation(int dwUserIndex, BatteryDeviceType devType, out BatteryInformation batteryInformationRef);

			public static unsafe int XInputSetState(int dwUserIndex, Vibration vibrationRef)
			{
				return XInputSetState_(dwUserIndex, (void*)(&vibrationRef));
			}

			[DllImport("openxinput1_3.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "XInputSetState")]
			private static extern unsafe int XInputSetState_(int arg0, void* arg1);

			[DllImport("openxinput1_3.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "XInputGetState")]
			public static extern int XInputGetState(int dwUserIndex, out State stateRef);

			[DllImport("openxinput1_3.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "XInputEnable")]
			public static extern void XInputEnable(RawBool arg0);

			[DllImport("openxinput1_3.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "XInputGetCapabilities")]
			public static extern int XInputGetCapabilities(int dwUserIndex, DeviceQueryType dwFlags, out Capabilities capabilitiesRef);

			// [DllImport("openxinput1_3.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "GetDeviceOnPort")]
			// public static extern int GetDeviceOnPort(uint dwUserIndex, ref DeviceInfo_t ppDevice, bool rescan);

			[DllImport("openxinput1_3.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "GetDevicePath")]
			[return: MarshalAs(UnmanagedType.BStr)]
			public static extern string GetDevicePath(uint dwUserIndex);
		}

		private readonly int userIndex;
		
		public OpenXinputController(int userIndex = 255)
		{
			this.userIndex = userIndex;
		}
		
		public BatteryInformation GetBatteryInformation(BatteryDeviceType batteryDeviceType)
		{
			BatteryInformation temp;
			ErrorCodeHelper.ToResult(Native.XInputGetBatteryInformation((int)this.userIndex, batteryDeviceType, out temp)).CheckError();
			return temp;
		}

		public Capabilities GetCapabilities(DeviceQueryType deviceQueryType)
		{
			Capabilities temp;
			ErrorCodeHelper.ToResult(Native.XInputGetCapabilities(userIndex, deviceQueryType, out temp)).CheckError();
			return temp;
		}

		public bool GetCapabilities(DeviceQueryType deviceQueryType, out Capabilities capabilities)
		{
			return Native.XInputGetCapabilities(userIndex, deviceQueryType, out capabilities) == 0;
		}

		public Result GetKeystroke(DeviceQueryType deviceQueryType, out Keystroke keystroke)
		{
			return ErrorCodeHelper.ToResult(Native.XInputGetKeystroke(userIndex, (int)deviceQueryType, out keystroke));
		}

		public State GetState()
		{
			State temp;
			ErrorCodeHelper.ToResult(Native.XInputGetState(userIndex, out temp)).CheckError();
			return temp;
		}

		public bool GetState(out State state)
		{
			return Native.XInputGetState(userIndex, out state) == 0;
		}

		public static void SetReporting(bool enableReporting)
		{
			Native.XInputEnable(enableReporting);
		}

		public Result SetVibration(Vibration vibration)
		{
			Result result = ErrorCodeHelper.ToResult(Native.XInputSetState(userIndex, vibration));
			result.CheckError();
			return result;
		}

		public bool IsConnected
		{
			get
			{
				State temp;
				return Native.XInputGetState(userIndex, out temp) == 0;
			}
		}
	}

}
