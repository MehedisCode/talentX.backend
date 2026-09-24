using FluentValidation;
using TalentX.Application.Common.Models;

namespace TalentX.Application.Features.Companies.Queries.GetCompanies;

public sealed class GetCompaniesQueryValidator : AbstractValidator<GetCompaniesQuery>
{
    private static readonly string[] AllowedSortBy = ["name", "openings", "vacancies", "newest"];
    private static readonly string[] AllowedSortDir = ["asc", "desc"];

    public GetCompaniesQueryValidator()
    {
        RuleFor(x => x.SortBy)
            .Must(value => value is null || AllowedSortBy.Contains(value.Trim().ToLowerInvariant()))
            .WithMessage("SortBy must be one of: name, openings, vacancies, newest.")
            .When(x => x.SortBy is not null);

        RuleFor(x => x.SortDir)
            .Must(value => value is null || AllowedSortDir.Contains(value.Trim().ToLowerInvariant()))
            .WithMessage("SortDir must be one of: asc, desc.")
            .When(x => x.SortDir is not null);
    }
}
