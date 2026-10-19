using System;
using System.ComponentModel;
using AlibreAddOnManager.Models;
using AlibreCatalogManager.Catalog;

namespace AlibreCatalogManager.Ui
{
    /// <summary>
    /// Property-editor view of one published version. Version, hash and size describe
    /// the package itself and are read-only; add a new version or Rehash instead.
    /// </summary>
    public sealed class VersionEditor
    {
        private const string Package = "Package";
        private const string Compatibility = "Compatibility";
        private const string Release = "Release";

        private readonly CatalogDocument _doc;
        private readonly CatalogItem _item;
        private readonly AddOnVersion _version;
        private readonly Action _changed;

        public VersionEditor(CatalogDocument doc, CatalogItem item, AddOnVersion version, Action changed)
        {
            _doc = doc;
            _item = item;
            _version = version;
            _changed = changed;
        }

        [Category(Package)]
        public string Version => _version.Version;

        [Category(Package), Description("Relative to index.json, or an absolute https:// URL (e.g. a GitHub release asset).")]
        public string PackageUrl
        {
            get => _version.PackageUrl;
            set { _version.PackageUrl = AddOnEditor.Clean(value); Changed(); }
        }

        [Category(Package)]
        public string Sha256 => _version.Sha256;

        [Category(Package)]
        public long? Size => _version.Size;

        [Category(Compatibility), Description("integrated (installable) or standalone (reserved).")]
        public string Kind
        {
            get => _version.Kind;
            set { _version.Kind = AddOnEditor.Clean(value); Changed(); }
        }

        [Category(Compatibility), Description("native or managed; must agree with the .adc <DLL type>.")]
        public string DllType
        {
            get => _version.DllType;
            set { _version.DllType = AddOnEditor.Clean(value)?.ToLowerInvariant(); Changed(); }
        }

        [Category(Compatibility), Description("x64, or anycpu for managed add-ons.")]
        public string Arch
        {
            get => _version.Arch;
            set { _version.Arch = AddOnEditor.Clean(value)?.ToLowerInvariant(); Changed(); }
        }

        [Category(Compatibility), Description("Lowest Alibre build (trailing number of IADRoot.Version). Empty for none.")]
        public int? MinAlibreBuild
        {
            get => _version.MinAlibreBuild;
            set { _version.MinAlibreBuild = value; Changed(); }
        }

        [Category(Compatibility), Description("Highest Alibre build. Empty for none.")]
        public int? MaxAlibreBuild
        {
            get => _version.MaxAlibreBuild;
            set { _version.MaxAlibreBuild = value; Changed(); }
        }

        [Category(Compatibility), Description("Comma-separated, e.g. vcredist-x64-14.")]
        public string Prerequisites
        {
            get => AddOnEditor.Join(_version.Prerequisites);
            set { _version.Prerequisites = AddOnEditor.Split(value); Changed(); }
        }

        [Category(Release), Description("yyyy-MM-dd")]
        public string Released
        {
            get => _version.Released;
            set { _version.Released = AddOnEditor.Clean(value); Changed(); }
        }

        [Category(Release)]
        [Multiline]
        public string ReleaseNotes
        {
            get => _version.ReleaseNotes;
            set { _version.ReleaseNotes = string.IsNullOrWhiteSpace(value) ? null : value; Changed(); }
        }

        private void Changed()
        {
            _doc.MarkManifestDirty(_item);
            _changed?.Invoke();
        }
    }
}
