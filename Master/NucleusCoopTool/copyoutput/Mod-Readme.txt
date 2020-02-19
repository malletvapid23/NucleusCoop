NucleusCoop Alpha 8 Mod - version 0.9.9.1

This a mod for NucleusCoop Alpha 8 and it includes the following:

1. New options in game scripts -

Game.KillMutexType = "Mutant"; 				//Specify the type of mutex to kill, for example "Mutant", "Event" or "Semaphore" | default: "Mutant"
Game.KillMutexDelay = 10; 				//# of seconds to wait before killing the mutex | Only works with killing not renaming | default: 0
Game.KeepSymLinkOnExit = true; 				//Enable or disable symlink files from being deleted when Nucleus is closed | default: false
Game.FakeFocus = true;					//Enable or disable the sending of focus messages to each game window at a regular interval | default: false
Game.HookFocus = true;					//Enable or disable hooks to trick the game into thinking it has focus | default: false
Game.HookInit = true;					//Enable or disable hooks of functions some games may try and use to prevent multiple instances from running | default: false
Game.ProcessorPriorityClass = "Normal";			//Sets the overall priority category for the associated process. Can be "AboveNormal", "High" or "RealTime" | default: "Normal"
Game.UseProcessor = "1,2,3,4";				//Sets the processors on which the game's threads can run on (provide the # of the cores in a comma-seperated list in quotations) | default: system delegation
Game.IdealProcessor = 2;				//Sets the preferred processor for the game's threads, used when the system schedules threads, to determine which processor to run the thread on | default: system delegation
Game.SymlinkFiles = ["game.exe","settings.cfg"];	//Symlink individual files from the game directory to the instanced folder
Game.CopyFiles = ["game.exe","settings.cfg"];		//Copy files from the game directory to the instanced folder
Game.HasDynamicWindowTitle = false;			//Work-around for ForceFocusWindowName having to match 1:1 with game window title for resizing, positioning and focus | default: false
Game.ForceWindowTitle = false;				//Forces the game window title to be whatever is specified in Game.Hook.ForceFocusWindowName (triggers once, after all instances have started) | default: false
Game.CMDLaunch = false;					//Launch a game using command prompt
Game.CMDOptions = ["ops1","ops2"];			//Specify command line options if game is launched using command prompt | requires CMDLaunch to be true, each element is for a different instance
Game.SetWindowHook = true;				//Prevent games from resizing window on their own | Hooks after all instances have been opened (see Game.SetWindowHookStart for an alternative)
Game.PromptBetweenInstances = true;			//Prompt the user with a messagebox to let the user decide when to open the next instance | default: false, PauseBetweenStarts STILL required
Game.HideCursor = true;					//Hide the mouse cursor in game instances
Game.HideDesktop = false;				//Hide the desktop background with a solid black background
Game.HideTaskbar = true;				//Most games hide the taskbar when placed on-top but for games that don't you can use this.
Game.RenameNotKillMutex = true;				//Instead of killing the mutex, rename it to something else | Requires Game.KillMutex to contain the EXACT name of the mutexes for this to work (no partial)
Game.IdInWindowTitle = true;				//Adds the process ID to the end of the window title
Game.ChangeExe = true;					//Will rename the game's executable to "<exe name> - Player X", x being the instance/player #
Game.HardcopyGame = true;				//Instead of Symlinking, create hard copies of the games files (copy/paste) | Be careful of storage, takes a LONG time to start a game when copying
Game.SupportsKeyboard = true;				//Enable/disable use of a keyboard player
Game.KeyboardPlayerFirst = true;			//If the keyboard player should be processed first
Game.UseX360ce = true;					//Before launching any games, NucleusCoop will open x360ce and let the user set up their controllers before continuing | close x360ce to continue | Don't use with custom dlls
Game.Hook.UseAlpha8CustomDll = false;			//Use the xinput custom dll from Alpha 8 instead of Alpha 10 | Will still force alpha 10 custom dll if game is x64
Game.Hook.FixResolution = false;			//Should the custom dll do the resizing? | Only works with Alpha 10 custom dll | default: false
Game.Hook.FixPosition = false;				//Should the custom dll do the repositioning? | Only works with Alpha 10 custom dll | default: false
Context.Hook.WindowX = 0;				//If manual positioning, what is the window's X coordinate | If both X and Y value > 0, window will be positioned manually
Context.Hook.WindowY = 0;				//If manual positioning, what is the window's Y coordinate | If both X and Y value > 0, window will be positioned manually
Context.Hook.ResWidth = 1280; 				//If manual resizing, what is the window's width | If both ResWidth and ResHeight value > 0, window will be positioned manually
Context.Hook.ResHeight = 720;				//If manual resizing, what is the window's height | If both ResWidth and ResHeight value > 0, window will be positioned manually
Game.KeepAspectRatio = false;				//Should the game window try and keep it's aspect ratio when being resized? | default: false
Game.PartialMutexSearch = false;			//When killing mutexes, should a partial search be done with the mutex name? | Renaming mutexes requires an exact match | default: false
Game.UseGoldberg = false;				//Use the built-in Goldberg features in Nucleus | default: false
Game.GoldbergNeedSteamInterface = false;		//Some older games require a steam_interfaces.txt file for Goldberg to work | Will first search orig game path and nucleus games path, if not found then tries to create one with the GoldbergNeedSteamInterface command
Game.GoldbergLanguage = "english";			//Manually specify what language you want Goldberg to use for the game | by default, Goldberg will use the language you chose in Steam
Game.OrigSteamDllPath = "C:\full path\steam_api.dll";	//If steam_interface.txt is required, provide full path here to the original steam_api(64).dll and Nucleus will create one if it can't find an existing copy
Game.XInputPlusDll = [ "xinput1_3.dll" ];		//Set up XInputPlus | If multiple dlls required, use comma seperated strings
Game.PlayersPerInstance = 2;				//If using XInputPlus or X360ce and there are multiple players playing the same instance, set the # per instance here
Game.UseDevReorder = true;				//Set up Devreorder
Game.CopyCustomUtils = [ "d3d9.dll" ];			//Copy the a file or folder you specify between the quotes to each instance folder, if the file/folder is located in Nucleus folder\utils\User. This function also accepts two additional parameters, a relative path from the game root folder if the file needs to be placed somewhere else within instance folder and one parameter to indicate which instances to copy the file to if it only needs to be in some. Use of parameters is separated by a | character. So it would look something like this [ "filename.ini|\\bin|1,2" ]. This example would copy filename.ini from Nucleus\utils\User to Instance0\bin and Instance1\bin. If you don't specify which instances, it will do them all by default. If you don't specify a path then root instance folder is used. If you want instances but root folder you would just do [ "filename.ini||1,2" ] | If copying multiple files, use comma seperated strings
Game.XboxOneControllerFix = false;			//When using x360ce, this will set certain hooktype that may work for xbox one controllers if the normal method does not work
Game.HexEditAllExes = [ "before|afters" ];		//Will do a text value replacement in a file for every instance, accepts two parameters, one on what to look for, and what it should be replaced with (seperated with a "|") | Works with multiple values | Works in conjunction with HexEditExe (this will trigger first)
Game.HexEditExe = [ "before|afters" ];			//Will do a text value replacement in a file for a specific instance, accepts two parameters, one on what to look for, and what it should be replaced with (seperated with a "|") | Works with multiple values, each comma seperated string is the order of the instances | Works in conjunction with HexEditAllExe (this will trigger second)
Game.HexEditFile = [ "filePath|before|afters" ]; 	//Works same as HexEditExe function but with a file you specify as an extra parameter (first one) | filePath is relative path from the game root folder
Game.HexEditAllFiles = [ "filePath|before|afters" ]; 	//Works same as HexEditAllExes function but with a file you specify as an extra parameter (first one) | filePath is relative path from the game root folder
Game.SetWindowHookStart = false;			//Prevent games from resizing window on their own | Hooks upon game starting up (see Game.SetWindowHook for an alternative)
Game.BlockRawInput = false;				//Disable raw input devices in game | default: false
Game.UseForceBindIP = false;				//Set up game instances with ForceBindIP; each instance having their own IP
Game.ResetWindows = false;				//After each new instance opens, resize, reposition and remove borders of the previous instance
Game.Description = "Hello World";			//Display a message to the end-user, that will appear when user selects the game for this script (bottom middle when placing controllers) | useful if there is anything the end-user needs to know before-hand | Only first two or three sentences will appear in UI, but full message can be viewed if user right clicks on the game in the list
Game.GoldbergExperimental = false;			//Use the experimental branch of Goldberg | Requires `Game.UseGoldberg = true` | default: false
Game.GoldbergIgnoreSteamAppId = false;			//When set to true, Goldberg will not create a steam_appid.txt file
Game.UseSteamStubDRMPatcher = false;			//Use UberPsyX's Steam Stub DRM Patcher to remove Steam Stub DRM from a protected executable
Game.HardlinkGame = false;				//Hardlink files instead of Symlink (or hard copy) | Directories will still be symlinked but files will be hardlinked
Game.SetForegroundWindowElsewhere = false;		//Set the foreground window to be something other than game windows
Game.PreventWindowDeactivation = false;			//Blocks the processing of the windows message that gets sent when the window loses focus
Game.SymlinkFolders = false;				//Folders by default are hardcopied, with this enabled, folders will be symlinked instead | warning files placed in symlink folders will appear in original game files too
Game.CreateSteamAppIdByExe = false;			//Create a steam_appid.txt file where the game executable is
Game.ForceSymlink = false;				//Force game to be symlinked each time it is ran
Game.UseProcessorsPerInstance = [ "1,2","3,4" ];	//Sets the processors on which an instances game's threads can run on (provide the # of the cores in a comma-seperated list in quotations) | default: system delegation
Game.UseDirectX9Wrapper = false;			//Use a Direct X wrapper to try and force DirectX 9 games to run windowed
Game.SteamStubDRMPatcherArch = "64";			//Force Steam Stub DRM Patcher to use either the x64 or x86 dll | Values: "64" or "86"
Game.GoldbergLobbyConnect = false;			//Should Goldberg Lobby Connect be used to connect the instances
Game.X360ceDll = [ "xinput1_3.dll" ];			//If x360ce dll should be named something OTHER than xinput1_3.dll | requires Game.Usex360ce to be set to true
Game.CMDBatchBefore = [ "0|ops1", "1|ops2" ];		//When using CMDLaunch (or UseForceBindIP), specify command lines to run BEFORE the game launches in same cmd session, syntax is instance and | symbol as a delimiter and the line you want that instance to run. If you want same line to run all instances, leave out the # and | (only write the line in quotes)
Game.CMDBatchAfter = [ "0|ops1", "1|ops2" ]; 		//When using CMDLaunch (or UseForceBindIP), specify command lines to run AFTER the game launches in same cmd session, syntax is instance and | symbol as a delimiter and the line you want that instance to run. If you want same line to run all instances, leave out the # and | (only write the line in quotes)
Game.GoldbergNoLocalSave = false;			//Do not create a local_save.txt file for Goldberg, saves are to use default game save location
Game.UseNucleusEnvironment = false;			//Use custom environment variables for games that use them, replaces some common paths (e.g. AppData) with C:\Users\<your username>\NucleusCoop
Game.ThirdPartyLaunch = false;				//Use if the game is launched outside of Nucleus | NOTE: You will not be able to use start up hooks or CMDLaunch with this option
Game.ForceProcessPick = false;				//Manually select the process that will be used for process manipulation, such as resizing, repositioning and used for post-launch hooks
Game.DirExclusions = ["dir1"];				//Folders (+ all its contents) listed here will not be linked or copied over to Nucleus game content folder, the instance folders
Game.KeepMonitorAspectRatio = false;			//Try and resize game window within player bounds to the aspect ratio of the monitor
Game.StartHookInstances = "1,2,3,4";			//If you only want specific instances to have starting hooks, specify them in a comma seperated string
Game.PostHookInstances = "1,2,3,4";			//If you only want specific instances to have post launch hooks, specify them in a comma seperated string
Game.FakeFocusInstances = "1,2,3,4";			//If you only want specific instances to have fake focus messages sent to, specify them in a comma seperated string
Game.KeyboardPlayerSkipFakeFocus = false;		//Should the keyboard player instance be skipped when fake focus messages are being sent to
Game.UserProfileConfigPath = "AppData\\Local\\Game\\Config"; //Relative path from user profile (e.g. C:\Users\ZeroFox) to game's config path | Used to provide some extra functionality (open/delete/copy over to Nucleus Environment)
Game.UserProfileSavePath = "AppData\\Local\\Game\\Saves"; //Relative path from user profile (e.g. C:\Users\ZeroFox) to game's save path | Used to provide some extra functionality (open/delete/copy over to Nucleus Environment)
Game.ForceUserProfileConfigCopy = false;		//Force the games config files in UserProfileConfigPath to copy over from system user profile to Nucleus environment
Game.ForceUserProfileSaveCopy = false;			//Force the games save files in UserProfileSavePath to copy over from system user profile to Nucleus environment
Game.PlayerSteamIDs = [ "1234","5678" ];		//A list of steam IDs to be used instead of the pre-defined ones Nucleuses uses | IDs will be used in order they are placed, i.e. instance 1 will be first non-empty string in array
Game.HexEditExeAddress = [ "1|address|bytes" ];		//Use this to replace bytes in a file at a specified address, can be for specific instances with optional 3rd argument | 1st arg: instance # (optional), 2nd arg: hex address offset, 3rd arg: new bytes
Game.HexEditFileAddress = [ "1|relativePath|address|bytes" ];	//Same as HexEditExeAddress but for a file other than exe | Need to provide relative path (from game root folder) + filename as 1st or 2nd arg if not specifying an instance



