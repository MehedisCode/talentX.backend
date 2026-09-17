namespace TalentX.Application.Common.Models;

public sealed record RenderJobDeactivatedEmailParameters(
    string RecipientName,
    string JobTitle,
    string ReasonLabel,
    string Details,
    string EditJobUrl
);
