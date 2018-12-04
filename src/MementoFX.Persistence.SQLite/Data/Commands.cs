using System.Linq.Expressions;

namespace MementoFX.Persistence.SQLite.Data
{
    internal static class Commands
    {
        public const string AlterTableAddColumnFormat = "ALTER TABLE {0} ADD {1} {2}";
        public const string CreateIndexFormat = "CREATE INDEX {0} ON {1} ({2})";
        public const string CreateIndexNameFormat = "IX_{0}_{1}";
        public const string CreateTableFormat = "CREATE TABLE {0}";
        private const string InsertIntoFormat = "INSERT INTO {0} ({1}) VALUES ({2})";
        public const string ParameterNameFormat = "{{0}}";
        public const string SelectTableExistsFormat = "PRAGMA table_info({0})";
        public const string SelectTopFormat = "SELECT TOP({0}) {1} FROM {2}";
        private const string SelectWhereFormat = "SELECT {0} FROM {1} WHERE";
        public const string TableNameParameterName = "@table_name";

        public static string BuildInsertCommandText(string tableName, string[] columns, string[] values)
        {
            return string.Format(InsertIntoFormat, tableName, JoinWithComma(columns), JoinWithComma(values));
        }

        public static string BuildSelectWhereCommandText(string tableName, params string[] columns)
        {
            return string.Format(SelectWhereFormat, JoinWithComma(columns), tableName);
        }

        public static string Enclose(params string[] args) => $"({JoinWithSpace(args)})";

        public static string JoinWithComma(params string[] value) => string.Join(",", value);

        public static string JoinWithSpace(params string[] value) => string.Join(" ", value);

        public static string ToSqlString(this ExpressionType expressionType)
        {
            switch (expressionType)
            {
                case ExpressionType.Add:
                    return "+";
                case ExpressionType.And:
                    return "&";
                case ExpressionType.AndAlso:
                    return "AND";
                case ExpressionType.Divide:
                    return "/";
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.ExclusiveOr:
                    return "^";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.Modulo:
                    return "%";
                case ExpressionType.Multiply:
                    return "*";
                case ExpressionType.Negate:
                    return "-";
                case ExpressionType.Not:
                    return "NOT";
                case ExpressionType.NotEqual:
                    return "<>";
                case ExpressionType.Or:
                    return "|";
                case ExpressionType.OrElse:
                    return "OR";
                case ExpressionType.Subtract:
                    return "-";
            }

            throw new System.Exception($"Unsupported node type: {expressionType}");
        }
    }
}
