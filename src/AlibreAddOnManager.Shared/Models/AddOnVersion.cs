using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AlibreAddOnManager.Models
{
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
}
