# playnite-LocalLibrary
Adds a library to Playnite for managing locally stored games.


## Instructions
### Before starting, create a new source for your local library (unless you already have one you want to use):
  1. From the main menu, go to Library -> Library Manager (you can also use Ctrl-W to get there).
  2. Select Sources on the left-hand side.
  3. Select Add at the bottom.
  4. Give your source a name and click OK and then Save.

### Configure the settings for the Local Library:
  1. From the main menu, go to Add-ons (you can also use F9).
  2. Under "Extensions settings" expand Libraries and select Local Library.

  **Required**
  1. Under "Source" select the source you are using for the Local Library.

  **Optional**
  1. The top option is a checkbox to use Actions instead of ROMs for storing the paths to your files.
  2. For the unarchiver setting, select whether you will use 7zip or WinRar and browse for the executable of whichever tool you chose.
    2a. This is only required if you have games that are compressed in an archive and just need to be unarchived to a selected folder in order to play.  

## Adding Games
  Add any game for which you own installation media manually, and set it to use the source created for your Library.
  
  You have a choice to use either ROMs or Actions to store the paths to your installation files.
  The addon defaults to ROMs, but you can select actions in the Local Library settings within Playnite.
  
  For either choice you should use the following structure:
  
  ### Main Installation Files
  Create a ROM or Action named "Install" or "Installer" pointing to the exe, ISO, or archive.
  
  ### Update Installation Files
  Create additional ROMs or Actions using any naming structure except "Install" or "Installer".
  One option is to number them sequentially and then append the version number:
     1. Update v1.2.1
     2. Update v1.3.5
     3. Update v2.0
  
  **Note: Updates currently only support exe files.**
