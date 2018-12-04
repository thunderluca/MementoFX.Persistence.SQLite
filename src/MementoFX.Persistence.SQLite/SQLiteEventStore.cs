using MementoFX.Messaging;
using MementoFX.Persistence.SQLite.Data;
using MementoFX.Persistence.SQLite.Helpers;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MementoFX.Persistence.Sqlite
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
            var eventType = typeof(T);
            var tableName = eventType.Name;

            if (!SQLiteDatabase.CheckIfTableExists(tableName))
            {
                return new T[0];
            }

            var sqlExpression = filter.ToSqlExpression();

            var query = Commands.BuildSelectWhereCommandText(tableName, "*") + " " + sqlExpression.CommandText;

            var args = sqlExpression.Parameters.Select(p => p.Value).ToArray();
            
            var collection = SQLiteDatabase.ExecuteQuery<T>(tableName, query, args);
            
            return collection;
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
            
            foreach (var descriptorsGroup in eventDescriptors.GroupBy(k => k.EventType))
            {
                var eventType = descriptorsGroup.Key;

                var tableName = eventType.Name;

                if (!SQLiteDatabase.CheckIfTableExists(tableName)) continue;

                var query = Commands.BuildSelectWhereCommandText(tableName, "*");

                var args = new List<object>();

                var filters = new List<string>();

                var counter = 0;

                for (var i = 0; i < descriptorsGroup.Count(); i++)
                {
                    var eventDescriptor = descriptorsGroup.ElementAt(i);
                    
                    var filter = Commands.JoinWithSpace(eventDescriptor.AggregateIdPropertyName, "=", string.Format(Commands.ParameterNameFormat, counter));
                    counter++;

                    filters.Add(filter);

                    args.Add(aggregateId);
                }

                var filtersText = filters.Count == 1 ? filters[0] : Commands.Enclose(string.Join(" OR ", filters));

                query = Commands.JoinWithSpace(query, filtersText);

                query = Commands.JoinWithSpace(query, "AND", nameof(DomainEvent.TimeStamp), "<=", string.Format(Commands.ParameterNameFormat, counter));
                counter++;

                args.Add(pointInTime.ToISO8601Date());

                if (!timelineId.HasValue)
                {
                    query = Commands.JoinWithSpace(query, "AND", nameof(DomainEvent.TimelineId), "IS", "NULL");
                }
                else
                {
                    query += Commands.JoinWithSpace(query, "AND", Commands.Enclose(nameof(DomainEvent.TimelineId), "IS", "NULL", "OR", nameof(DomainEvent.TimelineId), "=", string.Format(Commands.ParameterNameFormat, counter)));
                    counter++;

                    args.Add(timelineId.Value);
                }
                
                var collection = SQLiteDatabase.ExecuteQuery(eventType, tableName, query, args.ToArray());
                if (collection != null && collection.Count() > 0)
                {
                    foreach (var evt in collection)
                    {
                        events.Add((DomainEvent)evt);
                    }
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
            var eventType = @event.GetType();
            var tableName = eventType.Name;

            SQLiteDatabase.CreateOrUpdateTable(@event, eventType, tableName, autoIncrementalTableMigrations: true);

            var parametersData = @event.GetParametersData(eventType, SQLiteDatabase.StoreDateTimeAsTicks);

            var parametersNames = parametersData.Select(p => "?");

            var query = Commands.BuildInsertCommandText(tableName, parametersData.Select(t => t.Name).ToArray(), parametersNames.ToArray());

            var parameters = parametersData.Select(p => p.Value).ToArray();

            SQLiteDatabase.Execute(query, parameters);
        }
    }
}
