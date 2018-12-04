using System;

namespace MementoFX.Persistence.SQLite.Tests.Events
{
    public class PlainEvent : DomainEvent
    {
        public PlainEvent(Guid aggregateId, string title, DateTime date, double number)
        {
            this.AggregateId = aggregateId;
            this.Title = title;
            this.Date = date;
            this.Number = number;
        }

        public Guid AggregateId { get; private set; }

        public string Title { get; private set; }

        public DateTime Date { get; private set; }

        public double Number { get; private set; }
    }
}