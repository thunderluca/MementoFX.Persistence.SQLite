using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memento.Persistence.SQLite.Tests.Events
{
    public class ComplexCollectionEvent : DomainEvent
    {
        public ComplexCollectionEvent()
        {
        }

        public ComplexCollectionEvent(Guid secondId, Component[] components)
        {
            this.Id = Guid.NewGuid();
            this.SecondId = secondId;
            this.Components = components;
        }
        
        public Guid Id { get; internal set; }

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