2. New methods to be used in game scripts -

Context.ChangeXmlNodeInnerTextValue(string path, string nodeName, string newValue)		//Edit an XML element (previously only nodes and attributes)
Context.ReplaceLinesInTextFile(string path, string[] lineNumAndnewValues)			//Replace an entire line; for string array use the format: "lineNum|newValue", the | is required
Context.ReplacePartialLinesInTextFile(string path, string[] lineNumRegPtrnAndNewValues)		//Partially replace a line; for string array use the format: "lineNum|Regex pattern|newValue", the | is required
Context.RemoveLineInTextFile(string path, int lineNum)						//Removes a given line number completely
Context.RemoveLineInTextFile(string path, string txtInLine, SearchType type)			//Removes a given line number completely
			//Returns a line number (int), utilizes a newly created enum SearchType
	Each of the above methods, also have an overload method so you can specify a kind of encoding to use (enter string of encoding as last parameter, e.g. "utf-8", "utf-16", "us-ascii")
	- SearchTypes include: "Contains", "Full" and "StartsWith", use like so: Nucleus.SearchType.StartsWith
Context.CreateRegKey(string baseKey, string sKey, string subKey)				//Create a registry key for current user, baseKey can either be "HKEY_LOCAL_MACHINE" or "HKEY_CURRENT_USER"
Context.DeleteRegKey(string baseKey, string sKey, string subKey)				//Delete a registry key for current user, baseKey can either be "HKEY_LOCAL_MACHINE" or "HKEY_CURRENT_USER"
Context.EditRegKey(string baseKey, string sKey, string name, object data, RegType type)	//Edit a registry key for current user, baseKey can either be "HKEY_LOCAL_MACHINE" or "HKEY_CURRENT_USER"
	EditRegKey uses a custom registry data type to use, by using Nucleus.RegType.DWord for example. The last word can be of the same name of RegistryValueKind enum.
