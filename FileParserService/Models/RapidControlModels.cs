using System.Xml.Serialization;

namespace FileParserService.Models
{
    [XmlRoot("CombinedSamplerStatus")]
    public class CombinedSamplerStatus
    {
        public string ModuleState { get; set; } = string.Empty;
    }

    [XmlRoot("CombinedPumpStatus")]
    public class CombinedPumpStatus
    {
        public string ModuleState { get; set; } = string.Empty;
    }

    [XmlRoot("CombinedOvenStatus")]
    public class CombinedOvenStatus
    {
        public string ModuleState { get; set; } = string.Empty;
    }
}
