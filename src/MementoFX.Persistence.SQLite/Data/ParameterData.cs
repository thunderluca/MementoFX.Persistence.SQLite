using SQLite;
using System;

namespace MementoFX.Persistence.SQLite.Data
{
    internal class ParameterData
    {
        public ParameterData(string name, object value, SQLite3.ColType type)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(nameof(name));
            }

            this.Name = name;
            this.Value = value;
            this.Type = type;
        }

        public string Name { get; }

        public SQLite3.ColType Type { get; }

        public object Value { get; set; }

        public bool HasValue
        {
            get { return this.Value != null; }
        }
    }
}
