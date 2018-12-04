using SQLite;

namespace MementoFX.Persistence.SQLite.Data
{
    public class DbColumn
    {
        [Column("cid")]
        public int Cid { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("type")]
        public string Type { get; set; }

        [Column("notnull")]
        public int NotNull { get; set; }
        
        [Column("pk")]
        public int PrimaryKey { get; set; }
    }
}
