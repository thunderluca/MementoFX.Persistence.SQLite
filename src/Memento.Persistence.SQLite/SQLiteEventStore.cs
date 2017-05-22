using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Memento.Messaging;
using SQLite.Net;
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

        public override IEnumerable<DomainEvent> RetrieveEvents(Guid aggregateId, DateTime pointInTime, IEnumerable<EventMapping> eventDescriptors, Guid? timelineId)
        {
            var events = new List<DomainEvent>();

            var descriptorsGrouped = eventDescriptors
                .GroupBy(k => k.EventType);
            
            foreach (var descriptorsGroup in descriptorsGrouped)
            {
                var eventType = descriptorsGroup.Key;
                var tableName = eventType.Name;

                var mapping = SQLiteDatabase.GetMapping(eventType);

                foreach (var eventDescriptor in descriptorsGroup)
                {
                    var collection = SQLiteDatabase.StoreDateTimeAsTicks 
                        ? ExecuteQueryForDateTimeAsTicks(tableName, eventDescriptor.AggregateIdPropertyName, mapping, aggregateId, pointInTime, timelineId) 
                        : ExecuteQueryForDateTimeAsDates(tableName, eventDescriptor.AggregateIdPropertyName, mapping, aggregateId, pointInTime, timelineId);

                    foreach (var evt in collection)
                        events.Add((DomainEvent)evt);
                }
            }

            return events.OrderBy(e => e.TimeStamp);
        }

        private List<object> ExecuteQueryForDateTimeAsTicks(
            string tableName, 
            string aggregateIdPropertyName,
            TableMapping mapping,
            Guid aggregateId,
            DateTime pointInTime,
            Guid? timelineId)
        {
            var query = $"SELECT * FROM {tableName} WHERE "
                           + $"{aggregateIdPropertyName} = ? AND "
                           + $"{nameof(DomainEvent.TimeStamp)} <= ?"
                           + $" AND {nameof(DomainEvent.TimelineId)} IS NULL";

            if (timelineId.HasValue)
                query += $" OR {nameof(DomainEvent.TimelineId)} = ?";

            var queryParams = timelineId.HasValue
                ? new object[] { aggregateId, pointInTime.Ticks, timelineId.Value }
                : new object[] { aggregateId, pointInTime.Ticks };

            var collection = SQLiteDatabase.Query(mapping, query, queryParams);
            return collection;
        }

        private List<object> ExecuteQueryForDateTimeAsDates(
            string tableName,
            string aggregateIdPropertyName,
            TableMapping mapping,
            Guid aggregateId,
            DateTime pointInTime,
            Guid? timelineId)
        {
            var query = $"SELECT * FROM {tableName} WHERE "
                           + $"{aggregateIdPropertyName} = ? AND "
                           + $"{nameof(DomainEvent.TimeStamp)} <= date('?')"
                           + $" AND {nameof(DomainEvent.TimelineId)} IS NULL";

            if (timelineId.HasValue)
                query += $" OR {nameof(DomainEvent.TimelineId)} = ?";

            var queryParams = timelineId.HasValue
                ? new object[] { aggregateId, pointInTime.ToString("yyyy-MM-ss"), timelineId.Value }
                : new object[] { aggregateId, pointInTime.ToString("yyyy-MM-ss") };

            var collection = SQLiteDatabase.Query(mapping, query, queryParams);
            return collection;
        }

        protected override void _Save(DomainEvent @event)
        {
            SQLiteDatabase.CreateOrMigrateTable(@event.GetType());

            SQLiteDatabase.Insert(@event, @event.GetType());
        }
    }
}
