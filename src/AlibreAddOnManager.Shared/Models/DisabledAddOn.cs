using System.Runtime.Serialization;

namespace AlibreAddOnManager.Models
{
    /// <summary>A registry value removed by Disable, kept so Enable can restore it exactly.</summary>
    [DataContract]
    public class DisabledAddOn
    {
        [DataMember(Name = "id")] public string Id { get; set; }
        [DataMember(Name = "name", EmitDefaultValue = false)] public string Name { get; set; }
        [DataMember(Name = "folder")] public string Folder { get; set; }
        [DataMember(Name = "disabledUtc", EmitDefaultValue = false)] public string DisabledUtc { get; set; }
    }
}
