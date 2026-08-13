' DSH Launcher - Silent dsh starter
' Called by DshLauncher.Windows to start dsh in background
' Logs to %USERPROFILE%\.dsh-web.log

Dim shell, fso, logPath, logFile
Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")

logPath = shell.ExpandEnvironmentStrings("%USERPROFILE%") & "\.dsh-web.log"
Set logFile = fso.CreateTextFile(logPath, True)
logFile.WriteLine "[" & Now & "] Starting dsh via start-dsh.vbs"
logFile.Close

' Run npx in background, redirect output to log
shell.Run "cmd /c npx -y @deepseek-ai/dsh web >> """ & logPath & """ 2>&1", 0, False
