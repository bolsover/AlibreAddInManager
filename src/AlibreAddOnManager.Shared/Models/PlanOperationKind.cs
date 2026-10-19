namespace AlibreAddOnManager.Models
{
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
}
