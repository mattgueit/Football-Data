using System;
using System.Collections.Generic;
using System.Linq;

namespace get_football_data.ErrorHandling
{
    public static class Throw
    {
        public static void IfNull(object? obj, string objectName)
        {
            if (obj == null)
            {
                throw new Exception($"Data not found: {objectName}.");
            }
        }

        public static void IfNullOrEmpty(IEnumerable<object>? obj, string collectionName)
        {
            if (obj == null || !obj.Any())
            {
                throw new Exception($"Data not found: {collectionName}");
            }
        }
    }
}
