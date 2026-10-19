using System.Runtime.Serialization;

namespace AlibreAddOnManager.Models
{
    [DataContract]
    public class Publisher
    {
        [DataMember(Name = "name", Order = 1)] public string Name { get; set; }
        [DataMember(Name = "url", Order = 2, EmitDefaultValue = false)] public string Url { get; set; }
        [DataMember(Name = "email", Order = 3, EmitDefaultValue = false)] public string Email { get; set; }
    }
}
