using MementoFX.Persistence.SQLite.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MementoFX.Persistence.SQLite.Data
{
    internal static class ParameterDataExtensions
    {
        public static IEnumerable<ParameterData> GetParametersData(this object obj, Type type, bool storeDateTimeAsTicks)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            var properties = type.GetProperties();
            if (properties.Length == 0)
            {
                throw new InvalidOperationException();
            }

            foreach (var pi in properties)
            {
                var value = pi.GetValue(obj);

                var typeInfo = TypeHelper.GetTypeInfo(value == null, pi.PropertyType, storeDateTimeAsTicks);

                value = FixValue(value, typeInfo.IsClass);

                yield return new ParameterData(pi.Name, value, typeInfo.Type);
            }
        }

        private static object FixValue(object value, bool isClass)
        {
            if (value == null)
            {
                return null;
            }

            if (isClass)
            {
                return JsonConvert.SerializeObject(value);
            }

            if (TypeHelper.IsEnumType(value.GetType()))
            {
                return Convert.ToString((int)value, CultureInfo.InvariantCulture);
            }

            return value;
        }
    }
}
