echo "stopping service"
run C:\Services\DownloadFolderManager\_stop.cmd
SET mypath=%~dp0
echo %mypath:~0,-1%
break
echo "start copy"
break
xcopy %mypath:~0,-1% "C:\Services\DownloadFolderManager" /h /i /c /k /e /r /y
break
echo "finish copy"
break
echo "run service"
break
run C:\Services\DownloadFolderManager\_start.cmd

