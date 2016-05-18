//---------------------------------------------------------------------
// <copyright file="ColumnInfo.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Defines a single column of a table in an installer database.
    /// </summary>
    /// <remarks>Once created, a ColumnInfo object is immutable.</remarks>
    internal class ColumnInfo
    {
        private string name;
        private Type type;
        private int size;
        private bool isRequired;
        private bool isTemporary;
        private bool isLocalizable;

        /// <summary>
        /// Creates a new ColumnInfo object from a column definition.
        /// </summary>
        /// <param name="name">name of the column</param>
        /// <param name="columnDefinition">column definition string</param>
        /// <seealso cref="ColumnDefinitionString"/>
        public ColumnInfo(string name, string columnDefinition)
            : this(name, typeof(String), 0, false, false, false)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (columnDefinition == null)
            {
                throw new ArgumentNullException("columnDefinition");
            }

            switch (Char.ToLower(columnDefinition[0], CultureInfo.InvariantCulture))
            {
                case 'i': this.type = typeof(Int32);
                    break;
                case 'j': this.type = typeof(Int32); this.isTemporary = true;
                    break;
                case 'g': this.type = typeof(String); this.isTemporary = true;
                    break;
                case 'l': this.type = typeof(String); this.isLocalizable = true;
                    break;
                case 's': this.type = typeof(String);
                    break;
                case 'v': this.type = typeof(Stream);
                    break;
                default: throw new InstallerException();
            }

            this.isRequired = Char.IsLower(columnDefinition[0]);
            this.size = Int32.Parse(
                columnDefinition.Substring(1),
                CultureInfo.InvariantCulture.NumberFormat);
            if (this.type == typeof(Int32) && this.size <= 2)
            {
                this.type = typeof(Int16);
            }
        }

        /// <summary>
        /// Creates a new ColumnInfo object from a list of parameters.
        /// </summary>
        /// <param name="name">name of the column</param>
        /// <param name="type">type of the column; must be one of the following:
        /// Int16, Int32, String, or Stream</param>
        /// <param name="size">the maximum number of characters for String columns;
        /// ignored for other column types</param>
        /// <param name="isRequired">true if the column is required to have a non-null value</param>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public ColumnInfo(string name, Type type, int size, bool isRequired)
            : this(name, type, size, isRequired, false, false)
        {
        }

        /// <summary>
        /// Creates a new ColumnInfo object from a list of parameters.
        /// </summary>
        /// <param name="name">name of the column</param>
        /// <param name="type">type of the column; must be one of the following:
        /// Int16, Int32, String, or Stream</param>
        /// <param name="size">the maximum number of characters for String columns;
        /// ignored for other column types</param>
        /// <param name="isRequired">true if the column is required to have a non-null value</param>
        /// <param name="isTemporary">true to if the column is only in-memory and
        /// not persisted with the database</param>
        /// <param name="isLocalizable">for String columns, indicates the column
        /// is localizable; ignored for other column types</param>
        public ColumnInfo(string name, Type type, int size, bool isRequired, bool isTemporary, bool isLocalizable)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (type == typeof(Int32))
            {
                size = 4;
                isLocalizable = false;
            }
            else if (type == typeof(Int16))
            {
                size = 2;
                isLocalizable = false;
            }
            else if (type == typeof(String))
            {
            }
            else if (type == typeof(Stream))
            {
                isLocalizable = false;
            }
            else
            {
                throw new ArgumentOutOfRangeException("type");
            }

            this.name = name;
            this.type = type;
            this.size = size;
            this.isRequired = isRequired;
            this.isTemporary = isTemporary;
            this.isLocalizable = isLocalizable;
        }

        /// <summary>
        /// Gets the name of the column.
        /// </summary>
        /// <value>name of the column</value>
        public string Name
        {
            get { return this.name; }
        }

        /// <summary>
        /// Gets the type of the column as a System.Type.  This is one of the following: Int16, Int32, String, or Stream
        /// </summary>
        /// <value>type of the column</value>
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public Type Type
        {
            get { return this.type; }
        }

        /// <summary>
        /// Gets the type of the column as an integer that can be cast to a System.Data.DbType.  This is one of the following: Int16, Int32, String, or Binary
        /// </summary>
        /// <value>equivalent DbType of the column as an integer</value>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public int DBType
        {
            get
            {
                if (this.type == typeof(Int16)) return 10;
                else if (this.type == typeof(Int32)) return 11;
                else if (this.type == typeof(Stream)) return 1;
                else return 16;
            }
        }

        /// <summary>
        /// Gets the size of the column.
        /// </summary>
        /// <value>The size of integer columns this is either 2 or 4.  For string columns this is the maximum
        /// recommended length of the string, or 0 for unlimited length.  For stream columns, 0 is returned.</value>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public int Size
        {
            get { return this.size; }
        }

        /// <summary>
        /// Gets a value indicating whether the column must be non-null when inserting a record.
        /// </summary>
        /// <value>required status of the column</value>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool IsRequired
        {
            get { return this.isRequired; }
        }

        /// <summary>
        /// Gets a value indicating whether the column is temporary. Temporary columns are not persisted
        /// when the database is saved to disk.
        /// </summary>
        /// <value>temporary status of the column</value>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool IsTemporary
        {
            get { return this.isTemporary; }
        }

        /// <summary>
        /// Gets a value indicating whether the column is a string column that is localizable.
        /// </summary>
        /// <value>localizable status of the column</value>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool IsLocalizable
        {
            get { return this.isLocalizable; }
        }

        /// <summary>
        /// Gets an SQL fragment that can be used to create this column within a CREATE TABLE statement.
        /// </summary>
        /// <value>SQL fragment to be used for creating the column</value>
        /// <remarks><p>
        /// Examples:
        /// <list type="bullet">
        /// <item>LONG</item>
        /// <item>SHORT TEMPORARY</item>
        /// <item>CHAR(0) LOCALIZABLE</item>
        /// <item>CHAR(72) NOT NULL LOCALIZABLE</item>
        /// <item>OBJECT</item>
        /// </list>
        /// </p></remarks>
        public string SqlCreateString
        {
            get
            {
                StringBuilder s = new StringBuilder();
                s.AppendFormat("`{0}` ", this.name);
                if (this.type == typeof(Int16)) s.Append("SHORT");
                else if (this.type == typeof(Int32)) s.Append("LONG");
                else if (this.type == typeof(String)) s.AppendFormat("CHAR({0})", this.size);
                else s.Append("OBJECT");
                if (this.isRequired) s.Append(" NOT NULL");
                if (this.isTemporary) s.Append(" TEMPORARY");
                if (this.isLocalizable) s.Append(" LOCALIZABLE");
                return s.ToString();
            }
        }

        /// <summary>
        /// Gets a short string defining the type and size of the column.
        /// </summary>
        /// <value>
        /// The definition string consists
        /// of a single letter representing the data type followed by the width of the column (in characters
        /// when applicable, bytes otherwise). A width of zero designates an unbounded width (for example,
        /// long text fields and streams). An uppercase letter indicates that null values are allowed in
        /// the column.
        /// </value>
        /// <remarks><p>
        /// <list>
        /// <item>s? - String, variable length (?=1-255)</item>
        /// <item>s0 - String, variable length</item>
        /// <item>i2 - Short integer</item>
        /// <item>i4 - Long integer</item>
        /// <item>v0 - Binary Stream</item>
        /// <item>g? - Temporary string (?=0-255)</item>
        /// <item>j? - Temporary integer (?=0,1,2,4)</item>
        /// <item>l? - Localizable string, variable length (?=1-255)</item>
        /// <item>l0 - Localizable string, variable length</item>
        /// </list>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string ColumnDefinitionString
        {
            get
            {
                char t;
                if (this.type == typeof(Int16) || this.type == typeof(Int32))
                {
                    t = (this.isTemporary ? 'j' : 'i');
                }
                else if (this.type == typeof(String))
                {
                    t = (this.isTemporary ? 'g' : this.isLocalizable ? 'l' : 's');
                }
                else
                {
                    t = 'v';
                }
                return String.Format(
                    CultureInfo.InvariantCulture,
                    "{0}{1}",
                    (this.isRequired ? t : Char.ToUpper(t, CultureInfo.InvariantCulture)),
                    this.size);
            }
        }

        /// <summary>
        /// Gets the name of the column.
        /// </summary>
        /// <returns>Name of the column.</returns>
        public override string ToString()
        {
            return this.Name;
        }
    }
}
