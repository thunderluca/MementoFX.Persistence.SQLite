using Newtonsoft.Json;
using SQLite.Net;
using SQLite.Net.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memento.Persistence.SQLite
{
    public static class SQLiteHelper
    {
        public static SQLiteConnection CreateSQLiteConnection(ISQLitePlatform platform, string path)
        {
            BlobSerializerDelegate.SerializeDelegate serializerDelegate = obj =>
            {
                return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
            };

            BlobSerializerDelegate.DeserializeDelegate deserializerDelegate = (data, type) =>
            {
                using (var stream = new MemoryStream(data))
                {
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        return JsonSerializer.Create().Deserialize(reader, type);
                    }
                }
            };
            
            BlobSerializerDelegate.CanSerializeDelegate canDeserializeDelegate = (type) =>
            {
                return !(type == typeof(int))
                    && !(type == typeof(long))
                    && !(type == typeof(long))
                    && !(type == typeof(bool))
                    && !(type == typeof(Enum))
                    && !(type == typeof(float))
                    && !(type == typeof(double))
                    && !(type == typeof(string))
                    && !(type == typeof(DateTime))
                    && !(type == typeof(DateTimeOffset))
                    && !(type == typeof(byte[]))
                    && !(type == typeof(Guid));
            };

            var serializer = new BlobSerializerDelegate(serializerDelegate, deserializerDelegate, canDeserializeDelegate);

            return new SQLiteConnection(platform, path, true, serializer);
        }
    }
}
