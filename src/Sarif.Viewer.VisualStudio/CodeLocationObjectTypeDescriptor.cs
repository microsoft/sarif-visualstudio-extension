// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.Sarif.Viewer
{
    /// <summary>
    /// A custom type descriptor which enables the SARIF properties to be displayed
    /// in the Properties window.
    /// </summary>
    internal class CodeLocationObjectTypeDescriptor : ICustomTypeDescriptor
    {
        private readonly CodeLocationObject _item;

        public CodeLocationObjectTypeDescriptor(CodeLocationObject item)
        {
            this._item = item;
        }

        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this._item, true);
        }

        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(this._item, true);
        }

        public string GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this._item, true);
        }

        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(this._item, true);
        }

        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this._item, true);
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return null;
        }

        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this._item, editorBaseType, true);
        }

        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(this._item, true);
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this._item, attributes, true);
        }

        public PropertyDescriptorCollection GetProperties()
        {
            return TypeDescriptor.GetProperties(this._item, true);
        }

        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            var properties = new List<PropertyDescriptor>();

            foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(this._item, true))
            {
                if (propertyDescriptor.Name.Equals("Properties") && propertyDescriptor.PropertyType == typeof(Dictionary<string, string>))
                {
                    // These are the SARIF properties.
                    // Convert the key value pairs to individual properties.
                    var propertyBag = propertyDescriptor.GetValue(this._item) as Dictionary<string, string>;

                    foreach (string key in propertyBag.Keys)
                    {
                        properties.Add(new KeyValuePairPropertyDescriptor(key, propertyBag[key]));
                    }
                }
                else
                {
                    properties.Add(propertyDescriptor);
                }
            }

            return new PropertyDescriptorCollection(properties.ToArray(), true);
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this._item;
        }
    }
}
