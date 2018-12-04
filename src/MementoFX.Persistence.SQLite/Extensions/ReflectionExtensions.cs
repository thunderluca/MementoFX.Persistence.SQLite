using System;
using System.Reflection;

namespace System.Reflection
{
    internal static class ReflectionExtensions
    {
        public static PropertyInfo GetPublicOrPrivateProperty(this Type type, string propertyName)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        public static void SetPropertyValue(this object obj, string propertyName, object val)
        {
            SetPropertyValue(obj, obj.GetType(), propertyName, val);
        }

        public static void SetPropertyValue(this object obj, Type type, string propertyName, object val)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (type.BaseType != typeof(object))
            {
                SetPropertyValue(obj, type.BaseType, propertyName, val);
                return;
            }

            var propInfo = type.GetPublicOrPrivateProperty(propertyName);
            if (propInfo == null)
            {
                throw new ArgumentOutOfRangeException(nameof(propertyName), string.Format("Property {0} not found in type {1}", propertyName, type.FullName));
            }

            propInfo.SetValue(obj, val, null);
        }
    }
}
