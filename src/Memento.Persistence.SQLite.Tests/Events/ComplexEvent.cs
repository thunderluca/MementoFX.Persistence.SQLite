﻿using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memento.Persistence.SQLite.Tests.Events
{
    public class ComplexEvent : DomainEvent
    {
        public ComplexEvent() { }

        public ComplexEvent(Guid secondId, Component[] components)
        {
            this.Id = Guid.NewGuid();
            this.SecondId = secondId;
            this.Components = components;
        }

        [PrimaryKey]
        public Guid Id { get; internal set; }

        public Guid SecondId { get; private set; }

        public Component[] Components { get; private set; }

        public class Component
        {
            public Component() { }

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
