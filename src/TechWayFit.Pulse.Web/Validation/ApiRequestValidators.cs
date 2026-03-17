using FluentValidation;
using TechWayFit.Pulse.Contracts.Requests;

namespace TechWayFit.Pulse.Web.Validation;

public sealed class CreateSessionRequestValidator : AbstractValidator<CreateSessionRequest>
{
    public CreateSessionRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Settings).NotNull();
        RuleFor(x => x.JoinFormSchema).NotNull();
        RuleFor(x => x)
            .Must(x => !x.SessionStart.HasValue || !x.SessionEnd.HasValue || x.SessionStart <= x.SessionEnd)
            .WithMessage("Session start must be earlier than or equal to session end.");
    }
}

public sealed class AddActivityRequestValidator : AbstractValidator<AddActivityRequest>
{
    public AddActivityRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Order).GreaterThan(0).When(x => x.Order.HasValue);
        RuleFor(x => x.DurationMinutes).GreaterThan(0).When(x => x.DurationMinutes.HasValue);
    }
}

public sealed class BulkCreateActivitiesRequestValidator : AbstractValidator<BulkCreateActivitiesRequest>
{
    public BulkCreateActivitiesRequestValidator()
    {
        RuleFor(x => x.Activities).NotEmpty().Must(a => a.Count <= 100);
        RuleForEach(x => x.Activities).SetValidator(new BulkActivityItemValidator());
    }
}

public sealed class BulkActivityItemValidator : AbstractValidator<BulkActivityItem>
{
    public BulkActivityItemValidator()
    {
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DurationMinutes).GreaterThan(0).When(x => x.DurationMinutes.HasValue);
    }
}

public sealed class SubmitResponseRequestValidator : AbstractValidator<SubmitResponseRequest>
{
    public SubmitResponseRequestValidator()
    {
        RuleFor(x => x.ParticipantId).NotEmpty();
        RuleFor(x => x.Payload).NotEmpty();
    }
}

public sealed class JoinParticipantRequestValidator : AbstractValidator<JoinParticipantRequest>
{
    public JoinParticipantRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(100)
            .When(x => !x.IsAnonymous);
    }
}

public sealed class UpdateSessionRequestValidator : AbstractValidator<UpdateSessionRequest>
{
    public UpdateSessionRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TtlMinutes).GreaterThan(0).When(x => x.TtlMinutes.HasValue);
        RuleFor(x => x)
            .Must(x => !x.SessionStart.HasValue || !x.SessionEnd.HasValue || x.SessionStart <= x.SessionEnd)
            .WithMessage("Session start must be earlier than or equal to session end.");
    }
}

public sealed class UpdateSessionSettingsRequestValidator : AbstractValidator<UpdateSessionSettingsRequest>
{
    public UpdateSessionSettingsRequestValidator()
    {
        RuleFor(x => x.TtlMinutes).GreaterThan(0);
    }
}

public sealed class GenerateActivitiesRequestValidator : AbstractValidator<GenerateActivitiesRequest>
{
    public GenerateActivitiesRequestValidator()
    {
        RuleFor(x => x.TargetActivityCount).GreaterThan(0).When(x => x.TargetActivityCount.HasValue);
        RuleFor(x => x.DurationMinutes).GreaterThan(0).When(x => x.DurationMinutes.HasValue);
        RuleFor(x => x.ParticipantCount).GreaterThan(0).When(x => x.ParticipantCount.HasValue);
    }
}

public sealed class SetQuadrantItemRequestValidator : AbstractValidator<SetQuadrantItemRequest>
{
    public SetQuadrantItemRequestValidator()
    {
        RuleFor(x => x.ItemIndex).GreaterThanOrEqualTo(0);
    }
}

public sealed class ReorderActivitiesRequestValidator : AbstractValidator<ReorderActivitiesRequest>
{
    public ReorderActivitiesRequestValidator()
    {
        RuleFor(x => x.ActivityIds).NotEmpty();
    }
}
