using System.Xml.Serialization;
using System.Collections.Generic;

namespace FileParserService.Models
{
    [XmlRoot("InstrumentStatus")]
    public class InstrumentStatus
    {
        public string PackageID { get; set; } = string.Empty;

        [XmlElement("DeviceStatus")]
        public List<DeviceStatus> DeviceStatuses { get; set; } = new();
    }

    public class DeviceStatus
    {
        public string ModuleCategoryID { get; set; } = string.Empty;
        public int IndexWithinRole { get; set; }
        public string RapidControlStatus { get; set; } = string.Empty; // вложенный XML
    }
}
