﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Simple.Data.Extensions;

namespace Simple.Data.Ado.Schema
{
    internal class TableCollection : Collection<Table>
    {
        public TableCollection()
        {
        }

        public TableCollection(IEnumerable<Table> tables)
            : base(tables.ToList())
        {
        }

        /// <summary>
        /// Finds the Table with a name most closely matching the specified table name.
        /// This method will try an exact match first, then a case-insensitve search, then a pluralized or singular version.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <returns>A <see cref="Table"/> if a match is found; otherwise, <c>null</c>.</returns>
        public Table Find(string tableName)
        {
            if (tableName.Contains('.'))
            {
                string[] schemaDotTable = tableName.Split('.');
                return Find(schemaDotTable[schemaDotTable.Length - 1], schemaDotTable[0]);
            }
            Table table = FindTableWithName(tableName.Homogenize())
                          ?? FindTableWithPluralName(tableName.Homogenize())
                          ?? FindTableWithSingularName(tableName.Homogenize());

            if (table == null)
            {
                throw new UnresolvableObjectException(tableName, "No matching table found, or insufficient permissions.");
            }

            return table;
        }

        /// <summary>
        /// Finds the Table with a name most closely matching the specified table name.
        /// This method will try an exact match first, then a case-insensitve search, then a pluralized or singular version.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="schemaName"></param>
        /// <returns>A <see cref="Table"/> if a match is found; otherwise, <c>null</c>.</returns>
        public Table Find(string tableName, string schemaName)
        {
            Table table = (FindTableWithName(tableName.Homogenize(), schemaName.Homogenize())
                           ?? FindTableWithPluralName(tableName.Homogenize(), schemaName.Homogenize())
                           ?? FindTableWithSingularName(tableName.Homogenize(), schemaName.Homogenize())) ??
                          (FindTableWithName(tableName.Homogenize())
                                                                                                              ?? FindTableWithPluralName(tableName.Homogenize())
                                                                                                              ?? FindTableWithSingularName(tableName.Homogenize()));

            if (table == null)
            {
                throw new UnresolvableObjectException(schemaName + '.' + tableName,
                                                      "No matching table found, or insufficient permissions.");
            }

            return table;
        }

        private Table FindTableWithSingularName(string tableName, string schemaName)
        {
            return FindTableWithName(tableName.Singularize(), schemaName);
        }

        private Table FindTableWithPluralName(string tableName, string schemaName)
        {
            return FindTableWithName(tableName.Pluralize(), schemaName);
        }

        private Table FindTableWithName(string tableName, string schemaName)
        {
            return this.SingleOrDefault(t => t.HomogenizedName.Equals(tableName) &&
                                             (t.Schema == null || t.Schema.Homogenize().Equals(schemaName)));
        }

        private Table FindTableWithName(string tableName)
        {
            return this.SingleOrDefault(t => t.HomogenizedName.Equals(tableName));
        }

        private Table FindTableWithPluralName(string tableName)
        {
            return FindTableWithName(tableName.Pluralize());
        }

        private Table FindTableWithSingularName(string tableName)
        {
            if (tableName.IsPlural())
            {
                return FindTableWithName(tableName.Singularize());
            }

            return null;
        }

        public Table Find(ObjectName tableName)
        {
            return Find(tableName.Name, tableName.Schema);
        }
    }
}