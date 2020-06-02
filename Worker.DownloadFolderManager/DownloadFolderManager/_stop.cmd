ECHO off
taskkill /f /im mmc.exe
NET STOP DownloadFolderManager
SC DELETE DownloadFolderManager
taskkill /f /im DownloadFolderManager.exe