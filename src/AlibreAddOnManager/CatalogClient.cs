using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AlibreAddOnManager.Models;

namespace AlibreAddOnManager
{
    /// <summary>
    /// Reads the static catalog (index.json + per-add-on manifests) and downloads
    /// packages. Accepts https:// URLs, file:// URLs and plain local paths (for a
    /// catalog on a share or in a checkout). Plain http:// is refused.
    /// Catalog signature verification (design doc §3.3) is not implemented yet.
    /// </summary>
    public class CatalogClient
    {
        private static readonly HttpClient Http = CreateHttpClient();

        public Uri IndexUri { get; private set; }

        public async Task<CatalogIndex> LoadIndexAsync(string location)
        {
            IndexUri = ToUri(location);
            var index = Json.Parse<CatalogIndex>(await ReadAsync(IndexUri).ConfigureAwait(false));
            if (index.SchemaVersion != 1)
                throw new InvalidDataException($"Unsupported catalog schemaVersion {index.SchemaVersion}.");
            return index;
        }

        public async Task<AddOnManifest> LoadManifestAsync(CatalogEntry entry)
        {
            var manifest = Json.Parse<AddOnManifest>(await ReadAsync(Resolve(entry.ManifestUrl)).ConfigureAwait(false));
            if (!ManagerPaths.SameId(manifest.Id, entry.Id))
                throw new InvalidDataException($"Manifest id {manifest.Id} does not match catalog entry {entry.Id}.");
            return manifest;
        }

        /// <summary>Downloads into the per-user cache and checks size and SHA-256.</summary>
        public async Task<string> DownloadAsync(AddOnManifest manifest, AddOnVersion version)
        {
            if (string.IsNullOrWhiteSpace(version.Sha256))
                throw new InvalidDataException("Catalog entry has no sha256; refusing to install.");

            var uri = Resolve(version.PackageUrl);
            var fileName = Path.GetFileName(uri.LocalPath);
            if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                fileName = manifest.FolderName + "-" + version.Version + Packages.Extension;

            var directory = Path.Combine(ManagerPaths.CacheDirectory, Guid.Parse(manifest.Id).ToString("N"), SafeSegment(version.Version));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, fileName);

            if (!File.Exists(path) || !HashMatches(path, version.Sha256))
            {
                var data = await ReadAsync(uri).ConfigureAwait(false);
                if (version.Size.HasValue && data.LongLength != version.Size.Value)
                    throw new InvalidDataException($"Downloaded {data.LongLength} bytes; catalog says {version.Size.Value}.");
                File.WriteAllBytes(path, data);
            }

            if (!HashMatches(path, version.Sha256))
            {
                File.Delete(path);
                throw new InvalidDataException("Downloaded package does not match the catalog sha256.");
            }

            return path;
        }

        /// <summary>Newest version compatible with the running build, else the newest overall.</summary>
        public static AddOnVersion ChooseVersion(AddOnManifest manifest, int runningBuild)
        {
            var ordered = manifest.Versions
                .Where(v => v != null && !string.IsNullOrWhiteSpace(v.PackageUrl))
                .Where(v => string.IsNullOrEmpty(v.Kind) || v.Kind == "integrated")
                .OrderByDescending(v => v.Version, Comparer.Instance)
                .ToList();
            return ordered.FirstOrDefault(v => v.IsCompatibleWith(runningBuild)) ?? ordered.FirstOrDefault();
        }

        private Uri Resolve(string relativeOrAbsolute)
        {
            if (Uri.TryCreate(relativeOrAbsolute, UriKind.Absolute, out var absolute) && absolute.Scheme != Uri.UriSchemeFile)
                return Check(absolute);
            return Check(new Uri(IndexUri, relativeOrAbsolute));
        }

        private static Uri ToUri(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
                throw new ArgumentException("No catalog location set.");
            location = location.Trim();
            // Note "D:\catalog" also parses as an absolute file: URI, so file URIs
            // and plain paths share the local branch below.
            if (Uri.TryCreate(location, UriKind.Absolute, out var uri) && !uri.IsFile)
                return Check(uri);

            // A local path: a folder means its index.json.
            var path = Path.GetFullPath(uri != null && uri.IsFile ? uri.LocalPath : location);
            if (Directory.Exists(path)) path = Path.Combine(path, "index.json");
            return new Uri(path);
        }

        private static Uri Check(Uri uri)
        {
            if (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeFile) return uri;
            throw new InvalidOperationException($"Only https:// or local catalogs are allowed (got {uri.Scheme}://).");
        }

        private static async Task<byte[]> ReadAsync(Uri uri)
        {
            if (uri.IsFile)
                return File.ReadAllBytes(uri.LocalPath);
            using (var response = await Http.GetAsync(uri).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
        }

        private static bool HashMatches(string path, string sha256)
        {
            return string.Equals(Packages.Sha256File(path), sha256.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static string SafeSegment(string text)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var clean = new string((text ?? "unknown").Select(c => invalid.Contains(c) ? '_' : c).ToArray());
            return clean == "." || clean == ".." ? "_" : clean;
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AlibreAddOnManager/1.0");
            return client;
        }

        private sealed class Comparer : System.Collections.Generic.IComparer<string>
        {
            public static readonly Comparer Instance = new Comparer();
            public int Compare(string x, string y) => Packages.CompareVersions(x, y);
        }
    }
}
