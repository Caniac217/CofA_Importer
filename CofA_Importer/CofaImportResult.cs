using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CofA_Importer
{
    public class CofaImportResult
    {
        public List<string> Logs { get; set; } = new();
        public string? LogFilePath { get; set; }
        public bool PromptToOpenLog { get; set; } = false;
    }
}
