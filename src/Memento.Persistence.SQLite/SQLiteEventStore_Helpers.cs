using System;
using System.Collections.Generic;

namespace Memento.Persistence.SQLite
{
    public partial class SQLiteEventStore
    {
        private static IEnumerable<object> GetQueryParametersCollection(Guid aggregateId, DateTime pointInTime, Guid? timelineId)
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
