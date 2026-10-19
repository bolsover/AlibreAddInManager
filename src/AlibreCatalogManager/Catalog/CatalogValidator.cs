using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AlibreAddOnManager;
using AlibreAddOnManager.Models;

namespace AlibreCatalogManager.Catalog
{
    /// <summary>
    /// Checks a catalog the way the manager will consume it: every listing has a
    /// matching manifest, every version a package whose hash, size and contents
    /// (design doc §3.2) match what the manifest promises.
    /// </summary>
    public static class CatalogValidator
    {
        /// <param name="deep">Also open and validate every local package (slower).</param>
        public static List<CatalogIssue> Check(CatalogDocument doc, bool deep)
        {
            var issues = new List<CatalogIssue>(doc.LoadIssues);

            if (doc.SchemaVersion != 1)
                issues.Add(CatalogIssue.Error(null, $"index.json schemaVersion is {doc.SchemaVersion}; the manager only reads 1."));

            foreach (var group in doc.Items.Where(i => i.Listed).GroupBy(i => i.Id, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
                foreach (var item in group)
                    issues.Add(CatalogIssue.Error(item, $"Listed {group.Count()} times in index.json."));

            foreach (var group in doc.Items.Where(i => !string.IsNullOrWhiteSpace(i.Manifest?.FolderName))
                         .GroupBy(i => i.Manifest.FolderName, StringComparer.OrdinalIgnoreCase)
                         .Where(g => g.Select(i => i.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1))
                foreach (var item in group)
                    issues.Add(CatalogIssue.Error(item, $"Folder name '{group.Key}' is shared with another add-on; installing one overwrites the other."));

            foreach (var item in doc.Items)
                CheckItem(doc, item, deep, issues);

            return issues;
        }

        private static void CheckItem(CatalogDocument doc, CatalogItem item, bool deep, List<CatalogIssue> issues)
        {
            if (!item.Listed)
                issues.Add(CatalogIssue.Info(item, "Unlisted: the manifest is in addons/ but not in index.json."));

            if (!Guid.TryParse(item.Id, out _))
                issues.Add(CatalogIssue.Error(item, $"Id '{item.Id}' is not a GUID."));

            var entry = item.Entry;
            if (item.Listed && string.IsNullOrWhiteSpace(entry?.Name))
                issues.Add(CatalogIssue.Error(item, "The index entry has no name."));

            var icon = doc.ToLocalPath(entry?.IconUrl);
            if (icon != null && !File.Exists(icon))
                issues.Add(CatalogIssue.Warning(item, $"Icon {entry.IconUrl} not found."));

            var manifest = item.Manifest;
            if (manifest == null)
            {
                var remote = item.ManifestPath == null && !string.IsNullOrWhiteSpace(entry?.ManifestUrl);
                issues.Add(remote
                    ? CatalogIssue.Info(item, "Manifest is remote (" + entry.ManifestUrl + "); not checked.")
                    : CatalogIssue.Error(item, "Manifest: " + item.ManifestError));
                return;
            }

            if (manifest.SchemaVersion != 1)
                issues.Add(CatalogIssue.Error(item, $"Manifest schemaVersion is {manifest.SchemaVersion}; the manager only reads 1."));
            if (!ManagerPaths.SameId(manifest.Id, item.Id))
                issues.Add(CatalogIssue.Error(item, $"Manifest id {manifest.Id} does not match the index id {item.Id}."));
            if (!ManagerPaths.IsValidFolderName(manifest.FolderName))
                issues.Add(CatalogIssue.Error(item, $"Folder name '{manifest.FolderName}' is not a single plain folder name."));
            if (entry != null && !string.Equals(entry.Name, manifest.Name, StringComparison.Ordinal))
                issues.Add(CatalogIssue.Warning(item, $"Index name '{entry.Name}' differs from manifest name '{manifest.Name}'."));

            var versions = manifest.Versions.Where(v => v != null).ToList();
            if (versions.Count == 0)
            {
                issues.Add(CatalogIssue.Error(item, "No versions: there is nothing to install."));
                return;
            }

            foreach (var dup in versions.GroupBy(v => v.Version, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
                issues.Add(CatalogIssue.Error(item, $"Version {dup.Key} appears {dup.Count()} times."));

            var latest = item.LatestVersion?.Version;
            if (item.Listed && entry.LatestVersion != latest)
                issues.Add(CatalogIssue.Warning(item, $"index.json latestVersion is '{entry.LatestVersion}' but the newest version is {latest}."));

            foreach (var version in versions)
                CheckVersion(doc, item, version, deep, issues);
        }

        private static void CheckVersion(CatalogDocument doc, CatalogItem item, AddOnVersion version, bool deep, List<CatalogIssue> issues)
        {
            var label = "v" + version.Version + ": ";
            if (string.IsNullOrWhiteSpace(version.Version))
                issues.Add(CatalogIssue.Error(item, "A version has no version number."));
            if (!string.IsNullOrEmpty(version.Kind) && version.Kind != "integrated")
                issues.Add(CatalogIssue.Warning(item, label + $"kind '{version.Kind}' is not installable yet (only 'integrated')."));
            if (!string.IsNullOrEmpty(version.DllType) && version.DllType != "native" && version.DllType != "managed")
                issues.Add(CatalogIssue.Error(item, label + $"dllType '{version.DllType}' must be 'native' or 'managed'."));
            if (version.MinAlibreBuild.HasValue && version.MaxAlibreBuild.HasValue && version.MinAlibreBuild > version.MaxAlibreBuild)
                issues.Add(CatalogIssue.Error(item, label + "minAlibreBuild is above maxAlibreBuild."));
            if (string.IsNullOrWhiteSpace(version.Sha256))
                issues.Add(CatalogIssue.Error(item, label + "no sha256 — the manager refuses to install it."));
            if (string.IsNullOrWhiteSpace(version.PackageUrl))
            {
                issues.Add(CatalogIssue.Error(item, label + "no packageUrl."));
                return;
            }

            var file = doc.PackageFileFor(version);
            if (file == null)
            {
                issues.Add(CatalogIssue.Info(item, label + "package is remote (" + version.PackageUrl + "); not checked."));
                return;
            }

            if (!File.Exists(file))
            {
                issues.Add(CatalogIssue.Error(item, label + $"package {version.PackageUrl} not found."));
                return;
            }

            if (!string.IsNullOrWhiteSpace(version.Sha256) && !string.Equals(Packages.Sha256File(file), version.Sha256.Trim(), StringComparison.OrdinalIgnoreCase))
                issues.Add(CatalogIssue.Error(item, label + "sha256 does not match the package file (use Rehash if it was rebuilt on purpose)."));
            if (version.Size.HasValue && new FileInfo(file).Length != version.Size.Value)
                issues.Add(CatalogIssue.Error(item, label + $"size is {version.Size} but the package is {new FileInfo(file).Length} bytes."));

            if (!deep) return;

            var result = Packages.Validate(file, item.Id, version.DllType);
            foreach (var error in result.Errors) issues.Add(CatalogIssue.Error(item, label + error));
            foreach (var warning in result.Warnings) issues.Add(CatalogIssue.Warning(item, label + warning));
            if (result.Manifest == null) return;

            if (Packages.CompareVersions(result.Manifest.Version, version.Version) != 0)
                issues.Add(CatalogIssue.Error(item, label + $"the package's addon.json says version {result.Manifest.Version}."));
            if (item.Manifest != null && !string.Equals(result.Manifest.FolderName, item.Manifest.FolderName, StringComparison.OrdinalIgnoreCase))
                issues.Add(CatalogIssue.Warning(item, label + $"the package's folderName '{result.Manifest.FolderName}' differs from the manifest's '{item.Manifest.FolderName}'."));
        }
    }
}
