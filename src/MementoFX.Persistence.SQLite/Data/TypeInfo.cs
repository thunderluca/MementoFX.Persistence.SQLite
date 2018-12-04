using SQLite;

namespace MementoFX.Persistence.SQLite.Data
{
    internal class TypeInfo
    {
        public TypeInfo(SQLite3.ColType type, bool isNullable, bool isClass)
        {
            this.Type = type;
            this.IsNullable = isNullable;
            this.IsClass = isClass;
        }

        public SQLite3.ColType Type { get; }

        public bool IsNullable { get; }

        public bool IsClass { get; }

        public override string ToString()
        {
            return this.Type.ToString().ToUpper();
        }
    }
}
