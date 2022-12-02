using FluentValidation;

namespace BUTR.ModListServer.Options
{
    public sealed class ModListUploadOptionsValidator : AbstractValidator<ModListUploadOptions>
    {
        public ModListUploadOptionsValidator()
        {
            RuleFor(x => x.BaseUri).NotEmpty();
        }
    }

    public record ModListUploadOptions
    {
        public string? BaseUri { get; set; }
    }
}