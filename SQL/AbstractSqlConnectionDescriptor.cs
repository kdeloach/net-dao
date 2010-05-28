// Copyright (c) 2004-2010 Azavea, Inc.
// 
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using Azavea.Open.DAO.Util;

namespace Azavea.Open.DAO.SQL
{
    /// <summary>
    /// This class represents the information needed to establish a connection to a data
    /// source that speaks SQL (presumably a database).
    /// 
    /// This class, and any that extend it, should be thread safe.
    /// </summary>
    public abstract class AbstractSqlConnectionDescriptor : ConnectionDescriptor, ITransactionalConnectionDescriptor
    {
        /// <summary>
        /// Returns the appropriate data access layer for this connection.  The default
        /// implementation returns a normal SQL data access layer, but this may be
        /// overridden in particular DB connection descriptors.
        /// </summary>
        public override IDaLayer CreateDataAccessLayer()
        {
            return new SqlDaJoinableLayer(this, true);
        }

        /// <summary>
        /// Begins the transaction.  Returns a NEW ConnectionDescriptor that you should
        /// use for operations you wish to be part of the transaction.
        /// 
        /// NOTE: You MUST call Commit or Rollback on the returned ITransaction when you are done.
        /// </summary>
        /// <returns>The ConnectionDescriptor object to pass to calls that you wish to have
        ///          happen as part of this transaction.</returns>
        public ITransaction BeginTransaction()
        {
            return new SqlTransaction(this);
        }

        /// <summary>
        /// Returns a modulus sql string, something like "columnName % value", except
        /// that the syntax is DB-specific.  Throws NotImplementedException if not yet supported
        /// for a particular type of connection.
        /// </summary>
        /// <param name="columnName">The column used in the clause.</param>
        /// <returns>The pieces of SQL that go before and after the value you wish to mod by.</returns>
        public virtual SqlClauseWithValue MakeModulusClause(string columnName)
        {
            throw new NotImplementedException("Modulus is not supported for this connection: " + this);
        }

        /// <summary>
        /// Since different databases have different ideas of what a sequence is, this
        /// allows the utility class to support sequences across all different DBs.
        /// </summary>
        /// <param name="sequenceName">The name of the sequence we're getting an ID from.</param>
        /// <returns>A sql string that will retrieve a sequence integer value (I.E. something like
        ///          "SELECT NEXTVAL FROM sequenceName")</returns>
        public virtual string MakeSequenceValueQuery(string sequenceName)
        {
            throw new NotImplementedException("Sequences are not supported for this connection: " + this);
        }

        /// <summary>
        /// Returns a 'bitwise and' sql string, something like "columnName &amp; value", except
        /// that the syntax is DB-specific.  Throws NotImplementedException if not yet supported
        /// for a particular type of connection.
        /// </summary>
        /// <param name="columnName">The column used in the clause.</param>
        /// <returns>The pieces of SQL that go before and after the value you wish to AND by.</returns>
        public virtual SqlClauseWithValue MakeBitwiseAndClause(string columnName)
        {
            throw new NotImplementedException("Bitwise and is not supported for this connection: " + this);
        }

        /// <summary>
        /// Gets the last id generated in an ID column for a table.  Some databases can do this
        /// more efficiently or correctly than the default way ("select max idcol from table").
        /// </summary>
        /// <param name="tableName">The table the ID column belongs to.</param>
        /// <param name="idCol">The ID column for which to get the last generated ID value.</param>
        public virtual string MakeLastAutoGeneratedIdQuery(string tableName, string idCol)
        {
            StringBuilder sql = DbCaches.StringBuilders.Get();
            sql.Append("SELECT MAX(");
            sql.Append(idCol);
            sql.Append(") FROM ");
            sql.Append(tableName);
            string retVal = sql.ToString();
            DbCaches.StringBuilders.Return(sql);
            return retVal;
        }

