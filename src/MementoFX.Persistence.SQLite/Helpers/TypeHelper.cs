using MementoFX.Persistence.SQLite.Data;
using SQLite;
using System;

namespace MementoFX.Persistence.SQLite.Helpers
{
    internal static class TypeHelper
    {
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

        public static TypeInfo GetTypeInfo(bool isNull, Type type, bool storeDateTimeAsTicks)
        {
            var isNullable = isNull ? true : IsNullableType(type);

            var isClass = !type.IsValueType && type.IsClass && type != typeof(string) && type != typeof(Enum);

            var colType = GetColType(isNullable && type.IsValueType ? Nullable.GetUnderlyingType(type) : type, storeDateTimeAsTicks);

            return new TypeInfo(colType, isNullable, isClass);
        }
    }
}
