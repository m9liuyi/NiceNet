﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using NiceNet.Common;
using NiceNet.Data.Entity.Context;
using NiceNet.DataAcessLayer.Interface;
using NiceNet.QueryParameter;
using Z.BulkOperations;

namespace NiceNet.DataAcessLayer
{
    public class BaseDal<E> : IBaseDal<E> where E : BaseEntity
    {
        public XYFContext Context { get; set; }

        #region IBaseDal<E> Members

        public List<E> List<M>(M queryParameter) where M : BaseQueryParameters
        {
            var wheres = Extension.QueryParameterToWhere<E, M>(queryParameter);

            IQueryable<E> iQueryable = Context.Set<E>();
            if (wheres != null)
            {
                iQueryable = iQueryable.Where(wheres);
            }

            var result = iQueryable.HandlePagination<E, M>(queryParameter).ToList();

            return result;
        }

        public List<E> BulkInsert(List<E> entities)
        {
            List<AuditEntry> auditEntries = new List<AuditEntry>();

            Context.BulkInsert(entities, options =>
            {
                options.UseAudit = true;
                options.BulkOperationExecuted = bulkOperation => auditEntries.AddRange(bulkOperation.AuditEntries);
            });

            return auditEntries.AuditEntryToEntity<E>();
        }

        public List<E> BulkUpdate(List<E> entities)
        {
            List<AuditEntry> auditEntries = new List<AuditEntry>();

            Context.BulkUpdate(entities, options =>
            {
                options.UseAudit = true;
                options.BulkOperationExecuted = bulkOperation => auditEntries.AddRange(bulkOperation.AuditEntries);
            });

            return auditEntries.AuditEntryToEntity<E>();
        }

        public List<E> BulkUpdate<M>(M query, Dictionary<string, object> columnValues)
            where M : BaseQueryParameters
        {
            var wheres = Extension.QueryParameterToWhere<E, M>(query);

            List<AuditEntry> auditEntries = new List<AuditEntry>();

            Context.Set<E>()
                .Where<E>(wheres)
                .UpdateFromQuery<E>(p => Extension.PrepareEntity4UpdateFromQuery<E>(columnValues));

            return auditEntries.AuditEntryToEntity<E>();
        }

        public List<E> BulkRemove(List<E> entities)
        {
            List<AuditEntry> auditEntries = new List<AuditEntry>();

            Context.BulkDelete(entities, options =>
            {
                options.UseAudit = true;
                options.BulkOperationExecuted = bulkOperation => auditEntries.AddRange(bulkOperation.AuditEntries);
            });

            return auditEntries.AuditEntryToEntity<E>();
        }

        public List<E> BulkRemoveById<M>(M query)
            where M : BaseQueryParameters
        {
            if (query == null
                || !(query.PrimaryKey == null || (query.PrimaryKeys == null || query.PrimaryKeys.Count() == 0)))
            {
                return new List<E>();
            }

            List<AuditEntry> auditEntries = new List<AuditEntry>();

            var wheres = Extension.QueryParameterToWhere<E, M>(query);

            Context.Set<E>().Where(wheres).DeleteFromQuery<E>(options =>
            {
                options.UseAudit = true;
                options.BulkOperationExecuted = bulkOperation => auditEntries.AddRange(bulkOperation.AuditEntries);
            });

            return auditEntries.AuditEntryToEntity<E>();
        }

        public List<E> BulkDelete(List<E> entities)
        {
            List<AuditEntry> auditEntries = new List<AuditEntry>();

            Context.BulkUpdate(entities, options =>
            {
                options.UseAudit = true;
                options.BulkOperationExecuted = bulkOperation => auditEntries.AddRange(bulkOperation.AuditEntries);
            });

            return auditEntries.AuditEntryToEntity<E>();
        }

        public List<E> BulkDeleteById<M>(M query) where M : BaseQueryParameters
        {

            if (query == null 
                || !(query.PrimaryKey == null || (query.PrimaryKeys == null || query.PrimaryKeys.Count() == 0)))
            {
                return new List<E>();
            }

            var columnValues = new Dictionary<string, object>();
            columnValues.Add("IsDeleted", true);

            return this.BulkUpdate<M>(query, columnValues);
        }
        
        #endregion
    }
}
