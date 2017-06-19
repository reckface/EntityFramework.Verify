using EntityFramework.Verify;
using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Reflection;

namespace Linq2Sql.Verify
{
    public class Linq2SqlTypeRepository : IEntityTypeRepository
    {
        private Assembly _assembly;

        public Linq2SqlTypeRepository(Assembly assembly)
        {
            _assembly = assembly;
        }

        public IEnumerable<string> GetColumns(Type entity, IEnumerable<string> nameSpacesToIgnore)
        {
            return entity.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => IsColumn(p) &&
                        !p.ContainsNamespaces(nameSpacesToIgnore))
                .Select(p => GetName(p))
                .OrderBy(p => p);
        }

        private static string GetName(PropertyInfo p)
        {
            var columnAttribute = p.GetCustomAttribute<ColumnAttribute>();
            if (!string.IsNullOrEmpty(columnAttribute?.Name))
                return columnAttribute.Name;
            return p.Name;
        }

        public IEnumerable<DataEntity> GetEntityTypes()
        {
            var entityTypes = _assembly.GetExportedTypes()
                .Where(t => t.GetCustomAttribute<TableAttribute>() != null && t.GetProperties().Any())
                .Select(t => new DataEntity
                {
                    Name = t.GetCustomAttribute<TableAttribute>().Name ?? t.Name,
                    Type = t
                })
                .OrderBy(t => t.Name);
            return entityTypes;
        }

        public bool IsColumn(PropertyInfo prop)
        {
            if (!prop.CanWrite || !prop.CanRead || prop.GetSetMethod() == null)
                return false;
            return prop.GetCustomAttribute<ColumnAttribute>() != null;
        }
    }
}