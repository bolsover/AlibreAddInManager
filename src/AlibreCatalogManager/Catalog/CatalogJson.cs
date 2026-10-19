using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using AlibreAddOnManager;

namespace AlibreCatalogManager.Catalog
{
    /// <summary>
    /// Writes catalog JSON in the shape authors read and diff: unescaped slashes,
    /// short arrays of strings or numbers on one line, and the file's existing
    /// line endings — so saving one change does not rewrite the whole file.
    /// </summary>
    internal static class CatalogJson
    {
        // DataContractJsonSerializer writes "/" as "\/"; legal JSON, but noisy in every URL.
        // Only unescape a "\/" that is not itself the tail of an escaped backslash.
        private static readonly Regex EscapedSlash = new Regex(@"(?<!\\)((?:\\\\)*)\\/");

        private const string Scalar = @"(?:""(?:[^""\\\n]|\\.)*""|-?\d[\d.eE+-]*|true|false|null)";

        // An array whose elements are all scalars, laid out one per line.
        private static readonly Regex ScalarArray = new Regex(@"\[\n\s*(" + Scalar + @"(?:,\n\s*" + Scalar + @")*)\n\s*\]");

        private static readonly Regex ElementBreak = new Regex(@",\n\s*");

        public static string Serialize<T>(T value, string newline = "\r\n")
        {
            var text = EscapedSlash.Replace(Json.Serialize(value), "$1/").Replace("\r\n", "\n");
            text = ScalarArray.Replace(text, m => "[" + ElementBreak.Replace(m.Groups[1].Value, ", ") + "]");
            return (text.TrimEnd('\n') + "\n").Replace("\n", newline);
        }

        /// <summary>Writes only if the content differs; via a temp file so a crash never truncates.</summary>
        public static bool WriteIfChanged<T>(string path, T value)
        {
            var existing = File.Exists(path) ? File.ReadAllText(path) : null;
            var newline = existing != null && existing.Contains("\n") && !existing.Contains("\r\n") ? "\n" : "\r\n";
            var text = Serialize(value, newline);
            if (existing == text) return false;

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temp = path + ".tmp";
            File.WriteAllText(temp, text, new UTF8Encoding(false));
            if (existing != null)
                File.Replace(temp, path, null);
            else
                File.Move(temp, path);
            return true;
        }
    }
}
