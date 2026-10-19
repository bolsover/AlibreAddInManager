using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlibreAddOnManager.Models
{
    /// <summary>catalog/addons/{GUID}.json — every published version of one add-on.</summary>
    [DataContract]
    public class AddOnManifest
    {
        [DataMember(Name = "schemaVersion", Order = 1)] public int SchemaVersion { get; set; }
        [DataMember(Name = "id", Order = 2)] public string Id { get; set; }
        [DataMember(Name = "name", Order = 3)] public string Name { get; set; }
        [DataMember(Name = "folderName", Order = 4)] public string FolderName { get; set; }
        [DataMember(Name = "publisher", Order = 5, EmitDefaultValue = false)] public Publisher Publisher { get; set; }
        [DataMember(Name = "license", Order = 6, EmitDefaultValue = false)] public string License { get; set; }
        [DataMember(Name = "homepage", Order = 7, EmitDefaultValue = false)] public string Homepage { get; set; }
        [DataMember(Name = "description", Order = 8, EmitDefaultValue = false)] public string Description { get; set; }
        [DataMember(Name = "versions", Order = 9)] public List<AddOnVersion> Versions { get; set; } = new List<AddOnVersion>();
    }
}
