using MementoFX.Messaging;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MementoFX.Persistence.SQLite
{
    /// <summary>
    /// Provides an implementation of a Memento event store
    /// using SQLite as the storage
    /// </summary>
    public class SQLiteEventStore : EventStore
    {
        /// <summary>
        /// Gets or sets the reference to the SQLite database instance
        /// </summary>
        public static SQLiteConnection SQLiteDatabase { get; private set; }

        /// <summary>
        /// Creates a new instance of the event store
        /// </summary>
        /// <param name="connectionString">The connection string of document store to be used by the instance</param>
        /// <param name="eventDispatcher">The event dispatcher to be used by the instance</param>
        public SQLiteEventStore(string connectionString, IEventDispatcher eventDispatcher) : base(eventDispatcher)
        {
            if (SQLiteDatabase == null)
            {
                SQLiteDatabase = new SQLiteConnection(connectionString);
            }
        }

        /// <summary>
        /// Creates a new instance of the event store
        /// </summary>
        /// <param name="sqliteDatabase">The document store to be used by the instance</param>
        /// <param name="eventDispatcher">The event dispatcher to be used by the instance</param>
        public SQLiteEventStore(SQLiteConnection sqliteDatabase, IEventDispatcher eventDispatcher) : base(eventDispatcher)
        {
            SQLiteDatabase = sqliteDatabase ?? throw new ArgumentNullException(nameof(sqliteDatabase));
        }

        /// <summary>
        /// Retrieves all events of a type which satisfy a requirement
        /// </summary>
        /// <typeparam name="T">The type of the event</typeparam>
        /// <param name="filter">The requirement</param>
        /// <returns>The events which satisfy the given requirement</returns>
        public override IEnumerable<T> Find<T>(Expression<Func<T, bool>> filter)
        {
            SQLiteDatabase.CreateTable<T>();

            var events = SQLiteDatabase.Table<T>().Where(filter);

            return events;
        }

        /// <summary>
        /// Retrieves the desired events from the store
        /// </summary>
        /// <param name="aggregateId">The aggregate id</param>
        /// <param name="pointInTime">The point in time up to which the events have to be retrieved</param>
        /// <param name="eventDescriptors">The descriptors defining the events to be retrieved</param>
        /// <param name="timelineId">The id of the timeline from which to retrieve the events</param>
        /// <returns>The list of the retrieved events</returns>
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
                        + $"{nameof(DomainEvent.TimeStamp)} <= \"?\""
                        + $" AND {nameof(DomainEvent.TimelineId)} IS NULL";

                    if (timelineId.HasValue)
                        query += $" OR {nameof(DomainEvent.TimelineId)} = ?";

                    var queryParams = GetQueryParametersCollection(
                        //storeDateTimeAsTicks: SQLiteDatabase.StoreDateTimeAsTicks, 
                        aggregateId: aggregateId, 
                        pointInTime: pointInTime,
                        timelineId: timelineId).ToArray();

                    var collection = SQLiteDatabase.Query(mapping, query, queryParams);

                    foreach (var evt in collection)
                        events.Add((DomainEvent)evt);
                }
            }

            return events.OrderBy(e => e.TimeStamp);
        }

        /// <summary>
        /// Saves an event into the store
        /// </summary>
        /// <param name="event">The event to be saved</param>
        protected override void _Save(DomainEvent @event)
        {
            SQLiteDatabase.CreateTable(@event.GetType());

            SQLiteDatabase.Insert(@event, @event.GetType());
        }

        private static IEnumerable<object> GetQueryParametersCollection(Guid aggregateId, DateTime pointInTime, Guid? timelineId)
        {
            var queryParameters = new List<object>
            {
                aggregateId,
                pointInTime.ToISO8601Date()
            };

            if (timelineId.HasValue)
            {
                queryParameters.Add(timelineId.Value);
            }

            return queryParameters;
        }
    }
}
