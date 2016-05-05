//---------------------------------------------------------------------
// <copyright file="TableInfo.cs" company="Microsoft Corporation">
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
    using System.Collections.ObjectModel;
    using System.Text;

    /// <summary>
    /// Defines a table in an installation database.
    /// </summary>
    internal class TableInfo
    {
        private string name;
        private ColumnCollection columns;
        private ReadOnlyCollection<string> primaryKeys;

        /// <summary>
        /// Creates a table definition.
        /// </summary>
        /// <param name="name">Name of the table.</param>
        /// <param name="columns">Columns in the table.</param>
        /// <param name="primaryKeys">The primary keys of the table.</param>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public TableInfo(string name, ICollection<ColumnInfo> columns, IList<string> primaryKeys)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            if (columns == null || columns.Count == 0)
            {
                throw new ArgumentNullException("columns");
            }

            if (primaryKeys == null || primaryKeys.Count == 0)
            {
                throw new ArgumentNullException("primaryKeys");
            }

            this.name = name;
            this.columns = new ColumnCollection(columns);
            this.primaryKeys = new List<string>(primaryKeys).AsReadOnly();
            foreach (string primaryKey in this.primaryKeys)
            {
                if (!this.columns.Contains(primaryKey))
                {
                    throw new ArgumentOutOfRangeException("primaryKeys");
                }
            }
        }

        internal TableInfo(Database db, string name)
        {
            if (db == null)
            {
                throw new ArgumentNullException("db");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            this.name = name;

            using (View columnsView = db.OpenView("SELECT * FROM `{0}`", name))
            {
                this.columns = new ColumnCollection(columnsView);
            }

            this.primaryKeys = new ReadOnlyCollection<string>(
                TableInfo.GetTablePrimaryKeys(db, name));
        }

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        public string Name
        {
            get
            {
                return this.name;
            }
        }

        /// <summary>
        /// Gets information about the columns in this table.
        /// </summary>
        /// <remarks><p>
        /// This property queries the database every time it is called,
        /// to ensure the returned values are up-to-date. For best performance,
        /// hold onto the returned collection if using it more than once.
        /// </p></remarks>
        public ColumnCollection Columns
        {
            get
            {
                return this.columns;
            }
        }

        /// <summary>
        /// Gets the names of the columns that are primary keys of the table.
        /// </summary>
        public IList<string> PrimaryKeys
        {
            get
            {
                return this.primaryKeys;
            }
        }

        /// <summary>
        /// Gets an SQL CREATE string that can be used to create the table.
        /// </summary>
        public string SqlCreateString
        {
            get
            {
                StringBuilder s = new StringBuilder("CREATE TABLE `");
                s.Append(this.name);
                s.Append("` (");
                int count = 0;
                foreach (ColumnInfo col in this.Columns)
                {
                    if (count > 0)
                    {
                        s.Append(", ");
                    }
                    s.Append(col.SqlCreateString);
                    count++;
                }
                s.Append("  PRIMARY KEY ");
                count = 0;
                foreach (string key in this.PrimaryKeys)
                {
                    if (count > 0)
                    {
                        s.Append(", ");
                    }
                    s.AppendFormat("`{0}`", key);
                    count++;
                }
                s.Append(')');
                return s.ToString();
            }
        }

        /// <summary>
        /// Gets an SQL INSERT string that can be used insert a new record into the table.
        /// </summary>
        /// <remarks><p>
        /// The values are expressed as question-mark tokens, to be supplied by the record.
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string SqlInsertString
        {
            get
            {
                StringBuilder s = new StringBuilder("INSERT INTO `");
                s.Append(this.name);
                s.Append("` (");
                int count = 0;
                foreach (ColumnInfo col in this.Columns)
                {
                    if (count > 0)
                    {
                        s.Append(", ");
                    }

                    s.AppendFormat("`{0}`", col.Name);
                    count++;
                }
                s.Append(") VALUES (");
                while (count > 0)
                {
                    count--;
                    s.Append("?");

                    if (count > 0)
                    {
                        s.Append(", ");
                    }
                }
                s.Append(')');
                return s.ToString();
            }
        }

        /// <summary>
        /// Gets an SQL SELECT string that can be used to select all columns of the table.
        /// </summary>
        /// <remarks><p>
        /// The columns are listed explicitly in the SELECT string, as opposed to using "SELECT *".
        /// </p></remarks>
        public string SqlSelectString
        {
            get
            {
                StringBuilder s = new StringBuilder("SELECT ");
                int count = 0;
                foreach (ColumnInfo col in this.Columns)
                {
                    if (count > 0) s.Append(", ");
                    s.AppendFormat("`{0}`", col.Name);
                    count++;
                }
                s.AppendFormat(" FROM `{0}`", this.Name);
                return s.ToString();
            }
        }

        /// <summary>
        /// Gets a string representation of the table.
        /// </summary>
        /// <returns>The name of the table.</returns>
        public override string ToString()
        {
            return this.name;
        }

        private static IList<string> GetTablePrimaryKeys(Database db, string table)
        {
            if (table == "_Tables")
            {
                return new string[] { "Name" };
            }
            else if (table == "_Columns")
            {
                return new string[] { "Table", "Number" };
            }
            else if (table == "_Storages")
            {
                return new string[] { "Name" };
            }
            else if (table == "_Streams")
            {
                return new string[] { "Name" };
            }
            else
            {
                int hrec;
                uint ret = RemotableNativeMethods.MsiDatabaseGetPrimaryKeys(
                    (int) db.Handle, table, out hrec);
                if (ret != 0)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }

                using (Record rec = new Record((IntPtr) hrec, true, null))
                {
                    string[] keys = new string[rec.FieldCount];
                    for (int i = 0; i < keys.Length; i++)
                    {
                        keys[i] = rec.GetString(i + 1);
                    }

                    return keys;
                }
            }
        }
    }
}
