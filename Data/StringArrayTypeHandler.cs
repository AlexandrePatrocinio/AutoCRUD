using System.Data;
using Dapper;

public class StringArrayTypeHandler : SqlMapper.TypeHandler<string[]>
{
    public override void SetValue(IDbDataParameter parameter, string[]? value)
    {
        parameter.Value = string.Join(",", value ?? new String[] {});
    }

    public override string[]? Parse(object value)
    {
        return (value?.ToString() ?? string.Empty).Split(',');
    }
}
