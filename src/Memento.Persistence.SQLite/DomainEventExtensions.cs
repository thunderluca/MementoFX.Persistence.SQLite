using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memento.Persistence.SQLite
{
    public static class DomainEventExtensions
    {
        public static void SetId<T>(this T model, Guid id) where T : DomainEvent
        {
            typeof(T).BaseType.GetProperty(nameof(model.Id)).SetValue(model, id, null);
        }
    }
}
