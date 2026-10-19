using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using AlibreAddOnManager;
using AlibreAddOnManager.Models;

namespace AlibreCatalogManager.Catalog
{
    /// <summary>
    /// Builds a missing addon.json from the add-on's .adc, so a folder that was
    /// never prepared for the catalog (an older project, a vendor build output)
    /// can still be packaged. The .adc supplies id, name, folder name, DLL type,
    /// publisher and description; the DLL, when present, supplies version and arch.
    /// </summary>
    public static class AddOnManifestFactory
    {
        public const string DefaultVersion = "1.0.0";

        /// <summary>
        /// The single .adc in <paramref name="folder"/> with a GUID Identifier.
        /// Throws if there is none, or more than one (which one is the add-on is then a guess).
        /// </summary>
        public static AdcFile FindAdc(string folder)
        {
            var candidates = new List<AdcFile>();
            var unreadable = new List<string>();
            foreach (var file in Directory.GetFiles(folder, "*.adc"))
            {
                try
                {
                    var adc = AdcFile.Load(file);
                    if (Guid.TryParse(adc.Identifier, out _)) candidates.Add(adc);
                }
                catch (Exception e)
                {
                    unreadable.Add($"{Path.GetFileName(file)}: {e.Message}");
                }
            }

            if (candidates.Count == 1) return candidates[0];
            if (candidates.Count > 1)
                throw new InvalidOperationException(
                    $"No {Packages.ManifestEntry} in {folder}, and it holds several .adc files ({string.Join(", ", candidates.Select(a => Path.GetFileName(a.Path)))}); " +
                    $"write {Packages.ManifestEntry} by hand to say which add-on to package.");
            throw new InvalidOperationException(
                $"No {Packages.ManifestEntry} in {folder}, and no .adc there with an Identifier to build one from." +
                (unreadable.Count > 0 ? " Unreadable: " + string.Join("; ", unreadable) : string.Empty));
        }

        /// <summary>
        /// A manifest for the .adc in <paramref name="folder"/>. <paramref name="version"/>
        /// and <paramref name="arch"/> override what the DLL says (or is missing).
        /// </summary>
        public static PackageManifest FromAdc(string folder, string version = null, string arch = null)
        {
            var adc = FindAdc(folder);
            if (adc.DllType == AddOnDllType.Unrecognised)
                throw new InvalidOperationException($"{Path.GetFileName(adc.Path)} has <DLL type=\"{adc.DllTypeText}\">, which is not recognised.");

            var dllPath = adc.ResolveDllPath();
            var dll = dllPath != null && File.Exists(dllPath) ? dllPath : null;

            return new PackageManifest
            {
                SchemaVersion = 1,
                Id = ManagerPaths.NormalizeId(adc.Identifier),
                Name = adc.DisplayName,
                FolderName = FolderNameFor(adc),
                Version = version ?? VersionOf(dll) ?? DefaultVersion,
                DllType = AdcFile.DllTypeName(adc.DllType),
                Arch = arch ?? ArchOf(dll),
                Publisher = NullIfEmpty(adc.AuthorName),
                Description = NullIfEmpty(adc.Description)
            };
        }

        /// <summary>Builds addon.json from the .adc and writes it into <paramref name="folder"/>.</summary>
        public static PackageManifest WriteFromAdc(string folder, string version = null, string arch = null)
        {
            var manifest = FromAdc(folder, version, arch);
            File.WriteAllText(Path.Combine(folder, Packages.ManifestEntry), ToJson(manifest), new UTF8Encoding(false));
            return manifest;
        }

        /// <summary>The .adc's file name (the add-on's name by convention), else the DLL's.</summary>
        private static string FolderNameFor(AdcFile adc)
        {
            var fromAdc = Path.GetFileNameWithoutExtension(adc.Path);
            if (ManagerPaths.IsValidFolderName(fromAdc)) return fromAdc;
            var fromDll = Path.GetFileNameWithoutExtension(adc.DllLocation ?? string.Empty);
            if (ManagerPaths.IsValidFolderName(fromDll)) return fromDll;
            throw new InvalidOperationException($"Cannot derive a folder name from {Path.GetFileName(adc.Path)}.");
        }

        /// <summary>major.minor.patch of the DLL's file version; null if it has none.</summary>
        private static string VersionOf(string dll)
        {
            if (dll == null) return null;
            var info = FileVersionInfo.GetVersionInfo(dll);
            if (info.FileMajorPart == 0 && info.FileMinorPart == 0 && info.FileBuildPart == 0) return null;
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}", info.FileMajorPart, info.FileMinorPart, info.FileBuildPart);
        }

        private static string ArchOf(string dll)
        {
            if (dll == null) return null;
            try
            {
                var pe = PeInspector.Inspect(dll);
                if (pe.Machine == PeInfo.MachineAmd64) return "x64";
                return pe.MachineName == "AnyCPU" ? "anycpu" : null;
            }
            catch (Exception)
            {
                // Package validation reports a bad DLL; leave arch out here.
                return null;
            }
        }

        /// <summary>The same layout as the generator's addon.json template.</summary>
        private static string ToJson(PackageManifest m)
        {
            var fields = new List<string>
            {
                "\"schemaVersion\": " + m.SchemaVersion.ToString(CultureInfo.InvariantCulture),
                "\"id\": " + Str(m.Id),
                "\"name\": " + Str(m.Name),
                "\"folderName\": " + Str(m.FolderName),
                "\"version\": " + Str(m.Version)
            };
            if (m.DllType != null) fields.Add("\"dllType\": " + Str(m.DllType));
            if (m.Arch != null) fields.Add("\"arch\": " + Str(m.Arch));
            if (m.MinAlibreBuild != null) fields.Add("\"minAlibreBuild\": " + m.MinAlibreBuild.Value.ToString(CultureInfo.InvariantCulture));
            if (m.Publisher != null) fields.Add("\"publisher\": " + Str(m.Publisher));
            if (m.Description != null) fields.Add("\"description\": " + Str(m.Description));
            return "{\r\n  " + string.Join(",\r\n  ", fields) + "\r\n}\r\n";
        }

        /// <summary>A JSON string literal, escaped by the framework serializer.</summary>
        private static string Str(string value) => CatalogJson.Serialize(value ?? string.Empty, string.Empty).Trim();

        private static string NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
