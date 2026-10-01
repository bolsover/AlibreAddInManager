using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlibreAddOnManager.Models
{
    /// <summary>catalog/index.json — the lightweight listing fetched on every refresh.</summary>
    [DataContract]
    public class CatalogIndex
    {
        [DataMember(Name = "schemaVersion")] public int SchemaVersion { get; set; }
        [DataMember(Name = "generated", EmitDefaultValue = false)] public string Generated { get; set; }
        [DataMember(Name = "addons")] public List<CatalogEntry> AddOns { get; set; } = new List<CatalogEntry>();
    }
}
