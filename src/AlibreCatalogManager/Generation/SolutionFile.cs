using System;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace AlibreCatalogManager.Generation
{
    /// <summary>Minimal .slnx (XML solution) support: find one, and add a project to it.</summary>
    public static class SolutionFile
    {
        /// <summary>The nearest .slnx in <paramref name="folder"/> or any parent, or null.</summary>
        public static string FindNearest(string folder)
        {
            for (var dir = string.IsNullOrEmpty(folder) ? null : new DirectoryInfo(folder); dir != null; dir = dir.Parent)
            {
                if (!dir.Exists) continue;
                var found = dir.GetFiles("*.slnx");
                if (found.Length > 0) return found[0].FullName;
            }

            return null;
        }

        /// <summary>
        /// Adds a &lt;Project&gt; element as the last child of &lt;Solution&gt;. The text is
        /// spliced in rather than re-serialised so the file's existing layout is kept.
        /// </summary>
        public static void AddProject(string solutionPath, string projectFile, string projectGuid)
        {
            var text = File.ReadAllText(solutionPath);
            var relative = RelativePath(Path.GetDirectoryName(Path.GetFullPath(solutionPath)), projectFile);

            foreach (var project in XDocument.Parse(text).Root?.Descendants("Project") ?? new XElement[0])
                if (string.Equals((string) project.Attribute("Path"), relative, StringComparison.OrdinalIgnoreCase))
                    return;

            var close = text.LastIndexOf("</Solution>", StringComparison.Ordinal);
            if (close < 0) throw new InvalidDataException(solutionPath + " has no </Solution> element.");

            var element = new XElement("Project",
                new XAttribute("Path", relative),
                new XAttribute("Id", Guid.Parse(projectGuid).ToString("D")));

            // A C++ project has no Any CPU platform; build its x64 one in the solution's Any CPU.
            if (projectFile.EndsWith(".vcxproj", StringComparison.OrdinalIgnoreCase))
                element.Add(new XElement("Platform", new XAttribute("Solution", "*|Any CPU"), new XAttribute("Project", "x64")));

            var markup = element.ToString().Replace("\r\n", "\n").Replace("\n", Environment.NewLine + "  ");
            var updated = text.Substring(0, close) + "  " + markup + Environment.NewLine + text.Substring(close);
            File.WriteAllText(solutionPath, updated, new UTF8Encoding(false));
        }

        /// <summary>Forward-slash path relative to <paramref name="baseDir"/>, as .slnx files use.</summary>
        private static string RelativePath(string baseDir, string path)
        {
            var baseUri = new Uri(baseDir.TrimEnd('\\') + "\\");
            var relative = Uri.UnescapeDataString(baseUri.MakeRelativeUri(new Uri(Path.GetFullPath(path))).ToString());
            return relative.Replace('\\', '/');
        }
    }
}
