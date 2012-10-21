﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Simple.Data.Extensions;

namespace Simple.Data.Ado.Schema
{
    internal class ProcedureCollection : Collection<Procedure>
    {
        private readonly string _defaultSchema;

        public ProcedureCollection()
        {
        }

        public ProcedureCollection(IEnumerable<Procedure> procedures, string defaultSchema) : base(procedures.ToList())
        {
            _defaultSchema = defaultSchema;
        }

        /// <summary>
        /// Finds the procedure with a name most closely matching the specified procedure name.
        /// This method will try an exact match first, then a case-insensitve search, then a pluralized or singular version.
        /// </summary>
        /// <param name="procedureName">Name of the procedure.</param>
        /// <returns>A <see cref="Procedure"/> if a match is found; otherwise, <c>null</c>.</returns>
        public Procedure Find(string procedureName)
        {
            Procedure procedure = FindImpl(procedureName);

            if (procedure == null)
            {
                throw new UnresolvableObjectException(procedureName,
                                                      "No matching procedure found, or insufficient permissions.");
            }

            return procedure;
        }

        public bool IsProcedure(string procedureName)
        {
            try
            {
                return FindImpl(procedureName) != null;
            }
            catch (UnresolvableObjectException)
            {
                return false;
            }
        }

        private Procedure FindImpl(string procedureName)
        {
            if (procedureName.Contains('.'))
            {
                string[] schemaDotprocedure = procedureName.Split('.');
                if (schemaDotprocedure.Length != 2)
                    throw new InvalidOperationException("Could not resolve qualified procedure name.");
                return Find(schemaDotprocedure[1], schemaDotprocedure[0]);
            }
            if (!string.IsNullOrWhiteSpace(_defaultSchema))
            {
                return Find(procedureName, _defaultSchema);
            }
            return FindprocedureWithName(procedureName.Homogenize())
                   ?? FindprocedureWithPluralName(procedureName.Homogenize())
                   ?? FindprocedureWithSingularName(procedureName.Homogenize());
        }

        /// <summary>
        /// Finds the procedure with a name most closely matching the specified procedure name.
        /// This method will try an exact match first, then a case-insensitve search, then a pluralized or singular version.
        /// </summary>
        /// <param name="procedureName">Name of the procedure.</param>
        /// <param name="schemaName"></param>
        /// <returns>A <see cref="Procedure"/> if a match is found; otherwise, <c>null</c>.</returns>
        public Procedure Find(string procedureName, string schemaName)
        {
            Procedure procedure = FindprocedureWithName(procedureName.Homogenize(), schemaName.Homogenize())
                                  ?? FindprocedureWithPluralName(procedureName.Homogenize(), schemaName.Homogenize())
                                  ?? FindprocedureWithSingularName(procedureName.Homogenize(), schemaName.Homogenize());

            if (procedure == null)
            {
                throw new UnresolvableObjectException(schemaName + '.' + procedureName,
                                                      "No matching procedure found, or insufficient permissions.");
            }

            return procedure;
        }

        private Procedure FindprocedureWithSingularName(string procedureName, string schemaName)
        {
            return FindprocedureWithName(procedureName.Singularize(), schemaName);
        }

        private Procedure FindprocedureWithPluralName(string procedureName, string schemaName)
        {
            return FindprocedureWithName(procedureName.Pluralize(), schemaName);
        }

        private Procedure FindprocedureWithName(string procedureName, string schemaName)
        {
            return
                this.SingleOrDefault(
                    sp =>
                    sp.HomogenizedName.Equals(procedureName) &&
                    (sp.Schema == null || sp.Schema.Homogenize().Equals(schemaName)));
        }

        private Procedure FindprocedureWithName(string procedureName)
        {
            return this.SingleOrDefault(t => t.HomogenizedName.Equals(procedureName));
        }

        private Procedure FindprocedureWithPluralName(string procedureName)
        {
            return FindprocedureWithName(procedureName.Pluralize());
        }

        private Procedure FindprocedureWithSingularName(string procedureName)
        {
            if (procedureName.IsPlural())
            {
                return FindprocedureWithName(procedureName.Singularize());
            }

            return null;
        }

        public Procedure Find(ObjectName procedureName)
        {
            return Find(procedureName.Name, procedureName.Schema);
        }
    }
}