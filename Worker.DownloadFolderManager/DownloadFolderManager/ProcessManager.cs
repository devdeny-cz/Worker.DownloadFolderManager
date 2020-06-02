using Microsoft.Extensions.Logging;
using MimeMapping;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DownloadFolderManager
{
    public class ProcessManager
    {
        private const string TEMP_FOLDER = "temp";
        private readonly string[] zipExtensions = new string[] { ".zip", ".zipx", ".rar", ".rev", "r01", ".r00", ".7z", ".xz", ".tar", ".wim", ".bzip2" };
        private List<ProcessRule> _migrateRules;
        private List<ZipRule> _zipRules;
        public bool IsSetRules => _migrateRules?.Count > 0;

        private readonly ILogger _logger;

        public ProcessManager(ILogger logger)
        {
            _migrateRules = new List<ProcessRule>();
            _zipRules = new List<ZipRule>();
            _logger = logger;
        }

        public ProcessManager()
        {
            _migrateRules = new List<ProcessRule>();
            _zipRules = new List<ZipRule>();
            //_logger = new Logger<ProcessManager>();
            //logger = _logger;
        }

        internal void TryReadRules(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    FillMigrateRule(path);
                    FillZipRule(path);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        internal void ReloadRules(string path)
        {
            _migrateRules.Clear();
            _zipRules.Clear();
            TryReadRules(path);
        }

        private void FillZipRule(string path)
        {
            var excelData = new ExcelReader().GetMatrixData(path, "zip_rule");
            if (excelData != null && excelData.GetLength(0) > 1 && excelData.GetLength(1) >= 6)
            {

                // row 0 = header
                for (int rowIndex = 1; rowIndex < excelData.GetLength(0); rowIndex++)
                {
                    string name = excelData[rowIndex, 0];
                    var contentTypes = excelData[rowIndex, 1].Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    var extensions = excelData[rowIndex, 2].Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    var pattern = excelData[rowIndex, 3];
                    var sizeValue = excelData[rowIndex, 4];
                    var supportMigrateFolder = excelData[rowIndex, 5].ToLower() == "yes";


                    // valid
                    if (string.IsNullOrEmpty(pattern) && extensions.Length == 0 && contentTypes.Length == 0 && string.IsNullOrEmpty(sizeValue))
                    {
                        // invalid rule
                    }
                    else
                    {
                        var sizeUnit = string.IsNullOrEmpty(sizeValue) ? SizeUnitValue.EmptySize : SizeUnitValue.ParseSize(sizeValue);
                        var rule = new ZipRule(name, contentTypes, extensions, pattern, sizeUnit, supportMigrateFolder);
                        _zipRules.Add(rule);
                    }
                }
            }
        }

        private void FillMigrateRule(string path)
        {
            var excelData = new ExcelReader().GetMatrixData(path, "migrate_rule");
            if (excelData != null && excelData.GetLength(0) > 1 && excelData.GetLength(1) >= 5)
            {

                // row 0 = header
                for (int rowIndex = 1; rowIndex < excelData.GetLength(0); rowIndex++)
                {
                    string name = excelData[rowIndex, 0];
                    var contentTypes = excelData[rowIndex, 1].Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    var extensions = excelData[rowIndex, 2].Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    var pattern = excelData[rowIndex, 3];
                    var targetPath = excelData[rowIndex, 4];
                    // valid
                    if (string.IsNullOrEmpty(pattern) && extensions.Length == 0 && contentTypes.Length == 0)
                    {
                        // invalid rule
                    }
                    else if (string.IsNullOrEmpty(targetPath))
                    {
                        // invalid target path
                    }
                    else
                    {
                        var rule = new ProcessRule(name, contentTypes, extensions, pattern, targetPath);
                        _migrateRules.Add(rule);
                    }
                }
            }
        }



        internal void ProcessFile(string inputFilePath, string sourceDirectory)
        {
            string resultPath = inputFilePath;
            string directory = Path.GetDirectoryName(inputFilePath);
            string extension = Path.GetExtension(inputFilePath);
            string contentType = MimeUtility.GetMimeMapping(inputFilePath);
            string filenameWithoutExtension = Path.GetFileNameWithoutExtension(inputFilePath);
            // migrate just in main source directory
            if (Path.GetDirectoryName(inputFilePath) == sourceDirectory)
            {
                resultPath = RunMigrateFile(inputFilePath, directory, extension, contentType, filenameWithoutExtension);
            }

            RunZipFile(resultPath, extension, contentType, filenameWithoutExtension, sourceDirectory);


        }

        private string RunMigrateFile(string inputFilePath, string directory, string extension, string contentType, string filenameWithoutExtension)
        {
            string resultPath = inputFilePath;
            var potencionalRule = new List<Tuple<ProcessRule, int>>();
            foreach (var rule in _migrateRules)
            {
                int priority = 0;
                if (CheckExtension(extension, rule.Extensions))
                {
                    priority++;
                }
                else if (rule?.Extensions?.Length > 0)
                {
                    continue;
                }

                // check content type
                if (CheckContentType(contentType, rule.ContentTypes))
                {
                    priority++;
                }
                else if (rule?.ContentTypes?.Length > 0)
                {
                    continue;
                }

                if (CheckPattern(filenameWithoutExtension, rule.Pattern))
                {
                    priority++;
                }
                else if (!string.IsNullOrEmpty(rule.Pattern))
                {
                    continue;
                }

                if (priority > 0)
                {
                    potencionalRule.Add(new Tuple<ProcessRule, int>(rule, priority));
                }
            }
            var ruleWithMaxPriority = GetRuleWithMaxPriority(potencionalRule);
            if (ruleWithMaxPriority != null)
            {
                // apply rule
                string targetDirectory = ruleWithMaxPriority.TargetPath;
                if (Regex.IsMatch(targetDirectory, @"(^\\[^\\])|(^\/[^\/])"))
                {
                    targetDirectory = targetDirectory.Substring(1);
                }
                if (!Path.IsPathRooted(targetDirectory))
                {
                    targetDirectory = Path.Combine(directory, targetDirectory);
                }
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }
                // move file
                var outFilePath = Path.Combine(targetDirectory, Path.GetFileName(inputFilePath));
                try
                {
                    _logger.LogInformation($"File will move from {inputFilePath} to {outFilePath}");
                    File.Move(inputFilePath, outFilePath);
                    resultPath = outFilePath;
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, $"Unexception error while movefile {inputFilePath}");
                }
            }

            return resultPath;
        }


        private void RunZipFile(string inputFilePath, string extension, string contentType, string filenameWithoutExtension, string sourceDirectory)
        {
            var migrateDirectories = _migrateRules.Select(r => r.TargetPath).Distinct();
            foreach (var rule in _zipRules)
            {
                if (!rule.UseMigrateFolders && sourceDirectory != Path.GetDirectoryName(inputFilePath)) continue;
                if (zipExtensions.Contains(extension.ToLower())) continue; // skip ziped file
                if (rule.Extensions.Length > 0 && !CheckExtension(extension, rule.Extensions)) continue;
                if (rule.ContentTypes.Length > 0 && !CheckContentType(extension, rule.ContentTypes)) continue;
                if (!string.IsNullOrEmpty(rule.Pattern) && !CheckPattern(filenameWithoutExtension, rule.Pattern)) continue;
                if (!rule.SizeUnit.Compare(new FileInfo(inputFilePath).Length)) continue;

                _logger.LogInformation($"File will be zipped: {inputFilePath}");
                // if all rule is ok
                // move to temp (protect for next use file)
                var tempdirectory = Path.Combine(sourceDirectory, TEMP_FOLDER);
                if (!Directory.Exists(tempdirectory))
                {
                    Directory.CreateDirectory(tempdirectory);
                }
                var tempFileName = Path.Combine(tempdirectory, Path.GetFileName(inputFilePath));
                var outputZipName = inputFilePath + ".zip";

                try
                {
                    File.Move(inputFilePath, tempFileName);
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, $"Unexception error while movefile from {inputFilePath} to {tempFileName}");
                    continue;
                }

                //_logger.LogInformation($"Compresed file {tempFileName}");
                using (var zip = ZipFile.Open(outputZipName,ZipArchiveMode.Create))
                {
                    zip.CreateEntryFromFile(tempFileName, Path.GetFileName(inputFilePath), CompressionLevel.Optimal);
                }

                //_logger.LogInformation($"Delete temp file {tempFileName}");
                File.Delete(tempFileName);
            }
        }

        private bool CheckContentType(string contentType, IEnumerable<string> contentTypes)
        {
            foreach (var patternContentType in contentTypes)
            {
                string pattern = patternContentType;
                // change to regex situation type/* (example: image/* to image/.*) for regex matching
                if (patternContentType.EndsWith("/*"))
                {
                    pattern = pattern.Remove(pattern.Length - 2);
                    pattern += ".*";
                }
                if (Regex.IsMatch(contentType, pattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool CheckPattern(string filenameWithoutExtension, string pattern)
        {
            return !string.IsNullOrEmpty(pattern) && Regex.IsMatch(filenameWithoutExtension, pattern);
        }

        private static bool CheckExtension(string extension, string[] extensions)
        {
            return extensions.Contains(extension);
        }

        private ProcessRule GetRuleWithMaxPriority(List<Tuple<ProcessRule, int>> potencionalRules)
        {
            int maxPriotity = 0;
            ProcessRule strongestRule = null;
            foreach (var ruleAndPriority in potencionalRules)
            {
                if (ruleAndPriority.Item2 > maxPriotity)
                {
                    strongestRule = ruleAndPriority.Item1;
                    maxPriotity = ruleAndPriority.Item2;
                }
            }
            return strongestRule;
        }
    }
}
