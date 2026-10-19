using System.Collections.Generic;
using AlibreAddOnManager;

namespace AlibreCatalogManager.Catalog
{
    internal sealed class VersionComparer : IComparer<string>
    {
        public static readonly VersionComparer Instance = new VersionComparer();

        public int Compare(string x, string y) => Packages.CompareVersions(x, y);
    }
}
