using System;

namespace MementoFX.Persistence.SQLite.Tests.Events
{
    public class ComplexClassEvent : DomainEvent
    {
        public ComplexClassEvent(Guid aggId, SecondClass second)
        {
            this.AggId = aggId;
            this.Second = second;
        }

        public Guid AggId { get; private set; }
        
        public SecondClass Second { get; private set; }

        public class SecondClass
        {
            public SecondClass(string[] strings, TimeSpan time)
            {
                this.Strings = strings;
                this.Time = time;
            }

            public TimeSpan Time { get; set; }

            public string[] Strings { get; private set; }
        }
    }
}
