namespace DownloadFolderManager
{
    public class ZipRule
    {
        public ZipRule(string name, string[] contentTypes, string[] extensions, string pattern, SizeUnitValue sizeUnit, bool supportMigrateFolder)
        {
            Name = name;
            ContentTypes = contentTypes;
            Extensions = extensions;
            Pattern = pattern;
            SizeUnit = sizeUnit;
            UseMigrateFolders = true;
        }

        public string Name { get; set; }

        public string[] ContentTypes { get; private set; }

        public string[] Extensions { get; private set; }

        public string Pattern { get; private set; }

        public bool UseMigrateFolders = true;

        public SizeUnitValue SizeUnit{ get; set; }
    }
}
