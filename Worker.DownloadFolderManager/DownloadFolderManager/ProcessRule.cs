using System;
using System.Collections.Generic;
using System.Text;

namespace DownloadFolderManager
{
    public class ProcessRule
    {
        public string Name { get; private set; }
        public string[] ContentTypes { get; private set; }
        public string[] Extensions { get; private set; }

        public string Pattern { get; private set; }

        public string TargetPath { get; private set; }

        public ProcessRule(string name, string[] contentTypes, string[] extensions, string pattern, string targetPath)
        {
            Name = name;
            ContentTypes = contentTypes;
            Extensions = extensions;
            Pattern = pattern;
            TargetPath = targetPath;
        }

        public override string ToString()
        {
            return $"Name = {Name}; TargetPath = {TargetPath}";
        }
    }
}