Context.Nickname										//Use this in a game script to get the player's nickname
Context.GamepadGuid										//Get the raw gamepad guid
Context.x360ceGamepadGuid									//Get the x360ce formatted gamepad guid
Context.LocalIP											//Local IP address of the computer
Context.EnvironmentPlayer									//Path to current players Nucleus environment
Context.EnvironmentRoot										//Path to Nucleus environment root folder
Context.UserProfileConfigPath									//Relative path from user profile to game's config path | requires Game.UserProfileConfigPath be set
Context.UserProfileSavePath									//Relative path from user profile to game's save path | requires Game.UserProfileSavePath be set


3. 6 and 8 player support
4. Expanded Mutex Killing (doesn't only look for mutexs beginning with "Sessions")
5. Customizable global hotkeys - Hotkeys to close Nucleus, stop the current session and toggle game windows being top most. Edit hotkeys by selecting the new Settings button 
6. Bug fixes
7. Code optimization 

8. CMD Launch Environment Variables (used with CMDBatchBefore and CMDBatchAfter)
%NUCLEUS_EXE% 			= Exe filename (e.g. Halo.exe)
%NUCLEUS_INST_EXE_FOLDER% 	= Path the instance exe resides in (e.g. C:\Nucleus\content\Halo\Instance0\Binaries)
%NUCLEUS_INST_FOLDER% 		= Path of the instance folder (e.g. C:\Nucleus\content\Halo\Instance0\)
%NUCLEUS_FOLDER%		= Path where Nucleus Coop is being ran from (e.g. C:\Nucleus)
%NUCLEUS_ORIG_EXE_FOLDER%	= Path the original exe resides in (e.g. C:\Program Files\Halo\Binaries)
%NUCLEUS_ORIG_FOLDER%		= Path of the "root" original folder (e.g. C:\Proggram Files\Halo)


Known Issues: --------------------------------------------------------------------------------------

- Mouse constantly flickering back and forth with FakeFocus (can be avoided now using PreventWindowDeactivation)
- Mouse loses focus on game window for a split second on an on-going basis when using KBM (can be avoided now using PreventWindowDeactivation)
- Nucleus crashes and/or hooks don't install when using games that use launchers
- Force feedback does not work with Nucleus custom dlls


Changelog: -----------------------------------------------------------------------------------------

v0.9.9.1 - February 5, 2020
- Updated logic for LauncherExe. The file name in this field will now be launched via Nucleus but ExecutableName will be used to resize, reposition and hook. Launchers will no longer be looked for when grabbing process to manipulate. LauncherExe will be used for hex editing exes and change exes.
- LauncherExe can now also accept a full path to the launcher exe. This is if the launcher is outside of the game folder; NOTE: This file/folder contents will NOT be symlinked/copied, only the game root folder of ExecutableName. This means that hex edits and changing exe using a full path in LauncherExe WILL overwrite the original file!
- Added an option in game scripts to specify which instances get starting hooks, post-launch hooks and fake focus messages sent to (including a specific option for keyboard player)
- Added an option in game scripts to provide a relative config and save path from user profile for games (for use with Nucleus environment)
- Added an option in context menu/game options in UI to open or delete relative config and save paths (for system user profile and Nucleus)
- Added logic if the game is running in Nucleus environment and a user profile config and/or save path have been provided, files and folders will be copied over from the system user profile path (+ added an option to force it)
- Added an option in game scripts to manually enter steam IDs to be used
- Added an option in game scripts for games to try and keep monitor aspect ratio when being resized
- Added an option in game scripts to replace bytes in the exe or file at a specific offset
- Added new field in Context that can be used to grab the local IP address of the computer
- Added new fields in Context for Nucleus environment paths and relative game config/save paths
- If using Nucleus environment and goldberg, set the goldberg settings folder to the Nucleus appdata path
- Updated Goldberg emulator to latest git build (sha b4205535fbee455bee925ab3aa90780e00eead27)
- Hopefully, final fixes for aspect ratios being preserved via KeepAspectRatio and the new KeepMonitorAspectRatio options
- Fixed some issues with x360ce
- Fixed a bug that would cause infinite waiting if process data had already been assigned by the time it got checked
- Fixes for CMDBatchBefore/After (CMDOptions no longer required and will only use if there is a line for that instance, CMDBatchAfter now working)
- Fixed a bug that caused Nucleus to crash when using x360ce
- Fixed SmartSteamEmu not grabbing game process to resize, reposition and hook. SSE launches can now also use process picker
- Fixed bug preventing nickname from reverting back to default after removing a nickname
- Fixed bug that would sometimes cause an error and weird nicknames in UI, when disconnecting, reconnecting controllers
- Made some changes/improvements to context/game options menu and logging
- Game.HookInitDelay and Game.HookFocusInstances have been removed


v0.9.9 - January 22, 2020
- Added an option in game scripts to use Goldberg Lobby Connect (automatic)
- Thanks to Ilyaki, added an option in game scripts to use a custom environment (located at C:\Users\<your username>\NucleusCoop)
- Added an option in game scripts to specify if the game is launched outside of Nucleus (Nucleus will then not launch it, but continue to do everything else, hook, resize, repos etc)
- Added a last fail-safe if process is still not found; a new window will open asking user to manually select the process
- Added an option in game scripts to manually pick the process to be manipulated for things such as resizing, repositioning and used for post-launch hooks
- Added an option in game scripts to run other commands in command prompt before or after game launches when using CMDLaunch (or UseForceBindIP), also set some environment variables that can be used in commands
- Added DInput support for XInputPlus
- Added an option in game scripts to have Goldberg not create a local_save.txt file (use default save location)
- Added an option to specify a different x360ce dll name be used, if needed
- Added the ability to copy entire folders (and all its contents) in CopyCustomUtils
- Added an option in context menu/game options in UI to delete content folder
- Added an option in game scripts to completely ignore linking or copying a folder and all its contents
- Updated Goldberg emulator to latest git build (sha 3f44827326eff6d9cc385c27f0bded89ee7642ea)
- Replaced a function (GetDpiForWindow - used for GUI) that required Windows 10, with an alternative method, so older versions of Windows are now better supported
- Made auto goldberg case insensitive when replacing steam api dlls
- Fixed bug that would stop script notes from appearing in UI after stopping a session
- Fixed bug when KeepSymLinkOnExit = true and # of players increase when there are more than 4 players
- Fixed LauncherExe being required in script if process was not found, or if script uses CMDLaunch or UseForceBindIP
- Fixed "Process is not running" crashes when using start up hooks
- Fixed KeepAspectRatio not resizing correctly, windows will now be horizontally centered as well (to player bounds)
- Fixed crash when SymlinkGame is false


v0.9.8.2 - December 8, 2019
- Hook code has been cleaned up and some lingering issues with Easyhook in the past have been resolved *Thanks to @Ilyaki
- Completely reworked Autosearch. Fixed bug requiring admin rights, custom paths are now allowed, user can choose which found games to add, and so much more
- Instance folder will no longer be symlinked if SymlinkFolders is true
- Integrated and added an option in game scripts to use DirectX 9, Direct 3D wrapper (d3d9.dll) to try and run DirectX 9 games in windowed mode
- Added an option in game scripts to force steam stub DRM patcher to either use x64 or x86 architecture dll
- Added an option in game scripts to force symlink every time
- Added an option in game scripts to set processor affinity per instance
- Changed logic of setting processor affinity
- Nicknames assigned in Nucleus are no longer exclusive to Goldberg
- The keyboard player can now be assigned a nickname in Settings
- Fixed large files sometimes breaking the symlink process
- Fixed files not being symlinked with different amount of players under the new rule: files will only be copied or symlinked once, if needed
- Fixed Nicknames not always being updated if changed
- Fixed the black window from Hide Desktop not closing when stopping a game session (must press stop or stop session hotkey)
- Added an option in game scripts to set processor affinity per instance
- Changed logic of setting processor affinity

v0.9.8.1 - November 4, 2019
- Reverted folder and file exclusion logic to the way it was done pre-0.9.8 (but still kept improvements made to them)
	- DirSymlinkExclusion will force hardcopy of the folder (if it is to be symlinked), FileSymlinkExclusion will completely ignore the file (no link/copy), FileSymlinkCopyInstead will continue to just create hardcopy of file instead of symlinking it
- Fixed xinput plus controller mappings when keyboard player was any player except last
- Prompt between instances can now be delayed if PauseBetweenStarts has a value

v0.9.8 - November 1, 2019
- Nucleus no longer starts with administrative privileges and will prompt if it is needed, games will not be launched elevated now either
- Improved and changed logic on how files are copied/symlinked, much faster now and done all it once at the beginning
	!WARNING!: DirSymlinkExclusions did not work properly in original Nucleus Alpha 8 and now does. All files and subfolders of a DirSymlinkExclusion will be ignored no matter what the file is. Check your scripts!
- Files that get added to the instance folders (goldberg, xinput plus, etc) will no longer be copied over to the original game path and files (as long as SymlinkFolders is false)
	- If SymlinkFolders is true, files WILL be copied over to the original file path
- Files will only be copied or symlinked once, if needed; i.e. if Instance0 path exists with at least one file & KeepSymLinkOnExit is true, Nucleus will skip copying/linking
- Added an option in game scripts to prevent window deactivation
- Added an option in game scripts to hardlink files instead of symlink
- Added an option in game scripts to symlink folders (needed for some games)
- Added option in game scripts to use the experimental Goldberg branch
- Added an option in game scripts to ignore creating a steam_appid file (needed for some games)
- Added an option in game scripts to use Steam Stub DRM Patcher
- Added an option in game scripts to set the foreground window to something other than the games, needed for some games to balance out FPS
- Added an option in game scripts to create a steam_appid.txt file in the same folder as the game executable (needed for some games)
- Added game option to open the original executable directory
- Added number of players each game has in the UI game list, under the title
- Added option to right click on layout selection to go through the different layouts in reverse
- Added/tweaked some additional logging info
- Added more information to be displayed when selecting the Details option in UI
- Updated Goldberg emulator to latest git build (sha 2986b01d0cf34cd900f772cf4294ad387c104cf4)
- XInput plus and X360ce will now leave controller slots blank if player is using a keyboard (no controller input should work on keyboard instance)
- Scripts will now open in Notepad++ by default if installed, when using the Open Script option in UI
- Fixed Script Author notes in UI so that long notes will not overlap when placing controllers
- Fixed NeedsSteamEmulation option and added SSE support for x64 games
- Fixed IdInWindowTitle and ForceWindowTitle bug that would prevent them from working
- Fixed bug preventing mutexes being killed if there were two or more
- Fixed process with ID of 0 being killed
- Removed DotNetZip nuget package as it is no longer being used (replaced zip extractions with direct file operations)
- If a game script has an issue and game doesn't appear, when you fix the issue and launch Nucleus again, you no longer need to re-add the game (game will simply reappear)
- If a file is unable to be deleted after 10 tries during clean-up, do not throw an exception, just carry-on (details are logged)
- PromptBetweenInstances will now show the last prompt (to install hooks) if there are hooks to install OR if FakeFocus is set to true
- Renamed the Data folder to now be "content" (needed as some games mistake the Nucleus Data folder in the path as game files)
- Renamed the games folder to now be "scripts"

v0.9.7.2 - October 15, 2019
- Fixed crash that would happen if ForceFocusWindowName was left blank

v0.9.7.1 - October 14, 2019
- When placing controllers, you can now resize any player's screen to be full vertically or horizontally on any layout (custom layouts too!)
- Added a new option in game scripts to reset the previous window's size, position and borders as each new instance opens up 
- Added an option in game scripts to provide a description, that will appear in the UI when the user selects the script's game
- Added an option to manually specify what language Goldberg should use
- Experimental: UI will now scale based on monitor DPI and Font size
- Improved closing procedures; better chance of finding and killing any lingering game processes
- Only one copy of Nucleus Coop can be open now
- Fixed typo preventing start up hooks from working on x64 when using the delay method
- Fixed Nucleus not launching when there is an error with a game script; will now prompt the user about the error and not show the game in the game list
- Fixed a bug when stopping a session would sometimes trigger an error message
- Fixed a bug where third party files were being placed in wrong directory when working folder was set to something in game script
- Fixed not all the files in the instance folders not being deleted sometimes
- If Nucleus crashes, the error message is now logged in the debug log in addition to the error file generated in the Data folder, plus taskbar will now show if it was hidden
- Changed the "Unknown Game" error message to be clearer on what the issue is
- Instead of denying the user to add a game if the game's executable is already in the game list, it will now prompt the user if they would like to add the game anyway or not
- Removed pointless horizontal scroll bar on game list
- Other Minor UI changes

v0.9.7.0 - October 7, 2019
- Goldberg Emu is now built-in to Nucleus, 3 new options in game scripts that will fully automate the process
- Added a customizable custom layout slot when placing controllers/devices. Create your own layout in Settings!
- Fixed issue of mutexes with certain special characters not being killed or renamed properly
- Removed SmartSteamEmu embedded in Nucleus.Gaming.dll, greatly reducing the dll's file size. SmartSteamEmu is now accessed and available externally through the "utils" folder (also updated SSE to the latest version, 1.4.3)
- Added an option in game scripts to copy custom files if needed
- Added two options in game scripts to be able to do text value replacements in binary files
- Added two additional options in game scripts to be able to do text value replacements in a file you specify
- Added new util, ForceBindIP and an option in game scripts to set it up automatically
- Added new util, XInputPlus and an option in game scripts to set it up automatically
- Added new util, Devreorder and an option in game scripts to set it up automatically
- Added an option in game scripts to block raw input devices in game
- Updated cmd launch to now work with every instance (previously only worked on second instance)
- User can now right click on game list to access different options for that game
- Added an option in the UI to open the game script in notepad
- Added an option in the UI to open the game data folder
- Added an option in the UI to change the game icon
- Added an option to enable a debug log to be created
- Game.ExecutableName is no longer case sensitive when trying to add a game in the UI
- Fixed an empty error log text file from being created in instance folder whenever a hook was used
- Fixed x64 focus hooks and setwindow hook
- Added a SetWindowStart hook that can be applied as each game opens up, instead of later on with SetWindowHook
- Made game windows not be the top most when using Game.PromptBetweenInstances, until after the last prompt (keeps prompt on top and easier to switch-to)
- Improved x360ce so that the xinput dll file gets copied along with the executable so x360ce doesn't prompt to create one
- Exposed the raw gamepad guid of each player's controller in game scripts (changed the name of the x360ce formatted one for clarity)

v0.9.6.1a - September 22, 2019
- Fixed 6 and 8 player layouts
- Reverted mutex searching back to exact matches by default as this broke some games, but left the option to do partial searches if needed (some games do require this)

v0.9.6.0a - September 20, 2019
- Added an option in game scripts to rename mutexes instead of killing them
- Tweaked method of finding mutexes to be more inclusive. You can now provide a partial name of mutex to kill (you must still provide full name for renaming)
- Upgraded custom xinput dll to Alpha 10s' and added x64 custom dll support (alpha 10 custom dlls are now the default, but you can revert back to alpha 8 with a line in game script for compatability)
- Added 6 and 8 player support
- Added an option to delete games from directly within the NucleusCoop UI
- Added the ability to assign "nicknames" to controllers in settings, used to identify specific controllers
- Added the option to allow you to use those newly created nicknames in-game, instead of default "Player1", "Player2" etc (this is for games using Goldberg only)
- Added the ability to see which controller is which by simply holding down a button on a controller. The corresponding controller icon will light up
- Added an option in game scripts to keep the game's aspect ratio when resizing
- Added an option in game scripts to manually set width, height, as well as position of each game window
- Added an option in game scripts to launch and set up x360ce before launching games (make sure you aren't using custom dlls)
- Added an option in game scripts to have the user decide when to open the next instance, instead of each being opened automatically
- Added an option in game scripts to hide desktop background
- Added an option in game scripts to hide taskbar when launching games
- Added an option in game scripts to hide the mouse cursor
- Added an option in game scripts to add the process ID to the end of game windows (helpful for creating unique/different window titles)
- Added an option in game scripts to use different exe names for each instance
- Added an option in game scripts to Hardcopy game files instead of Symlinking
- Added an option in game scripts to resize and reposition the previous window after a new window opens (some games need this)
- Fixed a bug preventing x64 focus and setwindow hooks from working
- Fixed a bug preventing Nucleus from opening when multiple scripts use the same Game.ExecutableName value
- WIP: rudimentary support for DInput alongside XInput (more recognized in Nucleus, can be assigned to a screen like any instance - should work in game). *Most dinput devices will still need to be set up manually.
- WIP: rudimentary Keyboard support

v0.9.5.3a - August 30, 2019
- Fixed a bug that stopped Game.FakeFocus from working

v0.9.5.2a - August 30, 2019
- Fixed Halo not working correctly with more than 2 instances

v0.9.5.1a - August 29, 2019
- Added an option in game scripts to prevent games from resizing on their own
- Added an option in game scripts to copy files from the game directory to the instanced folder
- Added a parameter to the registry manipulation methods, you can now specify a base key to work from (local machine or current user)
- Created a custom solution in order for Halo Custom Edition to work in Nucleus
- Tweaked EditRegKey so it now takes an object data type as the data and a custom RegType to specify the registry type (DWord, QWord, Binary, etc), opens alot more possibilities
- Reworked launching games using command prompt, much better results now. CMDOptions can now be specified per game instance (first instance is ignored for now)
- Fixed an issue with focus hooks that would sometimes crash the game
- Fixed launching games that don't require mutexes be killed but still have initial hooks
- Fixed some other minor bugs

v0.9.5 - August 21, 2019
- Added an option in game scripts to hook into game functions to trick the game into thinking it has focus
- Added an option in game scripts to hook into game functions that try and prevent multiple instances from running
- Added @gymmer's "FocusFaker' method to further trick the game into thinking it has focus
- Added an option in game scripts to set the window priority of each game instance. Can be set to either "AboveNormal", "High" or "RealTime"
- Added an option in game scripts to assign which processor games run on. Can be either an ideal processor (safer, no gaurantee) or a fixed one despite availability
- Added the ability to edit registry keys. Required for some games
- Added an option in game scripts to work-around ForceFocusWindowName having to match 1:1 with game window title for resizing, positioning and focus. Needed for some games that change their window title (does not effect game launchers)
- Added an option to force the title of game windows to be whatever is specific in Game.Hook.ForceFocusWindowName
- Added an option to specify individual files to Symlink
- Added an option in game scripts to launch a game using cmd with specified options
- There is a new Settings window (see cog icon to right corner of Nucleus)
- You can now customize the Nucleus hotkeys to be whatever you'd like
- Fixed a bug that Nucleus did not work properly when game windows were closed manually
- Updated method to toggle game windows being top most. It is now more efficient and doesn't rely on game script
- Minor improvement in performance
- Cleaned up some code

v0.9.4.1a - August 7, 2019
- Fixed bug that broke the generic file manipulation methods

v0.9.4a - August 6, 2019
- Added two new methods for game scripts: RemoveLineInTextFile and FindLineNumberInTextFile
- All four of the generic file manipulation methods (Find, Remove, Replace, ReplacePartial) now include an overload method to specify the kind of encoding to use
- Added a new variable to be used in Remove & Find Line methods: Nucleus.SearchType. You can specify either "Contains", "Full" or "StartsWith" to get more accurate results. Now with the four file manipulation methods, you should now be able to edit ANY game text file for ANY game
- Fixed bug in original Nucleus that didn't allow 1 player to be able to start/play a game
- Fixed "one time use" bug in original Nucleus; Nucleus can now be used multiple times in one session by simply pressing the STOP Button in the app OR pressing the newly created hotkey for it: Ctrl+S
- Added a toggle to make the game windows not the top most windows. Useful if you want to use other programs in the background. Hotkey is Ctrl+A. Pressing it will either disable or enable the windows being top most, depending on its current state. This requires Game.GameName in Nucleus game scripts to work.

v0.9a - August 1, 2019
- Initial release


Credits: -----------------------------------------------------------------------------------------

Mod created by ZeroFox
NucleusCoop original created by lucasassislar
Special thanks to: Ilyaki for his hook soure code
Big thanks to: Talos91, PoundlandBacon and others in the Splitscreen Dreams discord for their help, suggestions, feedback and testing
Also credit to the respective developers of the third party utilities that Nucleus uses to help automate the whole process