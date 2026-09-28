using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlibreAddInManager.Models
{
    /// <summary>catalog/index.json — the lightweight listing fetched on every refresh.</summary>
    [DataContract]
    public class CatalogIndex
    {
        [DataMember(Name = "schemaVersion")] public int SchemaVersion { get; set; }
        [DataMember(Name = "generated", EmitDefaultValue = false)] public string Generated { get; set; }
        [DataMember(Name = "addons")] public List<CatalogEntry> AddOns { get; set; } = new List<CatalogEntry>();
    }

    [DataContract]
    public class CatalogEntry
    {
        [DataMember(Name = "id")] public string Id { get; set; }
        [DataMember(Name = "name")] public string Name { get; set; }
        [DataMember(Name = "publisher", EmitDefaultValue = false)] public string Publisher { get; set; }
        [DataMember(Name = "summary", EmitDefaultValue = false)] public string Summary { get; set; }
        [DataMember(Name = "latestVersion", EmitDefaultValue = false)] public string LatestVersion { get; set; }
        [DataMember(Name = "workspaces", EmitDefaultValue = false)] public List<string> Workspaces { get; set; }
        [DataMember(Name = "tags", EmitDefaultValue = false)] public List<string> Tags { get; set; }
        [DataMember(Name = "manifestUrl")] public string ManifestUrl { get; set; }
        [DataMember(Name = "iconUrl", EmitDefaultValue = false)] public string IconUrl { get; set; }
    }

    /// <summary>catalog/addons/{GUID}.json — every published version of one add-on.</summary>
    [DataContract]
    public class AddOnManifest
    {
        [DataMember(Name = "schemaVersion")] public int SchemaVersion { get; set; }
        [DataMember(Name = "id")] public string Id { get; set; }
        [DataMember(Name = "name")] public string Name { get; set; }
        [DataMember(Name = "folderName")] public string FolderName { get; set; }
        [DataMember(Name = "publisher", EmitDefaultValue = false)] public Publisher Publisher { get; set; }
        [DataMember(Name = "license", EmitDefaultValue = false)] public string License { get; set; }
        [DataMember(Name = "homepage", EmitDefaultValue = false)] public string Homepage { get; set; }
        [DataMember(Name = "description", EmitDefaultValue = false)] public string Description { get; set; }
        [DataMember(Name = "versions")] public List<AddOnVersion> Versions { get; set; } = new List<AddOnVersion>();
    }

    [DataContract]
    public class Publisher
    {
        [DataMember(Name = "name")] public string Name { get; set; }
        [DataMember(Name = "url", EmitDefaultValue = false)] public string Url { get; set; }
        [DataMember(Name = "email", EmitDefaultValue = false)] public string Email { get; set; }
    }

    [DataContract]
    public class AddOnVersion
    {
        [DataMember(Name = "version")] public string Version { get; set; }
        [DataMember(Name = "released", EmitDefaultValue = false)] public string Released { get; set; }
        /// <summary>"integrated" (v1) — "standalone" is reserved for later.</summary>
        [DataMember(Name = "kind", EmitDefaultValue = false)] public string Kind { get; set; }
        /// <summary>"native" or "managed"; must agree with the .adc &lt;DLL type&gt;.</summary>
        [DataMember(Name = "dllType", EmitDefaultValue = false)] public string DllType { get; set; }
        /// <summary>"x64", or "anycpu" for managed add-ons.</summary>
        [DataMember(Name = "arch", EmitDefaultValue = false)] public string Arch { get; set; }
        [DataMember(Name = "minAlibreBuild", EmitDefaultValue = false)] public int? MinAlibreBuild { get; set; }
        [DataMember(Name = "maxAlibreBuild", EmitDefaultValue = false)] public int? MaxAlibreBuild { get; set; }
        [DataMember(Name = "packageUrl")] public string PackageUrl { get; set; }
        [DataMember(Name = "sha256")] public string Sha256 { get; set; }
        [DataMember(Name = "size", EmitDefaultValue = false)] public long? Size { get; set; }
        [DataMember(Name = "prerequisites", EmitDefaultValue = false)] public List<string> Prerequisites { get; set; }
        [DataMember(Name = "releaseNotes", EmitDefaultValue = false)] public string ReleaseNotes { get; set; }

        public bool IsCompatibleWith(int build)
        {
            if (build <= 0) return true; // unknown build: don't filter
            if (MinAlibreBuild.HasValue && build < MinAlibreBuild.Value) return false;
            if (MaxAlibreBuild.HasValue && build > MaxAlibreBuild.Value) return false;
            return true;
        }
    }

    /// <summary>
    /// addon.json at the root of a .alibreaddon package — enough to install from a
    /// file with no catalog available.
    /// </summary>
    [DataContract]
    public class PackageManifest
    {
        [DataMember(Name = "schemaVersion")] public int SchemaVersion { get; set; }
        [DataMember(Name = "id")] public string Id { get; set; }
        [DataMember(Name = "name")] public string Name { get; set; }
        [DataMember(Name = "folderName")] public string FolderName { get; set; }
        [DataMember(Name = "version")] public string Version { get; set; }
        [DataMember(Name = "dllType", EmitDefaultValue = false)] public string DllType { get; set; }
        [DataMember(Name = "arch", EmitDefaultValue = false)] public string Arch { get; set; }
        [DataMember(Name = "minAlibreBuild", EmitDefaultValue = false)] public int? MinAlibreBuild { get; set; }
        [DataMember(Name = "maxAlibreBuild", EmitDefaultValue = false)] public int? MaxAlibreBuild { get; set; }
        [DataMember(Name = "publisher", EmitDefaultValue = false)] public string Publisher { get; set; }
        [DataMember(Name = "description", EmitDefaultValue = false)] public string Description { get; set; }
    }
}
