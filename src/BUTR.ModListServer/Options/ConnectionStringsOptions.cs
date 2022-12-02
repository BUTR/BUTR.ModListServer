using FluentValidation;

namespace BUTR.ModListServer.Options
{
    public sealed class ConnectionStringsOptionsValidator : AbstractValidator<ConnectionStringsOptions>
    {
        public ConnectionStringsOptionsValidator(HttpClient client)
        {
            RuleFor(x => x.Main).NotEmpty();
        }
    }

    public sealed record ConnectionStringsOptions
    {
        public required string Main { get; init; }
    }
}