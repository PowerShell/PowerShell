//---------------------------------------------------------------------
// <copyright file="QRecord.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller.Linq
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Generic record entity for queryable databases,
    /// and base for strongly-typed entity subclasses.
    /// </summary>
    /// <remarks>
    /// Several predefined specialized subclasses are provided for common
    /// standard tables. Subclasses for additional standard tables
    /// or custom tables are not necessary, but they are easy to create
    /// and make the coding experience much nicer.
    /// <para>When creating subclasses, the following attributes may be
    /// useful: <see cref="DatabaseTableAttribute"/>,
    /// <see cref="DatabaseColumnAttribute"/></para>
    /// </remarks>
    internal class QRecord
    {
        /// <summary>
        /// Do not call. Use QTable.NewRecord() instead.
        /// </summary>
        /// <remarks>
        /// Subclasses must also provide a public parameterless constructor.
        /// <para>QRecord constructors are only public due to implementation
        /// reasons (to satisfy the new() constraint on the QTable generic
        /// class). They are not intended to be called by user code other than
        /// a subclass constructor. If the constructor is invoked directly,
        /// the record instance will not be properly initialized (associated
        /// with a database table) and calls to methods on the instance
        /// will throw a NullReferenceException.</para>
        /// </remarks>
        /// <seealso cref="QTable&lt;TRecord&gt;.NewRecord()"/>
        public QRecord()
        {
        }

        internal QDatabase Database { get; set; }

        internal TableInfo TableInfo { get; set; }

        internal IList<string> Values { get; set; }

        internal bool Exists { get; set; }

        /// <summary>
        /// Gets the number of fields in the record.
        /// </summary>
        public int FieldCount
        {
            get
            {
                return this.Values.Count;
            }
        }

        /// <summary>
        /// Gets or sets a record field.
        /// </summary>
        /// <param name="field">column name of the field</param>
        /// <remarks>
        /// Setting a field value will automatically update the database.
        /// </remarks>
        public string this[string field]
        {
            get
            {
                if (field == null)
                {
                    throw new ArgumentNullException("field");
                }

                int index = this.TableInfo.Columns.IndexOf(field);
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException("field");
                }

                return this[index];
            }

            set
            {
                if (field == null)
                {
                    throw new ArgumentNullException("field");
                }

                this.Update(new string[] { field }, new string[] { value });
            }
        }

        /// <summary>
        /// Gets or sets a record field.
        /// </summary>
        /// <param name="index">zero-based column index of the field</param>
        /// <remarks>
        /// Setting a field value will automatically update the database.
        /// </remarks>
        public string this[int index]
        {
            get
            {
                if (index < 0 || index >= this.FieldCount)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                return this.Values[index];
            }

            set
            {
                if (index < 0 || index >= this.FieldCount)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                this.Update(new int[] { index }, new string[] { value });
            }
        }

        /// <summary>
        /// Used by subclasses to get a field as an integer.
        /// </summary>
        /// <param name="index">zero-based column index of the field</param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "I")]
        protected int I(int index)
        {
            string value = this[index];
            return value.Length > 0 ?
                Int32.Parse(value, CultureInfo.InvariantCulture) : 0;
        }

        /// <summary>
        /// Used by subclasses to get a field as a nullable integer.
        /// </summary>
        /// <param name="index">zero-based column index of the field</param>
        protected int? NI(int index)
        {
            string value = this[index];
            return value.Length > 0 ?
                new int?(Int32.Parse(value, CultureInfo.InvariantCulture)) : null;
        }

        /// <summary>
        /// Dumps all record fields to a string.
        /// </summary>
        public override string ToString()
        {
            StringBuilder buf = new StringBuilder(this.GetType().Name);
            buf.Append(" {");
            for (int i = 0; i < this.FieldCount; i++)
            {
                buf.AppendFormat("{0} {1} = {2}",
                    (i > 0 ? "," : String.Empty),
                    this.TableInfo.Columns[i].Name,
                    this[i]);
            }
            buf.Append(" }");
            return buf.ToString();
        }

        /// <summary>
        /// Update multiple fields in the record (and the database).
        /// </summary>
        /// <param name="fields">column names of fields to update</param>
        /// <param name="values">new values for each field being updated</param>
        public void Update(IList<string> fields, IList<string> values)
        {
            if (fields == null)
            {
                throw new ArgumentNullException("fields");
            }

            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            if (fields.Count == 0 || values.Count == 0 ||
                fields.Count > this.FieldCount ||
                values.Count != fields.Count)
            {
                throw new ArgumentOutOfRangeException("fields");
            }

            int[] indexes = new int[fields.Count];
            for (int i = 0; i < indexes.Length; i++)
            {
                if (fields[i] == null)
                {
                    throw new ArgumentNullException("fields[" + i + "]");
                }

                indexes[i] = this.TableInfo.Columns.IndexOf(fields[i]);

                if (indexes[i] < 0)
                {
                    throw new ArgumentOutOfRangeException("fields[" + i + "]");
                }
            }

            this.Update(indexes, values);
        }

        /// <summary>
        /// Update multiple fields in the record (and the database).
        /// </summary>
        /// <param name="indexes">column indexes of fields to update</param>
        /// <param name="values">new values for each field being updated</param>
        /// <remarks>
        /// The record (primary keys) must already exist in the table.
        /// <para>Updating primary key fields is not yet implemented; use Delete()
        /// and Insert() instead.</para>
        /// </remarks>
        public void Update(IList<int> indexes, IList<string> values)
        {
            if (indexes == null)
            {
                throw new ArgumentNullException("indexes");
            }

            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            if (indexes.Count == 0 || values.Count == 0 ||
                indexes.Count > this.FieldCount ||
                values.Count != indexes.Count)
            {
                throw new ArgumentOutOfRangeException("indexes");
            }

            bool primaryKeyChanged = false;
            for (int i = 0; i < indexes.Count; i++)
            {
                int index = indexes[i];
                if (index < 0 || index >= this.FieldCount)
                {
                    throw new ArgumentOutOfRangeException("index[" + i + "]");
                }

                ColumnInfo col = this.TableInfo.Columns[index];
                if (this.TableInfo.PrimaryKeys.Contains(col.Name))
                {
                    if (values[i] == null)
                    {
                        throw new ArgumentNullException("values[" + i + "]");
                    }

                    primaryKeyChanged = true;
                }
                else if (values[i] == null)
                {
                    if (col.IsRequired)
                    {
                        throw new ArgumentNullException("values[" + i + "]");
                    }
                }

                this.Values[index] = values[i];
            }

            if (this.Exists)
            {
                if (!primaryKeyChanged)
                {
                    int updateRecSize = indexes.Count + this.TableInfo.PrimaryKeys.Count;
                    using (Record updateRec = this.Database.CreateRecord(updateRecSize))
                    {
                        StringBuilder s = new StringBuilder("UPDATE `");
                        s.Append(this.TableInfo.Name);
                        s.Append("` SET");

                        for (int i = 0; i < indexes.Count; i++)
                        {
                            ColumnInfo col = this.TableInfo.Columns[indexes[i]];
                            if (col.Type == typeof(Stream))
                            {
                                throw new NotSupportedException(
                                    "Cannot update stream columns via QRecord.");
                            }

                            int index = indexes[i];
                            s.AppendFormat("{0} `{1}` = ?",
                                (i > 0 ? "," : String.Empty),
                                col.Name);

                            if (values[i] != null)
                            {
                                updateRec[i + 1] = values[i];
                            }
                        }

                        for (int i = 0; i < this.TableInfo.PrimaryKeys.Count; i++)
                        {
                            string key = this.TableInfo.PrimaryKeys[i];
                            s.AppendFormat(" {0} `{1}` = ?", (i == 0 ? "WHERE" : "AND"), key);
                            int index = this.TableInfo.Columns.IndexOf(key);
                            updateRec[indexes.Count + i + 1] = this.Values[index];

                        }

                        string updateSql = s.ToString();
                        TextWriter log = this.Database.Log;
                        if (log != null)
                        {
                            log.WriteLine();
                            log.WriteLine(updateSql);
                            for (int field = 1; field <= updateRecSize; field++)
                            {
                                log.WriteLine("    ? = " + updateRec.GetString(field));
                            }
                        }

                        this.Database.Execute(updateSql, updateRec);
                    }
                }
                else
                {
                    throw new NotImplementedException(
                        "Update() cannot handle changed primary keys yet.");
                    // TODO:
                    //   query using old values
                    //   update values
                    //   View.Replace
                }
            }
        }

        /// <summary>
        /// Inserts the record in the database.
        /// </summary>
        /// <remarks>
        /// The record (primary keys) may not already exist in the table.
        /// <para>Use <see cref="QTable&lt;TRecord&gt;.NewRecord()"/> to get a new
        /// record. Primary keys and all required fields
        /// must be filled in before insertion.</para>
        /// </remarks>
        public void Insert()
        {
            this.Insert(false);
        }

        /// <summary>
        /// Inserts the record into the table.
        /// </summary>
        /// <param name="temporary">true if the record is temporarily
        /// inserted, to be visible only as long as the database is open</param>
        /// <remarks>
        /// The record (primary keys) may not already exist in the table.
        /// <para>Use <see cref="QTable&lt;TRecord&gt;.NewRecord()"/> to get a new
        /// record. Primary keys and all required fields
        /// must be filled in before insertion.</para>
        /// </remarks>
        public void Insert(bool temporary)
        {
            using (Record updateRec = this.Database.CreateRecord(this.FieldCount))
            {
                string insertSql = this.TableInfo.SqlInsertString;
                if (temporary)
                {
                    insertSql += " TEMPORARY";
                }

                TextWriter log = this.Database.Log;
                if (log != null)
                {
                    log.WriteLine();
                    log.WriteLine(insertSql);
                }

                for (int index = 0; index < this.FieldCount; index++)
                {
                    ColumnInfo col = this.TableInfo.Columns[index];
                    if (col.Type == typeof(Stream))
                    {
                        throw new NotSupportedException(
                            "Cannot insert stream columns via QRecord.");
                    }

                    if (this.Values[index] != null)
                    {
                        updateRec[index + 1] = this.Values[index];
                    }

                    if (log != null)
                    {
                        log.WriteLine("    ? = " + this.Values[index]);
                    }
                }

                this.Database.Execute(insertSql, updateRec);
                this.Exists = true;
            }
        }

        /// <summary>
        /// Deletes the record from the table if it exists.
        /// </summary>
        public void Delete()
        {
            using (Record keyRec = this.Database.CreateRecord(this.TableInfo.PrimaryKeys.Count))
            {
                StringBuilder s = new StringBuilder("DELETE FROM `");
                s.Append(this.TableInfo.Name);
                s.Append("`");
                for (int i = 0; i < this.TableInfo.PrimaryKeys.Count; i++)
                {
                    string key = this.TableInfo.PrimaryKeys[i];
                    s.AppendFormat(" {0} `{1}` = ?", (i == 0 ? "WHERE" : "AND"), key);
                    int index = this.TableInfo.Columns.IndexOf(key);
                    keyRec[i + 1] = this.Values[index];
                }

                string deleteSql = s.ToString();

                TextWriter log = this.Database.Log;
                if (log != null)
                {
                    log.WriteLine();
                    log.WriteLine(deleteSql);

                    for (int i = 0; i < this.TableInfo.PrimaryKeys.Count; i++)
                    {
                        log.WriteLine("    ? = " + keyRec.GetString(i + 1));
                    }
                }

                this.Database.Execute(deleteSql, keyRec);
                this.Exists = false;
            }
        }

        /// <summary>
        /// Not yet implemented.
        /// </summary>
        public void Refresh()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not yet implemented.
        /// </summary>
        public void Assign()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not yet implemented.
        /// </summary>
        public bool Merge()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not yet implemented.
        /// </summary>
        public ICollection<ValidationErrorInfo> Validate()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not yet implemented.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
        public ICollection<ValidationErrorInfo> ValidateNew()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not yet implemented.
        /// </summary>
        public ICollection<ValidationErrorInfo> ValidateFields()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not yet implemented.
        /// </summary>
        public ICollection<ValidationErrorInfo> ValidateDelete()
        {
            throw new NotImplementedException();
        }
    }
}
