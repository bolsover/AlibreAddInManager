using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlibreAddOnManager.Models
{
    /// <summary>Written by the helper next to the plan so the add-on can show the outcome.</summary>
    [DataContract]
    public class PlanResult
    {
        [DataMember(Name = "completed")] public string Completed { get; set; }
        [DataMember(Name = "cancelled")] public bool Cancelled { get; set; }
        [DataMember(Name = "lines")] public List<string> Lines { get; set; } = new List<string>();
        [DataMember(Name = "failures")] public int Failures { get; set; }
    }
}
