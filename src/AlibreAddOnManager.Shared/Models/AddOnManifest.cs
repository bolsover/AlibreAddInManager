using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlibreAddOnManager.Models
{
    /// <summary>catalog/addons/{GUID}.json — every published version of one add-on.</summary>
    [DataContract]
    public class AddOnManifest
    {
        [DataMember(Name = "schemaVersion")] public int SchemaVersion { get; set; }
        [DataMember(Name = "id")] public string Id { get; set; }
        [DataMember(Name = "name")] public string Name { get; set; }
        [DataMember(Name = "folderName")] public string FolderName { get; set; }
        [DataMember(Name = "publisher", EmitDefaultValue = false)] public Publisher Publisher { get; set; }
        [DataMember(Name = "license", EmitDefaultValue = false)] public string License { get; set; }
        [DataMember(Name = "homepage", EmitDefaultValue = false)] public string Homepage { get; set; }
        [DataMember(Name = "description", EmitDefaultValue = false)] public string Description { get; set; }
        [DataMember(Name = "versions")] public List<AddOnVersion> Versions { get; set; } = new List<AddOnVersion>();
    }
}
