using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CofA_Importer
{
    public static class ConfigPathHelper
    {
        public static string GetWritableConfigDirectory()
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "INDOFINE", "ICC_CofA_Importer");

            Directory.CreateDirectory(path);
            return path;
        }

        public static string GetWritableConfigFilePath()
        {
            return Path.Combine(GetWritableConfigDirectory(), "appsettings.json");
        }
    }
}

