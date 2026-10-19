namespace AlibreAddOnManager
{
    public enum AddOnDllType
    {
        /// <summary>No type attribute: the documented C++/MFC COM DLL.</summary>
        Native,
        /// <summary>type="Managed": a .NET assembly exposing AlibreAddOnAssembly.AlibreAddOn.</summary>
        Managed,
        /// <summary>Some other type value — not guessed at.</summary>
        Unrecognised
    }
}
