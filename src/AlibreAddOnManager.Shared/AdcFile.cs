using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace AlibreAddOnManager
{
    /// <summary>Reader for an add-on configuration (.adc) file.</summary>
    public class AdcFile
    {
        public string Path { get; private set; }
        public string SpecificationVersion { get; private set; }
        public string FriendlyName { get; private set; }
        public string AuthorName { get; private set; }
        public string AuthorLink { get; private set; }
        public string DllLocation { get; private set; }
        public string DllTypeText { get; private set; }
        public AddOnDllType DllType { get; private set; }
        public string LoadedWhen { get; private set; }
        public string IconLocation { get; private set; }
        public string MenuText { get; private set; }
        public string Description { get; private set; }
        public string Workspace { get; private set; }
        public string Identifier { get; private set; }

        public string DisplayName =>
            !string.IsNullOrWhiteSpace(FriendlyName) ? FriendlyName.Trim()
            : !string.IsNullOrWhiteSpace(MenuText) ? MenuText.Trim()
            : System.IO.Path.GetFileNameWithoutExtension(Path);

        public static AdcFile Load(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                var adc = Parse(stream);
                adc.Path = path;
                return adc;
            }
        }

        public static AdcFile Parse(Stream stream)
        {
            var root = XDocument.Load(stream).Root;
            if (root == null || !string.Equals(root.Name.LocalName, "AlibreDesignAddOn", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Root element is not <AlibreDesignAddOn>.");

            var dll = Element(root, "DLL");
            var author = Element(root, "Author");
            var typeText = Attribute(dll, "type");

            var identifier = root.Elements()
                .Where(e => string.Equals(e.Name.LocalName, "Property", StringComparison.OrdinalIgnoreCase))
                .Where(e => string.Equals(Attribute(e, "name"), "Identifier", StringComparison.OrdinalIgnoreCase))
                .Select(e => Attribute(e, "value"))
                .FirstOrDefault();

            return new AdcFile
            {
                SpecificationVersion = Attribute(root, "specificationVersion"),
                FriendlyName = Attribute(root, "friendlyName"),
                AuthorName = Attribute(author, "name"),
                AuthorLink = Attribute(author, "link"),
                DllLocation = Attribute(dll, "location"),
                DllTypeText = typeText,
                DllType = string.IsNullOrWhiteSpace(typeText) ? AddOnDllType.Native
                    : string.Equals(typeText.Trim(), "Managed", StringComparison.OrdinalIgnoreCase) ? AddOnDllType.Managed
                    : AddOnDllType.Unrecognised,
                LoadedWhen = Attribute(dll, "loadedWhen"),
                IconLocation = Attribute(Element(root, "Icon"), "location"),
                MenuText = Attribute(Element(root, "Menu"), "text"),
                Description = Element(root, "Description")?.Value.Trim(),
                Workspace = Attribute(Element(root, "Workspace"), "type"),
                Identifier = identifier?.Trim()
            };
        }

        /// <summary>
        /// Finds the .adc in a folder whose Identifier matches — a folder can hold
        /// more than one .adc. Returns null if none matches.
        /// </summary>
        public static AdcFile FindForIdentifier(string folder, string id)
        {
            if (!Directory.Exists(folder)) return null;
            foreach (var file in Directory.GetFiles(folder, "*.adc"))
            {
                try
                {
                    var adc = Load(file);
                    if (ManagerPaths.SameId(adc.Identifier, id))
                        return adc;
                }
                catch (Exception)
                {
                    // Unreadable .adc: keep looking; the caller reports "no matching .adc".
                }
            }

            return null;
        }

        /// <summary>Resolves the DLL location against the .adc's folder (it may be absolute).</summary>
        public string ResolveDllPath()
        {
            if (string.IsNullOrWhiteSpace(DllLocation) || Path == null) return null;
            return System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path), DllLocation));
        }

        public static string DllTypeName(AddOnDllType type)
        {
            switch (type)
            {
                case AddOnDllType.Managed: return "managed";
                case AddOnDllType.Native: return "native";
                default: return "unrecognised";
            }
        }

        private static XElement Element(XElement parent, string localName)
        {
            return parent?.Elements().FirstOrDefault(e => string.Equals(e.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase));
        }

        private static string Attribute(XElement element, string localName)
        {
            return element?.Attributes().FirstOrDefault(a => string.Equals(a.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase))?.Value;
        }
    }
}
