using System;
using System.Collections.Generic;

namespace Memento.Persistence.SQLite
{
    /// <summary>
    /// Provides a collection of methods
    /// for SQLite database management
    /// </summary>
    public partial class SQLiteEventStore
    {
        private static Type[] SQLiteSuppoertedTypes =
        {
            typeof(int),
            typeof(long),
            typeof(bool),
            typeof(Enum),
            typeof(float),
            typeof(double),
            typeof(string),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(byte[]),
            typeof(Guid)
        };

        internal static IEnumerable<object> GetQueryParametersCollection(Guid aggregateId, DateTime pointInTime, Guid? timelineId)
        {
            var queryParameters = new List<object>
            {
                aggregateId,
                pointInTime.ToISO8601Date()
            };

            if (timelineId.HasValue)
                queryParameters.Add(timelineId.Value);

            return queryParameters;
        }
    }
}
