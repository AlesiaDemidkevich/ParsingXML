using System.Collections.Generic;

namespace Shared.Models
{
    public enum ModuleState
    {
        Online,
        Run,
        NotReady,
        Offline
    }

    public class ModuleModel
    {
        public int ModuleCategoryID { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public ModuleState ModuleState { get; set; }
    }

    public class FileMessage
    {
        public string FileName { get; set; } = string.Empty;
        public List<ModuleModel> Modules { get; set; } = new List<ModuleModel>();
    }
}
