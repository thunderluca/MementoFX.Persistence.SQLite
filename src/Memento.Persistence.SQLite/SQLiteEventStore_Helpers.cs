using Newtonsoft.Json;
using SQLite.Net;
using SQLite.Net.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Memento.Persistence.SQLite
{
    /// <summary>
    /// Provides a collection of methods
    /// for SQLite database management
    /// </summary>
    public partial class SQLiteEventStore
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

        /// <summary>
        /// Creates a new instance of SQLite database
        /// </summary>
        /// <param name="platform">The platform where SQLite database will be stored</param>
        /// <param name="databasePath">The path of SQLite database</param>
        /// <param name="storeDateTimeAsTicks">If true, date values will be stored as integer timestamp</param>
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

        internal static IEnumerable<object> GetQueryParametersCollection(
            bool storeDateTimeAsTicks,
            Guid aggregateId,
            DateTime pointInTime,
            Guid? timelineId)
        {
            var queryParameters = storeDateTimeAsTicks
                ? new List<object> { aggregateId, pointInTime.Ticks }
                : new List<object> { aggregateId, pointInTime.ToISO8601Date() };

            if (timelineId.HasValue)
                queryParameters.Add(timelineId.Value);

            return queryParameters;
        }
    }

    internal static class SQLiteExtensions
    {
        internal static void CreateOrMigrateTable<T>(this SQLiteConnection connection) where T : DomainEvent
        {
            CreateOrMigrateTable(connection, typeof(T));
        }

        internal static void CreateOrMigrateTable(this SQLiteConnection connection, Type tableType)
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
