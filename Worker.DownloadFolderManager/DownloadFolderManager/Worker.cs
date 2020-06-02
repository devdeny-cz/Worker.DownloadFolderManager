using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DownloadFolderManager
{
    public class Worker : BackgroundService
    {
        private List<string> sourceDirectories;
        private const string RULE_FILE_NAME = "rules.xlsx";
        private DateTime lastRuleFileAccessTime;
        private ProcessManager processManager;
        private const int WAIT_STANDARD = 5*60*1000; // 5 minute
        private const int WAIT_AFTERERROR = 10*60*1000; // 10 minute

        private readonly ILogger<Worker> _logger;
        private readonly string rulePath;

        public Worker(ILogger<Worker> logger)
        {
            rulePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), RULE_FILE_NAME);
            _logger = logger;
            lastRuleFileAccessTime = DateTime.MinValue;
            processManager = new ProcessManager(logger);
            sourceDirectories = new List<string>();
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            // try set rules
            
            if (File.Exists(rulePath))
            {
                _logger.LogInformation($"Try read rules {rulePath}");
                lastRuleFileAccessTime = File.GetLastWriteTime(rulePath);
                try
                {
                    processManager.TryReadRules(rulePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,"Getting data from rules error!");
                }
                FillSourceDir();
            }
            else
            {
                _logger.LogWarning($"Not exist rulepath = {rulePath}");
            }

            return base.StartAsync(cancellationToken);
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Executing");
            while (!stoppingToken.IsCancellationRequested)
            {
                bool state = false;
                // check exist rule and input dir (somebody can delete both or not created)
                if (sourceDirectories.Count == 0)
                {
                    _logger.LogInformation("Not exist any source directories");
                    FillSourceDir();
                }
                else if (!processManager.IsSetRules && !File.Exists(rulePath))
                {
                    _logger.LogError($"File with rules for process not exist. Filename = {rulePath}; Not exist in dir = {Directory.GetCurrentDirectory()}");
                }
                else
                {
                    // we have directory and rules
                    state = true;
                    // if we havent rules
                    if (!processManager.IsSetRules)
                    {
                        _logger.LogInformation("The rules was't set. Try set now.");
                        processManager.TryReadRules(rulePath);
                        lastRuleFileAccessTime = File.GetLastWriteTime(rulePath);
                    }
                    // check rule changes
                    else if (lastRuleFileAccessTime != File.GetLastWriteTime(rulePath))
                    {
                        // reload rules
                        _logger.LogInformation("The rules was change. Reload now.");
                        processManager.ReloadRules(rulePath);
                        lastRuleFileAccessTime = File.GetLastWriteTime(rulePath);
                    }
                    foreach (var sourceDirectory in sourceDirectories)
                    {
                        _logger.LogInformation($"Start working in directory {sourceDirectory}");
                        var sourceDirectoryInfo = new DirectoryInfo(sourceDirectory);
                        foreach (var fileInfo in sourceDirectoryInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
                        {
                            try
                            {
                                // jen source dir řeší migraci
                                processManager.ProcessFile(fileInfo.FullName, sourceDirectory);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Unknow exception error while processing file {fileInfo.FullName}");
                            }
                        }
                    }
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
 
                }
                await Task.Delay(state ? WAIT_STANDARD : WAIT_AFTERERROR, stoppingToken);
            }


        }
        private void FillSourceDir()
        {
            if (File.Exists(rulePath))
            {
                ExcelReader excelReader = new ExcelReader();
                var pathsSheetRows = excelReader.GetMatrixData(rulePath, "main");
                if (pathsSheetRows.GetLength(0) > 1)
                {
                    for (int rowIndex = 1; rowIndex < pathsSheetRows.GetLength(0); rowIndex++)
                    {
                        var sourcePath = pathsSheetRows[rowIndex, 0];
                        if (string.IsNullOrEmpty(sourcePath)) continue;
                        if (Directory.Exists(sourcePath))
                        {
                            sourceDirectories.Add(sourcePath);
                        }
                        else
                        {
                            _logger.LogWarning($"Folder for check is not exist! Path = {sourcePath}");
                        }

                    }
                }
            }
            else
            {
                _logger.LogError($"File not exist or access dinied = {rulePath}");
            }
        }
    }
}
