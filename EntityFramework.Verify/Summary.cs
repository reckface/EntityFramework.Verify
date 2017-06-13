using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace EntityFramework.Verify
{
    public class Summary
    {
        public Summary()
        {
            Properties = new string[] { };
            Columns = new string[] { };
            MissingColumns = new List<string>();
        }

        public Summary(string entity, string database, string table, IEnumerable<string> properties, IEnumerable<string> columns, int tolerance)
            : this()
        {
            Database = database;
            Entity = entity;
            Table = table;
            Properties = properties;
            Columns = columns;
            _tolerance = tolerance;
            MissingColumns = GetMissingColumns(tolerance);
        }

        public string Database { get; private set; }
        public string Entity { get; set; }
        public string Table { get; set; }
        public IEnumerable<string> MissingColumns { get; set; }
        public IEnumerable<string> Properties { get; set; }
        public IEnumerable<string> Columns { get; set; }
        public bool TableMissing { get; set; }
        private readonly int _tolerance;

        public Dictionary<string, string> Comparison
        {
            get { return Properties.ToDictionary(p => p, p => Columns.FirstOrDefault(c => Compare(_tolerance, p, c))); }
        }

        public bool HasMissingColumns
        {
            get { return MissingColumns.Any(); }
        }

        public IEnumerable<string> GetMissingColumns(int tolerance)
        {
            var missing = (!TableMissing)
                ? Properties.Where(p => !HasColumn(tolerance, p))
                    .ToList()
                : MissingColumns;
            return missing;
        }

        private bool HasColumn(int tolerance, string p)
        {
            var exists = Columns.Any(c => Compare(tolerance, p, c));
            return exists;
        }

        private static bool Compare(int tolerance, string property, string column)
        {
            var exists = string.Equals(property, column, StringComparison.InvariantCultureIgnoreCase) || (property.StartsWith(column) && Math.Abs(column.Length - property.Length) <= tolerance);
            return exists;
        }
    }
}