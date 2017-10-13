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
