using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EntityFramework.Verify
{
    public class EntityFrameworkTypeRepository<TModel> : IEntityTypeRepository
    {

        public IEnumerable<DataEntity> GetEntityTypes()
        {
            var entityTypes = typeof(TModel).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.PropertyType.IsGenericType)
                .Select(p => p.PropertyType.GetGenericArguments()[0])
                .Select(p => new DataEntity
                {
                    Name = p.Name,
                    Type = p
                });
            return entityTypes;
        }

        public IEnumerable<string> GetColumns(Type entity, IEnumerable<string> nameSpacesToIgnore)
        {
            var properties = entity.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(
                    p =>
                        IsColumn(p) &&
                        !p.ContainsNamespaces(nameSpacesToIgnore) &&
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
            return properties.Select(p => p.Name).OrderBy(p => p);//.ToArray();
        }
        public bool IsColumn(PropertyInfo prop)
        {
            if (!prop.CanWrite || !prop.CanRead || prop.GetSetMethod() == null)
                return false;

            return prop.DeclaringType != null && prop.DeclaringType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                                     .Any(f => f.Name.Contains("<" + prop.Name + ">"));
        }
    }
}