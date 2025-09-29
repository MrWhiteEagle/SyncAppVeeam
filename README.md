# SyncApp
**SyncApp** is a directory synchronization program writen in C# language that performs a a comparison between two  directories' contents periodically, based on an interval, source and destination directories provided by the user.
## Features

 - Visual tree of both directories' contents, along with "out of sync" nodes
 - Comparison of files based on MD5 hashing
 - User-provided interval, source and destination directories
 - CLI (Command Line Interface)
 - Extensive Logging of operations to both Console and a logfile (also provided by user)
 - Copying of files and directories from source to destination
 - Deletion of files and directories present in destination but not in source
## How to use
### Requirements

    - .Net 8.0 Runtime or newer
    - Windows-based system
### Usage
Download provided exe, or clone the repository into VisualStudio and build by running

		dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true
#### Should you build the program yourself by running the command above, program's .exe will be present at: [ProjectDirectory]/bin/Release/net8.0/win-x64/publish/
#### You can also run the program in Debug - this way you can enter your own arguments in the *altargs* array without using CLI to launch the program correctly.
The program is used by launching it from any CLI (f.e. powershell or CMD) and providing it with following arguments in any order:
   
		--source <sourceDirectoryPath>
		--path <destinationDirectoryPath>
		--interval <timeBetweenSyncAttempts> (Notation: xxsxxmxxhxxDxxMxxY)
		--log <logDirectoryPath>
#### Note - log path needs to be a directory, program creates a text file on each launch, and appends its contents on each sync.
The program parses interval time with specific notation allowing user to combine total time without need of manual addition of units.
#### Example: 
		--interval 20s30m1h1D
will result in a timespan of 1 day, 1 hour, 30minutes, and 20 seconds.
### Example command:
		SyncAppVeam.exe --source C:/Users/<UserName>/Downloads/SyncSourceDirectory --path C:/Users/<UserName>/Downloads/SyncDestinationDirectory --interval 1h30m --log /
Will result in program synchronizing two directories in User's Download directory every 1 hour and 30 minutes:
SyncSourceDirectory ---> SyncDestinationDirectory
And logging the output to program's location
#### Any files and subdirectories in source will be mirrored to destination.
#### Any files and subdirectories in destination that are not present in source will be deleted.

## Project structure:
```
SyncAppVeeam
     ├────── Program.cs
     ├────── Classes
     │       ├── SyncManagerService.cs
     │       ├── SynchronizationService.cs
     │       ├── TimerService.cs
     │       └── UserCLIService.cs
     └────── Models
             ├── INode.cs
             ├── FileNode.cs
             └── FolderNode.cs
```
