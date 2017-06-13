using System;
using System.Collections.Generic;
using System.Reflection;

namespace EntityFramework.Verify
{
    public interface IEntityTypeRepository
    {
        IEnumerable<DataEntity> GetEntityTypes();
        bool IsColumn(PropertyInfo prop);
        IEnumerable<string> GetColumns(Type prop, IEnumerable<string> nameSpacesToIgnore);
    }
}