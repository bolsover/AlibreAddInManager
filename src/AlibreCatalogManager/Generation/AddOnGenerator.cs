using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AlibreCatalogManager.Generation
{
    /// <summary>
    /// Writes a new, empty Alibre add-on project in C# or C++: the minimum that
    /// builds, loads into Alibre and can be packaged for the catalog — entry points,
    /// an IAlibreAddOn with one stateless command, a version gate, the .adc, an icon
    /// and addon.json, plus optional install / uninstall scripts.
    /// </summary>
    public static class AddOnGenerator
    {
        private static readonly Regex Token = new Regex("%%([A-Za-z]+)%%");

        /// <summary>Tokens whose values are already in target syntax and must not be escaped.</summary>
        private static readonly HashSet<string> RawTokens = new HashSet<string> { "AdcDllTypeAttribute", "DeployItems", "RequiredFiles" };

        /// <summary>A marker alone on its line; the line break goes with it, so it leaves no blank line.</summary>
        private static readonly Regex SectionMarkerLine = new Regex(@"^[ \t]*(%%[#^/][A-Za-z]+%%)[ \t]*\r?\n", RegexOptions.Multiline);

        /// <summary>
        /// %%#Flag%%…%%/Flag%% is kept when the flag is set, %%^Flag%%…%%/Flag%% when it is not.
        /// Sections do not nest.
        /// </summary>
        private static readonly Regex Section = new Regex(@"%%(?<kind>[#^])(?<flag>[A-Za-z]+)%%(?<body>.*?)%%/\k<flag>%%", RegexOptions.Singleline);

        private sealed class TemplateFile
        {
            public TemplateFile(string resource, string outputPath, bool deployOnly = false)
            {
                Resource = resource;
                OutputPath = outputPath;
                DeployOnly = deployOnly;
            }

            /// <summary>Embedded resource name: the template's folder and file name, e.g. "Common.addon.json.template".</summary>
            public string Resource { get; }

            /// <summary>Relative output path; may itself contain tokens.</summary>
            public string OutputPath { get; }

            public bool DeployOnly { get; }
        }

        /// <summary>The .adc, manifest and deploy scripts, for every language.</summary>
        private static readonly TemplateFile[] CommonTemplates =
        {
            new TemplateFile("Common.AddOn.adc.template", "%%AddOnName%%.adc"),
            new TemplateFile("Common.addon.json.template", "addon.json"),
            new TemplateFile("Common.install.ps1.template", @"deploy\install.ps1", true),
            new TemplateFile("Common.uninstall.ps1.template", @"deploy\uninstall.ps1", true)
        };

        private static readonly TemplateFile[] CSharpTemplates =
        {
            new TemplateFile("ManagedAddOn.AddOn.csproj.template", "%%AddOnName%%.csproj"),
            new TemplateFile("ManagedAddOn.AlibreAddOn.cs.template", "AlibreAddOn.cs"),
            new TemplateFile("ManagedAddOn.AddOnClass.cs.template", "%%AddOnName%%AddOn.cs"),
            new TemplateFile("ManagedAddOn.AlibreVersion.cs.template", "AlibreVersion.cs"),
            new TemplateFile("ManagedAddOn.AssemblyInfo.cs.template", @"Properties\AssemblyInfo.cs")
        };

        private static readonly TemplateFile[] CppTemplates =
        {
            new TemplateFile("NativeAddOn.AddOn.vcxproj.template", "%%AddOnName%%.vcxproj"),
            new TemplateFile("NativeAddOn.pch.h.template", "pch.h"),
            new TemplateFile("NativeAddOn.pch.cpp.template", "pch.cpp"),
            new TemplateFile("NativeAddOn.AlibreAddOn.cpp.template", "AlibreAddOn.cpp"),
            new TemplateFile("NativeAddOn.AddOnClass.h.template", "%%AddOnName%%AddOn.h"),
            new TemplateFile("NativeAddOn.AddOnClass.cpp.template", "%%AddOnName%%AddOn.cpp"),
            new TemplateFile("NativeAddOn.AlibreVersion.h.template", "AlibreVersion.h"),
            new TemplateFile("NativeAddOn.AlibreVersion.cpp.template", "AlibreVersion.cpp")
        };

        private static IEnumerable<TemplateFile> TemplatesFor(AddOnLanguage language)
        {
            switch (language)
            {
                case AddOnLanguage.CSharp: return CSharpTemplates.Concat(CommonTemplates);
                case AddOnLanguage.Cpp: return CppTemplates.Concat(CommonTemplates);
                default: throw new ArgumentOutOfRangeException(nameof(language), language, null);
            }
        }

        public static GenerationResult Generate(AddOnSpec spec)
        {
            var errors = spec.Validate();
            if (errors.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors));

            var tokens = BuildTokens(spec);
            var flags = new Dictionary<string, bool>
            {
                // AddOnInvoke runs the command, for an add-on launched from the Home window.
                ["HomeWorkspace"] = spec.InvokedFromHome
            };
            var folder = Path.GetFullPath(spec.ProjectFolder);
            var createdFolder = !Directory.Exists(folder);
            var result = new GenerationResult { ProjectFolder = folder };

            try
            {
                Directory.CreateDirectory(folder);

                foreach (var template in TemplatesFor(spec.Language).Where(t => spec.IncludeDeployScripts || !t.DeployOnly))
                {
                    var relative = Render(template.OutputPath, tokens, v => v);
                    var text = Render(ApplySections(ReadTemplate(template.Resource), flags), tokens, TextEscaping.ForFile(relative));
                    WriteText(Path.Combine(folder, relative), text, relative.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase));
                    result.Files.Add(relative);
                }

                var icon = spec.Name + ".ico";
                IconWriter.Write(Path.Combine(folder, icon), Initials(spec.FriendlyName), spec.IconColor);
                result.Files.Add(icon);

                result.ProjectFile = Path.GetFullPath(spec.ProjectFile);
            }
            catch
            {
                // Leave nothing half-written behind; a pre-existing (empty) folder is kept.
                if (createdFolder && Directory.Exists(folder))
                    try { Directory.Delete(folder, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                throw;
            }

            // Last, so a failure here leaves a complete project that can be added by hand.
            if (spec.SolutionPath != null)
            {
                SolutionFile.AddProject(spec.SolutionPath, result.ProjectFile, tokens["ProjectGuid"]);
                result.SolutionUpdated = spec.SolutionPath;
            }

            return result;
        }

        private static Dictionary<string, string> BuildTokens(AddOnSpec spec)
        {
            var projectGuid = Guid.NewGuid().ToString("D").ToUpperInvariant();
            var menuIdRoot = new Random().Next(1000, 9000) * 10;
            var description = string.IsNullOrWhiteSpace(spec.Description) ? spec.FriendlyName : spec.Description.Trim();

            var deployItems = spec.IncludeDeployScripts
                ? "        <None Include=\"deploy\\install.ps1\"/>\r\n        <None Include=\"deploy\\uninstall.ps1\"/>\r\n"
                : string.Empty;

            // install.ps1 refuses to run unless these are in the build output.
            var required = new List<string> { spec.Name + ".dll", spec.Name + ".adc", spec.Name + ".ico", "addon.json" };
            var requiredFiles = string.Join(",\r\n", required.Select(f => "    '" + TextEscaping.PowerShellSingleQuoted(f) + "'"));

            return new Dictionary<string, string>
            {
                ["AddOnName"] = spec.Name,
                ["FriendlyName"] = spec.FriendlyName.Trim(),
                ["MenuText"] = spec.MenuText.Trim(),
                ["AuthorName"] = spec.AuthorName.Trim(),
                ["AuthorLink"] = (spec.AuthorLink ?? string.Empty).Trim(),
                ["Description"] = description,
                ["Workspace"] = spec.Workspace,
                ["Version"] = spec.Version,
                ["AssemblyVersion"] = spec.Version + ".0",
                ["MinAlibreBuild"] = spec.MinAlibreBuild.ToString(),
                ["AddOnGuid"] = spec.AdcId,
                ["ProjectGuid"] = projectGuid,
                ["AlibreProgramDir"] = spec.AlibreProgramDir.TrimEnd('\\'),
                ["Copyright"] = $"Copyright (c) {DateTime.Now.Year}. {spec.AuthorName.Trim()}",
                ["MenuIdRoot"] = menuIdRoot.ToString(),
                ["MenuIdCommand"] = (menuIdRoot + 1).ToString(),
                // Alibre (and the add-on manager) take a <DLL> with no type attribute as native.
                ["AdcDllTypeAttribute"] = spec.IsNative ? string.Empty : " type=\"Managed\"",
                ["ManifestDllType"] = spec.IsNative ? "native" : "managed",
                ["DeployItems"] = deployItems,
                ["RequiredFiles"] = requiredFiles
            };
        }

        /// <summary>Keeps or drops each conditional section according to its flag.</summary>
        private static string ApplySections(string template, Dictionary<string, bool> flags)
        {
            var text = SectionMarkerLine.Replace(template, "$1");
            return Section.Replace(text, m =>
            {
                var flag = m.Groups["flag"].Value;
                if (!flags.TryGetValue(flag, out var set))
                    throw new InvalidOperationException("Unknown template section " + flag);
                return set == (m.Groups["kind"].Value == "#") ? m.Groups["body"].Value : string.Empty;
            });
        }

        private static string Render(string template, Dictionary<string, string> tokens, Func<string, string> escape)
        {
            return Token.Replace(template, m =>
            {
                var name = m.Groups[1].Value;
                if (!tokens.TryGetValue(name, out var value))
                    throw new InvalidOperationException("Unknown template token " + m.Value);
                return RawTokens.Contains(name) ? value : escape(value);
            });
        }

        private static string ReadTemplate(string name)
        {
            var assembly = typeof(AddOnGenerator).Assembly;
            using (var stream = assembly.GetManifestResourceStream("Templates." + name))
            {
                if (stream == null) throw new InvalidOperationException("Missing embedded template " + name);
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                    return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Windows PowerShell 5.1 reads BOM-less files as ANSI, so scripts get a BOM;
        /// everything else is plain UTF-8, which every JSON reader accepts.
        /// </summary>
        private static void WriteText(string path, string text, bool withBom)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            text = Regex.Replace(text, "\r?\n", "\r\n");
            File.WriteAllText(path, text, new UTF8Encoding(withBom));
        }

        /// <summary>Up to two letters for the icon: the first letters of the first two words.</summary>
        private static string Initials(string friendlyName)
        {
            var words = Regex.Matches(Regex.Replace(friendlyName ?? string.Empty, "['’]", ""), @"[\p{L}\p{N}]+").Cast<Match>().Select(m => m.Value).ToList();
            if (words.Count == 0) return "A";
            if (words.Count == 1) return words[0].Substring(0, Math.Min(2, words[0].Length)).ToUpperInvariant();
            return (words[0].Substring(0, 1) + words[1].Substring(0, 1)).ToUpperInvariant();
        }
    }
}
