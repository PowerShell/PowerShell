//---------------------------------------------------------------------
// <copyright file="ColumnCollection.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Collection of column information related to a <see cref="TableInfo"/> or
    /// <see cref="View"/>.
    /// </summary>
    internal sealed class ColumnCollection : ICollection<ColumnInfo>
    {
        private IList<ColumnInfo> columns;
        private string formatString;

        /// <summary>
        /// Creates a new ColumnCollection based on a specified list of columns.
        /// </summary>
        /// <param name="columns">columns to be added to the new collection</param>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public ColumnCollection(ICollection<ColumnInfo> columns)
        {
            if (columns == null)
            {
                throw new ArgumentNullException("columns");
            }

            this.columns = new List<ColumnInfo>(columns);
        }

        /// <summary>
        /// Creates a new ColumnCollection that is associated with a database table.
        /// </summary>
        /// <param name="view">view that contains the columns</param>
        internal ColumnCollection(View view)
        {
            if (view == null)
            {
                throw new ArgumentNullException("view");
            }

            this.columns = ColumnCollection.GetViewColumns(view);
        }

        /// <summary>
        /// Gets the number of columns in the collection.
        /// </summary>
        /// <value>number of columns in the collection</value>
        public int Count
        {
            get
            {
                return this.columns.Count;
            }
        }

        /// <summary>
        /// Gets a boolean value indicating whether the collection is read-only.
        /// A ColumnCollection is read-only if it is associated with a <see cref="View"/>
        /// or a read-only <see cref="Database"/>.
        /// </summary>
        /// <value>read-only status of the collection</value>
        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets information about a specific column in the collection.
        /// </summary>
        /// <param name="columnIndex">1-based index into the column collection</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="columnIndex"/> is less
        /// than 1 or greater than the number of columns in the collection</exception>
        public ColumnInfo this[int columnIndex]
        {
            get
            {
                if (columnIndex >= 0 && columnIndex < this.columns.Count)
                {
                    return this.columns[columnIndex];
                }
                else
                {
                    throw new ArgumentOutOfRangeException("columnIndex");
                }
            }
        }

        /// <summary>
        /// Gets information about a specific column in the collection.
        /// </summary>
        /// <param name="columnName">case-sensitive name of a column collection</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="columnName"/> does
        /// not exist in the collection</exception>
        public ColumnInfo this[string columnName]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(columnName))
                {
                    throw new ArgumentNullException("columnName");
                }

                foreach (ColumnInfo colInfo in this.columns)
                {
                    if (colInfo.Name == columnName)
                    {
                        return colInfo;
                    }
                }

                throw new ArgumentOutOfRangeException("columnName");
            }
        }

        /// <summary>
        /// Not supported because the collection is read-only.
        /// </summary>
        /// <param name="item">information about the column being added</param>
        /// <exception cref="InvalidOperationException">the collection is read-only</exception>
        public void Add(ColumnInfo item)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Not supported because the collection is read-only.
        /// </summary>
        /// <exception cref="InvalidOperationException">the collection is read-only</exception>
        public void Clear()
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Checks if a column with a given name exists in the collection.
        /// </summary>
        /// <param name="columnName">case-sensitive name of the column to look for</param>
        /// <returns>true if the column exists in the collection, false otherwise</returns>
        public bool Contains(string columnName)
        {
            return this.IndexOf(columnName) >= 0;
        }

        /// <summary>
        /// Checks if a column with a given name exists in the collection.
        /// </summary>
        /// <param name="column">column to look for, with case-sensitive name</param>
        /// <returns>true if the column exists in the collection, false otherwise</returns>
        bool ICollection<ColumnInfo>.Contains(ColumnInfo column)
        {
            return this.Contains(column.Name);
        }

        /// <summary>
        /// Gets the index of a column within the collection.
        /// </summary>
        /// <param name="columnName">case-sensitive name of the column to look for</param>
        /// <returns>0-based index of the column, or -1 if not found</returns>
        public int IndexOf(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
            {
                throw new ArgumentNullException("columnName");
            }

            for (int index = 0; index < this.columns.Count; index++)
            {
                if (this.columns[index].Name == columnName)
                {
                    return index;
                }
            }
            return -1;
        }

        /// <summary>
        /// Copies the columns from this collection into an array.
        /// </summary>
        /// <param name="array">destination array to be filed</param>
        /// <param name="arrayIndex">offset into the destination array where copying begins</param>
        public void CopyTo(ColumnInfo[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            this.columns.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Not supported because the collection is read-only.
        /// </summary>
        /// <param name="column">column to remove</param>
        /// <returns>true if the column was removed, false if it was not found</returns>
        /// <exception cref="InvalidOperationException">the collection is read-only</exception>
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "column")]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        bool ICollection<ColumnInfo>.Remove(ColumnInfo column)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Gets an enumerator over the columns in the collection.
        /// </summary>
        /// <returns>An enumerator of ColumnInfo objects.</returns>
        public IEnumerator<ColumnInfo> GetEnumerator()
        {
            return this.columns.GetEnumerator();
        }

        /// <summary>
        /// Gets a string suitable for printing all the values of a record containing these columns.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string FormatString
        {
            get
            {
                if (this.formatString == null)
                {
                    this.formatString = CreateFormatString(this.columns);
                }
                return this.formatString;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static string CreateFormatString(IList<ColumnInfo> columns)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < columns.Count; i++)
            {
                if (columns[i].Type == typeof(Stream))
                {
                    sb.AppendFormat("{0} = [Binary Data]", columns[i].Name);
                }
                else
                {
                    sb.AppendFormat("{0} = [{1}]", columns[i].Name, i + 1);
                }

                if (i < columns.Count - 1)
                {
                    sb.Append(", ");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets an enumerator over the columns in the collection.
        /// </summary>
        /// <returns>An enumerator of ColumnInfo objects.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Creates ColumnInfo objects for the associated view.
        /// </summary>
        /// <returns>dynamically-generated list of columns</returns>
        private static IList<ColumnInfo> GetViewColumns(View view)
        {
            IList<string> columnNames = ColumnCollection.GetViewColumns(view, false);
            IList<string> columnTypes = ColumnCollection.GetViewColumns(view, true);

            int count = columnNames.Count;
            if (columnTypes[count - 1] == "O0")
            {
                // Weird.. the "_Tables" table returns a second column with type "O0" -- ignore it.
                count--;
            }

            IList<ColumnInfo> columnsList = new List<ColumnInfo>(count);
            for (int i = 0; i < count; i++)
            {
                columnsList.Add(new ColumnInfo(columnNames[i], columnTypes[i]));
            }

            return columnsList;
        }

        /// <summary>
        /// Gets a list of column names or column-definition-strings for the
        /// associated view.
        /// </summary>
        /// <param name="view">the view to that defines the columns</param>
        /// <param name="types">true to return types (column definition strings),
        /// false to return names</param>
        /// <returns>list of column names or types</returns>
        private static IList<string> GetViewColumns(View view, bool types)
        {
            int recordHandle;
            int typesFlag = types ? 1 : 0;
            uint ret = RemotableNativeMethods.MsiViewGetColumnInfo(
                (int) view.Handle, (uint) typesFlag, out recordHandle);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }

            using (Record rec = new Record((IntPtr) recordHandle, true, null))
            {
                int count = rec.FieldCount;
                IList<string> columnsList = new List<string>(count);

                // Since we must be getting all strings of limited length,
                // this code is faster than calling rec.GetString(field).
                for (int field = 1; field <= count; field++)
                {
                    uint bufSize = 256;
                    StringBuilder buf = new StringBuilder((int) bufSize);
                    ret = RemotableNativeMethods.MsiRecordGetString((int) rec.Handle, (uint) field, buf, ref bufSize);
                    if (ret != 0)
                    {
                        throw InstallerException.ExceptionFromReturnCode(ret);
                    }
                    columnsList.Add(buf.ToString());
                }
                return columnsList;
            }
        }
    }
}
