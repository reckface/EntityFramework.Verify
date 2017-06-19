using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EntityFramework.Verify
{

    public class ModelVerification
    {
        public ModelVerification(IEntityTypeRepository typeRepository, ITableFactory tableFactory, int tolerance)
        {
            _tolerance = tolerance;
            _tableFactory = tableFactory;
            NameSpacesToIgnore = Enumerable.Empty<string>();
            _typeRepository = typeRepository;
        }

        private readonly int _tolerance;
        public IEnumerable<string> NameSpacesToIgnore { get; set; }

        public IEnumerable<Summary> Report
        {
            get
            {
                _report = _report ?? GenerateReport();
                return _report.Where(v => v.HasMissingColumns || v.TableMissing);
            }
        }

        
        private TableModel[] _tables = { };
        private IEnumerable<Summary> _report;
        private readonly IEntityTypeRepository _typeRepository;
        private readonly ITableFactory _tableFactory;
        

        /// <summary>
        /// Runs the verification of the model and returns a lookup with all the entities with missing columns
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Summary> GenerateReport()
        {
            var modelValidation = getVerificationResults(5 - _tolerance);
            return modelValidation;
        }

        /// <summary>
        /// Formats the verification results for logging
        /// </summary>
        /// <param name="title">Message title</param>
        /// <param name="database"></param>
        /// <param name="modelValidation"></param>
        /// <returns></returns>
        public static string BuildMessage(string title, string database, IEnumerable<Summary> modelValidation)
        {
            var verificationResults = modelValidation as Summary[] ?? modelValidation.ToArray();
            if (!verificationResults.Any())
                return $"{title} matches the database schema {database}";
            var oneTab = string.Concat(Environment.NewLine, "    ");
            var twoTabs = string.Concat(oneTab, "    ");
            var results = verificationResults
                .Where(v => v.HasMissingColumns)
                .Select(v => $"{v.Entity}: {twoTabs}{string.Join(twoTabs, v.MissingColumns)}");
            return $"{title} - {database} Has some errors: {oneTab}{string.Join(oneTab, results)}";
        }


        /// <summary>
        /// Lists the SQL column names for the table matching the entity name
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="tolerance">How strict should the match be</param>
        /// <returns></returns>
        public IEnumerable<string> GetSqlColumnNames(TableModel table, string entityName, int tolerance)
        {
            return _tableFactory.GetMatchingTableColumns(table, entityName, tolerance);
        }
        
        public IEnumerable<TableModel> GetAllSqlTableNames()
        {
            return _tableFactory.GetTables();
        }

        private IEnumerable<string> GetEntityPropertyNames(Type t)
        {
            return _typeRepository.GetColumns(t, NameSpacesToIgnore);
        }
        

        private static bool containsNamespace(PropertyInfo p, string ns)
        {
            bool contains = p.PropertyType.FullName.Contains(ns);
            return contains;
        }

        private List<Summary> getVerificationResults(int tolerance)
        {
            var properties = new List<Summary>();
            if (string.IsNullOrEmpty(_tableFactory.ConnectionString))
            {
                properties.Add(new Summary
                {
                    Entity = "Invalid Connection",
                    MissingColumns = new List<string>(),
                    TableMissing = true
                });
                return properties;
            }
            var missingTables = new string[] { };
            try
            {
                // List all the table names in the database
                _tables = GetAllSqlTableNames().ToArray();
                // List all the entity types
                var entityTypes = _typeRepository.GetEntityTypes(); // getEntityTypes<T>();

                // List all the entity names
                var entityNames = entityTypes.Select(t => t.Name).Where(t => t != null).ToArray();
                // List tables missing from the database
                missingTables = entityNames.Where(e => !TableExists(e, tolerance)).ToArray();
                // build results for tables that are present
                properties = entityTypes
                    .Where(t => !missingTables.Contains(t.Name))
                    .Select(
                        p => GetSummary(tolerance, p)).
                    ToList();

            }
            catch (Exception ex)
            {
                properties.Add(new Summary
                {
                    Entity = "Database",
                    MissingColumns = new List<string> { ex.Message },
                    TableMissing = true
                });
            }
            // Build list of missing tables - more serious so inserted at top
            if (missingTables.Any())
                properties.Insert(0,
                    new Summary
                    {
                        Entity = "Missing tables",
                        MissingColumns = missingTables,
                        TableMissing = true
                    });
            return properties.ToList();
        }

        private Summary GetSummary(int tolerance, DataEntity p)
        {
            var table = getTableModel(p.Name, tolerance);
            return new Summary(p.Name, table.Database, table.Name, GetEntityPropertyNames(p.Type), GetSqlColumnNames(table, p.Name, tolerance), tolerance);
        }
        
        private bool TableExists(string entity, int tolerance)
        {
            var exists = _tables.Any(t => Compare(tolerance, entity, t.Name));
            return exists;
        }

        private static bool Compare(int tolerance, string entity, string table)
        {
            if (string.IsNullOrEmpty(table))
                return true;
            var exists = entity == table || (table.StartsWith(entity) && Math.Abs(table.Length - entity.Length) <= tolerance);
            return exists;
        }

        private TableModel getTableModel(string entity, int tolerance)
        {
            return _tables
                .OrderBy(t => t.Name)
                .ThenBy(t => !string.IsNullOrEmpty(t.Name) ? t.Name.Length : 0)
                .FirstOrDefault(t => Compare(tolerance, entity, t.Name));
        }
    }
}