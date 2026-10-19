using System.Runtime.Serialization;

namespace AlibreAddOnManager.Models
{
    /// <summary>Per-user settings: %APPDATA%\AlibreAddOnManager\settings.json.</summary>
    [DataContract]
    public class ManagerSettings
    {
        [DataMember(Name = "catalogUrl", EmitDefaultValue = false)] public string CatalogUrl { get; set; }
    }
}
