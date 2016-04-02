using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace EntityFramework.Verify
{
    public class ModelVerification<TModel>
    {
        public ModelVerification(string connectionString, int tolerance)
        {
            _tolerance = tolerance;
            _connectionString = connectionString;
            Database = new SqlConnectionStringBuilder(_connectionString).InitialCatalog;
            NameSpacesToIgnore = Enumerable.Empty<string>();
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

        private readonly string _connectionString;
        private string[] _tables = { };
        private IEnumerable<Summary> _report;

        public string Database { get; }

        /// <summary>
        /// Runs the verification of the model and returns a lookup with all the entities with missing columns
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Summary> GenerateReport()
        {
            var modelValidation = getVerificationResults<TModel>(5 - _tolerance);
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
        public IEnumerable<string> GetSqlColumnNames(string entityName, int tolerance)
        {
            const string query = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @Table ";
            var table = getTableName(entityName, tolerance);
            if (string.IsNullOrEmpty(table))
            {
                throw new InvalidOperationException($"Table '{entityName}' does not exist in database: {Database}");
            }

            return GetList(query, new Dictionary<string, object>
            {
                {"Table", table }
            });
        }

        public IEnumerable<string> GetList(string query, Dictionary<string, object> filters = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var cmd = new SqlCommand(query, connection);

                if (filters != null)
                    cmd.Parameters.AddRange(filters.Select(f => new SqlParameter(f.Key, f.Value)).ToArray());
                connection.Open();
                //cmd.CommandType = CommandType.Text;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Create a Favorites instance
                        var item = reader[0].ToString();
                        // ... etc ...
                        yield return item;
                    }
                }
            }
        }
        public IEnumerable<string> GetAllSqlTableNames()
        {
            const string query = "SELECT TABLE_NAME  FROM INFORMATION_SCHEMA.TABLES";
            return GetList(query);
        }

        private IEnumerable<string> GetEntityPropertyNames(Type t)
        {
            var properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(
                    p =>
                        IsEfProperty(p) &&
                        !containsNamespaces(p) &&
                        // to avoid navigation properties
                        !p.PropertyType.FullName.Contains("Collection"))
                .Select((p, i) => new
                {
                    Index = i,
                    Property = p,
                    p.Name,
                    // We can use the index and other things later if we decide to
                })
                .ToArray();
            // Also these conditions excludes navigation properties: !p.PropertyType.FullName.Contains("AWM.Data") && !p.PropertyType.FullName.Contains("Collection")
            return properties.Select(p => p.Name);//.ToArray();
        }

        public static bool IsEfProperty(PropertyInfo prop)
        {
            if (!prop.CanWrite || !prop.CanRead || prop.GetSetMethod() == null)
                return false;

            return prop.DeclaringType != null && prop.DeclaringType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                                     .Any(f => f.Name.Contains("<" + prop.Name + ">"));
        }

        private bool containsNamespaces(PropertyInfo p)
        {
            return NameSpacesToIgnore.Any(n => containsNamespace(p, n));
        }

        private static bool containsNamespace(PropertyInfo p, string ns)
        {
            bool contains = p.PropertyType.FullName.Contains(ns);
            return contains;
        }

        private List<Summary> getVerificationResults<T>(int tolerance)
        {
            var properties = new List<Summary>();
            if (string.IsNullOrEmpty(_connectionString))
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
                var entityTypes = getEntityTypes<T>();

                // List all the entity names
                var entityNames = entityTypes.Select(t => t.Name).Where(t => t != null).ToArray();
                // List tables missing from the database
                missingTables = entityNames.Where(e => !TableExists(e, tolerance)).ToArray();
                // build results for tables that are present
                properties = entityTypes
                    .Where(t => !missingTables.Contains(t.Name))
                    .Select(
                        p => new Summary(p.Name, Database, getTableName(p.Name, tolerance), GetEntityPropertyNames(p), GetSqlColumnNames(p.Name, tolerance), tolerance)).
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

        private Type[] getEntityTypes<T>()
        {
            var entityTypes = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.PropertyType.IsGenericType)
                .Select(p => p.PropertyType.GetGenericArguments()[0]).ToArray();
            return entityTypes;
        }

        private bool TableExists(string entity, int tolerance)
        {
            var exists = _tables.Any(t => Compare(tolerance, entity, t));
            return exists;
        }

        private static bool Compare(int tolerance, string entity, string table)
        {
            if (string.IsNullOrEmpty(table))
                return true;
            var exists = entity == table || (table.StartsWith(entity) && Math.Abs(table.Length - entity.Length) <= tolerance);
            return exists;
        }

        private string getTableName(string entity, int tolerance)
        {
            return _tables
                .OrderBy(t => t)
                .ThenBy(t => !string.IsNullOrEmpty(t) ? t.Length : 0)
                .FirstOrDefault(t => Compare(tolerance, entity, t));
        }
    }
}