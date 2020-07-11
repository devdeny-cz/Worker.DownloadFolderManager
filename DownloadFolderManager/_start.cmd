@ECHO off
dotnet restore
dotnet publish -o %CD%
SC Create DownloadFolderManager binPath="%CD%\DownloadFolderManager.exe" displayname="DownloadFolderManager" type= own error= severe 

net start DownloadFolderManager