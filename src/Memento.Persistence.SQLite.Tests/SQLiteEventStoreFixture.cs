using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Memento.Messaging;
using Memento.Persistence.SQLite.Tests.Events;
using Moq;
using SharpTestsEx;
#if X86 || X64
using SQLite.Net.Platform.Win32;
#else
using SQLite.Net.Platform.Generic;
#endif
using static Memento.Persistence.SQLite.SQLiteHelper;

namespace Memento.Persistence.SQLite.Tests
{
    [TestFixture]
    public class SQLiteEventStoreFixture
    {
        private IEventStore EventStore = null;
        private string databasePath = Path.Combine(Path.GetTempPath(), "local.db");

        [SetUp]
        public void SetUp()
        {
            var bus = new Mock<IEventDispatcher>().Object;
#if X86 || X64
            var sqlitePlatform = new SQLitePlatformWin32();
#else
            var sqlitePlatform = new SQLitePlatformGeneric(); 
#endif
            var sqliteConnection = CreateSQLiteConnection(sqlitePlatform, databasePath);
            EventStore = new SQLiteEventStore(sqliteConnection, bus);
        }

        [Test]
        public void SQLiteEventStore_Throws_When_EventDispatcher_Is_Null()
        {
            Executing.This(() => new SQLiteEventStore(null))
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ValueOf
                .ParamName
                .Should()
                .Be
                .EqualTo("eventDispatcher");
        }

        [Test]
        public void Save_Throws_When_Event_Is_Null()
        {
            Executing.This(() => EventStore.Save(null))
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ValueOf
                .ParamName
                .Should()
                .Be
                .EqualTo("event");
        }

        [Test]
        public void Save_Should_Allow_Retrieval()
        {
            var @event = new PlainEvent(Guid.NewGuid(), "Hello Memento", DateTime.UtcNow, double.MaxValue);
            var eventToIgnore = new PlainEvent(Guid.NewGuid(), "Hello Mastreeno", DateTime.UtcNow, 0.0D);
            EventStore.Save(@event);
            EventStore.Save(eventToIgnore);

            var eventDescriptors = new List<EventMapping>
            {
                new EventMapping { AggregateIdPropertyName = nameof(PlainEvent.AggregateId), EventType = typeof(PlainEvent) }
            };

            var events = EventStore.Find<PlainEvent>(pe => pe.AggregateId == @event.AggregateId);
            Assert.AreEqual(events.Count(), 1);
            Assert.AreEqual(events.First().Id, @event.Id);
            Assert.AreEqual(events.First().TimeStamp.ToLocalTime(), @event.TimeStamp.ToLocalTime());
            Assert.AreEqual(events.First().Title, @event.Title);
            Assert.AreEqual(events.First().Date, @event.Date);
            Assert.AreEqual(events.First().Number, @event.Number);
        }

        [Test]
        public void Save_Should_Allow_Complex_Types()
        {
            var @event = new ComplexEvent(Guid.NewGuid(), new[]
            {
                new ComplexEvent.Component("Hi", 51)
            });
            Executing.This(() => EventStore.Save(@event))
                .Should()
                .NotThrow();
        }

        [Test]
        public void Save_Should_Allow_Retrieval_Of_Complex_Types()
        {
            var @event = new ComplexEvent(Guid.NewGuid(), new[]
            {
                new ComplexEvent.Component("Hi", 51)
            });
            var eventToIgnore = new ComplexEvent(Guid.NewGuid(), new[]
            {
                new ComplexEvent.Component("Torino", 15)
            });
            EventStore.Save(@event);
            EventStore.Save(eventToIgnore);

            var eventDescriptors = new List<EventMapping>
            {
                new EventMapping { AggregateIdPropertyName = nameof(PlainEvent.AggregateId), EventType = typeof(PlainEvent) }
            };

            var events = EventStore.Find<ComplexEvent>(pe => pe.SecondId == @event.SecondId);
            Assert.AreEqual(events.Count(), 1);
            Assert.AreEqual(events.First().Id, @event.Id);
            Assert.AreEqual(events.First().TimeStamp.ToLocalTime(), @event.TimeStamp.ToLocalTime());
            Assert.AreEqual(events.First().Components.First().Title, @event.Components.First().Title);
            Assert.AreEqual(events.First().Components.First().Number, @event.Components.First().Number);
        }
    }
}
