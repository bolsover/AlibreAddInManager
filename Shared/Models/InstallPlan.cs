using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlibreAddInManager.Models
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

    public static class PlanOperationKind
    {
        /// <summary>Install or update a package into %ProgramData%\Alibre AddOns\&lt;folderName&gt;.</summary>
        public const string Install = "install";
        /// <summary>Remove a tracked add-on: registry value and its folder.</summary>
        public const string Uninstall = "uninstall";
        /// <summary>Remove the registry value only; remember it for Enable.</summary>
        public const string Disable = "disable";
        /// <summary>Restore a registry value removed by Disable.</summary>
        public const string Enable = "enable";
        /// <summary>Remove a registry value whose folder or .adc is missing.</summary>
        public const string RemoveRegistration = "removeRegistration";
    }

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
