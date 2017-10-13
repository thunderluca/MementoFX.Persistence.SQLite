using Memento.Messaging;
using Memento.Persistence.SQLite.Tests.Events;
using Moq;
using NUnit.Framework;
using SharpTestsEx;
using SQLite;
using System;
using System.IO;
using System.Linq;

namespace Memento.Persistence.SQLite
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

            var sqliteConnection = new SQLiteConnection(databasePath);
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

            //var events = EventStore.Find<PlainEvent>(pe => pe.AggregateId == @event.AggregateId).ToArray();
            var events = ((SQLiteEventStore)EventStore)._Find<PlainEvent>(pe => pe.AggregateId == @event.AggregateId).ToArray();
            Assert.AreEqual(events.Length, 1);
            Assert.AreEqual(events.First().Id, @event.Id);
            Assert.AreEqual(events.First().TimeStamp.ToMilliSeconds(), @event.TimeStamp.ToMilliSeconds());
            Assert.AreEqual(events.First().Title, @event.Title);
            Assert.AreEqual(events.First().Date, @event.Date);
            Assert.AreEqual(events.First().Number, @event.Number);
        }

        [Test]
        public void RetrieveEvents_Should_Allow_Retrieval()
        {
            var firstEvent = new PlainEvent(Guid.NewGuid(), "Hello Memento", DateTime.UtcNow, double.MaxValue);
            var secondEvent = new PlainEvent(Guid.NewGuid(), "Hello Mastreeno", DateTime.UtcNow, 0.0D);
            EventStore.Save(firstEvent);
            EventStore.Save(secondEvent);

            var eventDescriptors = new[]
            {
                new EventMapping { AggregateIdPropertyName = nameof(PlainEvent.AggregateId), EventType = typeof(PlainEvent) }
            };

            var events = EventStore.RetrieveEvents(firstEvent.AggregateId, firstEvent.TimeStamp.AddDays(1), eventDescriptors, timelineId: null)
                .Cast<PlainEvent>()
                .ToArray();
            Assert.AreEqual(events.Length, 1);
        }

        //[Test]
        //public void Save_Should_Allow_Complex_Types()
        //{
        //    var @event = new ComplexCollectionEvent(Guid.NewGuid(), new[]
        //    {
        //        new ComplexCollectionEvent.Component("Hi", 51)
        //    });
        //    var nestedClassEvent = new ComplexClassEvent(Guid.NewGuid(), new ComplexClassEvent.SecondClass("STAR WARS".Split(' ')));
        //    Executing.This(() => EventStore.Save(@event))
        //        .Should()
        //        .NotThrow();
        //    Executing.This(() => EventStore.Save(nestedClassEvent))
        //        .Should()
        //        .NotThrow();
        //}

        //[Test]
        //public void Save_Should_Allow_Retrieval_Of_Complex_Types()
        //{
        //    var @event = new ComplexCollectionEvent(Guid.NewGuid(), new[]
        //    {
        //        new ComplexCollectionEvent.Component("Hi", 51)
        //    });
        //    var eventToIgnore = new ComplexCollectionEvent(Guid.NewGuid(), new[]
        //    {
        //        new ComplexCollectionEvent.Component("Torino", 15)
        //    });
        //    var nestedClassEvent = new ComplexClassEvent(Guid.NewGuid(), new ComplexClassEvent.SecondClass("STAR WARS".Split(' ')));
        //    EventStore.Save(@event);
        //    EventStore.Save(eventToIgnore);
        //    EventStore.Save(nestedClassEvent);

        //    //var events = EventStore.Find<ComplexCollectionEvent>(cce => cce.SecondId == @event.SecondId).ToArray();
        //    var events = ((SQLiteEventStore)EventStore)._Find<ComplexCollectionEvent>(cce => cce.SecondId == @event.SecondId).ToArray();
        //    Assert.AreEqual(events.Length, 1);
        //    Assert.AreEqual(events.First().Id, @event.Id);
        //    Assert.AreEqual(events.First().SecondId, @event.SecondId);
        //    Assert.AreEqual(events.First().TimeStamp, @event.TimeStamp);
        //    Assert.AreEqual(events.First().Components.First().Title, @event.Components.First().Title);
        //    Assert.AreEqual(events.First().Components.First().Number, @event.Components.First().Number);

        //    //var nestedClassEvents = EventStore.Find<ComplexClassEvent>(cce => cce.AggId == nestedClassEvent.AggId);
        //    var nestedClassEvents = ((SQLiteEventStore)EventStore)._Find<ComplexClassEvent>(cce => cce.AggId == nestedClassEvent.AggId);
        //    Assert.AreEqual(nestedClassEvents.First().Id, nestedClassEvent.Id);
        //    Assert.AreEqual(nestedClassEvents.First().TimeStamp, nestedClassEvent.TimeStamp);
        //    Assert.AreEqual(nestedClassEvents.First().AggId, nestedClassEvent.AggId);
        //    Assert.AreNotEqual(nestedClassEvents.First().Second, null);
        //    Assert.Contains(nestedClassEvent.Second.Strings.First(), nestedClassEvents.First().Second.Strings);
        //    Assert.Contains(nestedClassEvent.Second.Strings.Last(), nestedClassEvents.First().Second.Strings);
        //}

        //[Test]
        //public void Find_Allow_Filter_By_Complex_Property()
        //{
        //    var @event = new ComplexClassEvent(Guid.NewGuid(), new ComplexClassEvent.SecondClass(new string[0]));
        //    EventStore.Save(@event);

        //    //var events = EventStore.Find<ComplexClassEvent>(e => e.Second != null && e.Second.Strings.Length == 0).ToArray();
        //    var events = ((SQLiteEventStore)EventStore)
        //        ._Find<ComplexClassEvent>(e => e.Second != null && e.Second.Strings.Length == 0)
        //        .ToArray();

        //    Assert.IsNotEmpty(events);
        //}
    }

    public static class DateTimeExtensions
    {
        internal static long ToMilliSeconds(this DateTime dateTime)
        {
            if (dateTime == null)
            {
                throw new ArgumentNullException(nameof(dateTime));
            }

            return (long)dateTime.ToUniversalTime()
                .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                .TotalMilliseconds;
        }
    }
}
