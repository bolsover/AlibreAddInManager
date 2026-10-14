using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlibreAddOnManager.Models
{
    /// <summary>catalog/index.json — the lightweight listing fetched on every refresh.</summary>
    [DataContract]
    public class CatalogIndex
    {
        [DataMember(Name = "schemaVersion", Order = 1)] public int SchemaVersion { get; set; }
        [DataMember(Name = "generated", Order = 2, EmitDefaultValue = false)] public string Generated { get; set; }
        [DataMember(Name = "addons", Order = 3)] public List<CatalogEntry> AddOns { get; set; } = new List<CatalogEntry>();
    }
}
