using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Memento.Messaging;
using SQLite.Net;
using SQLite.Net.Interop;
#if X86 || X64
using SQLite.Net.Platform.Win32;
#else
using SQLite.Net.Platform.Generic;
#endif
using static Memento.Persistence.SQLite.SQLiteHelper;

namespace Memento.Persistence.SQLite
{
    public class SQLiteEventStore : EventStore
    {
        public static SQLiteConnection SQLiteDatabase { get; private set; }

        public SQLiteEventStore(IEventDispatcher eventDispatcher) : base(eventDispatcher)
        {
            if (SQLiteDatabase == null)
            {
                var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString;
#if X86 || X64
                var sqlitePlatform = new SQLitePlatformWin32();
#else
                var sqlitePlatform = new SQLitePlatformGeneric();
#endif
                SQLiteDatabase = CreateSQLiteConnection(sqlitePlatform, connectionString);
            }
        }

        public SQLiteEventStore(SQLiteConnection sqliteDatabase, IEventDispatcher eventDispatcher) : base(eventDispatcher)
        {
            if (sqliteDatabase == null)
                throw new ArgumentNullException(nameof(sqliteDatabase));

            SQLiteDatabase = sqliteDatabase;
        }

        public override IEnumerable<T> Find<T>(Func<T, bool> filter)
        {
            SQLiteDatabase.CreateOrMigrateTable<T>();

            return SQLiteDatabase.Table<T>().Where(filter);
        }

        private static string GetCommandTextSuffix(bool timelineIdHasValue)
        {
            if (!timelineIdHasValue)
                return $" AND {nameof(DomainEvent.TimelineId)} IS NULL";
            else
                return $" AND {nameof(DomainEvent.TimelineId)} IS NULL OR {nameof(DomainEvent.TimelineId)} = ?";
        }

        public override IEnumerable<DomainEvent> RetrieveEvents(Guid aggregateId, DateTime pointInTime, IEnumerable<EventMapping> eventDescriptors, Guid? timelineId)
        {
            var firstDayYear = new DateTime(pointInTime.Year, 1, 1);

            var events = new List<DomainEvent>();

            var descriptorsGrouped = eventDescriptors
                .GroupBy(k => k.EventType);

            var querySuffix = GetCommandTextSuffix(timelineId.HasValue);

            foreach (var descriptorsGroup in descriptorsGrouped)
            {
                var eventType = descriptorsGroup.Key;
                var tableName = eventType.Name;

                var mapping = SQLiteDatabase.GetMapping(eventType);

                foreach (var eventDescriptor in descriptorsGroup)
                {
                    var query = $"SELECT * FROM {tableName} WHERE "
                        + $"{eventDescriptor.AggregateIdPropertyName} = ? AND "
                        + $"{nameof(DomainEvent.TimeStamp)} BETWEEN {firstDayYear.Ticks} AND {pointInTime.Ticks}";

                    query += querySuffix;

                    var queryParams = timelineId.HasValue 
                        ? new object[] { aggregateId, timelineId.Value }
                        : new object[] { aggregateId };

                    var collection = SQLiteDatabase.Query(mapping, query, queryParams);

                    foreach (var evt in collection)
                        events.Add((DomainEvent)evt);
                }
            }

            return events.OrderBy(e => e.TimeStamp);
        }

        protected override void _Save(DomainEvent @event)
        {
            var eventType = @event.GetType();

            SQLiteDatabase.CreateOrMigrateTable(eventType);

            SQLiteDatabase.Insert(@event, eventType);
        }
    }
}
