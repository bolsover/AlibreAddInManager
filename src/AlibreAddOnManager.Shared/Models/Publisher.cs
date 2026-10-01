using System.Runtime.Serialization;

namespace AlibreAddOnManager.Models
{
    [DataContract]
    public class Publisher
    {
        [DataMember(Name = "name")] public string Name { get; set; }
        [DataMember(Name = "url", EmitDefaultValue = false)] public string Url { get; set; }
        [DataMember(Name = "email", EmitDefaultValue = false)] public string Email { get; set; }
    }
}
