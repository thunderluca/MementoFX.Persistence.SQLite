using SQLite;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace MementoFX.Persistence.SQLite.Helpers
{
    internal static class TypeHelper
    {
        public static Type GetClrType(string typeName, bool isNullable)
        {
            var colType = GetColTypeByName(typeName);

            switch (colType)
            {
                case SQLite3.ColType.Text:
                    return typeof(string);
                case SQLite3.ColType.Integer:
                    return isNullable ? typeof(long?) : typeof(long);
                case SQLite3.ColType.Float:
                    return isNullable ? typeof(double?) : typeof(double);
                case SQLite3.ColType.Blob:
                    return typeof(byte[]);
                default:
                    throw new NotSupportedException($"Unsupported column type: {colType}");
            }
        }

        private static SQLite3.ColType GetColTypeByName(string typeName)
        {
            if (typeName == null)
            {
                return SQLite3.ColType.Null;
            }

            if (string.IsNullOrWhiteSpace(typeName))
            {
                return SQLite3.ColType.Blob;
            }

            switch (typeName.ToUpper())
            {
                case "TXT":
                case "TEXT":
                    return SQLite3.ColType.Text;
                case "NUM":
                case "NUMERIC":
                case "INT":
                case "INTEGER":
                    return SQLite3.ColType.Integer;
                case "FLOAT":
                case "REAL":
                    return SQLite3.ColType.Float;
                default:
                    return SQLite3.ColType.Null;
            }
        }

        private static SQLite3.ColType GetColType(Type type, bool storeDateTimeAsTicks)
        {
            if (IsEnumType(type))
            {
                return SQLite3.ColType.Integer;
            }

            if (type == typeof(Guid))
            {
                return SQLite3.ColType.Text;
            }

            if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            {
                return storeDateTimeAsTicks ? SQLite3.ColType.Integer : SQLite3.ColType.Text;
            }

            if (type == typeof(char) || type == typeof(string))
            {
                return SQLite3.ColType.Text;
            }

            if (type == typeof(long) || type == typeof(ulong))
            {
                return SQLite3.ColType.Integer;
            }

            if (type == typeof(int) || type == typeof(uint))
            {
                return SQLite3.ColType.Integer;
            }

            if (type == typeof(short) || type == typeof(ushort))
            {
                return SQLite3.ColType.Integer;
            }

            if (type == typeof(sbyte) || type == typeof(byte))
            {
                return SQLite3.ColType.Integer;
            }

            if (type == typeof(double))
            {
                return SQLite3.ColType.Float;
            }

            if (type == typeof(float))
            {
                return SQLite3.ColType.Float;
            }

            if (type == typeof(decimal))
            {
                return SQLite3.ColType.Float;
            }

            if (type == typeof(byte[]))
            {
                return SQLite3.ColType.Blob;
            }

            if (type == typeof(bool) || type == typeof(bool?))
            {
                return SQLite3.ColType.Integer;
            }

            return SQLite3.ColType.Text;
        }

        public static Data.TypeInfo GetTypeInfo(bool isNull, Type type, bool storeDateTimeAsTicks)
        {
            var isNullable = isNull ? true : IsNullableType(type);

            var isClass = !type.IsValueType && type.IsClass && type != typeof(string) && type != typeof(Enum);

            var colType = GetColType(isNullable && type.IsValueType ? Nullable.GetUnderlyingType(type) : type, storeDateTimeAsTicks);

            return new Data.TypeInfo(colType, isNullable, isClass);
        }

        public static bool IsEnumType(Type type)
        {
            if (IsNullableType(type))
            {
                return Nullable.GetUnderlyingType(type)?.IsEnum ?? false;
            }

            return type.IsEnum;
        }

        public static bool IsNullableType(Type type)
        {
            return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
        }

        private static Type ToNullableType(this Type type)
        {
            if (type == null || type == typeof(void))
            {
                return null;
            }

            if (!type.IsValueType || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)))
            {
                return type;
            }

            return typeof(Nullable<>).MakeGenericType(type);
        }

        public static object TryConvertFloatValue(double @float, PropertyInfo property)
        {
            if (@float.GetType() != property.PropertyType)
            {
                return Convert.ChangeType(@float, property.PropertyType);
            }

            return @float;
        }

        public static object TryConvertIntegerValue(long integer, PropertyInfo property, bool storeDateTimeAsTicks)
        {
            if (integer.GetType() == property.PropertyType)
            {
                return integer;
            }

            if (IsEnumType(property.PropertyType))
            {
                var enumType = IsNullableType(property.PropertyType) ? Nullable.GetUnderlyingType(property.PropertyType) : property.PropertyType;

                var enumValues = Enum.GetValues(enumType);

                var values = new object[enumValues.Length];

                enumValues.CopyTo(values, 0);

                return values.SingleOrDefault(v => (int)v == integer) ?? integer;
            }

            if (storeDateTimeAsTicks && (property.PropertyType == typeof(DateTime)
                || property.PropertyType == typeof(DateTime?)
                || property.PropertyType == typeof(DateTimeOffset)
                || property.PropertyType == typeof(DateTimeOffset?)))
            {
                return DateTime.FromBinary(integer);
            }

            return Convert.ChangeType(integer, property.PropertyType);
        }

        public static object TryConvertStringValue(string @string, PropertyInfo property, bool storeDateTimeAsTicks)
        {
            if (string.IsNullOrWhiteSpace(@string))
            {
                return @string;
            }

            if (!storeDateTimeAsTicks && DateTime.TryParse(@string, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime dateTime))
            {
                return dateTime;
            }

            if (!storeDateTimeAsTicks && DateTimeOffset.TryParse(@string, out DateTimeOffset dateTimeOffset))
            {
                return dateTimeOffset;
            }

            if (property.PropertyType == typeof(Guid) || property.PropertyType == typeof(Guid?))
            {
                if (Guid.TryParse(@string, out Guid guid))
                {
                    return guid;
                }

                return null;
            }

            if (JsonHelper.TryDeserializeObject(@string, property.PropertyType, out object obj))
            {
                return obj;
            }

            if (property.PropertyType == typeof(TimeSpan) && TimeSpan.TryParse(@string, out TimeSpan timeSpan))
            {
                return timeSpan;
            }

            return @string;
        }
    }
}
