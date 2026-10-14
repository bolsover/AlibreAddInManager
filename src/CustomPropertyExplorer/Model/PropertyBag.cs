using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CustomPropertyExplorer.Model
{
    /// <summary>
    /// A dynamic set of string-valued properties for the PropertyGrid — the C#
    /// stand-in for the scripts' PropertyDisplayObject, which added attributes at
    /// run time. Entries with a setter are editable; the rest are read-only.
    /// </summary>
    internal sealed class PropertyBag : ICustomTypeDescriptor
    {
        private readonly List<Entry> _entries = new List<Entry>();

        public PropertyBag(string title)
        {
            Title = title;
        }

        /// <summary>What the bag describes, for the status bar.</summary>
        public string Title { get; }

        public int Count => _entries.Count;

        public void Add(string category, string name, string value, Action<string> setter = null, string description = null)
        {
            _entries.Add(new Entry(category, name, value ?? "None", setter, description));
        }

        /// <summary>
        /// Adds a property whose value is read by <paramref name="read"/>; if the
        /// read throws, the entry shows the error instead of the add-on failing.
        /// </summary>
        public void Add(string category, string name, Func<string> read)
        {
            string value;
            try { value = read(); }
            catch (Exception e) { value = "Error: " + e.Message; }
            Add(category, name, value);
        }

        private sealed class Entry
        {
            public Entry(string category, string name, string value, Action<string> setter, string description)
            {
                Category = category;
                Name = name;
                Value = value;
                Setter = setter;
                Description = description;
            }

            public string Category { get; }
            public string Name { get; }
            public string Value { get; set; }
            public Action<string> Setter { get; }
            public string Description { get; }
        }

        private sealed class EntryDescriptor : PropertyDescriptor
        {
            private readonly Entry _entry;

            public EntryDescriptor(Entry entry, int index)
                : base("p" + index, new Attribute[]
                {
                    new CategoryAttribute(entry.Category),
                    new DisplayNameAttribute(entry.Name),
                    new DescriptionAttribute(entry.Description ?? entry.Name)
                })
            {
                _entry = entry;
            }

            public string EntryName => _entry.Name;

            public override Type ComponentType => typeof(PropertyBag);
            public override bool IsReadOnly => _entry.Setter == null;
            public override Type PropertyType => typeof(string);
            public override bool CanResetValue(object component) => false;
            public override void ResetValue(object component) { }
            public override bool ShouldSerializeValue(object component) => false;
            public override object GetValue(object component) => _entry.Value;

            public override void SetValue(object component, object value)
            {
                if (_entry.Setter == null) return;
                var text = value == null ? string.Empty : Convert.ToString(value);
                // A throwing setter is reported by the PropertyGrid and the old value kept.
                _entry.Setter(text);
                _entry.Value = text;
                OnValueChanged(component, EventArgs.Empty);
            }
        }

        // --- ICustomTypeDescriptor --------------------------------------------------

        public PropertyDescriptorCollection GetProperties(Attribute[] attributes) => GetProperties();

        public PropertyDescriptorCollection GetProperties()
        {
            var descriptors = new PropertyDescriptor[_entries.Count];
            for (var i = 0; i < _entries.Count; i++)
                descriptors[i] = new EntryDescriptor(_entries[i], i);
            return new PropertyDescriptorCollection(descriptors, true);
        }

        public AttributeCollection GetAttributes() => AttributeCollection.Empty;
        public string GetClassName() => Title;
        public string GetComponentName() => Title;
        public TypeConverter GetConverter() => new TypeConverter();
        public EventDescriptor GetDefaultEvent() => null;
        public PropertyDescriptor GetDefaultProperty() => null;
        public object GetEditor(Type editorBaseType) => null;
        public EventDescriptorCollection GetEvents(Attribute[] attributes) => EventDescriptorCollection.Empty;
        public EventDescriptorCollection GetEvents() => EventDescriptorCollection.Empty;
        public object GetPropertyOwner(PropertyDescriptor pd) => this;
    }
}
