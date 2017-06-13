using System.Collections.Generic;

namespace EntityFramework.Verify
{
    public interface ITableFactory
    {
        string ConnectionString { get; }
        IEnumerable<TableModel> GetTables();
        IEnumerable<string> GetMatchingTableColumns(TableModel table, string entityName, int tolerance);
        IEnumerable<string> GetTableColumns(TableModel table);
    }
}