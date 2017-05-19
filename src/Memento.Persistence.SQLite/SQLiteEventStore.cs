using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Memento.Messaging;
using System.Reflection;
using SQLite.Net;
using SQLite.Net.Interop;
using SQLite.Net.Platform.Generic;

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

                SQLiteDatabase = new SQLiteConnection(new SQLitePlatformGeneric(), connectionString);
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
            SQLiteDatabase.CreateTable<T>(CreateFlags.ImplicitPK);

            return SQLiteDatabase.Table<T>().Where(filter);
        }

        private static string GetCommandTextSuffix(Guid? timelineId)
        {
            if (!timelineId.HasValue)
                return $" AND {nameof(DomainEvent.TimelineId)} is null";
            else
                return $" AND {nameof(DomainEvent.TimelineId)} is null OR {nameof(DomainEvent.TimelineId)} = $timelineId";
        }

        public override IEnumerable<DomainEvent> RetrieveEvents(Guid aggregateId, DateTime pointInTime, IEnumerable<EventMapping> eventDescriptors, Guid? timelineId)
        {
            var firstDayYear = new DateTime(pointInTime.Year, 1, 1);

            var events = new List<DomainEvent>();

            var descriptorsGrouped = eventDescriptors
                .GroupBy(k => k.EventType);

            var suffixCommandText = GetCommandTextSuffix(timelineId);

            foreach (var descriptorsGroup in descriptorsGrouped)
            {
                var eventType = descriptorsGroup.Key;
                var tableName = eventType.Name;

                foreach (var eventDescriptor in descriptorsGroup)
                {
                    var commandText = $"SELECT * FROM $tableName WHERE $aggregateIdPropertyName = $aggregateId AND strftime('%s', {nameof(DomainEvent.TimeStamp)}) BETWEEN strftime('%s', $firstDayYear) AND strftime('%s', $pointInTime)";
                    commandText += suffixCommandText;

                    var command = SQLiteDatabase.CreateCommand(commandText);
                    command.Bind("$tableName", tableName);
                    command.Bind("$aggregateIdPropertyName", eventDescriptor.AggregateIdPropertyName);
                    command.Bind("$aggregateId", aggregateId);
                    command.Bind("$firstDayYear", firstDayYear.ToString("yyyy-MM-dd"));
                    command.Bind("$pointInTime", firstDayYear.ToString("yyyy-MM-dd"));

                    var collection = command.ExecuteQuery<object>(SQLiteDatabase.GetMapping(eventType));
                    foreach (var evt in collection)
                        events.Add((DomainEvent)evt);
                }
            }

            return events.OrderBy(e => e.TimeStamp);
        }

        protected override void _Save(DomainEvent @event)
        {
            var eventType = @event.GetType();

            SQLiteDatabase.CreateTable(eventType);

            var propertiesNames = eventType
                .GetProperties(BindingFlags.Public)
                .Select(pi => pi.Name)
                .ToArray();

            var propertiesValues = propertiesNames
                .Select(pn => eventType.GetProperty(pn).GetValue(@event, null))
                .ToArray();

            var commandText = "INSERT INTO $tableName (";
            for (var i = 0; i < propertiesNames.Length; i++)
            {
                commandText += $"$c{i},";
            }
            commandText += ") VALUES (";
            for (var i = 0; i < propertiesValues.Length; i++)
            {
                commandText += $"$v{i},";
            }
            commandText += ")";

            var command = SQLiteDatabase.CreateCommand(commandText);
            command.Bind("$tableName", eventType.Name);
            for (var i = 0; i < propertiesNames.Length; i++)
            {
                command.Bind($"$c{i}", propertiesNames[i]);
            }
            for (var i = 0; i < propertiesValues.Length; i++)
            {
                command.Bind($"$v{i}", propertiesValues[i]);
            }

            command.ExecuteNonQuery();
        }
    }
}
