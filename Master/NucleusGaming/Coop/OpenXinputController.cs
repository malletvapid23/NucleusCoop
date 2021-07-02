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
	class OpenXinputController
	{
		static class Native
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
