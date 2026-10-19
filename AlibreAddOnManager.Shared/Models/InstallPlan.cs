using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlibreAddOnManager.Models
{
    /// <summary>
    /// The batch of changes the unelevated add-on hands to the elevated helper.
    /// </summary>
    [DataContract]
    public class InstallPlan
    {
        [DataMember(Name = "schemaVersion")] public int SchemaVersion { get; set; } = 1;
        [DataMember(Name = "created")] public string Created { get; set; }

        /// <summary>
        /// True when an operation replaces or deletes files of an add-on Alibre may
        /// have loaded (the DLL is locked), so the helper must wait for Alibre to exit.
        /// </summary>
        [DataMember(Name = "requiresAlibreClosed")] public bool RequiresAlibreClosed { get; set; }

        [DataMember(Name = "operations")] public List<PlanOperation> Operations { get; set; } = new List<PlanOperation>();
    }
}
