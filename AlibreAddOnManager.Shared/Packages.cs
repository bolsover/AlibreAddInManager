using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using AlibreAddOnManager.Models;

namespace AlibreAddOnManager
{
    /// <summary>
    /// A .alibreaddon package is a zip whose root is the deploy folder: addon.json,
    /// the .adc, the DLL, the icon and any other runtime files.
    /// </summary>
    public static class Packages
    {
        public const string Extension = ".alibreaddon";
        public const string ManifestEntry = "addon.json";

        private static readonly string[] NativeExports = { "AddOnLoad", "AddOnInvoke", "AddOnUnload" };

        public static string Sha256File(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Checks a package against design doc §3.2. expectedId / expectedDllType
        /// come from the catalog manifest when installing from the directory.
        /// </summary>
        public static PackageValidationResult Validate(string packagePath, string expectedId = null, string expectedDllType = null)
        {
            var result = new PackageValidationResult();
            try
            {
                using (var zip = ZipFile.OpenRead(packagePath))
                    ValidateArchive(zip, expectedId, expectedDllType, result);
            }
            catch (InvalidDataException e)
            {
                result.Errors.Add("Not a valid package (zip): " + e.Message);
            }
            catch (IOException e)
            {
                result.Errors.Add("Could not read package: " + e.Message);
            }

            return result;
        }

        private static void ValidateArchive(ZipArchive zip, string expectedId, string expectedDllType, PackageValidationResult result)
        {
            foreach (var entry in zip.Entries)
                if (!IsSafeEntryName(entry.FullName))
                    result.Errors.Add($"Unsafe path in package: '{entry.FullName}'.");
            if (!result.IsValid) return;

            var manifestEntry = zip.Entries.FirstOrDefault(e => string.Equals(e.FullName, ManifestEntry, StringComparison.OrdinalIgnoreCase));
            if (manifestEntry == null)
            {
                result.Errors.Add("Package has no addon.json at its root.");
                return;
            }

            using (var stream = manifestEntry.Open())
            {
                try { result.Manifest = Json.Parse<PackageManifest>(stream); }
                catch (Exception e) { result.Errors.Add("addon.json is not valid: " + e.Message); return; }
            }

            var manifest = result.Manifest;
            if (!Guid.TryParse(manifest.Id, out _))
                result.Errors.Add("addon.json 'id' is not a GUID.");
            if (!ManagerPaths.IsValidFolderName(manifest.FolderName))
                result.Errors.Add($"addon.json 'folderName' '{manifest.FolderName}' is not a single plain folder name.");
            if (string.IsNullOrWhiteSpace(manifest.Version))
                result.Errors.Add("addon.json has no 'version'.");
            if (!string.IsNullOrWhiteSpace(expectedId) && !ManagerPaths.SameId(expectedId, manifest.Id))
                result.Errors.Add($"Package id {manifest.Id} does not match the catalog id {expectedId}.");
            if (!result.IsValid) return;

            // The .adc at the root whose Identifier equals the package id.
            foreach (var adcEntry in zip.Entries.Where(e => !e.FullName.Contains("/") && e.FullName.EndsWith(".adc", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    using (var stream = adcEntry.Open())
                    {
                        var adc = AdcFile.Parse(stream);
                        if (ManagerPaths.SameId(adc.Identifier, manifest.Id)) { result.Adc = adc; break; }
                    }
                }
                catch (Exception e)
                {
                    result.Warnings.Add($"{adcEntry.FullName} could not be read: {e.Message}");
                }
            }

            if (result.Adc == null)
            {
                result.Errors.Add("No .adc at the package root has Identifier " + manifest.Id + " — Alibre would silently skip the add-on.");
                return;
            }

            var adcFile = result.Adc;
            if (adcFile.DllType == AddOnDllType.Unrecognised)
                result.Errors.Add($"The .adc <DLL type=\"{adcFile.DllTypeText}\"> is not recognised.");

            var declared = !string.IsNullOrWhiteSpace(expectedDllType) ? expectedDllType : manifest.DllType;
            if (!string.IsNullOrWhiteSpace(declared) &&
                !string.Equals(declared, AdcFile.DllTypeName(adcFile.DllType), StringComparison.OrdinalIgnoreCase))
                result.Errors.Add($"Manifest says dllType '{declared}' but the .adc declares a {AdcFile.DllTypeName(adcFile.DllType)} DLL.");

            var location = adcFile.DllLocation;
            if (string.IsNullOrWhiteSpace(location))
            {
                result.Errors.Add("The .adc has no <DLL location>.");
                return;
            }

            var normalized = location.Replace('\\', '/');
            if (Path.IsPathRooted(location) || !IsSafeEntryName(normalized))
            {
                result.Errors.Add($"<DLL location=\"{location}\"> must be a relative path inside the add-on folder.");
                return;
            }

            var dllEntry = zip.Entries.FirstOrDefault(e => string.Equals(e.FullName, normalized, StringComparison.OrdinalIgnoreCase));
            if (dllEntry == null)
            {
                result.Errors.Add($"The DLL '{location}' named in the .adc is not in the package.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(adcFile.IconLocation) &&
                !zip.Entries.Any(e => string.Equals(e.FullName, adcFile.IconLocation.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase)))
                result.Warnings.Add($"Icon '{adcFile.IconLocation}' named in the .adc is not in the package.");

            byte[] image;
            using (var stream = dllEntry.Open())
            using (var memory = new MemoryStream())
            {
                stream.CopyTo(memory);
                image = memory.ToArray();
            }

            try
            {
                result.Dll = PeInspector.Inspect(image);
            }
            catch (InvalidDataException e)
            {
                result.Errors.Add($"{location}: {e.Message}");
                return;
            }

            if (adcFile.DllType != AddOnDllType.Unrecognised && !result.Dll.IsLoadableBy64BitHost(adcFile.DllType, out var reason))
                result.Errors.Add($"{location}: {reason}.");

            if (adcFile.DllType == AddOnDllType.Native)
            {
                var missing = NativeExports.Where(n => !result.Dll.ExportNames.Contains(n)).ToList();
                if (missing.Count > 0)
                    result.Errors.Add($"{location} does not export {string.Join(", ", missing)} (undecorated).");
            }

            if (adcFile.SpecificationVersion != "2")
                result.Warnings.Add($"The .adc specificationVersion is '{adcFile.SpecificationVersion}'; MenuIcon and dedicated ribbon tabs need \"2\".");
        }

        /// <summary>Zip-slip protection: relative, no '..', no drive or UNC prefix.</summary>
        public static bool IsSafeEntryName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            var n = name.Replace('\\', '/');
            if (n.StartsWith("/") || n.Contains(":")) return false;
            return n.Split('/').All(part => part != ".." && part != ".");
        }

        /// <summary>Extracts entry by entry, refusing anything that would land outside target.</summary>
        public static void ExtractSafely(string packagePath, string targetDirectory)
        {
            var root = Path.GetFullPath(targetDirectory).TrimEnd('\\') + "\\";
            Directory.CreateDirectory(root);
            using (var zip = ZipFile.OpenRead(packagePath))
            {
                foreach (var entry in zip.Entries)
                {
                    if (!IsSafeEntryName(entry.FullName))
                        throw new InvalidDataException($"Unsafe path in package: '{entry.FullName}'.");

                    var destination = Path.GetFullPath(Path.Combine(root, entry.FullName.Replace('/', '\\')));
                    if (!destination.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException($"Package entry escapes the target folder: '{entry.FullName}'.");

                    if (entry.FullName.EndsWith("/"))
                    {
                        Directory.CreateDirectory(destination);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    entry.ExtractToFile(destination, true);
                }
            }
        }

        /// <summary>Loose version compare: numeric fields first, then ordinal text.</summary>
        public static int CompareVersions(string a, string b)
        {
            if (Version.TryParse(Strip(a), out var va) && Version.TryParse(Strip(b), out var vb))
                return va.CompareTo(vb);
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private static string Strip(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return "0";
            v = v.Trim().TrimStart('v', 'V');
            var dash = v.IndexOfAny(new[] { '-', '+' });
            v = dash >= 0 ? v.Substring(0, dash) : v;
            return v.Contains(".") ? v : v + ".0";
        }
    }
}
