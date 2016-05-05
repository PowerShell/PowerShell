//---------------------------------------------------------------------
// <copyright file="TableCollection.cs" company="Microsoft Corporation">
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

    /// <summary>
    /// Contains information about all the tables in a Windows Installer database.
    /// </summary>
    internal class TableCollection : ICollection<TableInfo>
    {
        private Database db;

        internal TableCollection(Database db)
        {
            this.db = db;
        }

        /// <summary>
        /// Gets the number of tables in the database.
        /// </summary>
        public int Count
        {
            get
            {
                return this.GetTables().Count;
            }
        }

        /// <summary>
        /// Gets a boolean value indicating whether the collection is read-only.
        /// A TableCollection is read-only when the database is read-only.
        /// </summary>
        /// <value>read-only status of the collection</value>
        public bool IsReadOnly
        {
            get
            {
                return this.db.IsReadOnly;
            }
        }

        /// <summary>
        /// Gets information about a given table.
        /// </summary>
        /// <param name="table">case-sensitive name of the table</param>
        /// <returns>information about the requested table, or null if the table does not exist in the database</returns>
        public TableInfo this[string table]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(table))
                {
                    throw new ArgumentNullException("table");
                }

                if (!this.Contains(table))
                {
                    return null;
                }

                return new TableInfo(this.db, table);
            }
        }

        /// <summary>
        /// Adds a new table to the database.
        /// </summary>
        /// <param name="item">information about the table to be added</param>
        /// <exception cref="InvalidOperationException">a table with the same name already exists in the database</exception>
        public void Add(TableInfo item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (this.Contains(item.Name))
            {
                throw new InvalidOperationException();
            }

            this.db.Execute(item.SqlCreateString);
        }

        /// <summary>
        /// Removes all tables (and all data) from the database.
        /// </summary>
        public void Clear()
        {
            foreach (string table in this.GetTables())
            {
                this.Remove(table);
            }
        }

        /// <summary>
        /// Checks if the database contains a table with the given name.
        /// </summary>
        /// <param name="item">case-sensitive name of the table to search for</param>
        /// <returns>True if the table exists, false otherwise.</returns>
        public bool Contains(string item)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                throw new ArgumentNullException("item");
            }
            uint ret = RemotableNativeMethods.MsiDatabaseIsTablePersistent((int) this.db.Handle, item);
            if (ret == 3)  // MSICONDITION_ERROR
            {
                throw new InstallerException();
            }
            return ret != 2;  // MSICONDITION_NONE
        }

        bool ICollection<TableInfo>.Contains(TableInfo item)
        {
            return this.Contains(item.Name);
        }

        /// <summary>
        /// Copies the table information from this collection into an array.
        /// </summary>
        /// <param name="array">destination array to be filed</param>
        /// <param name="arrayIndex">offset into the destination array where copying begins</param>
        public void CopyTo(TableInfo[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            foreach (string table in this.GetTables())
            {
                array[arrayIndex++] = new TableInfo(this.db, table);
            }
        }

        /// <summary>
        /// Removes a table from the database.
        /// </summary>
        /// <param name="item">case-sensitive name of the table to be removed</param>
        /// <returns>true if the table was removed, false if the table did not exist</returns>
        public bool Remove(string item)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                throw new ArgumentNullException("item");
            }

            if (!this.Contains(item))
            {
                return false;
            }
            this.db.Execute("DROP TABLE `{0}`", item);
            return true;
        }

        bool ICollection<TableInfo>.Remove(TableInfo item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            return this.Remove(item.Name);
        }

        /// <summary>
        /// Enumerates the tables in the database.
        /// </summary>
        public IEnumerator<TableInfo> GetEnumerator()
        {
            foreach (string table in this.GetTables())
            {
                yield return new TableInfo(this.db, table);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private IList<string> GetTables()
        {
            return this.db.ExecuteStringQuery("SELECT `Name` FROM `_Tables`");
        }
    }
}
