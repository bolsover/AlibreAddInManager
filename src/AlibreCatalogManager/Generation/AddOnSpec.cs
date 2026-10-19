using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AlibreCatalogManager.Generation
{
    /// <summary>Everything needed to generate a new, empty add-on project.</summary>
    public sealed class AddOnSpec
    {
        /// <summary>Values the .adc &lt;Workspace type&gt; accepts.</summary>
        public static readonly string[] Workspaces = { "Always", "Part", "SheetMetal", "Assembly", "Drawing", "Home", "Never" };

        /// <summary>Names that would clash with the interop namespaces or the entry-point type.</summary>
        private static readonly string[] ReservedNames = { "AlibreAddOn", "AlibreX", "AlibreAddOnAssembly", "System", "Microsoft" };

        /// <summary>C# reserved keywords, which cannot be used as a namespace or project name.</summary>
        private static readonly HashSet<string> CSharpKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
            "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit",
            "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int",
            "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out",
            "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try",
            "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
        };

        public AddOnLanguage Language { get; set; } = AddOnLanguage.CSharp;

        /// <summary>Project, assembly, namespace and deploy-folder name.</summary>
        public string Name { get; set; }

        public string FriendlyName { get; set; }
        public string MenuText { get; set; }
        public string AuthorName { get; set; }
        public string AuthorLink { get; set; }
        public string Description { get; set; }
        public string Workspace { get; set; } = "Always";

        /// <summary>Catalog version, major.minor.patch.</summary>
        public string Version { get; set; } = "1.0.0";

        public int MinAlibreBuild { get; set; } = 29000;

        /// <summary>The .adc Identifier and registry value name.</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Alibre's Program folder: the AlibreX / AlibreAddOn interop assemblies, or for C++ their type libraries.</summary>
        public string AlibreProgramDir { get; set; }

        /// <summary>Folder the project folder is created in.</summary>
        public string OutputRoot { get; set; }

        public bool IncludeDeployScripts { get; set; } = true;

        /// <summary>Optional .slnx to add the new project to; null to skip.</summary>
        public string SolutionPath { get; set; }

        public Color IconColor { get; set; } = Color.FromArgb(30, 110, 180);

        public string ProjectFolder => Path.Combine(OutputRoot ?? string.Empty, Name ?? string.Empty);

        /// <summary>The generated project file: a .vcxproj for C++, else a .csproj.</summary>
        public string ProjectFile => Path.Combine(ProjectFolder, (Name ?? string.Empty) + (Language == AddOnLanguage.Cpp ? ".vcxproj" : ".csproj"));

        /// <summary>The .adc &lt;DLL type&gt;: C++ builds a native DLL, C# a managed one.</summary>
        public bool IsNative => Language == AddOnLanguage.Cpp;

        /// <summary>
        /// A Home add-on is launched from the Home window (Utilities → Add-ons…), which calls the
        /// AddOnInvoke entry point rather than the IAlibreAddOn menu; the generated AddOnInvoke then
        /// runs the add-on's command.
        /// </summary>
        public bool InvokedFromHome => Workspace == "Home";

        public string AdcId => Id.ToString("B").ToUpperInvariant();

        public IList<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Enter a project name.");
            else if (!Regex.IsMatch(Name, "^[A-Za-z_][A-Za-z0-9_]*$") || CSharpKeywords.Contains(Name))
                errors.Add("The project name must be a C# identifier: letters, digits and underscores, not starting with a digit.");
            else if (ReservedNames.Contains(Name, StringComparer.OrdinalIgnoreCase))
                errors.Add($"\"{Name}\" clashes with an Alibre or .NET namespace; choose another project name.");

            RequireSingleLine(errors, FriendlyName, "display name");
            RequireSingleLine(errors, MenuText, "menu text");
            RequireSingleLine(errors, AuthorName, "author name");

            if (!string.IsNullOrWhiteSpace(AuthorLink) &&
                (!Uri.TryCreate(AuthorLink, UriKind.Absolute, out var link) || (link.Scheme != Uri.UriSchemeHttps && link.Scheme != Uri.UriSchemeHttp)))
                errors.Add("The author link must be an http(s) URL, or left empty.");

            if (!Workspaces.Contains(Workspace))
                errors.Add("Choose a workspace.");

            if (string.IsNullOrWhiteSpace(Version) || !Regex.IsMatch(Version, @"^\d+\.\d+\.\d+$"))
                errors.Add("The version must be major.minor.patch, e.g. 1.0.0.");

            if (MinAlibreBuild <= 0)
                errors.Add("The minimum Alibre build must be a positive number.");

            if (!Enum.IsDefined(typeof(AddOnLanguage), Language))
                errors.Add("Choose a language.");

            var references = IsNative ? new[] { "AlibreX_64.tlb", "AlibreAddOn_64.tlb" } : new[] { "AlibreX.dll", "AlibreAddOn.dll" };
            if (string.IsNullOrWhiteSpace(AlibreProgramDir) || !references.All(r => File.Exists(Path.Combine(AlibreProgramDir, r))))
                errors.Add($"The Alibre Program folder must contain {references[0]} and {references[1]}.");

            if (string.IsNullOrWhiteSpace(OutputRoot) || !Directory.Exists(OutputRoot))
                errors.Add("Choose an existing output folder.");
            else if (!string.IsNullOrWhiteSpace(Name) && Directory.Exists(ProjectFolder) && Directory.EnumerateFileSystemEntries(ProjectFolder).Any())
                errors.Add($"{ProjectFolder} already exists and is not empty.");

            if (SolutionPath != null && !File.Exists(SolutionPath))
                errors.Add($"Solution file {SolutionPath} does not exist.");

            return errors;
        }

        private static void RequireSingleLine(List<string> errors, string value, string what)
        {
            if (string.IsNullOrWhiteSpace(value))
                errors.Add($"Enter the {what}.");
            else if (value.IndexOfAny(new[] { '\r', '\n' }) >= 0)
                errors.Add($"The {what} must be a single line.");
        }
    }
}
