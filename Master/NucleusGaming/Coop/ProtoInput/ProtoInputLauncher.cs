using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Nucleus.Gaming.Coop.InputManagement;

namespace Nucleus.Gaming.Coop.ProtoInput
{
	public static class ProtoInputLauncher
	{
		private static List<uint> trackedInstanceHandles = new List<uint>();

		public static void InjectStartup(
			string exePath, 
			string commandLine, 
			uint processCreationFlags, 
			string dllFolderPath, 
			int instanceIndex,
			GenericGameInfo gen,
			PlayerInfo player,
			out uint pid,
			int mouseHandle = -1,
			int keyboardHandle = -1,
			int controllerIndex = 0)
		{
			//TODO: nucleus environment with proto input startup

			if (!dllFolderPath.EndsWith("\\"))
				dllFolderPath += "\\";

			uint instanceHandle = ProtoInput.protoInput.EasyHookInjectStartup(exePath, commandLine, 0, dllFolderPath, out pid);


			if (instanceHandle == 0)
			{
				MessageBox.Show("ProtoInput failed to startup inject", "Error", MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
			else
			{
				SetupInstance(instanceHandle, instanceIndex, gen, player, mouseHandle, keyboardHandle, controllerIndex);
				
				ProtoInput.protoInput.WakeUpProcess(instanceHandle);
			}
		}

		public static void InjectRuntime(
			bool easyHookMethod,
			bool easyHookStealthMethod,
			bool remoteLoadLibMethod,
			uint pid,
			string dllFolderPath,
			int instanceIndex,
			GenericGameInfo gen,
			PlayerInfo player,
			int mouseHandle = -1,
			int keyboardHandle = -1,
			int controllerIndex = 0)
		{
			if (!dllFolderPath.EndsWith("\\"))
				dllFolderPath += "\\";

			uint instanceHandle = 0;

			if (easyHookStealthMethod)
				instanceHandle = ProtoInput.protoInput.EasyHookStealthInjectRuntime(pid, dllFolderPath);
			else if (easyHookMethod)
				instanceHandle = ProtoInput.protoInput.EasyHookInjectRuntime(pid, dllFolderPath);
			else if (remoteLoadLibMethod)
				instanceHandle = ProtoInput.protoInput.RemoteLoadLibraryInjectRuntime(pid, dllFolderPath);

			if (instanceHandle == 0)
			{
				MessageBox.Show("ProtoInput failed to runtime inject", "Error", MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
			else
			{
				SetupInstance(instanceHandle, instanceIndex, gen, player, mouseHandle, keyboardHandle, controllerIndex);
			}
		}

		private static void SetupInstance(uint instanceHandle, int instanceIndex, GenericGameInfo gen, PlayerInfo player, int mouseHandle, int keyboardHandle, int controllerIndex)
		{
			Debug.WriteLine("Setting up ProtoInput instance " + instanceIndex);

			player.ProtoInputInstanceHandle = instanceHandle;

			ProtoInput.protoInput.SetupState(instanceHandle, instanceIndex);

			if (gen.ProtoInput.RegisterRawInputHook) ProtoInput.protoInput.InstallHook(instanceHandle, ProtoInput.ProtoHookIDs.RegisterRawInputHookID);
			if (gen.ProtoInput.GetRawInputDataHook) ProtoInput.protoInput.InstallHook(instanceHandle, ProtoInput.ProtoHookIDs.GetRawInputDataHookID);
			if (gen.ProtoInput.MessageFilterHook) ProtoInput.protoInput.InstallHook(instanceHandle, ProtoInput.ProtoHookIDs.MessageFilterHookID);
			if (gen.ProtoInput.GetCursorPosHook) ProtoInput.protoInput.InstallHook(instanceHandle, ProtoInput.ProtoHookIDs.GetCursorPosHookID);
			if (gen.ProtoInput.SetCursorPosHook) ProtoInput.protoInput.InstallHook(instanceHandle, ProtoInput.ProtoHookIDs.SetCursorPosHookID);
			if (gen.ProtoInput.GetKeyStateHook) ProtoInput.protoInput.InstallHook(instanceHandle, ProtoInput.ProtoHookIDs.GetKeyStateHookID);
			if (gen.ProtoInput.GetAsyncKeyStateHook) ProtoInput.protoInput.InstallHook(instanceHandle, ProtoInput.ProtoHookIDs.GetAsyncKeyStateHookID);
			if (gen.ProtoInput.GetKeyboardStateHook) ProtoInput.protoInput.InstallHook(instanceHandle, ProtoInput.ProtoHookIDs.GetKeyboardStateHookID);
			if (gen.ProtoInput.CursorVisibilityHook) ProtoInput.protoInput.InstallHook(instanceHandle, ProtoInput.ProtoHookIDs.CursorVisibilityStateHookID);
			if (gen.ProtoInput.ClipCursorHook) ProtoInput.protoInput.InstallHook(instanceHandle, ProtoInput.ProtoHookIDs.ClipCursorHookID);
			if (gen.ProtoInput.FocusHooks) ProtoInput.protoInput.InstallHook(instanceHandle, ProtoInput.ProtoHookIDs.FocusHooksHookID);
			if (gen.ProtoInput.RenameHandlesHook) ProtoInput.protoInput.InstallHook(instanceHandle, ProtoInput.ProtoHookIDs.RenameHandlesHookID);

			ProtoInput.protoInput.SetUseOpenXinput(instanceHandle, gen.ProtoInput.UseOpenXinput);
			ProtoInput.protoInput.SetUseDinputRedirection(instanceHandle, gen.ProtoInput.UseDinputRedirection);
			if (gen.ProtoInput.XinputHook) ProtoInput.protoInput.InstallHook(instanceHandle, ProtoInput.ProtoHookIDs.XinputHookID);

			if (player.IsDInput) ProtoInput.protoInput.SetDinputDeviceGUID(instanceHandle, player.GamepadGuid);
			if (gen.ProtoInput.DinputDeviceHook) ProtoInput.protoInput.InstallHook(instanceHandle, ProtoInput.ProtoHookIDs.DinputOrderHookID);

			if (gen.ProtoInput.RawInputFilter) ProtoInput.protoInput.EnableMessageFilter(instanceHandle, ProtoInput.ProtoMessageFilterIDs.RawInputFilterID);
			if (gen.ProtoInput.MouseMoveFilter) ProtoInput.protoInput.EnableMessageFilter(instanceHandle, ProtoInput.ProtoMessageFilterIDs.MouseMoveFilterID);
			if (gen.ProtoInput.MouseActivateFilter) ProtoInput.protoInput.EnableMessageFilter(instanceHandle, ProtoInput.ProtoMessageFilterIDs.MouseActivateFilterID);
			if (gen.ProtoInput.WindowActivateFilter) ProtoInput.protoInput.EnableMessageFilter(instanceHandle, ProtoInput.ProtoMessageFilterIDs.WindowActivateFilterID);
			if (gen.ProtoInput.WindowActvateAppFilter) ProtoInput.protoInput.EnableMessageFilter(instanceHandle, ProtoInput.ProtoMessageFilterIDs.WindowActivateAppFilterID);
			if (gen.ProtoInput.MouseWheelFilter) ProtoInput.protoInput.EnableMessageFilter(instanceHandle, ProtoInput.ProtoMessageFilterIDs.MouseWheelFilterID);
			if (gen.ProtoInput.MouseButtonFilter) ProtoInput.protoInput.EnableMessageFilter(instanceHandle, ProtoInput.ProtoMessageFilterIDs.MouseButtonFilterID);
			if (gen.ProtoInput.KeyboardButtonFilter) ProtoInput.protoInput.EnableMessageFilter(instanceHandle, ProtoInput.ProtoMessageFilterIDs.KeyboardButtonFilterID);

			if (gen.ProtoInput.BlockedMessages != null)
			{
				foreach (uint blockedMessage in gen.ProtoInput.BlockedMessages)
				{
					ProtoInput.protoInput.EnableMessageBlock(instanceHandle, blockedMessage);
				}
			}

			ProtoInput.protoInput.SetupMessagesToSend(instanceHandle,
								gen.ProtoInput.SendMouseWheelMessages,
								gen.ProtoInput.SendMouseButtonMessages,
								gen.ProtoInput.SendMouseMovementMessages,
								gen.ProtoInput.SendKeyboardButtonMessages);

			if (gen.ProtoInput.EnableFocusMessageLoop)
				ProtoInput.protoInput.StartFocusMessageLoop(instanceHandle,
									  gen.ProtoInput.FocusLoopIntervalMilliseconds,
									  gen.ProtoInput.FocusLoop_WM_ACTIVATE,
									  gen.ProtoInput.FocusLoop_WM_ACTIVATEAPP,
									  gen.ProtoInput.FocusLoop_WM_NCACTIVATE,
									  gen.ProtoInput.FocusLoop_WM_SETFOCUS,
									  gen.ProtoInput.FocusLoop_WM_MOUSEACTIVATE);

			ProtoInput.protoInput.SetDrawFakeCursor(instanceHandle, gen.ProtoInput.DrawFakeCursor);

			if (gen.ProtoInput.RenameHandles != null)
			{
				foreach (string renameHandle in gen.ProtoInput.RenameHandles)
				{
					ProtoInput.protoInput.AddHandleToRename(instanceHandle, renameHandle);
				}
			}

			if (gen.ProtoInput.RenameNamedPipes != null)
			{
				foreach (string renamePipe in gen.ProtoInput.RenameNamedPipes)
				{
					ProtoInput.protoInput.AddNamedPipeToRename(instanceHandle, renamePipe);
				}
			}

			if (mouseHandle != -1)
				ProtoInput.protoInput.AddSelectedMouseHandle(instanceHandle, (uint)mouseHandle);

			if (keyboardHandle != -1)
				ProtoInput.protoInput.AddSelectedKeyboardHandle(instanceHandle, (uint)keyboardHandle);

			ProtoInput.protoInput.SetControllerIndex(instanceHandle, controllerIndex < 0 ? 0 : (uint)controllerIndex);

			//SetExternalFreezeFakeInput(instanceHandle, !isInputCurrentlyLocked && freezeGameInputWhileInputNotLocked);

			trackedInstanceHandles.Add(instanceHandle);

			if (gen.ProtoInput.FreezeExternalInputWhenInputNotLocked)
				NotifyInputLockChange();
		}

		public static void NotifyInputLockChange()
		{
			bool freezeExternal = !LockInput.IsLocked;

			foreach (var instanceHandle in trackedInstanceHandles)
			{
				ProtoInput.protoInput.SetExternalFreezeFakeInput(instanceHandle, freezeExternal);
			}
		}
	}
}
