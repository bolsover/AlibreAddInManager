using System;
using System.Collections.Generic;
using System.Linq;
using AlibreAddOnManager;
using AlibreAddOnManager.Models;

namespace AlibreCatalogManager.Catalog
{
    /// <summary>
    /// One add-on in an open catalog: its index.json listing and its
    /// addons/{GUID}.json manifest. An unlisted add-on keeps its listing here so
    /// re-listing restores the summary, tags and workspaces.
    /// </summary>
    public sealed class CatalogItem
    {
        public string Id { get; set; }

        public CatalogEntry Entry { get; set; }

        /// <summary>Null when the manifest is missing, unreadable or remote.</summary>
        public AddOnManifest Manifest { get; set; }

        /// <summary>Full local path of the manifest; null for a remote manifestUrl.</summary>
        public string ManifestPath { get; set; }

        /// <summary>Why <see cref="Manifest"/> is null, if it is.</summary>
        public string ManifestError { get; set; }

        /// <summary>True when the add-on appears in index.json.</summary>
        public bool Listed { get; set; }

        /// <summary>The manifest must be written on save.</summary>
        public bool ManifestDirty { get; set; }

        public bool IsEditable => Manifest != null && ManifestPath != null;

        public string Name => Entry?.Name ?? Manifest?.Name ?? Id;

        public string Publisher => Entry?.Publisher ?? Manifest?.Publisher?.Name;

        public IEnumerable<AddOnVersion> VersionsNewestFirst =>
            (Manifest?.Versions ?? new List<AddOnVersion>()).Where(v => v != null).OrderByDescending(v => v.Version, VersionComparer.Instance);

        public AddOnVersion LatestVersion => VersionsNewestFirst.FirstOrDefault();

        public bool Matches(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter)) return true;
            var haystack = string.Join("\n", Name, Id, Publisher, Entry?.Summary, Manifest?.FolderName,
                string.Join(" ", Entry?.Tags ?? new List<string>()));
            return haystack.IndexOf(filter.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
