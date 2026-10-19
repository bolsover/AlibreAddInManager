using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlibreAddOnManager.Models
{
    [DataContract]
    public class CatalogEntry
    {
        [DataMember(Name = "id", Order = 1)] public string Id { get; set; }
        [DataMember(Name = "name", Order = 2)] public string Name { get; set; }
        [DataMember(Name = "publisher", Order = 3, EmitDefaultValue = false)] public string Publisher { get; set; }
        [DataMember(Name = "summary", Order = 4, EmitDefaultValue = false)] public string Summary { get; set; }
        [DataMember(Name = "latestVersion", Order = 5, EmitDefaultValue = false)] public string LatestVersion { get; set; }
        [DataMember(Name = "workspaces", Order = 6, EmitDefaultValue = false)] public List<string> Workspaces { get; set; }
        [DataMember(Name = "tags", Order = 7, EmitDefaultValue = false)] public List<string> Tags { get; set; }
        [DataMember(Name = "manifestUrl", Order = 8)] public string ManifestUrl { get; set; }
        [DataMember(Name = "iconUrl", Order = 9, EmitDefaultValue = false)] public string IconUrl { get; set; }
    }
}
