using System;
using System.Collections.Generic;
using AlibreX;

namespace CustomPropertyExplorer.Model
{
    /// <summary>
    /// Reads and writes a design's custom properties through
    /// IADDesignProperties.CustomProperty. The API has no way to enumerate them,
    /// so the explorers track a list of names to show: the scripts' defaults
    /// A–G, plus any name the user looks up or sets.
    /// </summary>
    internal sealed class CustomProperties
    {
        public const string NotDefined = "(not defined)";

        private static readonly string[] DefaultNames = { "A", "B", "C", "D", "E", "F", "G" };

        private readonly IADDesignSession _design;
        private readonly List<string> _names = new List<string>(DefaultNames);

        public CustomProperties(IADDesignSession design)
        {
            _design = design;
        }

        public IReadOnlyList<string> Names => _names;

        /// <summary>Adds a name to the displayed list if it isn't there already.</summary>
        public void Track(string name)
        {
            name = name?.Trim();
            if (string.IsNullOrEmpty(name) || _names.Contains(name)) return;
            _names.Add(name);
        }

        /// <summary>
        /// The value of a custom property: "" when it is defined but empty,
        /// <see cref="NotDefined"/> when the API rejects the name.
        /// </summary>
        public string Get(string name)
        {
            try
            {
                var value = _design.DesignProperties.CustomProperty(name);
                return value == null ? string.Empty : Convert.ToString(value);
            }
            catch (Exception)
            {
                return NotDefined;
            }
        }

        /// <summary>Like <see cref="Get"/>, but lets the API's error through.</summary>
        public object Lookup(string name) => _design.DesignProperties.CustomProperty(name);

        /// <summary>
        /// Sets a custom property. Throws if Alibre refuses — the property must
        /// already be defined on the design or on this PC.
        /// </summary>
        public void Set(string name, string value)
        {
            _design.DesignProperties.CustomProperty(name, value);
        }

        public IEnumerable<KeyValuePair<string, string>> Values()
        {
            foreach (var name in _names)
                yield return new KeyValuePair<string, string>(name, Get(name));
        }
    }
}
