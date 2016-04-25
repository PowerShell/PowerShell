//---------------------------------------------------------------------
// <copyright file="DatabaseQuery.cs" company="Microsoft Corporation">
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
    using System.Globalization;

    internal partial class Database
    {
        /// <summary>
        /// Gets a View object representing the query specified by a SQL string.
        /// </summary>
        /// <param name="sqlFormat">SQL query string, which may contain format items</param>
        /// <param name="args">Zero or more objects to format</param>
        /// <returns>A View object representing the query specified by a SQL string</returns>
        /// <exception cref="BadQuerySyntaxException">the SQL syntax is invalid</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// The <paramref name="sqlFormat"/> parameter is formatted using <see cref="String.Format(string,object[])"/>.
        /// </p><p>
        /// The View object should be <see cref="InstallerHandle.Close"/>d after use.
        /// It is best that the handle be closed manually as soon as it is no longer
        /// needed, as leaving lots of unused handles open can degrade performance.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidatabaseopenview.asp">MsiDatabaseOpenView</a>
        /// </p></remarks>
        public View OpenView(string sqlFormat, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(sqlFormat))
            {
                throw new ArgumentNullException("sqlFormat");
            }

            string sql = (args == null || args.Length == 0 ? sqlFormat :
                String.Format(CultureInfo.InvariantCulture, sqlFormat, args));
            int viewHandle;
            uint ret = RemotableNativeMethods.MsiDatabaseOpenView((int) this.Handle, sql, out viewHandle);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }

            return new View((IntPtr) viewHandle, sql, this);
        }

        /// <summary>
        /// Executes the query specified by a SQL string.  The query may not be a SELECT statement.
        /// </summary>
        /// <param name="sqlFormat">SQL query string, which may contain format items</param>
        /// <param name="args">Zero or more objects to format</param>
        /// <exception cref="BadQuerySyntaxException">the SQL syntax is invalid</exception>
        /// <exception cref="InstallerException">the View could not be executed</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// The <paramref name="sqlFormat"/> parameter is formatted using
        /// <see cref="String.Format(string,object[])"/>.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidatabaseopenview.asp">MsiDatabaseOpenView</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewexecute.asp">MsiViewExecute</a>
        /// </p></remarks>
        public void Execute(string sqlFormat, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(sqlFormat))
            {
                throw new ArgumentNullException("sqlFormat");
            }

            this.Execute(
                args == null || args.Length == 0 ?
                    sqlFormat : String.Format(CultureInfo.InvariantCulture, sqlFormat, args),
                (Record) null);
        }

        /// <summary>
        /// Executes the query specified by a SQL string.  The query may not be a SELECT statement.
        /// </summary>
        /// <param name="sql">SQL query string</param>
        /// <param name="record">Optional Record object containing the values that replace
        /// the parameter tokens (?) in the SQL query.</param>
        /// <exception cref="BadQuerySyntaxException">the SQL syntax is invalid</exception>
        /// <exception cref="InstallerException">the View could not be executed</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidatabaseopenview.asp">MsiDatabaseOpenView</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewexecute.asp">MsiViewExecute</a>
        /// </p></remarks>
        public void Execute(string sql, Record record)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentNullException("sql");
            }

            using (View view = this.OpenView(sql))
            {
                view.Execute(record);
            }
        }

        /// <summary>
        /// Executes the specified SQL SELECT query and returns all results.
        /// </summary>
        /// <param name="sqlFormat">SQL query string, which may contain format items</param>
        /// <param name="args">Zero or more objects to format</param>
        /// <returns>All results combined into an array</returns>
        /// <exception cref="BadQuerySyntaxException">the SQL syntax is invalid</exception>
        /// <exception cref="InstallerException">the View could not be executed</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// The <paramref name="sqlFormat"/> parameter is formatted using
        /// <see cref="String.Format(string,object[])"/>.
        /// </p><p>
        /// Multiple rows columns will be collapsed into a single one-dimensional list.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidatabaseopenview.asp">MsiDatabaseOpenView</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewexecute.asp">MsiViewExecute</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewfetch.asp">MsiViewFetch</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public IList ExecuteQuery(string sqlFormat, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(sqlFormat))
            {
                throw new ArgumentNullException("sqlFormat");
            }

            return this.ExecuteQuery(
                args == null || args.Length == 0 ?
                    sqlFormat : String.Format(CultureInfo.InvariantCulture, sqlFormat, args),
                (Record) null);
        }

        /// <summary>
        /// Executes the specified SQL SELECT query and returns all results.
        /// </summary>
        /// <param name="sql">SQL SELECT query string</param>
        /// <param name="record">Optional Record object containing the values that replace
        /// the parameter tokens (?) in the SQL query.</param>
        /// <returns>All results combined into an array</returns>
        /// <exception cref="BadQuerySyntaxException">the SQL syntax is invalid</exception>
        /// <exception cref="InstallerException">the View could not be executed</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// Multiple rows columns will be collapsed into a single one-dimensional list.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidatabaseopenview.asp">MsiDatabaseOpenView</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewexecute.asp">MsiViewExecute</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewfetch.asp">MsiViewFetch</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public IList ExecuteQuery(string sql, Record record)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentNullException("sql");
            }

            using (View view = this.OpenView(sql))
            {
                view.Execute(record);
                IList results = new ArrayList();
                int fieldCount = 0;

                foreach (Record rec in view) using (rec)
                {
                    if (fieldCount == 0) fieldCount = rec.FieldCount;
                    for (int i = 1; i <= fieldCount; i++)
                    {
                        results.Add(rec[i]);
                    }
                }

                return results;
            }
        }

        /// <summary>
        /// Executes the specified SQL SELECT query and returns all results as integers.
        /// </summary>
        /// <param name="sqlFormat">SQL query string, which may contain format items</param>
        /// <param name="args">Zero or more objects to format</param>
        /// <returns>All results combined into an array</returns>
        /// <exception cref="BadQuerySyntaxException">the SQL syntax is invalid</exception>
        /// <exception cref="InstallerException">the View could not be executed</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// The <paramref name="sqlFormat"/> parameter is formatted using
        /// <see cref="String.Format(string,object[])"/>.
        /// </p><p>
        /// Multiple rows columns will be collapsed into a single one-dimensional list.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidatabaseopenview.asp">MsiDatabaseOpenView</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewexecute.asp">MsiViewExecute</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewfetch.asp">MsiViewFetch</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public IList<int> ExecuteIntegerQuery(string sqlFormat, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(sqlFormat))
            {
                throw new ArgumentNullException("sqlFormat");
            }

            return this.ExecuteIntegerQuery(
                args == null || args.Length == 0 ?
                    sqlFormat : String.Format(CultureInfo.InvariantCulture, sqlFormat, args),
                (Record) null);
        }

        /// <summary>
        /// Executes the specified SQL SELECT query and returns all results as integers.
        /// </summary>
        /// <param name="sql">SQL SELECT query string</param>
        /// <param name="record">Optional Record object containing the values that replace
        /// the parameter tokens (?) in the SQL query.</param>
        /// <returns>All results combined into an array</returns>
        /// <exception cref="BadQuerySyntaxException">the SQL syntax is invalid</exception>
        /// <exception cref="InstallerException">the View could not be executed</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// Multiple rows columns will be collapsed into a single one-dimensional list.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidatabaseopenview.asp">MsiDatabaseOpenView</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewexecute.asp">MsiViewExecute</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewfetch.asp">MsiViewFetch</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public IList<int> ExecuteIntegerQuery(string sql, Record record)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentNullException("sql");
            }

            using (View view = this.OpenView(sql))
            {
                view.Execute(record);
                IList<int> results = new List<int>();
                int fieldCount = 0;

                foreach (Record rec in view) using (rec)
                {
                    if (fieldCount == 0) fieldCount = rec.FieldCount;
                    for (int i = 1; i <= fieldCount; i++)
                    {
                        results.Add(rec.GetInteger(i));
                    }
                }

                return results;
            }
        }

        /// <summary>
        /// Executes the specified SQL SELECT query and returns all results as strings.
        /// </summary>
        /// <param name="sqlFormat">SQL query string, which may contain format items</param>
        /// <param name="args">Zero or more objects to format</param>
        /// <returns>All results combined into an array</returns>
        /// <exception cref="BadQuerySyntaxException">the SQL syntax is invalid</exception>
        /// <exception cref="InstallerException">the View could not be executed</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// The <paramref name="sqlFormat"/> parameter is formatted using
        /// <see cref="String.Format(string,object[])"/>.
        /// </p><p>
        /// Multiple rows columns will be collapsed into a single on-dimensional list.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidatabaseopenview.asp">MsiDatabaseOpenView</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewexecute.asp">MsiViewExecute</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewfetch.asp">MsiViewFetch</a>
        /// </p></remarks>
        public IList<string> ExecuteStringQuery(string sqlFormat, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(sqlFormat))
            {
                throw new ArgumentNullException("sqlFormat");
            }

            return this.ExecuteStringQuery(
                args == null || args.Length == 0 ?
                    sqlFormat : String.Format(CultureInfo.InvariantCulture, sqlFormat, args),
                (Record) null);
        }

        /// <summary>
        /// Executes the specified SQL SELECT query and returns all results as strings.
        /// </summary>
        /// <param name="sql">SQL SELECT query string</param>
        /// <param name="record">Optional Record object containing the values that replace
        /// the parameter tokens (?) in the SQL query.</param>
        /// <returns>All results combined into an array</returns>
        /// <exception cref="BadQuerySyntaxException">the SQL syntax is invalid</exception>
        /// <exception cref="InstallerException">the View could not be executed</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// Multiple rows columns will be collapsed into a single on-dimensional list.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidatabaseopenview.asp">MsiDatabaseOpenView</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewexecute.asp">MsiViewExecute</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewfetch.asp">MsiViewFetch</a>
        /// </p></remarks>
        public IList<string> ExecuteStringQuery(string sql, Record record)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentNullException("sql");
            }

            using (View view = this.OpenView(sql))
            {
                view.Execute(record);
                IList<string> results = new List<string>();
                int fieldCount = 0;

                foreach (Record rec in view) using (rec)
                {
                    if (fieldCount == 0) fieldCount = rec.FieldCount;
                    for (int i = 1; i <= fieldCount; i++)
                    {
                        results.Add(rec.GetString(i));
                    }
                }

                return results;
            }
        }

        /// <summary>
        /// Executes the specified SQL SELECT query and returns a single result.
        /// </summary>
        /// <param name="sqlFormat">SQL query string, which may contain format items</param>
        /// <param name="args">Zero or more objects to format</param>
        /// <returns>First field of the first result</returns>
        /// <exception cref="BadQuerySyntaxException">the SQL syntax is invalid</exception>
        /// <exception cref="InstallerException">the View could not be executed
        /// or the query returned 0 results</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// The <paramref name="sqlFormat"/> parameter is formatted using
        /// <see cref="String.Format(string,object[])"/>.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidatabaseopenview.asp">MsiDatabaseOpenView</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewexecute.asp">MsiViewExecute</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewfetch.asp">MsiViewFetch</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public object ExecuteScalar(string sqlFormat, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(sqlFormat))
            {
                throw new ArgumentNullException("sqlFormat");
            }

            return this.ExecuteScalar(
                args == null || args.Length == 0 ?
                    sqlFormat : String.Format(CultureInfo.InvariantCulture, sqlFormat, args),
                (Record) null);
        }

        /// <summary>
        /// Executes the specified SQL SELECT query and returns a single result.
        /// </summary>
        /// <param name="sql">SQL SELECT query string</param>
        /// <param name="record">Optional Record object containing the values that replace
        /// the parameter tokens (?) in the SQL query.</param>
        /// <returns>First field of the first result</returns>
        /// <exception cref="BadQuerySyntaxException">the SQL syntax is invalid</exception>
        /// <exception cref="InstallerException">the View could not be executed
        /// or the query returned 0 results</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidatabaseopenview.asp">MsiDatabaseOpenView</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewexecute.asp">MsiViewExecute</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiviewfetch.asp">MsiViewFetch</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public object ExecuteScalar(string sql, Record record)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentNullException("sql");
            }

            View view = this.OpenView(sql);
            Record rec = null;
            try
            {
                view.Execute(record);
                rec = view.Fetch();
                if (rec == null)
                {
                    throw InstallerException.ExceptionFromReturnCode((uint) NativeMethods.Error.NO_MORE_ITEMS);
                }
                return rec[1];
            }
            finally
            {
                if (rec != null) rec.Close();
                view.Close();
            }
        }
    }
}
