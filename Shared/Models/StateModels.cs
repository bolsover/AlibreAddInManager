using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlibreAddInManager.Models
{
    /// <summary>
    /// C:\ProgramData\Alibre AddOns\.manager\state.json — written only by the
    /// elevated helper, readable by everyone.
    /// </summary>
    [DataContract]
    public class ManagerState
    {
        [DataMember(Name = "schemaVersion")] public int SchemaVersion { get; set; } = 1;
        [DataMember(Name = "tracked")] public List<TrackedAddOn> Tracked { get; set; } = new List<TrackedAddOn>();
        [DataMember(Name = "disabled")] public List<DisabledAddOn> Disabled { get; set; } = new List<DisabledAddOn>();
    }

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

    /// <summary>A registry value removed by Disable, kept so Enable can restore it exactly.</summary>
    [DataContract]
    public class DisabledAddOn
    {
        [DataMember(Name = "id")] public string Id { get; set; }
        [DataMember(Name = "name", EmitDefaultValue = false)] public string Name { get; set; }
        [DataMember(Name = "folder")] public string Folder { get; set; }
        [DataMember(Name = "disabledUtc", EmitDefaultValue = false)] public string DisabledUtc { get; set; }
    }

    /// <summary>Per-user settings: %APPDATA%\AlibreAddInManager\settings.json.</summary>
    [DataContract]
    public class ManagerSettings
    {
        [DataMember(Name = "catalogUrl", EmitDefaultValue = false)] public string CatalogUrl { get; set; }
    }
}
