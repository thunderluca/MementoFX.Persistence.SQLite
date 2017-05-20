using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memento.Persistence.SQLite.Tests.Events
{
    public class PlainEvent : DomainEvent
    {
        public PlainEvent() { }

        public PlainEvent(Guid aggregateId, string title, DateTime date, double number) : base()
        {
            this.Id = Guid.NewGuid();
            this.AggregateId = aggregateId;
            this.Title = title;
            this.Date = date;
            this.Number = number;
        }

        [PrimaryKey]
        public Guid Id { get; internal set; }

        public Guid AggregateId { get; private set; }

        public string Title { get; private set; }

        public DateTime Date { get; private set; }

        public double Number { get; private set; }
    }
}