using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memento.Persistence.SQLite.Tests.Events
{
    public class PlainEvent : DomainEvent
    {
        public PlainEvent(string title, DateTime date, double number)
        {
            this.Title = title;
            this.Date = date;
            this.Number = number;
        }

        public string Title { get; private set; }

        public DateTime Date { get; private set; }

        public double Number { get; private set; }
    }
}