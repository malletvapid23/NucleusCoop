# Nucleus Co-Op & Unofficial Mod
Nucles Co-Op is a tool for Windows that allows split-screen play on many games that do not initially support it. Its purpose is to make it as easy as possible for the average user to play games locally.

This repo is an unofficial mod of the Nucleus Co-Op application. The mod is based off of the official Nucleus Co-Op Alpha 8 build and features enhancements and quality of life changes including:
- Huge increase to the amount of compabitle games
- Much more customization (via game scripts)
- Support for any number of players
- Quality of life improvements
- Bug fixes
- And so much more!

View the full list of features/changes and changelog in Mod-Readme.txt in releases.

# Disclaimer
I am NOT associated with the original project or its authors. Nucleus Co-Op was originally created by Lucas Assis. I am merely a fan of split screen gaming and the original project! I wanted to continue this work to continue building upon the already faboulous tool that is NucleusCo-Op.


Github link to the original project: https://github.com/lucasassislar/nucleuscoop

Subscribe the official Nucleus Co-Op subreddit: https://www.reddit.com/r/nucleuscoop/

Join the official Nucleus Co-Op Discord: https://discord.gg/jrbPvKW

^ You can certainly find me in those places as well

# How does Nucleus Co-Op work?
For Alpha 8, all games use a generic handler that can handle pretty much all situations.
To add a new game, you can just create a new *.js file on the games folder, and describe what your game needs to run.
Now, what the GenericHandler actually does?

When the user hits play:
- If the game needs modifications to the save files, we backup them so when the splitscreen session ends we can return all the configurations back to normal.
- The app symlinks the entire game folder to the Data folder, so we can launch each instance of the game with custom DLLs.
- Runs the JavaScript engine, so any custom code that needs to be executed by player ID runs
- We copy a custom xinput dll specific for each gamepad: Each xinput passthroughs a specific gamepad input to the 1st gamepad (xinput1 just passtroughs, xinput2 passes to 2, i.e).
- If needed, we extract SmartSteamEmu and start the game using it.
- Now we keep track of the processes, looking for the launcher and the actual game window, so we can position it correctly on the screen.

# How can you contribute?
Please report any bugs you may find and provide any feedback you have regarding the mod. I am always open to suggestions and I want to make split-screen available for every game, for everyone! Don't forget to create game scripts and share!

You can find me on the Nucleus subreddit/discord as well as the Splitscreen Dreams discord, a special community created specifically for making games non-split-screen games split-screen. Message me on discord, ZeroFox#5866, or Talos91#8419 if you're interested in contributing.

In addition, I accept donations should you wish to support my endevor. It is greatly appreciated but completely voluntary, I will continue my best to help the community and enhance this project.

You can donate via PayPal through this link:

[![paypal](https://www.paypalobjects.com/en_US/i/btn/btn_donate_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=WUXKHLAD3A3LE&source=url)


Thank you ^_^

# Credits
Original NucleusCoop Project: Lucas Assis (lucasassislar)
Mod: ZeroFox
Multiple keyboards/mice & hooks: Ilyaki
Website & handler API: r-mach

Additional credits to all original developers of third party utilities Nucleus uses:
Mr_Goldberg (Goldberg Emulator), syahmixp (SmartSteamEmu), EJocys (x360ce), 0dd14 Lab (Xinput Plus), r1ch (ForceBindIP), HaYDeN (Flawless Widescreen), briankendall (devreorder), VerGreeneyes (DirectXWrapper)
