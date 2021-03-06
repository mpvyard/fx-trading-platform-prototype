// -----
// GNU General Public License
// The Forex Professional Analyzer is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation; either version 3 of the License, or (at your option) any later version. 
// The Forex Professional Analyzer is distributed in the hope that it will be useful, but without any warranty; without even the implied warranty of merchantability or fitness for a particular purpose.  
// See the GNU Lesser General Public License for more details.
// -----

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace fxpa
{
    /// <summary>
    /// Provides a more comprehensible, user friendly and flexible way of interfacing and object.
    /// </summary>
    public partial class CustomPropertiesControl : UserControl
    {
        protected List<Control> propertiesControls = new List<Control>();

        protected const int InterControlMargin = 5;

        bool _isReadOnly = false;
        public bool IsReadOnly
        {
            get { return _isReadOnly; }
            set { _isReadOnly = value; }
        }

        protected int _startingYLocation = 0;

        IPropertyContainer _selectedObject;
        public IPropertyContainer SelectedObject
        {
            get { return _selectedObject; }
            set
            {
                _selectedObject = value;
                UpdateUI();
            }
        }

        List<string> _filteringPropertiesNames = new List<string>();
        /// <summary>
        /// Properties with those names will not be displayed.
        /// </summary>
        public List<string> FilteringPropertiesNames
        {
            get { return _filteringPropertiesNames; }
        }

        /// <summary>
        /// 
        /// </summary>
        public CustomPropertiesControl()
        {
            InitializeComponent();

            //_filteringPropertiesNames.Add("Enabled");
        }

        private void CustomPropertiesControl_Load(object sender, EventArgs e)
        {
            _startingYLocation = InterControlMargin;
        }

        protected virtual void OnUpdateUI(int startingYValue)
        {
        }

        /// <summary>
        /// Main UI logic function.
        /// </summary>
        /// <returns>Y axis value of the dynamic last control.</returns>
        protected void UpdateUI()
        {
            int lastYValue = _startingYLocation; // checkBoxEnabled.Bottom + InterControlMargin;
            //lastYValue = Math.Max(checkBoxEnabled.Bottom + InterControlMargin, _startingYLocation);

            if (this.DesignMode)
            {
                return;
            }
            // Clear existing indicator custom parameters controls.
            foreach (Control control in propertiesControls)
            {
                control.Parent = null;
                control.Tag = null;
                control.Dispose();
            }
            
            propertiesControls.Clear();

            if (SelectedObject == null)
            {
                OnUpdateUI(lastYValue);
                return;
            }

            // Gather indicator custom parameters.
            Type type = SelectedObject.GetType();
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            Dictionary<string, PropertyInfo> actualProperties = new Dictionary<string, PropertyInfo>();
            // Filter properties.
            foreach (PropertyInfo info in properties)
            {
                if (_filteringPropertiesNames.Contains(info.Name) == false)
                {
                    if (actualProperties.ContainsKey(info.Name) == false)
                    {// Also if a parent and child define same property, with "new" only show childs.
                        actualProperties.Add(info.Name, info);
                    }
                }
            }

            // Handle default properties of the SelectedObject class.
            foreach (PropertyInfo info in actualProperties.Values)
            {
                if (info.CanRead == false)
                {// We do not process write only properties.
                    continue;
                }

                Type propertyType = info.PropertyType;
                bool isReadOnly = info.CanWrite == false || IsReadOnly;

                object value = info.GetValue(SelectedObject, null);

                Type underlyingType = Nullable.GetUnderlyingType(propertyType);
                if (underlyingType != null)
                {// Unwrap nullable properties.
                    propertyType = underlyingType;

                    if (value == null)
                    {// Nullable enums with null values not displayed.
                        continue;
                    }
                }

                AddDynamicPropertyValueControl(info.Name, propertyType, value, info, isReadOnly, ref lastYValue);
            }

            // Handle dynamic generic properties of the indicator as well.
            foreach (string name in SelectedObject.GetPropertiesNames())
            {
                AddDynamicPropertyValueControl(name, SelectedObject.GetPropertyType(name), SelectedObject.GetPropertyValue(name), name, IsReadOnly, ref lastYValue);
            }

            OnUpdateUI(lastYValue);
        }

        /// <summary>
        /// Helper to create the corresponding label.
        /// </summary>
        protected void AddDynamicPropertyLabel(string labelTitle, ref int yLocation)
        {
            Label label = new Label();
            label.Text = labelTitle;
            label.Top = yLocation;
            label.AutoSize = true;
            this.Controls.Add(label);
            yLocation = label.Bottom + InterControlMargin;
            propertiesControls.Add(label);
        }

        /// <summary>
        /// Helper to create the corresponding control.
        /// </summary>
        protected void AddDynamicPropertyValueControl(string propertyName, Type propertyType, object value, object tag, bool isReadOnly, ref int yLocation)
        {
            Control control = null;
            if (propertyType.IsEnum)
            {
                AddDynamicPropertyLabel(propertyName, ref yLocation);

                string stringValue = value.ToString();

                ComboBox propertyValuesComboBox = new ComboBox();
                control = propertyValuesComboBox;
                propertyValuesComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                propertyValuesComboBox.Enabled = !isReadOnly;
                string[] names = Enum.GetNames(propertyType);
                propertyValuesComboBox.Items.AddRange(names);

                for (int i = 0; i < propertyValuesComboBox.Items.Count; i++)
                {
                    if (propertyValuesComboBox.Items[i].ToString() == stringValue)
                    {
                        propertyValuesComboBox.SelectedIndex = i;
                        break;
                    }
                }

                //propertyValuesComboBox.Top = yLocation;
                propertyValuesComboBox.Tag = tag;

                propertyValuesComboBox.SelectedIndexChanged += new EventHandler(propertyValues_SelectedIndexChanged);
            }
            if (propertyType == typeof(string))
            {
                AddDynamicPropertyLabel(propertyName, ref yLocation);

                TextBox textBox = new TextBox();
                textBox.Text = (string)value;

                textBox.Enabled = !isReadOnly;
                textBox.Tag = tag;
                //textBox.Top = yLocation;
                control = textBox;
                textBox.TextChanged += new EventHandler(textBox_TextChanged);
            }
            if (propertyType == typeof(double)
                || propertyType == typeof(float)
                || propertyType == typeof(int)
                || propertyType == typeof(short)
                || propertyType == typeof(long))
            {
                AddDynamicPropertyLabel(propertyName, ref yLocation);

                NumericUpDown propertyValueNumeric = new NumericUpDown();
                control = propertyValueNumeric;
                propertyValueNumeric.ReadOnly = isReadOnly;
                // Enabled also needed, since readonly only blocks text, not up down buttons.
                propertyValueNumeric.Enabled = !isReadOnly;
                propertyValueNumeric.Minimum = decimal.MinValue;
                propertyValueNumeric.Maximum = decimal.MaxValue;
                propertyValueNumeric.Value = decimal.Parse(value.ToString());
                propertyValueNumeric.Tag = tag;
                //propertyValueNumeric.Top = yLocation;
                propertyValueNumeric.ValueChanged += new EventHandler(propertyValue_ValueChanged);
            }
            else if (propertyType == typeof(bool))
            {
                CheckBox propertyValue = new CheckBox();
                control = propertyValue;
                propertyValue.Checked = (bool)value;
                propertyValue.Text = propertyName;
                propertyValue.Enabled = !isReadOnly;
                //propertyValue.Top = yLocation;
                propertyValue.Tag = tag;
                propertyValue.CheckedChanged += new EventHandler(propertyValue_CheckedChanged);
            }
            else if (propertyType == typeof(Pen))
            {
                PenControl penControl = new PenControl();
                control = penControl;
                penControl.BorderStyle = BorderStyle.FixedSingle;
                penControl.Pen = (Pen)value;
                penControl.PenChangedEvent += new PenControl.PenChangedDelegate(penControl_PenChangedEvent);
                penControl.Tag = tag;
                penControl.PenName = propertyName;
                penControl.ReadOnly = IsReadOnly;
                //penControl.Top = yLocation;
            }
            else
            {
                //SystemMonitor.Error("Failed to display indicator property type [" + propertyType.Name + "].");
            }

            if (control != null)
            {
                control.Top = yLocation;

                propertiesControls.Add(control);
                this.Controls.Add(control);
                yLocation = control.Bottom + InterControlMargin;

                control.Width = this.Width - InterControlMargin;
                this.Height = control.Bottom;
            }

        }

        /// <summary>
        /// Helper.
        /// </summary>
        protected Type GetPropertyTypeByTag(object tag)
        {
            if (tag is string)
            {// Is generic dynamic property.
                return _selectedObject.GetPropertyType(tag as string);
            }

            // Is normal property.
            PropertyInfo info = (PropertyInfo)tag;
            if (Nullable.GetUnderlyingType(info.PropertyType) != null)
            {// Is nullable, establish underlying.
                return Nullable.GetUnderlyingType(info.PropertyType);
            }
            else
            {// Direct aquisition.
                return info.PropertyType;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void penControl_PenChangedEvent(PenControl control)
        {
            SetObjectPropertyValueByTag(control.Tag, control.Pen);
        }

        void textBox_TextChanged(object sender, EventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            SetObjectPropertyValueByTag(textBox.Tag, textBox.Text);
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void propertyValues_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox propertyValuesComboBox = (ComboBox)sender;
            if (propertyValuesComboBox.SelectedIndex < 0)
            {
                return;
            }

            object newValue = Enum.Parse(GetPropertyTypeByTag(propertyValuesComboBox.Tag), propertyValuesComboBox.SelectedItem.ToString());
            
            SetObjectPropertyValueByTag(propertyValuesComboBox.Tag, newValue);
        }

        void propertyValue_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown propertyValueNumeric = (NumericUpDown)sender;
            Type propertyType = GetPropertyTypeByTag(propertyValueNumeric.Tag);

            // Since numeric may display different types of values, and the type info is lost in entering,
            // now extract it back to pass properly to the indicator.

            object value = null;
            if (propertyType == typeof(int))
            {
                value = (int)propertyValueNumeric.Value;
            }
            else if (propertyType == typeof(double))
            {
                value = (double)propertyValueNumeric.Value;
            }
            else if (propertyType == typeof(Single))
            {
                value = (Single)propertyValueNumeric.Value;
            }
            else if (propertyType == typeof(long))
            {
                value = (long)propertyValueNumeric.Value;
            }
            else
            {
                //SystemMonitor.Error("Unsupported input type in numeric box.");
            }

            SetObjectPropertyValueByTag(propertyValueNumeric.Tag, value);
        }


        /// <summary>
        /// Helper.
        /// </summary>
        void SetObjectPropertyValueByTag(object tag, object value)
        {
            if (tag is string)
            {// This is a dynamic generic property.
                _selectedObject.SetPropertyValue(tag as string, value);
            }
            else if (tag is PropertyInfo)
            {// Direct property of the indicator.
                ((PropertyInfo)tag).SetValue(_selectedObject, value, null);
            }
            else
            {
                //SystemMonitor.Error("Unrecognized tag type for indicator property.");
            }

            _selectedObject.PropertyChanged();
        }

        void propertyValue_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox propertyValueCheckbox = (CheckBox)sender;
            SetObjectPropertyValueByTag(propertyValueCheckbox.Tag, propertyValueCheckbox.Checked);
        }

        private void checkBoxEnabled_CheckedChanged(object sender, EventArgs e)
        {
            //_selectedObject.Enabled = checkBoxEnabled.Checked;
        }

        private void CustomPropertiesControl_SizeChanged(object sender, EventArgs e)
        {
            foreach (Control control in this.Controls)
            {
                control.Width = this.Width - InterControlMargin;
            }
        }


    }
}
