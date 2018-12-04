using MementoFX.Persistence.SQLite.Data;
using SQLite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace MementoFX.Persistence.SQLite.Helpers
{
    internal static class TableHelper
    {
        private static void AlterTableAddColumn(this SQLiteConnection connection, PropertyInfo property, object obj, string tableName)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            var typeInfo = TypeHelper.GetTypeInfo(property.GetValue(obj) == null, property.PropertyType, connection.StoreDateTimeAsTicks);

            var query = string.Format(Commands.AlterTableAddColumnFormat, tableName, property.Name, typeInfo.ToString());

            connection.Execute(query);
        }

        public static bool CheckIfTableExists(this SQLiteConnection connection, string tableName)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException(nameof(tableName));
            }

            var tableColumns = connection.GetTableInfo(tableName);

            return tableColumns != null && tableColumns.Count > 0;
        }

        private static void CreateIndex(this SQLiteConnection connection, string tableName, string columnName)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            var indexName = string.Format(Commands.CreateIndexNameFormat, tableName, columnName);

            var query = string.Format(Commands.CreateIndexFormat, indexName, tableName, columnName);

            connection.Execute(query);
        }

        public static void CreateOrUpdateTable(this SQLiteConnection connection, object obj, Type type, string tableName, bool autoIncrementalTableMigrations)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException(nameof(tableName));
            }

            if (!connection.CheckIfTableExists(tableName))
            {
                connection.CreateTable(obj, type, tableName);

                var indexesColumnNames = GetBaseDomainEventProperties();
                if (indexesColumnNames != null && indexesColumnNames.Length > 0)
                {
                    foreach (var columnName in indexesColumnNames)
                    {
                        connection.CreateIndex(tableName, columnName);
                    }
                }
            }
            else if (autoIncrementalTableMigrations)
            {
                var tableSchema = connection.GetTableSchema(tableName);

                var properties = type.GetProperties().Where(property => tableSchema.All(t => !string.Equals(t.Item1, property.Name, StringComparison.OrdinalIgnoreCase))).ToArray();
                if (properties.Length > 0)
                {
                    foreach (var property in properties)
                    {
                        connection.AlterTableAddColumn(property, obj, tableName);
                    }
                }
            }
        }

        private static void CreateTable(this SQLiteConnection connection, object obj, Type type, string tableName)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            var query = string.Empty;

            var properties = type.GetProperties();
            if (properties.Length == 0)
            {
                throw new InvalidOperationException();
            }

            for (var i = 0; i < properties.Length; i++)
            {
                var typeInfo = TypeHelper.GetTypeInfo(properties[i].GetValue(obj) == null, properties[i].PropertyType, connection.StoreDateTimeAsTicks);

                var value = typeInfo.ToString();

                if (!typeInfo.IsNullable)
                {
                    value = Commands.JoinWithSpace(value, "NOT NULL");
                }

                query += Commands.JoinWithSpace(i == 0 ? string.Empty : ",", properties[i].Name, value);
            }

            query = Commands.JoinWithSpace(string.Format(Commands.CreateTableFormat, tableName), Commands.Enclose(query.Trim()));

            connection.Execute(query);
        }

        private static string[] GetBaseDomainEventProperties()
        {
            return new[] { nameof(DomainEvent.Id), nameof(DomainEvent.TimelineId), nameof(DomainEvent.TimeStamp) };
        }

        public static IList<Tuple<string, Type>> GetTableSchema(this SQLiteConnection connection, string tableName)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            var query = string.Format(Commands.SelectTableExistsFormat, tableName);

            var columnsData = connection.Query<DbColumn>(query);

            if (columnsData == null || columnsData.Count == 0) return new List<Tuple<string, Type>>();
            
            return columnsData.Select(MapColumnType).ToList();
        }

        private static Tuple<string, Type> MapColumnType(DbColumn column)
        {
            var isNullable = Convert.ToBoolean(column.NotNull);

            var columnType = TypeHelper.GetClrType(column.Type, isNullable);

            return Tuple.Create(column.Name, columnType);
        }

        public static IEnumerable<object> ExecuteQuery(this SQLiteConnection connection, Type type, string tableName, string query, params object[] args)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException(nameof(query));
            }

            if (!connection.CheckIfTableExists(tableName))
            {
                return new object[0];
            }

            var constructorInfos = type.GetConstructors();
            if (constructorInfos == null || constructorInfos.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(constructorInfos));
            }

            if (args != null&& args.Length > 0)
            {
                for (var i = 0; i < args.Length; i++)
                {
                    if (args[i] is string || args[i] is Guid)
                    {
                        args[i] = $"'{args[i]}'";
                    }
                }

                query = string.Format(query, args);
            }

            var constructorInfo = constructorInfos[0];

            var properties = type.GetProperties();

            var parameters = constructorInfo.GetParameters();

            object instance = null;
            var objectActivator = ReflectionHelper.GetActivator(constructorInfo);

            var collection = new List<object>();

            using (var stmt = SQLite3.Prepare2(connection.Handle, query))
            {
                while (SQLite3.Step(stmt) == SQLite3.Result.Row)
                {
                    var dictionary = GetDataDictionary(stmt, properties, connection.StoreDateTimeAsTicks);

                    var values = new object[parameters.Length];

                    for (var p = 0; p < parameters.Length; p++)
                    {
                        var kvp = dictionary.SingleOrDefault(pv => string.Equals(pv.Key, parameters[p].Name, StringComparison.OrdinalIgnoreCase));
                        if (kvp.Value == null || kvp.Equals(default(KeyValuePair<string, object>))) continue;

                        values[p] = kvp.Value;

                        dictionary.Remove(kvp.Key);
                    }

                    instance = objectActivator(values);

                    if (dictionary.Keys.Count > 0)
                    {
                        foreach (var kvp in dictionary)
                        {
                            instance.SetPropertyValue(kvp.Key, kvp.Value);
                        }
                    }

                    collection.Add(instance);
                }
            }

            return collection;
        }

        public static IEnumerable<T> ExecuteQuery<T>(this SQLiteConnection connection, string tableName, string query, params object[] args) where T : class
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException(nameof(query));
            }

            if (!connection.CheckIfTableExists(tableName))
            {
                return new T[0];
            }

            var type = typeof(T);

            var constructorInfos = type.GetConstructors();
            if (constructorInfos == null || constructorInfos.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(constructorInfos));
            }

            if (args != null && args.Length > 0)
            {
                for (var i = 0; i < args.Length; i++)
                {
                    if (args[i] is string || args[i] is Guid)
                    {
                        args[i] = $"'{args[i]}'";
                    }
                }

                query = string.Format(query, args);
            }

            var constructorInfo = constructorInfos[0];

            var properties = type.GetProperties();

            var parameters = constructorInfo.GetParameters();

            T instance = null;
            var objectActivator = ReflectionHelper.GetActivator<T>(constructorInfo);

            var collection = new List<T>();

            using (var stmt = SQLite3.Prepare2(connection.Handle, query))
            {
                while (SQLite3.Step(stmt) == SQLite3.Result.Row)
                {
                    var dictionary = GetDataDictionary(stmt, properties, connection.StoreDateTimeAsTicks);

                    var values = new object[parameters.Length];

                    for (var p = 0; p < parameters.Length; p++)
                    {
                        var kvp = dictionary.SingleOrDefault(pv => string.Equals(pv.Key, parameters[p].Name, StringComparison.OrdinalIgnoreCase));
                        if (kvp.Value == null || kvp.Equals(default(KeyValuePair<string, object>))) continue;

                        values[p] = kvp.Value;

                        dictionary.Remove(kvp.Key);
                    }

                    instance = objectActivator(values);

                    if (dictionary.Keys.Count > 0)
                    {
                        foreach (var kvp in dictionary)
                        {
                            instance.SetPropertyValue(kvp.Key, kvp.Value);
                        }
                    }

                    collection.Add(instance);
                }
            }

            return collection;
        }

        private static IDictionary<string, object> GetDataDictionary(SQLitePCL.sqlite3_stmt stmt, PropertyInfo[] properties, bool storeDateTimeAsTicks)
        {
            var dictionary = new Dictionary<string, object>();

            var columns = SQLite3.ColumnCount(stmt);

            for (var index = 0; index < columns; index++)
            {
                var colName = SQLite3.ColumnName(stmt, index);

                var property = properties.FirstOrDefault(p => p.Name == colName);
                if (property == null) continue;

                switch (SQLite3.ColumnType(stmt, index))
                {
                    case SQLite3.ColType.Text:
                        {
                            var @string = SQLite3.ColumnText(stmt, index);

                            if (@string == null)
                            {
                                dictionary.Add(colName, null);
                                break;
                            }

                            if (string.IsNullOrWhiteSpace(@string))
                            {
                                dictionary.Add(colName, @string);
                                break;
                            }

                            if (!storeDateTimeAsTicks && DateTime.TryParse(@string, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime dateTime))
                            {
                                dictionary.Add(colName, dateTime);
                                break;
                            }

                            if (!storeDateTimeAsTicks && DateTimeOffset.TryParse(@string, out DateTimeOffset dateTimeOffset))
                            {
                                dictionary.Add(colName, dateTimeOffset);
                                break;
                            }

                            if (property.PropertyType == typeof(Guid) || property.PropertyType == typeof(Guid?))
                            {
                                if (Guid.TryParse(@string, out Guid guid))
                                {
                                    dictionary.Add(colName, guid);
                                    break;
                                }

                                dictionary.Add(colName, null);
                                break;
                            }

                            if (JsonHelper.TryDeserializeObject(@string, property.PropertyType, out object obj))
                            {
                                dictionary.Add(colName, obj);
                                break;
                            }

                            if (property.PropertyType == typeof(TimeSpan) && TimeSpan.TryParse(@string, out TimeSpan timeSpan))
                            {
                                dictionary.Add(colName, timeSpan);
                                break;
                            }

                            dictionary.Add(colName, @string);
                            break;
                        }
                    case SQLite3.ColType.Integer:
                        {
                            object integer = SQLite3.ColumnInt64(stmt, index);

                            if (integer.GetType() != property.PropertyType)
                            {
                                if (storeDateTimeAsTicks && (property.PropertyType == typeof(DateTime)
                                    || property.PropertyType == typeof(DateTime?)
                                    || property.PropertyType == typeof(DateTimeOffset)
                                    || property.PropertyType == typeof(DateTimeOffset?)))
                                {
                                    var @long = (long)integer;
                                    integer = DateTime.FromBinary(@long);
                                }
                                else
                                {
                                    integer = Convert.ChangeType(integer, property.PropertyType);
                                }
                            }

                            dictionary.Add(colName, integer);
                            break;
                        }
                    case SQLite3.ColType.Float:
                        {
                            object @float = SQLite3.ColumnDouble(stmt, index);

                            if (@float.GetType() != property.PropertyType)
                            {
                                @float = Convert.ChangeType(@float, property.PropertyType);
                            }

                            dictionary.Add(colName, @float);
                            break;
                        }
                    case SQLite3.ColType.Blob:
                        {
                            var blob = SQLite3.ColumnBlob(stmt, index);
                            dictionary.Add(colName, blob);
                            break;
                        }
                    case SQLite3.ColType.Null:
                        {
                            dictionary.Add(colName, null);
                            break;
                        }
                }
            }

            return dictionary;
        }
    }
}
