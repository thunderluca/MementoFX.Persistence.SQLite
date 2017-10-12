using System;

namespace Memento.Persistence.SQLite.Tests.Events
{
    public class ComplexClassEvent : DomainEvent
    {
        public ComplexClassEvent() //SQLite ContractResolver need it
        {
        }

        public ComplexClassEvent(Guid aggId, SecondClass second)
        {
            this.AggId = aggId;
            this.Second = second;
        }

        public Guid AggId { get; private set; }
        
        public SecondClass Second { get; private set; }

        public class SecondClass
        {
            public SecondClass(string[] strings)
            {
                this.Strings = strings;
            }
            
            public string[] Strings { get; private set; }
        }
    }
}
