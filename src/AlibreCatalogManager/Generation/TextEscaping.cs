using System;
using System.Security;
using System.Text;

namespace AlibreCatalogManager.Generation
{
    /// <summary>
    /// Escapes a token value for the file it is substituted into, so user input
    /// such as a quote or ampersand in a description cannot break the syntax.
    /// </summary>
    internal static class TextEscaping
    {
        public static string Xml(string value) => SecurityElement.Escape(value ?? string.Empty);

        public static string Json(string value) => Quoted(value, false);

        public static string CSharp(string value) => Quoted(value, true);

        /// <summary>
        /// Body of a C++ (wide) string literal, also safe in a // comment line.
        /// Control characters become three-digit octal escapes, which, unlike \x,
        /// cannot swallow a following character.
        /// </summary>
        public static string Cpp(string value)
        {
            var sb = new StringBuilder();
            foreach (var c in value ?? string.Empty)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20 || c == 0x7F)
                            sb.Append('\\').Append(Convert.ToString(c, 8).PadLeft(3, '0'));
                        else
                            sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// For a PowerShell single-quoted string. PowerShell also treats the
        /// typographic single quotes as quote characters, so double those too.
        /// </summary>
        public static string PowerShellSingleQuoted(string value)
        {
            var sb = new StringBuilder();
            foreach (var c in value ?? string.Empty)
            {
                if (c == '\'' || (c >= 0x2018 && c <= 0x201B)) sb.Append(c);
                sb.Append(c);
            }

            return sb.ToString();
        }

        /// <summary>Body of a C# or JSON double-quoted string literal.</summary>
        private static string Quoted(string value, bool csharp)
        {
            var sb = new StringBuilder();
            foreach (var c in value ?? string.Empty)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20 || (csharp && (c == 0x85 || c == 0x2028 || c == 0x2029)))
                            sb.Append("\\u").Append(((int) c).ToString("x4"));
                        else
                            sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        public static Func<string, string> ForFile(string relativePath)
        {
            var path = relativePath.ToLowerInvariant();
            if (path.EndsWith(".cs")) return CSharp;
            if (path.EndsWith(".cpp") || path.EndsWith(".h")) return Cpp;
            if (path.EndsWith(".json")) return Json;
            if (path.EndsWith(".ps1")) return PowerShellSingleQuoted;
            if (path.EndsWith(".adc") || path.EndsWith(".csproj") || path.EndsWith(".vcxproj")) return Xml;
            throw new ArgumentException("No escaping rule for " + relativePath);
        }
    }
}