        /// <summary>
        /// Returns the SQL statement to create an index on a table.  There may be
        /// database-specific additional keywords required (such as "COMPUTE STATISTICS").
        /// The default implementation returns a simple standard-sql create index statement.
        /// </summary>
        /// <param name="indexName">Name of the index to create.</param>
        /// <param name="isUnique">Is this a unique index?</param>
        /// <param name="tableName">What table to create the index on.</param>
        /// <param name="columnNames">The columns included in the index.</param>
        public virtual string MakeCreateIndexCommand(string indexName,
                                                                 bool isUnique, string tableName,
                                                                 IEnumerable<string> columnNames)
        {
            StringBuilder sql = DbCaches.StringBuilders.Get();
            sql.Append("CREATE ");
            if (isUnique)
            {
                sql.Append("UNIQUE ");
            }
            sql.Append("INDEX ");
            sql.Append(indexName);
            sql.Append(" ON ");
            sql.Append(tableName);
            sql.Append(" (");
            bool first = true;
            foreach (string colName in columnNames)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sql.Append(",");
                }
                sql.Append(colName);
            }
            sql.Append(") ");
            string retVal = sql.ToString();
            DbCaches.StringBuilders.Return(sql);
            return retVal;
        }

        /// <summary>
        /// Nearly every DB in the universe supports truncate, but a few (cough Access cough)
        /// do not.
        /// </summary>
        /// <returns>True if "truncate table blah" will work, false if you need to do
        ///          "delete from table blah" instead (which will be slower).</returns>
        public virtual bool SupportsTruncate()
        {
            return true;
        }

        /// <summary>
        /// Depending on the database and connection info, it may not always be appropriate to
        /// pool connections.  This allows the connection descriptor to decide.
        /// </summary>
        /// <returns>True if connection pooling should be used, false otherwise.</returns>
        public abstract bool UsePooling();

        /// <summary>
        /// This method returns a database connection to the database specified by
        /// this connection descriptor.  This is not meant to be called by client
        /// code, only by the utilities in the Azavea.Database assembly.
        /// </summary>
        /// <returns>An IDbConnection of the appropriate type (OleDb, Sql Server native, etc).</returns>
        public abstract DbConnection CreateNewConnection();

        /// <summary>
        /// Each driver seems to have its own way of marking parameters ("?", ":param", "@PARAM", etc).
        /// So, the database utilities class always uses "?" and relies on the specific descriptor to
        /// replace the ? with the appropriate names in the SQL, and also to set the parameters on
        /// the command object.
        /// </summary>
        /// <param name="cmd">Database command (with .Text already populated with sql) that needs
        ///                   the parameters set on it.</param>
        /// <param name="parameters">The parameter values, in the order that the ?'s appear in the
        ///                          command's text.  This collection should not be null.</param>
        public abstract void SetParametersOnCommand(IDbCommand cmd, IEnumerable parameters);

        /// <summary>
        /// This method returns a DbDataAdapter that can be used to fill DataSets.
        /// </summary>
        /// <param name="cmd">The command that the adapter will be executing.</param>
        /// <returns>A DbDataAdapter of the appropriate type (OleDb, native, etc).</returns>
        public abstract DbDataAdapter CreateNewAdapter(IDbCommand cmd);

        /// <summary>
        /// Does this database require that we alias the columns explicitly if we are
        /// aliasing the table name?  Most DBs will alias the columns for you (I.E. if you
        /// "SELECT ID FROM TABLE1 AS ALIAS1" then the column will be called "ALIAS1.ID", etc).
        /// However some require that you alias the columns specifically (cough SQLite cough).
        /// </summary>
        /// <returns>True if aliasing the table names is not enough.</returns>
        public abstract bool NeedToAliasColumns();

        /// <summary>
        /// Does the database require the "AS" keyword for aliasing a column?
        /// Most do not, but some (MS Access) do.
        /// </summary>
        /// <returns>True if "AS" is required when aliasing a column.</returns>
        public abstract bool NeedAsForColumnAliases();

        /// <summary>
        /// Some databases want the " AS " keyword, some want the alias in quotes
        /// (cough SQLite cough), or square brackets (cough Microsoft cough), or 
        /// whatever.  This provides the database-specific stuff that comes before
        /// the alias.
        /// </summary>
        /// <returns>The keyword, quotes, brackets, etc used before the alias
        ///          when aliasing a column.</returns>
        public abstract string ColumnAliasPrefix();

        /// <summary>
        /// Some databases want the " AS " keyword, some want the alias in quotes
        /// (cough SQLite cough), or square brackets (cough Microsoft cough), or 
        /// whatever.  This provides the database-specific stuff that comes after
        /// the alias.
        /// </summary>
        /// <returns>The keyword, quotes, brackets, etc used after the alias
        ///          when aliasing a column.</returns>
        public abstract string ColumnAliasSuffix();

        /// <summary>
        /// Some databases (cough MS Access cough) include the prefix/suffix
        /// around the alias in the result "column" names.  
        /// </summary>
        /// <returns>Whether to expect quotes/brackets/etc around the 
        ///          aliased column names in the DataReader.</returns>
        public virtual bool ColumnAliasWrappersInResults()
        {
            return false;
        }

        /// <summary>
        /// Some databases want the " AS " keyword, some don't (cough Oracle cough).
        /// This provides the database-specific stuff that comes before the alias.
        /// </summary>
        /// <returns>The keyword, quotes, brackets, etc used before the alias
        ///          when aliasing a table.</returns>
        public abstract string TableAliasPrefix();

        /// <summary>
        /// Some databases want the " AS " keyword, some don't (cough Oracle cough).
        /// This provides the database-specific stuff that comes after the alias.
        /// </summary>
        /// <returns>The keyword, quotes, brackets, etc used after the alias
        ///          when aliasing a table.</returns>
        public abstract string TableAliasSuffix();

        /// <summary>
        /// Not all databases have the same syntax for full outer joins.  Most use
        /// "FULL OUTER JOIN" but some do not.
        /// </summary>
        /// <returns>The key word (or words) that mean "OUTER JOIN".</returns>
        public virtual string FullOuterJoinKeyword()
        {
            return "FULL OUTER JOIN";
        }

        /// <summary>
        /// UPPER and LOWER are not actually consistent across databases.
        /// </summary>
        /// <returns>The function name that converts a string to lower case.</returns>
        public virtual string LowerCaseFunction()
        {
            return "LOWER";
        }
    }
}