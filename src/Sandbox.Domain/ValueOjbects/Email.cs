using Sandbox.Domain.Exceptions;

namespace Sandbox.Domain.ValueObjects;

public class Email
{
    public string Value { get; private set; }

    private Email(string Value)
    {
        this.Value = Value;
    }

    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty");

        if (!email.Contains("@"))
                throw new DomainException("Invalid email format");

        return new Email(email);
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}