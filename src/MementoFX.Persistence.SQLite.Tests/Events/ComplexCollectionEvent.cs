using System;

namespace MementoFX.Persistence.SQLite.Tests.Events
{
    public class ComplexCollectionEvent : DomainEvent
    {
        public ComplexCollectionEvent() //SQLite ContractResolver need it
        {
        }

        public ComplexCollectionEvent(Guid secondId, Component[] components)
        {
            this.SecondId = secondId;
            this.Components = components;
        }

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
