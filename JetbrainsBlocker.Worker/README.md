Service automatically looks up Toolbox folder and blocks all executables in it. You can manually add .exe paths in "ManuallyInstalledExecutables" section in appsettings.json

1. Edit (if needed) and copy appsettings.json to `C:\Program Data\jetbrains-blocker\`
```shell
mkdir "C:\Program Data\jetbrains-blocker"
cp appsettings.json "C:\Program Data\jetbrains-blocker\appsettings.json"
```

2. Copy jetbrains-blocker executable somewhere in user directory.
```shell
sc.exe create <new_service_name> binPath= "<path_to_the_service_executable>" start= "auto"
```

> You can find logs in Windows Event Viewer.