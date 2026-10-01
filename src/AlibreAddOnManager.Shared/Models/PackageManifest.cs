using System.Runtime.Serialization;

namespace AlibreAddOnManager.Models
{
    /// <summary>
    /// addon.json at the root of a .alibreaddon package — enough to install from a
    /// file with no catalog available.
    /// </summary>
    [DataContract]
    public class PackageManifest
    {
        [DataMember(Name = "schemaVersion")] public int SchemaVersion { get; set; }
        [DataMember(Name = "id")] public string Id { get; set; }
        [DataMember(Name = "name")] public string Name { get; set; }
        [DataMember(Name = "folderName")] public string FolderName { get; set; }
        [DataMember(Name = "version")] public string Version { get; set; }
        [DataMember(Name = "dllType", EmitDefaultValue = false)] public string DllType { get; set; }
        [DataMember(Name = "arch", EmitDefaultValue = false)] public string Arch { get; set; }
        [DataMember(Name = "minAlibreBuild", EmitDefaultValue = false)] public int? MinAlibreBuild { get; set; }
        [DataMember(Name = "maxAlibreBuild", EmitDefaultValue = false)] public int? MaxAlibreBuild { get; set; }
        [DataMember(Name = "publisher", EmitDefaultValue = false)] public string Publisher { get; set; }
        [DataMember(Name = "description", EmitDefaultValue = false)] public string Description { get; set; }
    }
}
