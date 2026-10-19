using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlibreAddOnManager.Models
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
}
