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
        //private string databasePath = Path.Combine(Path.GetTempPath(), "local.db");
        private string databasePath = Path.Combine(Path.GetTempPath(), "local2.db");

        [SetUp]
        public void SetUp()
        {
            var bus = new Mock<IEventDispatcher>().Object;
#if X86 || X64
            var sqlitePlatform = new SQLitePlatformWin32();
#else
            var sqlitePlatform = new SQLitePlatformGeneric(); 
#endif
            var sqliteConnection = CreateSQLiteConnection(sqlitePlatform, databasePath, storeDateTimeAsTicks: false);
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
            
            var events = EventStore.Find<PlainEvent>(pe => pe.AggregateId == @event.AggregateId);
            Assert.AreEqual(events.Count(), 1);
            Assert.AreEqual(events.First().Id, @event.Id);
            Assert.AreEqual(events.First().TimeStamp.ToLocalTime(), @event.TimeStamp.ToLocalTime());
            Assert.AreEqual(events.First().Title, @event.Title);
            Assert.AreEqual(events.First().Date.ToLocalTime(), @event.Date.ToLocalTime());
            Assert.AreEqual(events.First().Number, @event.Number);
        }

        [Test]
        public void Save_Should_Allow_Complex_Types()
        {
            var @event = new ComplexCollectionEvent(Guid.NewGuid(), new[]
            {
                new ComplexCollectionEvent.Component("Hi", 51)
            });
            var nestedClassEvent = new ComplexClassEvent(Guid.NewGuid(), new ComplexClassEvent.SecondClass("STAR WARS".Split(' ')));
            Executing.This(() => EventStore.Save(@event))
                .Should()
                .NotThrow();
            Executing.This(() => EventStore.Save(nestedClassEvent))
                .Should()
                .NotThrow();
        }

        [Test]
        public void Save_Should_Allow_Retrieval_Of_Complex_Types()
        {
            var @event = new ComplexCollectionEvent(Guid.NewGuid(), new[]
            {
                new ComplexCollectionEvent.Component("Hi", 51)
            });
            var eventToIgnore = new ComplexCollectionEvent(Guid.NewGuid(), new[]
            {
                new ComplexCollectionEvent.Component("Torino", 15)
            });
            var nestedClassEvent = new ComplexClassEvent(Guid.NewGuid(), new ComplexClassEvent.SecondClass("STAR WARS".Split(' ')));
            EventStore.Save(@event);
            EventStore.Save(eventToIgnore);
            EventStore.Save(nestedClassEvent);

            var events = EventStore.Find<ComplexCollectionEvent>(cce => cce.SecondId == @event.SecondId);
            Assert.AreEqual(events.Count(), 1);
            Assert.AreEqual(events.First().Id, @event.Id);
            Assert.AreEqual(events.First().SecondId, @event.SecondId);
            Assert.AreEqual(events.First().TimeStamp.ToLocalTime(), @event.TimeStamp.ToLocalTime());
            Assert.AreEqual(events.First().Components.First().Title, @event.Components.First().Title);
            Assert.AreEqual(events.First().Components.First().Number, @event.Components.First().Number);

            var nestedClassEvents = EventStore.Find<ComplexClassEvent>(cce => cce.AggId == nestedClassEvent.AggId);
            Assert.AreEqual(nestedClassEvents.First().Id, nestedClassEvent.Id);
            Assert.AreEqual(nestedClassEvents.First().TimeStamp.ToLocalTime(), nestedClassEvent.TimeStamp.ToLocalTime());
            Assert.AreEqual(nestedClassEvents.First().AggId, nestedClassEvent.AggId);
            Assert.AreNotEqual(nestedClassEvents.First().Second, null);
            Assert.Contains(nestedClassEvent.Second.Strings.First(), nestedClassEvents.First().Second.Strings);
            Assert.Contains(nestedClassEvent.Second.Strings.Last(), nestedClassEvents.First().Second.Strings);
        }

        [Test]
        public void RetrieveEvents_Should_Allow_Retrieval()
        {
            var firstEvent = new ComplexCollectionEvent(Guid.NewGuid(), new[]
            {
                new ComplexCollectionEvent.Component("Hi", 51)
            });
            var secondEvent = new ComplexCollectionEvent(Guid.NewGuid(), new[]
            {
                new ComplexCollectionEvent.Component("Torino", 15)
            });
            EventStore.Save(firstEvent);
            EventStore.Save(secondEvent);

            var eventDescriptors = new[]
            {
                new EventMapping { AggregateIdPropertyName = nameof(ComplexCollectionEvent.SecondId), EventType = typeof(ComplexCollectionEvent) }
            };

            var events = EventStore.RetrieveEvents(firstEvent.SecondId, DateTime.Now.AddDays(-1), eventDescriptors, timelineId: null)
                .Cast<ComplexCollectionEvent>();
            Assert.AreEqual(events.Count(), 0);
        }

        [Test]
        public void Find_Allow_Filter_By_Complex_Property()
        {
            var @event = new ComplexClassEvent(Guid.NewGuid(), new ComplexClassEvent.SecondClass(new string[0]));
            EventStore.Save(@event);

            var events = EventStore.Find<ComplexClassEvent>(e => e.Second != null && e.Second.Strings.Length == 0);
            Assert.IsNotEmpty(events.ToArray());
        }
    }
}
