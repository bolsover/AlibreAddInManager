using System.Runtime.Serialization;

namespace AlibreAddOnManager.Models
{
    [DataContract]
    public class PlanOperation
    {
        [DataMember(Name = "kind")] public string Kind { get; set; }
        [DataMember(Name = "id")] public string Id { get; set; }
        [DataMember(Name = "name", EmitDefaultValue = false)] public string Name { get; set; }

        // Install only
        [DataMember(Name = "folderName", EmitDefaultValue = false)] public string FolderName { get; set; }
        [DataMember(Name = "packagePath", EmitDefaultValue = false)] public string PackagePath { get; set; }
        [DataMember(Name = "sha256", EmitDefaultValue = false)] public string Sha256 { get; set; }
        [DataMember(Name = "version", EmitDefaultValue = false)] public string Version { get; set; }
        [DataMember(Name = "source", EmitDefaultValue = false)] public string Source { get; set; }

        public string Describe()
        {
            switch (Kind)
            {
                case PlanOperationKind.Install: return $"Install {Name} {Version}";
                case PlanOperationKind.Uninstall: return $"Uninstall {Name}";
                case PlanOperationKind.Disable: return $"Disable {Name}";
                case PlanOperationKind.Enable: return $"Enable {Name}";
                case PlanOperationKind.RemoveRegistration: return $"Remove broken registration {Name ?? Id}";
                default: return $"{Kind} {Name ?? Id}";
            }
        }
    }
}
