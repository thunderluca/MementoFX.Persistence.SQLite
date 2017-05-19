using NUnit.Framework;
using SQLite.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Memento.Messaging;
using Moq;
using SharpTestsEx;
using Memento.Persistence.SQLite.Tests.Events;
#if X86 || X64
using SQLite.Net.Platform.Win32;
#else
using SQLite.Net.Platform.Generic;
#endif

namespace Memento.Persistence.SQLite.Tests
{
    [TestFixture]
    public class SQLiteEventStoreFixture
    {
        private IEventStore EventStore = null;
        private string databasePath = Path.GetTempFileName();

        [SetUp]
        public void SetUp()
        {
            var bus = new Mock<IEventDispatcher>().Object;
#if X86 || X64
            var sqlitePlatform = new SQLitePlatformWin32();
#else
            var sqlitePlatform = new SQLitePlatformGeneric(); 
#endif
            var sqliteConnection = new SQLiteConnection(sqlitePlatform, databasePath);
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
            var @event = new PlainEvent("Hello Memento", DateTime.UtcNow, double.MaxValue);
            EventStore.Save(@event);

            var events = EventStore.Find<PlainEvent>(pe => pe.Id == @event.Id);
            Assert.AreEqual(events.Count(), 1);
            Assert.AreEqual(events.First().Id, @event.Id);
            Assert.AreEqual(events.First().Title, @event.Title);
            Assert.AreEqual(events.First().Date, @event.Date);
            Assert.AreEqual(events.First().Number, @event.Number);
        }
    }
}
