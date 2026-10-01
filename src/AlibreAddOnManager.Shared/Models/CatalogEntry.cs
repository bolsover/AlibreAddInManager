using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlibreAddOnManager.Models
{
    [DataContract]
    public class CatalogEntry
    {
        [DataMember(Name = "id")] public string Id { get; set; }
        [DataMember(Name = "name")] public string Name { get; set; }
        [DataMember(Name = "publisher", EmitDefaultValue = false)] public string Publisher { get; set; }
        [DataMember(Name = "summary", EmitDefaultValue = false)] public string Summary { get; set; }
        [DataMember(Name = "latestVersion", EmitDefaultValue = false)] public string LatestVersion { get; set; }
        [DataMember(Name = "workspaces", EmitDefaultValue = false)] public List<string> Workspaces { get; set; }
        [DataMember(Name = "tags", EmitDefaultValue = false)] public List<string> Tags { get; set; }
        [DataMember(Name = "manifestUrl")] public string ManifestUrl { get; set; }
        [DataMember(Name = "iconUrl", EmitDefaultValue = false)] public string IconUrl { get; set; }
    }
}
