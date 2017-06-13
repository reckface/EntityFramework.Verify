using System;
using System.Collections;
using System.Collections.Generic;

namespace EntityFramework.Verify
{
    public class DataEntity
    {
        public string Name { get; set; }
        public Type Type { get; set; }
    }
    public class TableModel
    {
        public string Name { get; set; }
        public string Database { get; set; }

        public IEnumerable<string> Columns { get; set; }
    }
}