using Newtonsoft.Json;
using SQLite;
using SQLite.Net;
using SQLite.Net.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Memento.Persistence.SQLite
{
    public static class SQLiteHelper
    {
        private static Type[] SQLiteSuppoertedTypes =
        {
            typeof(int),
            typeof(long),
            typeof(bool),
            typeof(Enum),
            typeof(float),
            typeof(double),
            typeof(string),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(byte[]),
            typeof(Guid)
        };

        private static BlobSerializerDelegate.SerializeDelegate serializerDelegate = obj => 
            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));

        private static BlobSerializerDelegate.DeserializeDelegate deserializerDelegate = (data, type) =>
        {
            using (var stream = new MemoryStream(data))
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return JsonSerializer.Create().Deserialize(reader, type);
                }
            }
        };

        private static BlobSerializerDelegate.CanSerializeDelegate canDeserializeDelegate = type => 
            SQLiteSuppoertedTypes.All(t => t != type);

        public static SQLiteConnection CreateSQLiteConnection(ISQLitePlatform platform, string databasePath, bool storeDateTimeAsTicks = true)
        {
            var serializer = new BlobSerializerDelegate(
                serializeDelegate: serializerDelegate, 
                deserializeDelegate: deserializerDelegate, 
                canDeserializeDelegate: canDeserializeDelegate);

            return new SQLiteConnection(
                sqlitePlatform: platform,
                databasePath: databasePath, 
                storeDateTimeAsTicks: storeDateTimeAsTicks, 
                serializer: serializer);
        }

        public static void CreateOrMigrateTable<T>(this SQLiteConnection connection) where T : DomainEvent
        {
            CreateOrMigrateTable(connection, typeof(T));
        }

        public static void CreateOrMigrateTable(this SQLiteConnection connection, Type tableType)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            var exisistingTableInfo = connection.GetTableInfo(tableType.Name);
            if (exisistingTableInfo == null || exisistingTableInfo.Count == 0)
            {
                connection.CreateTable(tableType, CreateFlags.ImplicitPK | CreateFlags.ImplicitIndex);
                return;
            }
            connection.MigrateTable(tableType);
        }
    }
}
