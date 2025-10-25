using System;
using System.Collections.Generic;

namespace ParadoxTranslator.Models
{
    public class Project
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string? GameType { get; set; } // HoI4, Stellaris, CK3, etc.
        public List<string> FilePaths { get; set; } = new();
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastOpenedDate { get; set; } = DateTime.Now;
        public string SourceLanguage { get; set; } = "en";
        public string TargetLanguage { get; set; } = "vi";
    }
}
