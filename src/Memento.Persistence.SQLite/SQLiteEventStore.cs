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
                    var query = $"SELECT * FROM {tableName} WHERE "
                        + $"{eventDescriptor.AggregateIdPropertyName} = ? AND "
                        + $"{nameof(DomainEvent.TimeStamp)} <= {(SQLiteDatabase.StoreDateTimeAsTicks ? "date('?')" : "?")}"
                        + $" AND {nameof(DomainEvent.TimelineId)} IS NULL";

                    if (timelineId.HasValue)
                        query += $" OR {nameof(DomainEvent.TimelineId)} = ?";

                    var queryParams = GetQueryParametersCollection(
                        storeDateTimeAsTicks: SQLiteDatabase.StoreDateTimeAsTicks, 
                        aggregateId: aggregateId, 
                        pointInTime: pointInTime,
                        timelineId: timelineId);

                    var collection = SQLiteDatabase.Query(mapping, query, queryParams);

                    foreach (var evt in collection)
                        events.Add((DomainEvent)evt);
                }
            }

            return events.OrderBy(e => e.TimeStamp);
        }

        private IEnumerable<object> GetQueryParametersCollection(
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

        protected override void _Save(DomainEvent @event)
        {
            SQLiteDatabase.CreateOrMigrateTable(@event.GetType());

            SQLiteDatabase.Insert(@event, @event.GetType());
        }
    }
}
