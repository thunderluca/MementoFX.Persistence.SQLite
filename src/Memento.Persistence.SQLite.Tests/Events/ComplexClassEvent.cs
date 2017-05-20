﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memento.Persistence.SQLite.Tests.Events
{
    public class ComplexClassEvent : DomainEvent
    {
        public ComplexClassEvent() //SQLite ContractResolver need it
        {
        }

        public ComplexClassEvent(Guid aggId, SecondClass second)
        {
            this.Id = Guid.NewGuid();
            this.AggId = aggId;
            this.Second = second;
        }

        public Guid Id { get; internal set; } //SQLite cannot use DomainEvent.Id because of private setter

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
