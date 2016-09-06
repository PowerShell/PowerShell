//---------------------------------------------------------------------
// <copyright file="QTable.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller.Linq
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;

    /// <summary>
    /// Represents one table in a LINQ-queryable Database.
    /// </summary>
    /// <typeparam name="TRecord">type that represents one record in the table</typeparam>
    /// <remarks>
    /// This class is the primary gateway to all LINQ to MSI query functionality.
    /// <para>The TRecord generic parameter may be the general <see cref="QRecord" />
    /// class, or a specialized subclass of QRecord.</para>
    /// </remarks>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    internal sealed class QTable<TRecord> : IOrderedQueryable<TRecord>, IQueryProvider
        where TRecord : QRecord, new()
    {
        private QDatabase db;
        private TableInfo tableInfo;

        /// <summary>
        /// Infers the name of the table this instance will be
        /// associated with.
        /// </summary>
        /// <returns>table name</returns>
        /// <remarks>
        /// The table name is retrieved from a DatabaseTableAttribute
        /// on the record type if it exists; otherwise the name is
        /// derived from the name of the record type itself.
        /// (An optional underscore suffix on the record type name is dropped.)
        /// </remarks>
        private static string InferTableName()
        {
            foreach (DatabaseTableAttribute attr in typeof(TRecord).GetCustomAttributes(
                typeof(DatabaseTableAttribute), false))
            {
                string tableName = attr.Table;
                if (!string.IsNullOrWhiteSpace(tableName))
                {
                    return tableName;
                }
            }

            string recordTypeName = typeof(TRecord).Name;
            if (recordTypeName[recordTypeName.Length - 1] == '_')
            {
                return recordTypeName.Substring(0, recordTypeName.Length - 1);
            }
            else
            {
                return recordTypeName;
            }
        }

        /// <summary>
        /// Creates a new QTable, inferring the table name
        /// from the name of the record type parameter.
        /// </summary>
        /// <param name="db">database that contains the table</param>
        public QTable(QDatabase db)
            : this(db, InferTableName())
        {
        }

        /// <summary>
        /// Creates a new QTable with an explicit table name.
        /// </summary>
        /// <param name="db">database that contains the table</param>
        /// <param name="table">name of the table</param>
        public QTable(QDatabase db, string table)
        {
            if (db == null)
            {
                throw new ArgumentNullException("db");
            }

            if (string.IsNullOrWhiteSpace(table))
            {
                throw new ArgumentNullException("table");
            }

            this.db = db;
            this.tableInfo = db.Tables[table];
            if (this.tableInfo == null)
            {
                throw new ArgumentException(
                    "Table does not exist in database: " + table);
            }
        }

        /// <summary>
        /// Gets schema information about the table.
        /// </summary>
        public TableInfo TableInfo
        {
            get
            {
                return this.tableInfo;
            }
        }

        /// <summary>
        /// Gets the database this table is associated with.
        /// </summary>
        public QDatabase Database
        {
            get
            {
                return this.db;
            }
        }

        /// <summary>
        /// Enumerates over all records in the table.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<TRecord> GetEnumerator()
        {
            string query = this.tableInfo.SqlSelectString;

            TextWriter log = this.db.Log;
            if (log != null)
            {
                log.WriteLine();
                log.WriteLine(query);
            }

            using (View view = db.OpenView(query))
            {
                view.Execute();

                ColumnCollection columns = this.tableInfo.Columns;
                int columnCount = columns.Count;
                bool[] isBinary = new bool[columnCount];

                for (int i = 0; i < isBinary.Length; i++)
                {
                    isBinary[i] = columns[i].Type == typeof(System.IO.Stream);
                }

                foreach (Record rec in view) using (rec)
                {
                    string[] values = new string[columnCount];
                    for (int i = 0; i < values.Length; i++)
                    {
                        values[i] = isBinary[i] ? "[Binary Data]" : rec.GetString(i + 1);
                    }

                    TRecord trec = new TRecord();
                    trec.Database = this.Database;
                    trec.TableInfo = this.TableInfo;
                    trec.Values = values;
                    trec.Exists = true;
                    yield return trec;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<TRecord>) this).GetEnumerator();
        }

        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            Query<TElement> q = new Query<TElement>(this.Database, expression);

            MethodCallExpression methodCallExpression = (MethodCallExpression) expression;
            string methodName = methodCallExpression.Method.Name;
            if (methodName == "Where")
            {
                LambdaExpression argumentExpression = (LambdaExpression)
                    ((UnaryExpression) methodCallExpression.Arguments[1]).Operand;
                q.BuildQuery(this.TableInfo, argumentExpression);
            }
            else if (methodName == "OrderBy")
            {
                LambdaExpression argumentExpression = (LambdaExpression)
                    ((UnaryExpression) methodCallExpression.Arguments[1]).Operand;
                q.BuildSequence(this.TableInfo, argumentExpression);
            }
            else if (methodName == "Select")
            {
                LambdaExpression argumentExpression = (LambdaExpression)
                    ((UnaryExpression) methodCallExpression.Arguments[1]).Operand;
                q.BuildNullQuery(this.TableInfo, typeof(TRecord), argumentExpression);
                q.BuildProjection(null, argumentExpression);
            }
            else if (methodName == "Join")
            {
                ConstantExpression constantExpression = (ConstantExpression)
                    methodCallExpression.Arguments[1];
                IQueryable inner = (IQueryable) constantExpression.Value;
                q.PerformJoin(
                    this.TableInfo,
                    typeof(TRecord),
                    inner,
                    GetJoinLambda(methodCallExpression.Arguments[2]),
                    GetJoinLambda(methodCallExpression.Arguments[3]),
                    GetJoinLambda(methodCallExpression.Arguments[4]));
            }
            else
            {
                throw new NotSupportedException(
                    "Query operation not supported: " + methodName);
            }

            return q;
        }

        private static LambdaExpression GetJoinLambda(Expression expression)
        {
            UnaryExpression unaryExpression = (UnaryExpression) expression;
            return (LambdaExpression) unaryExpression.Operand;
        }

        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            return ((IQueryProvider) this).CreateQuery<TRecord>(expression);
        }

        TResult IQueryProvider.Execute<TResult>(Expression expression)
        {
            throw new NotSupportedException(
                "Direct method calls not supported -- use AsEnumerable() instead.");
        }

        object IQueryProvider.Execute(Expression expression)
        {
            throw new NotSupportedException(
                "Direct method calls not supported -- use AsEnumerable() instead.");
        }

        IQueryProvider IQueryable.Provider
        {
            get
            {
                return this;
            }
        }

        Type IQueryable.ElementType
        {
            get
            {
                return typeof(TRecord);
            }
        }

        Expression IQueryable.Expression
        {
            get
            {
                return Expression.Constant(this);
            }
        }

        /// <summary>
        /// Creates a new record that can be inserted into this table.
        /// </summary>
        /// <returns>a record with all fields initialized to null</returns>
        /// <remarks>
        /// Primary keys and required fields must be filled in with
        /// non-null values before the record can be inserted.
        /// <para>The record is tied to this table in this database;
        /// it cannot be inserted into another table or database.</para>
        /// </remarks>
        public TRecord NewRecord()
        {
            TRecord rec = new TRecord();
            rec.Database = this.Database;
            rec.TableInfo = this.TableInfo;
            IList<string> values = new List<string>(this.TableInfo.Columns.Count);
            for (int i = 0; i < this.TableInfo.Columns.Count; i++)
            {
                values.Add(null);
            }
            rec.Values = values;
            return rec;
        }
    }
}
