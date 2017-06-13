using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace EntityFramework.Verify
{
    public class SqlTableRepository : ITableFactory
    {
        public SqlTableRepository(string connectionString)
        {
            ConnectionString = connectionString;
            _tables = new List<TableModel>();
            _database = new SqlConnectionStringBuilder(ConnectionString).InitialCatalog;
        }
        private readonly List<TableModel> _tables;


        private readonly string _database;

        public string ConnectionString { get; }

        private TableModel getTableName(string entity, int tolerance)
        {
            return _tables
                .OrderBy(t => t.Name)
                .ThenBy(t => !string.IsNullOrEmpty(t.Name) ? t.Name.Length : 0)
                .FirstOrDefault(t => Compare(tolerance, entity, t.Name));
        }
        private static bool Compare(int tolerance, string entity, string table)
        {
            if (string.IsNullOrEmpty(table))
                return true;
            var exists = entity == table || (table.StartsWith(entity) && Math.Abs(table.Length - entity.Length) <= tolerance);
            return exists;
        }
        public IEnumerable<TableModel> GetTables()
        {
            const string query = "SELECT TABLE_NAME  FROM INFORMATION_SCHEMA.TABLES";
            _tables.Clear();
            foreach (var tableName in EnumerateDataItems(ConnectionString, query))
            {
                var table = new TableModel
                {
                    Name = tableName,
                    Database = _database
                };
                _tables.Add(table);
                yield return table;
            }
        }
        public IEnumerable<string> GetMatchingTableColumns(TableModel table, string entityName, int tolerance)
        {
            var dataTable = getTableName(entityName, tolerance);
            if (string.IsNullOrEmpty(dataTable.Name))
            {
                throw new InvalidOperationException($"Table '{dataTable}' does not exist in database: {_database}");
            }
            return dataTable.Columns;
        }
        public IEnumerable<string> GetTableColumns(TableModel table)
        {
            const string query = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @Table ";
            return EnumerateDataItems(ConnectionString, query, new Dictionary<string, object>
            {
                {"Table", table.Name }
            }).OrderBy(t => t);
        }
        public IEnumerable<string> EnumerateDataItems(string connectionString, string query, Dictionary<string, object> filters = null)
        {
            using (var connection = new SqlConnection(connectionString))
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

        public string GetDatabaseName(string table = null)
        {
            return _database;
        }
        
        
    }
}