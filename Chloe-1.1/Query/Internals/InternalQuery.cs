﻿using Chloe.Core;
using Chloe.Core.Database;
using Chloe.Database;
using Chloe.Infrastructure;
using Chloe.Mapper;
using Chloe.Query.Mapping;
using Chloe.Query.QueryState;
using Chloe.Query.Visitors;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;

namespace Chloe.Query.Internals
{
    public class InternalQuery<T> : IEnumerable<T>, IEnumerable
    {
        IQuery<T> _query;
        InternalDbSession _dbSession;
        IDbServiceProvider _dbServiceProvider;

        internal InternalQuery(IQuery<T> query, InternalDbSession dbSession, IDbServiceProvider dbServiceProvider)
        {
            this._query = query;
            this._dbSession = dbSession;
            this._dbServiceProvider = dbServiceProvider;
        }

        DbCommandFactor GenerateCommandFactor()
        {
            DbExpressionVisitorBase visitor = this._dbServiceProvider.CreateDbExpressionVisitor();
            IQueryState qs = QueryExpressionReducer.ReduceQueryExpression(this._query.QueryExpression);
            MappingData data = qs.GenerateMappingData();
            ISqlState sqlState = data.SqlQuery.Accept(visitor);

            IObjectActivtor objectActivtor = data.MappingEntity.CreateObjectActivtor();
            string cmdText = sqlState.ToSql();
            IDictionary<string, object> parameters = visitor.ParameterStorage;

            DbCommandFactor commandFactor = new DbCommandFactor(objectActivtor, cmdText, parameters);
            return commandFactor;
        }

        public IEnumerator<T> GetEnumerator()
        {
            DbCommandFactor commandFactor = this.GenerateCommandFactor();

#if DEBUG
            Debug.WriteLine(commandFactor.CommandText);
#endif

            var enumerator = QueryEnumeratorCreator.CreateEnumerator<T>(this._dbSession, commandFactor);
            return enumerator;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public override string ToString()
        {
            DbCommandFactor commandFactor = this.GenerateCommandFactor();
            return commandFactor.CommandText;
        }
    }

    public class InternalQueryHelper
    {
        public static InternalQuery<T> CreateQuery<T>(IQuery<T> query, IDbConnection conn)
        {
            return new InternalQuery<T>(query, new InternalDbSession(conn), null);
        }
    }
}
