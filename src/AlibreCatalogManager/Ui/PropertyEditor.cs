using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AlibreCatalogManager.Ui
{
    /// <summary>Marks a string property that <see cref="PropertyEditor"/> edits in a multi-line box.</summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class MultilineAttribute : Attribute
    {
    }

    /// <summary>
    /// A small PropertyGrid for WPF. Shows the public properties of <see cref="SelectedObject"/>
    /// grouped by [Category], in declaration order, with the focused property's [Description]
    /// underneath. Strings, ints and nullable ints are edited in text boxes; properties without
    /// a public setter are read-only. A value is committed when its box loses focus or on Enter
    /// (Ctrl+Enter in a multi-line box); Esc restores the current value.
    /// </summary>
    public sealed class PropertyEditor : UserControl
    {
        private readonly Grid _rows = new Grid { Margin = new Thickness(4) };
        private readonly TextBlock _descriptionTitle = new TextBlock { FontWeight = FontWeights.Bold };
        private readonly TextBlock _description = new TextBlock { TextWrapping = TextWrapping.Wrap };
        private object _selectedObject;

        public PropertyEditor()
        {
            _rows.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 110 });
            _rows.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var descriptionPanel = new StackPanel { Margin = new Thickness(6, 4, 6, 6) };
            descriptionPanel.Children.Add(_descriptionTitle);
            descriptionPanel.Children.Add(_description);
            var descriptionBorder = new Border
            {
                BorderThickness = new Thickness(0, 1, 0, 0),
                BorderBrush = SystemColors.ActiveBorderBrush,
                MinHeight = 52,
                Child = descriptionPanel
            };
            DockPanel.SetDock(descriptionBorder, Dock.Bottom);

            var dock = new DockPanel();
            dock.Children.Add(descriptionBorder);
            dock.Children.Add(new ScrollViewer { Content = _rows, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
            Content = dock;
        }

        /// <summary>The object being edited, or null for an empty editor.</summary>
        public object SelectedObject
        {
            get => _selectedObject;
            set
            {
                _selectedObject = value;
                Rebuild();
            }
        }

        private void Rebuild()
        {
            _rows.Children.Clear();
            _rows.RowDefinitions.Clear();
            ShowDescription(null);
            if (_selectedObject == null) return;

            var properties = _selectedObject.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetIndexParameters().Length == 0 && IsSupported(p.PropertyType))
                .OrderBy(p => p.MetadataToken)
                .GroupBy(p => p.GetCustomAttribute<CategoryAttribute>()?.Category ?? "Misc");

            foreach (var category in properties)
            {
                var header = new TextBlock
                {
                    Text = category.Key,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, _rows.RowDefinitions.Count == 0 ? 0 : 10, 0, 2)
                };
                AddRow(header, null);
                foreach (var property in category) AddProperty(property);
            }
        }

        private static bool IsSupported(Type type) =>
            type == typeof(string) || type == typeof(int) || type == typeof(int?) || type == typeof(long) || type == typeof(long?);

        private void AddProperty(PropertyInfo property)
        {
            var readOnly = property.SetMethod == null || !property.SetMethod.IsPublic;
            var multiline = property.GetCustomAttribute<MultilineAttribute>() != null;
            var description = property.GetCustomAttribute<DescriptionAttribute>()?.Description;

            var box = new TextBox
            {
                IsReadOnly = readOnly,
                Text = Format(property.GetValue(_selectedObject)),
                AcceptsReturn = multiline,
                TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
                VerticalScrollBarVisibility = multiline ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
                Height = multiline ? 70 : double.NaN,
                VerticalContentAlignment = multiline ? VerticalAlignment.Top : VerticalAlignment.Center,
                Margin = new Thickness(0, 1, 0, 1)
            };
            if (readOnly) box.Foreground = SystemColors.GrayTextBrush;

            var label = new TextBlock
            {
                Text = property.Name,
                VerticalAlignment = multiline ? VerticalAlignment.Top : VerticalAlignment.Center,
                Margin = new Thickness(8, multiline ? 4 : 0, 8, 0)
            };

            box.GotKeyboardFocus += (s, e) => ShowDescription(property.Name, description);
            if (!readOnly)
            {
                box.LostKeyboardFocus += (s, e) => Commit(property, box);
                box.PreviewKeyDown += (s, e) =>
                {
                    if (e.Key == Key.Enter && (!multiline || Keyboard.Modifiers.HasFlag(ModifierKeys.Control)))
                    {
                        Commit(property, box);
                        e.Handled = true;
                    }
                    else if (e.Key == Key.Escape)
                    {
                        box.Text = Format(property.GetValue(_selectedObject));
                        e.Handled = true;
                    }
                };
            }

            AddRow(label, box);
        }

        private void AddRow(UIElement first, UIElement second)
        {
            var row = _rows.RowDefinitions.Count;
            _rows.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(first, row);
            if (second == null)
                Grid.SetColumnSpan(first, 2);
            else
            {
                Grid.SetRow(second, row);
                Grid.SetColumn(second, 1);
                _rows.Children.Add(second);
            }

            _rows.Children.Add(first);
        }

        /// <summary>Writes the box's text back to the property if it changed; reverts and explains a bad number.</summary>
        private void Commit(PropertyInfo property, TextBox box)
        {
            if (_selectedObject == null) return;
            var current = Format(property.GetValue(_selectedObject));
            if (box.Text == current) return;

            if (!TryParse(property.PropertyType, box.Text, out var value))
            {
                Dialogs.Show(this, $"'{box.Text}' is not a valid value for {property.Name}.", "Invalid value",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                box.Text = current;
                return;
            }

            try
            {
                property.SetValue(_selectedObject, value);
            }
            catch (TargetInvocationException e) when (e.InnerException != null)
            {
                Dialogs.Error(this, "Cannot set " + property.Name, e.InnerException);
            }

            // The setter may normalise the value (trim, lower-case, empty → null).
            box.Text = Format(property.GetValue(_selectedObject));
        }

        private static string Format(object value) => Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;

        private static bool TryParse(Type type, string text, out object value)
        {
            value = null;
            var underlying = Nullable.GetUnderlyingType(type);
            if (type == typeof(string))
            {
                value = text;
                return true;
            }

            if (underlying != null && string.IsNullOrWhiteSpace(text)) return true;

            var target = underlying ?? type;
            if (target == typeof(int) && int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out var i))
            {
                value = i;
                return true;
            }

            if (target == typeof(long) && long.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out var l))
            {
                value = l;
                return true;
            }

            return false;
        }

        private void ShowDescription(string title, string text = null)
        {
            _descriptionTitle.Text = title ?? string.Empty;
            _description.Text = text ?? string.Empty;
        }
    }
}
