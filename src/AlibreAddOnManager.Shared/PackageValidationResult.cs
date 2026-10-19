using System.Collections.Generic;
using AlibreAddOnManager.Models;

namespace AlibreAddOnManager
{
    public class PackageValidationResult
    {
        public PackageManifest Manifest { get; set; }
        public AdcFile Adc { get; set; }
        public PeInfo Dll { get; set; }
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
        public bool IsValid => Errors.Count == 0;
    }
}
