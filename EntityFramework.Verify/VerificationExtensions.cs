using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EntityFramework.Verify
{
    public static class VerificationExtensions
    {

        public static bool ContainsNamespaces(this PropertyInfo p, IEnumerable<string> nameSpacesToIgnore)
        {
            return nameSpacesToIgnore.Any(n => containsNamespace(p, n));
        }

        private static bool containsNamespace(PropertyInfo p, string ns)
        {
            bool contains = p.PropertyType.FullName.Contains(ns);
            return contains;
        }
    }
}