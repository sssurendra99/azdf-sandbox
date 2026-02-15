using System.Data;
using Dapper;
using Sandbox.Domain.ValueObjects;

namespace Sandbox.Infrastructure.Persistence.TypeHandlers;

public class EmailTypeHandler : SqlMapper.TypeHandler<Email?>
{
    public override void SetValue(IDbDataParameter parameter, Email? value)
    {
        parameter.Value = value?.Value ?? (object)DBNull.Value;
    }

    public override Email? Parse(object value)
    {
        if (value == null || value == DBNull.Value)
            return null;

        var emailString = value.ToString();
        if (string.IsNullOrWhiteSpace(emailString))
            return null;

        return Email.Create(emailString);
    }
}
