using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CofA_Importer
{
    public class AppSettings
    {
        public string? CofaPath { get; set; }
        public string? RejectedDirectory { get; set; }
        public string? LogDirectory { get; set; }
        public bool ShowOpenLogPrompt { get; set; } = true;
    }
}
