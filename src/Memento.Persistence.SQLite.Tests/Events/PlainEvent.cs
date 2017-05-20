using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memento.Persistence.SQLite.Tests.Events
{
    public class PlainEvent : DomainEvent
    {
        public PlainEvent() //SQLite ContractResolver need it
        {
        }

        public PlainEvent(Guid aggregateId, string title, DateTime date, double number)
        {
            this.Id = Guid.NewGuid();
            this.AggregateId = aggregateId;
            this.Title = title;
            this.Date = date;
            this.Number = number;
        }

        public Guid Id { get; internal set; } //SQLite cannot use DomainEvent.Id because of private setter

        public Guid AggregateId { get; private set; }

        public string Title { get; private set; }

        public DateTime Date { get; private set; }

        public double Number { get; private set; }
    }
}