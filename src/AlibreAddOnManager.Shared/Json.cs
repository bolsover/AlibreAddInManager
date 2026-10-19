using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace AlibreAddOnManager
{
    /// <summary>
    /// Minimal JSON helpers built on DataContractJsonSerializer.
    /// Deliberately avoids Newtonsoft/System.Text.Json: this code is loaded into
    /// the Alibre process alongside other add-ons, and a private copy of a common
    /// JSON library is a classic source of assembly-binding conflicts.
    /// </summary>
    public static class Json
    {
        public static T Parse<T>(Stream stream)
        {
            var serializer = new DataContractJsonSerializer(typeof(T), Settings());
            return (T) serializer.ReadObject(stream);
        }

        public static T Parse<T>(byte[] data)
        {
            using (var stream = new MemoryStream(data))
                return Parse<T>(stream);
        }

        public static T ReadFile<T>(string path)
        {
            using (var stream = File.OpenRead(path))
                return Parse<T>(stream);
        }

        public static void WriteFile<T>(string path, T value)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            // Write to a temp file then replace, so a crash never leaves a truncated file.
            var temp = path + ".tmp";
            File.WriteAllText(temp, Serialize(value), new UTF8Encoding(false));
            if (File.Exists(path))
                File.Replace(temp, path, null);
            else
                File.Move(temp, path);
        }

        public static string Serialize<T>(T value)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, false, true, "  "))
                {
                    var serializer = new DataContractJsonSerializer(typeof(T), Settings());
                    serializer.WriteObject(writer, value);
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private static DataContractJsonSerializerSettings Settings()
        {
            return new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true };
        }
    }
}
