//---------------------------------------------------------------------
// <copyright file="Query.cs" company="Microsoft Corporation">
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
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// Implements the LINQ to MSI query functionality.
    /// </summary>
    /// <typeparam name="T">the result type of the current query --
    /// either some kind of QRecord, or some projection of record data</typeparam>
    internal sealed class Query<T> : IOrderedQueryable<T>, IQueryProvider
    {
        private QDatabase db;
        private Expression queryableExpression;
        private List<TableInfo> tables;
        private List<Type> recordTypes;
        private List<string> selectors;
        private string where;
        private List<object> whereParameters;
        private List<TableColumn> orderbyColumns;
        private List<TableColumn> selectColumns;
        private List<TableColumn> joinColumns;
        private List<Delegate> projectionDelegates;

        internal Query(QDatabase db, Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            this.db = db;
            this.queryableExpression = expression;
            this.tables = new List<TableInfo>();
            this.recordTypes = new List<Type>();
            this.selectors = new List<string>();
            this.whereParameters = new List<object>();
            this.orderbyColumns = new List<TableColumn>();
            this.selectColumns = new List<TableColumn>();
            this.joinColumns = new List<TableColumn>();
            this.projectionDelegates = new List<Delegate>();
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (this.selectColumns.Count == 0)
            {
                AddAllColumns(this.tables[0], this.selectColumns);
            }

            string query = this.CompileQuery();
            return this.InvokeQuery(query);
        }

        private string CompileQuery()
        {
            bool explicitTables = this.tables.Count > 1;

            StringBuilder queryBuilder = new StringBuilder("SELECT");

            for (int i = 0; i < this.selectColumns.Count; i++)
            {
                queryBuilder.AppendFormat(
                    CultureInfo.InvariantCulture,
                    (explicitTables ? "{0} `{1}`.`{2}`" : "{0} `{2}`"),
                    (i > 0 ? "," : String.Empty),
                    this.selectColumns[i].Table.Name,
                    this.selectColumns[i].Column.Name);
            }

            for (int i = 0; i < this.tables.Count; i++)
            {
                queryBuilder.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "{0} `{1}`",
                    (i == 0 ? " FROM" : ","),
                    this.tables[i].Name);
            }

            bool startedWhere = false;
            for (int i = 0; i < this.joinColumns.Count - 1; i += 2)
            {
                queryBuilder.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "{0} `{1}`.`{2}` = `{3}`.`{4}` ",
                    (i == 0 ? " WHERE" : "AND"),
                    this.joinColumns[i].Table,
                    this.joinColumns[i].Column,
                    this.joinColumns[i + 1].Table,
                    this.joinColumns[i + 1].Column);
                startedWhere = true;
            }

            if (this.where != null)
            {
                queryBuilder.Append(startedWhere ? "AND " : " WHERE");
                queryBuilder.Append(this.where);
            }

            for (int i = 0; i < this.orderbyColumns.Count; i++)
            {
                VerifyOrderByColumn(this.orderbyColumns[i]);

                queryBuilder.AppendFormat(
                    CultureInfo.InvariantCulture,
                    (explicitTables ? "{0} `{1}`.`{2}`" : "{0} `{2}`"),
                    (i == 0 ? " ORDER BY" : ","),
                    this.orderbyColumns[i].Table.Name,
                    this.orderbyColumns[i].Column.Name);
            }

            return queryBuilder.ToString();
        }

        private static void VerifyOrderByColumn(TableColumn tableColumn)
        {
            if (tableColumn.Column.Type != typeof(int) &&
                tableColumn.Column.Type != typeof(short))
            {
                throw new NotSupportedException(
                    "Cannot orderby column: " + tableColumn.Column.Name +
                    "; orderby is only supported on integer fields");
            }
        }

        private IEnumerator<T> InvokeQuery(string query)
        {
            TextWriter log = this.db.Log;
            if (log != null)
            {
                log.WriteLine();
                log.WriteLine(query);
            }

            using (View queryView = this.db.OpenView(query))
            {
                if (this.whereParameters != null && this.whereParameters.Count > 0)
                {
                    using (Record paramsRec = this.db.CreateRecord(this.whereParameters.Count))
                    {
                        for (int i = 0; i < this.whereParameters.Count; i++)
                        {
                            paramsRec[i + 1] = this.whereParameters[i];

                            if (log != null)
                            {
                                log.WriteLine("    ? = " + this.whereParameters[i]);
                            }
                        }

                        queryView.Execute(paramsRec);
                    }
                }
                else
                {
                    queryView.Execute();
                }

                foreach (Record resultRec in queryView) using (resultRec)
                {
                    yield return this.GetResult(resultRec);
                }
            }
        }

        private T GetResult(Record resultRec)
        {
            object[] results = new object[this.tables.Count];

            for (int i = 0; i < this.tables.Count; i++)
            {
                string[] values = new string[this.tables[i].Columns.Count];
                for (int j = 0; j < this.selectColumns.Count; j++)
                {
                    TableColumn col = this.selectColumns[j];
                    if (col.Table.Name == this.tables[i].Name)
                    {
                        int index = this.tables[i].Columns.IndexOf(
                            col.Column.Name);
                        if (index >= 0)
                        {
                            if (col.Column.Type == typeof(Stream))
                            {
                                values[index] = "[Binary Data]";
                            }
                            else
                            {
                                values[index] = resultRec.GetString(j + 1);
                            }
                        }
                    }
                }

                QRecord result = (QRecord) this.recordTypes[i]
                    .GetConstructor(Type.EmptyTypes).Invoke(null);
                result.Database = this.db;
                result.TableInfo = this.tables[i];
                result.Values = values;
                result.Exists = true;
                results[i] = result;
            }

            if (this.projectionDelegates.Count > 0)
            {
                object resultsProjection = results[0];
                for (int i = 1; i <= results.Length; i++)
                {
                    if (i < results.Length)
                    {
                        resultsProjection = this.projectionDelegates[i - 1]
                            .DynamicInvoke(new object[] { resultsProjection, results[i] });
                    }
                    else
                    {
                        resultsProjection = this.projectionDelegates[i - 1]
                            .DynamicInvoke(resultsProjection);
                    }
                }

                return (T) resultsProjection;
            }
            else
            {
                return (T) (object) results[0];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>) this).GetEnumerator();
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            Query<TElement> q = new Query<TElement>(this.db, expression);
            q.tables.AddRange(this.tables);
            q.recordTypes.AddRange(this.recordTypes);
            q.selectors.AddRange(this.selectors);
            q.where = this.where;
            q.whereParameters.AddRange(this.whereParameters);
            q.orderbyColumns.AddRange(this.orderbyColumns);
            q.selectColumns.AddRange(this.selectColumns);
            q.joinColumns.AddRange(this.joinColumns);
            q.projectionDelegates.AddRange(this.projectionDelegates);

            MethodCallExpression methodCallExpression = (MethodCallExpression) expression;
            string methodName = methodCallExpression.Method.Name;
            if (methodName == "Select")
            {
                LambdaExpression argumentExpression = (LambdaExpression)
                    ((UnaryExpression) methodCallExpression.Arguments[1]).Operand;
                q.BuildProjection(null, argumentExpression);
            }
            else if (methodName == "Where")
            {
                LambdaExpression argumentExpression = (LambdaExpression)
                    ((UnaryExpression) methodCallExpression.Arguments[1]).Operand;
                q.BuildQuery(null, argumentExpression);
            }
            else if (methodName == "ThenBy")
            {
                LambdaExpression argumentExpression = (LambdaExpression)
                    ((UnaryExpression) methodCallExpression.Arguments[1]).Operand;
                q.BuildSequence(null, argumentExpression);
            }
            else if (methodName == "Join")
            {
                ConstantExpression constantExpression = (ConstantExpression)
                    methodCallExpression.Arguments[1];
                IQueryable inner = (IQueryable) constantExpression.Value;
                q.PerformJoin(
                    null,
                    null,
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

        public IQueryable CreateQuery(Expression expression)
        {
            return this.CreateQuery<T>(expression);
        }

        private static LambdaExpression GetJoinLambda(Expression expression)
        {
            UnaryExpression unaryExpression = (UnaryExpression) expression;
            return (LambdaExpression) unaryExpression.Operand;
        }

        public TResult Execute<TResult>(Expression expression)
        {
            throw new NotSupportedException(
                "Direct method calls not supported -- use AsEnumerable() instead.");
        }

        object IQueryProvider.Execute(Expression expression)
        {
            throw new NotSupportedException(
                "Direct method calls not supported -- use AsEnumerable() instead.");
        }

        public IQueryProvider Provider
        {
            get
            {
                return this;
            }
        }

        public Type ElementType
        {
            get
            {
                return typeof(T);
            }
        }

        public Expression Expression
        {
            get
            {
                return this.queryableExpression;
            }
        }

        internal void BuildQuery(TableInfo tableInfo, LambdaExpression expression)
        {
            if (tableInfo != null)
            {
                this.tables.Add(tableInfo);
                this.recordTypes.Add(typeof(T));
                this.selectors.Add(expression.Parameters[0].Name);
            }

            StringBuilder queryBuilder = new StringBuilder();

            this.ParseQuery(expression.Body, queryBuilder);

            this.where = queryBuilder.ToString();
        }

        internal void BuildNullQuery(TableInfo tableInfo, Type recordType, LambdaExpression expression)
        {
            this.tables.Add(tableInfo);
            this.recordTypes.Add(recordType);
            this.selectors.Add(expression.Parameters[0].Name);
        }

        private void ParseQuery(Expression expression, StringBuilder queryBuilder)
        {
            queryBuilder.Append("(");

            BinaryExpression binaryExpression;
            UnaryExpression unaryExpression;
            MethodCallExpression methodCallExpression;

            if ((binaryExpression = expression as BinaryExpression) != null)
            {
                switch (binaryExpression.NodeType)
                {
                    case ExpressionType.AndAlso:
                        this.ParseQuery(binaryExpression.Left, queryBuilder);
                        queryBuilder.Append(" AND ");
                        this.ParseQuery(binaryExpression.Right, queryBuilder);
                        break;

                    case ExpressionType.OrElse:
                        this.ParseQuery(binaryExpression.Left, queryBuilder);
                        queryBuilder.Append(" OR ");
                        this.ParseQuery(binaryExpression.Right, queryBuilder);
                        break;

                    case ExpressionType.Equal:
                    case ExpressionType.NotEqual:
                    case ExpressionType.GreaterThan:
                    case ExpressionType.LessThan:
                    case ExpressionType.GreaterThanOrEqual:
                    case ExpressionType.LessThanOrEqual:
                        this.ParseQueryCondition(binaryExpression, queryBuilder);
                        break;

                    default:
                        throw new NotSupportedException(
                                  "Expression type not supported: " + binaryExpression.NodeType );
                }
            }
            else if ((unaryExpression = expression as UnaryExpression) != null)
            {
                throw new NotSupportedException(
                    "Expression type not supported: " + unaryExpression.NodeType);
            }
            else if ((methodCallExpression = expression as MethodCallExpression) != null)
            {
                throw new NotSupportedException(
                    "Method call not supported: " + methodCallExpression.Method.Name + "()");
            }
            else
            {
                throw new NotSupportedException(
                    "Query filter expression not supported: " + expression);
            }

            queryBuilder.Append(")");
        }

        private static ExpressionType OppositeExpression(ExpressionType e)
        {
            switch (e)
            {
                case ExpressionType.LessThan:
                    return ExpressionType.GreaterThan;
                case ExpressionType.LessThanOrEqual:
                    return ExpressionType.GreaterThanOrEqual;
                case ExpressionType.GreaterThan:
                    return ExpressionType.LessThan;
                case ExpressionType.GreaterThanOrEqual:
                    return ExpressionType.LessThanOrEqual;
                default:
                    return e;
            }
        }

        private static bool IsIntegerType(Type t)
        {
            return
                t == typeof(sbyte) ||
                t == typeof(byte) ||
                t == typeof(short) ||
                t == typeof(ushort) ||
                t == typeof(int) ||
                t == typeof(uint) ||
                t == typeof(long) ||
                t == typeof(ulong);
        }

        private void ParseQueryCondition(
            BinaryExpression binaryExpression, StringBuilder queryBuilder)
        {
            bool swap;
            string column = this.GetConditionColumn(binaryExpression, out swap);
            queryBuilder.Append(column);

            ExpressionType expressionType = binaryExpression.NodeType;
            if (swap)
            {
                expressionType = OppositeExpression(expressionType);
            }

            LambdaExpression valueExpression = Expression.Lambda(
                swap ? binaryExpression.Left : binaryExpression.Right);
            object value = valueExpression.Compile().DynamicInvoke();

            bool valueIsInt = false;
            if (value != null)
            {
                if (IsIntegerType(value.GetType()))
                {
                    valueIsInt = true;
                }
                else
                {
                    value = value.ToString();
                }
            }

            switch (expressionType)
            {
                case ExpressionType.Equal:
                    if (value == null)
                    {
                        queryBuilder.Append(" IS NULL");
                    }
                    else if (valueIsInt)
                    {
                        queryBuilder.Append(" = ");
                        queryBuilder.Append(value);
                    }
                    else
                    {
                        queryBuilder.Append(" = ?");
                        this.whereParameters.Add(value);
                    }
                    return;

                case ExpressionType.NotEqual:
                    if (value == null)
                    {
                        queryBuilder.Append(" IS NOT NULL");
                    }
                    else if (valueIsInt)
                    {
                        queryBuilder.Append(" <> ");
                        queryBuilder.Append(value);
                    }
                    else
                    {
                        queryBuilder.Append(" <> ?");
                        this.whereParameters.Add(value);
                    }
                    return;
            }

            if (value == null)
            {
                throw new InvalidOperationException(
                    "A null value was used in a greater-than/less-than operation.");
            }

            if (!valueIsInt)
            {
                throw new NotSupportedException(
                    "Greater-than/less-than operators not supported on strings.");
            }

            switch (expressionType)
            {
                case ExpressionType.LessThan:
                    queryBuilder.Append(" < ");
                    break;

                case ExpressionType.LessThanOrEqual:
                    queryBuilder.Append(" <= ");
                    break;

                case ExpressionType.GreaterThan:
                    queryBuilder.Append(" > ");
                    break;

                case ExpressionType.GreaterThanOrEqual:
                    queryBuilder.Append(" >= ");
                    break;

                default:
                    throw new NotSupportedException(
                        "Unsupported query expression type: " + expressionType);
            }

            queryBuilder.Append(value);
        }

        private string GetConditionColumn(
            BinaryExpression binaryExpression, out bool swap)
        {
            MemberExpression memberExpression;
            MethodCallExpression methodCallExpression;

            if (((memberExpression = binaryExpression.Left as MemberExpression) != null) ||
                ((binaryExpression.Left.NodeType == ExpressionType.Convert ||
                  binaryExpression.Left.NodeType == ExpressionType.ConvertChecked) &&
                 (memberExpression = ((UnaryExpression) binaryExpression.Left).Operand
                  as MemberExpression) != null))
            {
                string column = this.GetConditionColumn(memberExpression);
                if (column != null)
                {
                    swap = false;
                    return column;
                }
            }
            else if (((memberExpression = binaryExpression.Right as MemberExpression) != null) ||
                     ((binaryExpression.Right.NodeType == ExpressionType.Convert ||
                       binaryExpression.Right.NodeType == ExpressionType.ConvertChecked) &&
                      (memberExpression = ((UnaryExpression) binaryExpression.Right).Operand
                       as MemberExpression) != null))
            {
                string column = this.GetConditionColumn(memberExpression);
                if (column != null)
                {
                    swap = true;
                    return column;
                }
            }
            else if ((methodCallExpression = binaryExpression.Left as MethodCallExpression) != null)
            {
                string column = this.GetConditionColumn(methodCallExpression);
                if (column != null)
                {
                    swap = false;
                    return column;
                }
            }
            else if ((methodCallExpression = binaryExpression.Right as MethodCallExpression) != null)
            {
                string column = this.GetConditionColumn(methodCallExpression);
                if (column != null)
                {
                    swap = true;
                    return column;
                }
            }

            throw new NotSupportedException(
                "Unsupported binary expression: " + binaryExpression);
        }

        private string GetConditionColumn(MemberExpression memberExpression)
        {
            string columnName = GetColumnName(memberExpression.Member);
            string selectorName = GetConditionSelectorName(memberExpression.Expression);
            string tableName = this.GetConditionTable(selectorName, columnName);
            return this.FormatColumn(tableName, columnName);
        }

        private string GetConditionColumn(MethodCallExpression methodCallExpression)
        {
            LambdaExpression argumentExpression =
                Expression.Lambda(methodCallExpression.Arguments[0]);
            string columnName = (string) argumentExpression.Compile().DynamicInvoke();
            string selectorName = GetConditionSelectorName(methodCallExpression.Object);
            string tableName = this.GetConditionTable(selectorName, columnName);
            return this.FormatColumn(tableName, columnName);
        }

        private static string GetConditionSelectorName(Expression expression)
        {
            ParameterExpression parameterExpression;
            MemberExpression memberExpression;
            if ((parameterExpression = expression as ParameterExpression) != null)
            {
                return parameterExpression.Name;
            }
            else if ((memberExpression = expression as MemberExpression) != null)
            {
                return memberExpression.Member.Name;
            }
            else
            {
                throw new NotSupportedException(
                    "Unsupported conditional selector expression: " + expression);
            }
        }

        private string GetConditionTable(string selectorName, string columnName)
        {
            string tableName = null;

            for (int i = 0; i < this.tables.Count; i++)
            {
                if (this.selectors[i] == selectorName)
                {
                    tableName = this.tables[i].Name;
                    break;
                }
            }

            if (tableName == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture,
                    "Conditional expression contains column {0}.{1} " +
                    "from a table that is not in the query.",
                    selectorName,
                    columnName));
            }

            return tableName;
        }

        private string FormatColumn(string tableName, string columnName)
        {
            if (tableName != null && this.tables.Count > 1)
            {
                return String.Format(CultureInfo.InvariantCulture, "`{0}`.`{1}`", tableName, columnName);
            }
            else
            {
                return String.Format(CultureInfo.InvariantCulture, "`{0}`", columnName);
            }
        }

        private static string GetColumnName(MemberInfo memberInfo)
        {
            foreach (var attr in memberInfo.GetCustomAttributes(
                typeof(DatabaseColumnAttribute), false))
            {
                return ((DatabaseColumnAttribute) attr).Column;
            }

            return memberInfo.Name;
        }

        internal void BuildProjection(TableInfo tableInfo, LambdaExpression expression)
        {
            if (tableInfo != null)
            {
                this.tables.Add(tableInfo);
                this.recordTypes.Add(typeof(T));
                this.selectors.Add(expression.Parameters[0].Name);
            }

            this.FindColumns(expression, this.selectColumns);
            this.projectionDelegates.Add(expression.Compile());
        }

        internal void BuildSequence(TableInfo tableInfo, LambdaExpression expression)
        {
            if (tableInfo != null)
            {
                this.tables.Add(tableInfo);
                this.recordTypes.Add(typeof(T));
                this.selectors.Add(expression.Parameters[0].Name);
            }

            this.FindColumns(expression.Body, this.orderbyColumns);
        }

        private static void AddAllColumns(TableInfo tableInfo, IList<TableColumn> columnList)
        {
            foreach (ColumnInfo column in tableInfo.Columns)
            {
                columnList.Add(new TableColumn(tableInfo, column));
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private void FindColumns(Expression expression, IList<TableColumn> columnList)
        {
            if (expression is ParameterExpression)
            {
                ParameterExpression e = expression as ParameterExpression;
                string selector = e.Name;
                for (int i = 0; i < this.tables.Count; i++)
                {
                    if (this.selectors[i] == selector)
                    {
                        AddAllColumns(this.tables[i], columnList);
                        break;
                    }
                }
            }
            else if (expression.NodeType == ExpressionType.MemberAccess)
            {
                this.FindColumns(expression as MemberExpression, columnList);
            }
            else if (expression is MethodCallExpression)
            {
                this.FindColumns(expression as MethodCallExpression, columnList);
            }
            else if (expression is BinaryExpression)
            {
                BinaryExpression e = expression as BinaryExpression;
                this.FindColumns(e.Left, columnList);
                this.FindColumns(e.Right, columnList);
            }
            else if (expression is UnaryExpression)
            {
                UnaryExpression e = expression as UnaryExpression;
                this.FindColumns(e.Operand, columnList);
            }
            else if (expression is ConditionalExpression)
            {
                ConditionalExpression e = expression as ConditionalExpression;
                this.FindColumns(e.Test, columnList);
                this.FindColumns(e.IfTrue, columnList);
                this.FindColumns(e.IfFalse, columnList);
            }
            else if (expression is InvocationExpression)
            {
                InvocationExpression e = expression as InvocationExpression;
                this.FindColumns(e.Expression, columnList);
                this.FindColumns(e.Arguments, columnList);
            }
            else if (expression is LambdaExpression)
            {
                LambdaExpression e = expression as LambdaExpression;
                this.FindColumns(e.Body, columnList);
            }
            else if (expression is ListInitExpression)
            {
                ListInitExpression e = expression as ListInitExpression;
                this.FindColumns(e.NewExpression, columnList);
                foreach (ElementInit ei in e.Initializers)
                {
                    this.FindColumns(ei.Arguments, columnList);
                }
            }
            else if (expression is MemberInitExpression)
            {
                MemberInitExpression e = expression as MemberInitExpression;
                this.FindColumns(e.NewExpression, columnList);
                foreach (MemberAssignment b in e.Bindings)
                {
                    this.FindColumns(b.Expression, columnList);
                }
            }
            else if (expression is NewExpression)
            {
                NewExpression e = expression as NewExpression;
                this.FindColumns(e.Arguments, columnList);
            }
            else if (expression is NewArrayExpression)
            {
                NewArrayExpression e = expression as NewArrayExpression;
                this.FindColumns(e.Expressions, columnList);
            }
            else if (expression is TypeBinaryExpression)
            {
                TypeBinaryExpression e = expression as TypeBinaryExpression;
                this.FindColumns(e.Expression, columnList);
            }
        }

        private void FindColumns(IEnumerable<Expression> expressions, IList<TableColumn> columnList)
        {
            foreach (Expression expression in expressions)
            {
                this.FindColumns(expression, columnList);
            }
        }

        private void FindColumns(MemberExpression memberExpression, IList<TableColumn> columnList)
        {
            string selector = null;
            MemberExpression objectMemberExpression;
            ParameterExpression objectParameterExpression;
            if ((objectParameterExpression = memberExpression.Expression as
                ParameterExpression) != null)
            {
                selector = objectParameterExpression.Name;
            }
            else if ((objectMemberExpression = memberExpression.Expression as
                MemberExpression) != null)
            {
                selector = objectMemberExpression.Member.Name;
            }

            if (selector != null)
            {
                for (int i = 0; i < this.tables.Count; i++)
                {
                    if (this.selectors[i] == selector)
                    {
                        string columnName = GetColumnName(memberExpression.Member);
                        ColumnInfo column = this.tables[i].Columns[columnName];
                        columnList.Add(new TableColumn(this.tables[i], column));
                        break;
                    }
                }
            }

            selector = memberExpression.Member.Name;
            for (int i = 0; i < this.tables.Count; i++)
            {
                if (this.selectors[i] == selector)
                {
                    AddAllColumns(this.tables[i], columnList);
                    break;
                }
            }
        }

        private void FindColumns(MethodCallExpression methodCallExpression, IList<TableColumn> columnList)
        {
            if (methodCallExpression.Method.Name == "get_Item" &&
                methodCallExpression.Arguments.Count == 1 &&
                methodCallExpression.Arguments[0].Type == typeof(string))
            {
                string selector = null;
                MemberExpression objectMemberExpression;
                ParameterExpression objectParameterExpression;
                if ((objectParameterExpression = methodCallExpression.Object as ParameterExpression) != null)
                {
                    selector = objectParameterExpression.Name;
                }
                else if ((objectMemberExpression = methodCallExpression.Object as MemberExpression) != null)
                {
                    selector = objectMemberExpression.Member.Name;
                }

                if (selector != null)
                {
                    for (int i = 0; i < this.tables.Count; i++)
                    {
                        if (this.selectors[i] == selector)
                        {
                            LambdaExpression argumentExpression =
                                Expression.Lambda(methodCallExpression.Arguments[0]);
                            string columnName = (string)
                                argumentExpression.Compile().DynamicInvoke();
                            ColumnInfo column = this.tables[i].Columns[columnName];
                            columnList.Add(new TableColumn(this.tables[i], column));
                            break;
                        }
                    }
                }
            }

            if (methodCallExpression.Object != null && methodCallExpression.Object.NodeType != ExpressionType.Parameter)
            {
                this.FindColumns(methodCallExpression.Object, columnList);
            }
        }

        internal void PerformJoin(
            TableInfo tableInfo,
            Type recordType,
            IQueryable joinTable,
            LambdaExpression outerKeySelector,
            LambdaExpression innerKeySelector,
            LambdaExpression resultSelector)
        {
            if (joinTable == null)
            {
                throw new ArgumentNullException("joinTable");
            }

            if (tableInfo != null)
            {
                this.tables.Add(tableInfo);
                this.recordTypes.Add(recordType);
                this.selectors.Add(outerKeySelector.Parameters[0].Name);
            }

            PropertyInfo tableInfoProp = joinTable.GetType().GetProperty("TableInfo");
            if (tableInfoProp == null)
            {
                throw new NotSupportedException(
                    "Cannot join with object: " + joinTable.GetType().Name +
                    "; join is only supported on another QTable.");
            }

            TableInfo joinTableInfo = (TableInfo) tableInfoProp.GetValue(joinTable, null);
            if (joinTableInfo == null)
            {
                throw new InvalidOperationException("Missing join table info.");
            }

            this.tables.Add(joinTableInfo);
            this.recordTypes.Add(joinTable.ElementType);
            this.selectors.Add(innerKeySelector.Parameters[0].Name);
            this.projectionDelegates.Add(resultSelector.Compile());

            int joinColumnCount = this.joinColumns.Count;
            this.FindColumns(outerKeySelector.Body, this.joinColumns);
            if (this.joinColumns.Count > joinColumnCount + 1)
            {
                throw new NotSupportedException("Join operations involving " +
                  "multiple columns are not supported.");
            }
            else if (this.joinColumns.Count != joinColumnCount + 1)
            {
                throw new InvalidOperationException("Bad outer key selector for join.");
            }

            this.FindColumns(innerKeySelector.Body, this.joinColumns);
            if (this.joinColumns.Count > joinColumnCount + 2)
            {
                throw new NotSupportedException("Join operations involving " +
                  "multiple columns not are supported.");
            }
            if (this.joinColumns.Count != joinColumnCount + 2)
            {
                throw new InvalidOperationException("Bad inner key selector for join.");
            }
        }
    }

    internal class TableColumn
    {
        public TableColumn(TableInfo table, ColumnInfo column)
        {
            this.Table = table;
            this.Column = column;
        }

        public TableInfo Table { get; set; }
        public ColumnInfo Column { get; set; }
    }
}
