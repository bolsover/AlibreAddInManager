namespace AlibreCatalogManager.Generation
{
    /// <summary>The language a new add-on project is written in.</summary>
    public enum AddOnLanguage
    {
        /// <summary>A managed .NET Framework class library (.csproj).</summary>
        CSharp,

        /// <summary>A native Win32 x64 DLL (.vcxproj).</summary>
        Cpp
    }
}
