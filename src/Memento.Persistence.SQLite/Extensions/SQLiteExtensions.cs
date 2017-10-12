using System;

namespace SQLite
{
    internal static class SQLiteExtensions
    {
        internal static void CreateOrMigrateTable<T>(this SQLiteConnection connection)
        {
            CreateOrMigrateTable(connection, typeof(T));
        }

        internal static void CreateOrMigrateTable(this SQLiteConnection connection, Type tableType)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            connection.CreateTable(tableType, CreateFlags.ImplicitPK | CreateFlags.ImplicitIndex);
        }
    }
}
