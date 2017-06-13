using System;
using System.Linq;
using System.Collections.Generic;

namespace EntityFramework.Verify
{
    public class MultiConnectionSqlTableRepository : ITableFactory
    {
        public MultiConnectionSqlTableRepository(params string[] connectionStrings)
        {
            _connectionStrings = connectionStrings;
            _tableFactories = _connectionStrings.Select(conn => new SqlTableRepository(conn)).ToArray();
            _dataTables = new Dictionary<string, List<TableModel>>();
        }
        public string Database { get; }

        public string ConnectionString
        {
            get
            {
                var tableFactory = _tableFactories.FirstOrDefault(t => !string.IsNullOrEmpty(t.ConnectionString));
                return tableFactory?.ConnectionString;
            }
        }

        private readonly Dictionary<string, List<TableModel>> _dataTables;
        private readonly IEnumerable<string> _connectionStrings;
        private readonly IEnumerable<ITableFactory> _tableFactories;
        public IEnumerable<TableModel> GetTables()
        {
            _dataTables.Clear();
            foreach (var factory in _tableFactories)
            {
                _dataTables[factory.ConnectionString] = new List<TableModel>();
                foreach (var table in factory.GetTables())
                {
                    _dataTables[factory.ConnectionString].Add(table);
                    table.Columns = factory.GetTableColumns(table).ToArray();
                    yield return table;
                }
            }
        }
        public IEnumerable<string> GetMatchingTableColumns(TableModel table, string entityName, int tolerance)
        {
            var factory = _tableFactories.First(tf => tf.ConnectionString.Contains(table.Database));
            return factory.GetMatchingTableColumns(table, entityName, tolerance);
        }

        public IEnumerable<string> GetTableColumns(TableModel table)
        {
            return table.Columns;
        }
    }
}