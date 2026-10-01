namespace AlibreAddOnManager
{
    public enum AddOnStatus
    {
        /// <summary>This add-on manager itself.</summary>
        Self,
        /// <summary>Installed by this tool; its files are ours to replace or delete.</summary>
        Tracked,
        /// <summary>Registered by hand, a vendor installer or a developer build — never delete its files.</summary>
        External,
        /// <summary>Registry value removed by Disable; can be restored.</summary>
        Disabled,
        /// <summary>Registry points at a missing folder, or no .adc there carries the id.</summary>
        Broken
    }
}
