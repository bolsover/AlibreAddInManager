using System.Runtime.Serialization;

namespace AlibreAddOnManager.Models
{
    /// <summary>An add-on this tool installed and therefore owns.</summary>
    [DataContract]
    public class TrackedAddOn
    {
        [DataMember(Name = "id")] public string Id { get; set; }
        [DataMember(Name = "name")] public string Name { get; set; }
        [DataMember(Name = "version")] public string Version { get; set; }
        [DataMember(Name = "folder")] public string Folder { get; set; }
        [DataMember(Name = "source", EmitDefaultValue = false)] public string Source { get; set; }
        [DataMember(Name = "sha256", EmitDefaultValue = false)] public string Sha256 { get; set; }
        [DataMember(Name = "installedUtc", EmitDefaultValue = false)] public string InstalledUtc { get; set; }
    }
}
