using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memento.Persistence.SQLite.Tests.Events
{
    public class ComplexCollectionEvent : DomainEvent
    {
        public ComplexCollectionEvent() //SQLite ContractResolver need it
        {
        }

        public ComplexCollectionEvent(Guid secondId, Component[] components)
        {
            this.Id = Guid.NewGuid();
            this.SecondId = secondId;
            this.Components = components;
        }

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        public Guid Id { get; internal set; } //SQLite cannot use DomainEvent.Id because of private setter
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword

        public Guid SecondId { get; private set; }

        public Component[] Components { get; private set; }

        public class Component
        {
            public Component(string title, int number)
            {
                this.Title = title;
                this.Number = number;
            }

            public string Title { get; private set; }

            public int Number { get; private set; }
        }
    }
}
