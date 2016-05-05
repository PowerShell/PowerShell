//---------------------------------------------------------------------
// <copyright file="customactiondata.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// Contains a collection of key-value pairs suitable for passing between
    /// immediate and deferred/rollback/commit custom actions.
    /// </summary>
    /// <remarks>
    /// Call the <see cref="CustomActionData.ToString" /> method to get a string
    /// suitable for storing in a property and reconstructing the custom action data later.
    /// </remarks>
    /// <seealso cref="Session.CustomActionData"/>
    /// <seealso cref="Session.DoAction(string,CustomActionData)"/>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    internal sealed class CustomActionData : IDictionary<string, string>
    {
        /// <summary>
        /// "CustomActionData" literal property name.
        /// </summary>
        public const string PropertyName = "CustomActionData";

        private const char DataSeparator = ';';
        private const char KeyValueSeparator = '=';

        private IDictionary<string, string> data;

        /// <summary>
        /// Creates a new empty custom action data object.
        /// </summary>
        public CustomActionData() : this(null)
        {
        }

        /// <summary>
        /// Reconstructs a custom action data object from data that was previously
        /// persisted in a string.
        /// </summary>
        /// <param name="keyValueList">Previous output from <see cref="CustomActionData.ToString" />.</param>
        public CustomActionData(string keyValueList)
        {
            this.data = new Dictionary<string, string>();

            if (keyValueList != null)
            {
                this.Parse(keyValueList);
            }
        }

        /// <summary>
        /// Adds a key and value to the data collection.
        /// </summary>
        /// <param name="key">Case-sensitive data key.</param>
        /// <param name="value">Data value (may be null).</param>
        /// <exception cref="ArgumentException">the key does not consist solely of letters,
        /// numbers, and the period, underscore, and space characters.</exception>
        public void Add(string key, string value)
        {
            CustomActionData.ValidateKey(key);
            this.data.Add(key, value);
        }

        /// <summary>
        /// Adds a value to the data collection, using XML serialization to persist the object as a string.
        /// </summary>
        /// <param name="key">Case-sensitive data key.</param>
        /// <param name="value">Data value (may be null).</param>
        /// <exception cref="ArgumentException">the key does not consist solely of letters,
        /// numbers, and the period, underscore, and space characters.</exception>
        /// <exception cref="NotSupportedException">The value type does not support XML serialization.</exception>
        /// <exception cref="InvalidOperationException">The value could not be serialized.</exception>
        public void AddObject<T>(string key, T value)
        {
            if (value == null)
            {
                this.Add(key, null);
            }
            else if (typeof(T) == typeof(string) ||
                     typeof(T) == typeof(CustomActionData)) // Serialize nested CustomActionData
            {
                this.Add(key, value.ToString());
            }
            else
            {
                string valueString = CustomActionData.Serialize<T>(value);
                this.Add(key, valueString);
            }
        }

        /// <summary>
        /// Gets a value from the data collection, using XML serialization to load the object from a string.
        /// </summary>
        /// <param name="key">Case-sensitive data key.</param>
        /// <exception cref="InvalidOperationException">The value could not be deserialized.</exception>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public T GetObject<T>(string key)
        {
            string value = this[key];
            if (value == null)
            {
                return default(T);
            }
            else if (typeof(T) == typeof(string))
            {
                // Funny casting because the compiler doesn't know T is string here.
                return (T) (object) value;
            }
            else if (typeof(T) == typeof(CustomActionData))
            {
                // Deserialize nested CustomActionData.
                return (T) (object) new CustomActionData(value);
            }
            else if (value.Length == 0)
            {
                return default(T);
            }
            else
            {
                return CustomActionData.Deserialize<T>(value);
            }
        }

        /// <summary>
        /// Determines whether the data contains an item with the specified key.
        /// </summary>
        /// <param name="key">Case-sensitive data key.</param>
        /// <returns>true if the data contains an item with the key; otherwise, false</returns>
        public bool ContainsKey(string key)
        {
            return this.data.ContainsKey(key);
        }

        /// <summary>
        /// Gets a collection object containing all the keys of the data.
        /// </summary>
        public ICollection<string> Keys
        {
            get
            {
                return this.data.Keys;
            }
        }

        /// <summary>
        /// Removes the item with the specified key from the data.
        /// </summary>
        /// <param name="key">Case-sensitive data key.</param>
        /// <returns>true if the item was successfully removed from the data;
        /// false if an item with the specified key was not found</returns>
        public bool Remove(string key)
        {
            return this.data.Remove(key);
        }

        /// <summary>
        /// Gets the value with the specified key.
        /// </summary>
        /// <param name="key">Case-sensitive data key.</param>
        /// <param name="value">Value associated with the specified key, or
        /// null if an item with the specified key was not found</param>
        /// <returns>true if the data contains an item with the specified key; otherwise, false.</returns>
        public bool TryGetValue(string key, out string value)
        {
            return this.data.TryGetValue(key, out value);
        }

        /// <summary>
        /// Gets a collection containing all the values of the data.
        /// </summary>
        public ICollection<string> Values
        {
            get
            {
                return this.data.Values;
            }
        }

        /// <summary>
        /// Gets or sets a data value with a specified key.
        /// </summary>
        /// <param name="key">Case-sensitive data key.</param>
        /// <exception cref="ArgumentException">the key does not consist solely of letters,
        /// numbers, and the period, underscore, and space characters.</exception>
        public string this[string key]
        {
            get
            {
                return this.data[key];
            }
            set
            {
                CustomActionData.ValidateKey(key);
                this.data[key] = value;
            }
        }

        /// <summary>
        /// Adds an item with key and value to the data collection.
        /// </summary>
        /// <param name="item">Case-sensitive data key, with a data value that may be null.</param>
        /// <exception cref="ArgumentException">the key does not consist solely of letters,
        /// numbers, and the period, underscore, and space characters.</exception>
        public void Add(KeyValuePair<string, string> item)
        {
            CustomActionData.ValidateKey(item.Key);
            this.data.Add(item);
        }

        /// <summary>
        /// Removes all items from the data.
        /// </summary>
        public void Clear()
        {
            if (this.data.Count > 0)
            {
                this.data.Clear();
            }
        }

        /// <summary>
        /// Determines whether the data contains a specified item.
        /// </summary>
        /// <param name="item">The data item to locate.</param>
        /// <returns>true if the data contains the item; otherwise, false</returns>
        public bool Contains(KeyValuePair<string, string> item)
        {
            return this.data.Contains(item);
        }

        /// <summary>
        /// Copies the data to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">Destination array.</param>
        /// <param name="arrayIndex">Index in the array at which copying begins.</param>
        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            this.data.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Gets the number of items in the data.
        /// </summary>
        public int Count
        {
            get
            {
                return this.data.Count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the data is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Removes an item from the data.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns>true if the item was successfully removed from the data;
        /// false if the item was not found</returns>
        public bool Remove(KeyValuePair<string, string> item)
        {
            return this.data.Remove(item);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return this.data.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable) this.data).GetEnumerator();
        }

        /// <summary>
        /// Gets a string representation of the data suitable for persisting in a property.
        /// </summary>
        /// <returns>Data string in the form "Key1=Value1;Key2=Value2"</returns>
        public override string ToString()
        {
            StringBuilder buf = new StringBuilder();

            foreach (KeyValuePair<string, string> item in this.data)
            {
                if (buf.Length > 0)
                {
                    buf.Append(CustomActionData.DataSeparator);
                }

                buf.Append(item.Key);

                if (item.Value != null)
                {
                    buf.Append(CustomActionData.KeyValueSeparator);
                    buf.Append(CustomActionData.Escape(item.Value));
                }
            }

            return buf.ToString();
        }

        /// <summary>
        /// Ensures that a key contains valid characters.
        /// </summary>
        /// <param name="key">key to be validated</param>
        /// <exception cref="ArgumentException">the key does not consist solely of letters,
        /// numbers, and the period, underscore, and space characters.</exception>
        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];
                if (!Char.IsLetterOrDigit(c) && c != '_' && c != '.' &&
                    !(i > 0 && i < key.Length - 1 && c == ' '))
                {
                    throw new ArgumentOutOfRangeException("key");
                }
            }
        }

        /// <summary>
        /// Serializes a value into an XML string.
        /// </summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="value">Value to be serialized.</param>
        /// <returns>Serialized value data as a string.</returns>
        private static string Serialize<T>(T value)
        {
            XmlWriterSettings xws = new XmlWriterSettings();
            xws.OmitXmlDeclaration = true;

            StringWriter sw = new StringWriter(CultureInfo.InvariantCulture);
            using (XmlWriter xw = XmlWriter.Create(sw, xws))
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add(string.Empty, String.Empty); // Prevent output of any namespaces

                XmlSerializer ser = new XmlSerializer(typeof(T));
                ser.Serialize(xw, value, ns);

                return sw.ToString();
            }
        }

        /// <summary>
        /// Deserializes a value from an XML string.
        /// </summary>
        /// <typeparam name="T">Expected type of the value.</typeparam>
        /// <param name="value">Serialized value data.</param>
        /// <returns>Deserialized value object.</returns>
        private static T Deserialize<T>(string value)
        {
            StringReader sr = new StringReader(value);
            using (XmlReader xr = XmlReader.Create(sr))
            {
                XmlSerializer ser = new XmlSerializer(typeof(T));
                return (T) ser.Deserialize(xr);
            }
        }

        /// <summary>
        /// Escapes a value string by doubling any data-separator (semicolon) characters.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Escaped value string</returns>
        private static string Escape(string value)
        {
            value = value.Replace(String.Empty + CustomActionData.DataSeparator, String.Empty + CustomActionData.DataSeparator + CustomActionData.DataSeparator);
            return value;
        }

        /// <summary>
        /// Unescapes a value string by undoubling any doubled data-separator (semicolon) characters.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Unescaped value string</returns>
        private static string Unescape(string value)
        {
            value = value.Replace(String.Empty + CustomActionData.DataSeparator + CustomActionData.DataSeparator, String.Empty + CustomActionData.DataSeparator);
            return value;
        }

        /// <summary>
        /// Loads key-value pairs from a string into the data collection.
        /// </summary>
        /// <param name="keyValueList">key-value pair list of the form returned by <see cref="ToString"/></param>
        private void Parse(string keyValueList)
        {
            int itemStart = 0;
            while (itemStart < keyValueList.Length)
            {
                // Find the next non-escaped data separator.
                int semi = itemStart - 2;
                do
                {
                    semi = keyValueList.IndexOf(CustomActionData.DataSeparator, semi + 2);
                }
                while (semi >= 0 && semi < keyValueList.Length - 1 && keyValueList[semi + 1] == CustomActionData.DataSeparator);

                if (semi < 0)
                {
                    semi = keyValueList.Length;
                }

                // Find the next non-escaped key-value separator.
                int equals = itemStart - 2;
                do
                {
                    equals = keyValueList.IndexOf(CustomActionData.KeyValueSeparator, equals + 2);
                }
                while (equals >= 0 && equals < keyValueList.Length - 1 && keyValueList[equals + 1] == CustomActionData.KeyValueSeparator);

                if (equals < 0 || equals > semi)
                {
                    equals = semi;
                }

                string key = keyValueList.Substring(itemStart, equals - itemStart);
                string value = null;

                // If there's a key-value separator before the next data separator, then the item has a value.
                if (equals < semi)
                {
                    value = keyValueList.Substring(equals + 1, semi - (equals + 1));
                    value = CustomActionData.Unescape(value);
                }

                // Add non-duplicate items to the collection.
                if (key.Length > 0 && !this.data.ContainsKey(key))
                {
                    this.data.Add(key, value);
                }

                // Move past the data separator to the next item.
                itemStart = semi + 1;
            }
        }
    }
}
