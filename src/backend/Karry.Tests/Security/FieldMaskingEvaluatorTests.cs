using FluentAssertions;
using Karry.Application.Security;
using Karry.Domain.Identity;
using Xunit;

namespace Karry.Tests.Security;

public sealed class FieldMaskingEvaluatorTests
{
    private readonly FieldMaskingEvaluator _evaluator = new();

    [Theory]
    [InlineData(SystemRoles.Admin, Resources.Users, FieldVisibility.Visible)]
    [InlineData(SystemRoles.Operator, Resources.Shifts, FieldVisibility.Visible)]
    [InlineData(SystemRoles.Operator, Resources.WearParts, FieldVisibility.Masked)]
    [InlineData(SystemRoles.Executive, Resources.Users, FieldVisibility.Masked)]
    [InlineData(SystemRoles.Executive, Resources.Ledger, FieldVisibility.Visible)]
    [InlineData(SystemRoles.Operator, Resources.Ledger, FieldVisibility.Hidden)]
    [InlineData(SystemRoles.Storekeeper, Resources.Warehouse, FieldVisibility.Visible)]
    [InlineData(SystemRoles.Storekeeper, Resources.ScaleTickets, FieldVisibility.Hidden)]
    public void Evaluate_MatchesCatalog(string role, string resource, FieldVisibility expected)
    {
        _evaluator.Evaluate(role, resource).Should().Be(expected);
    }

    [Fact]
    public void Mask_HidesValue()
    {
        FieldMaskingEvaluator.Mask("Sensitive").Should().Be("••••••");
        FieldMaskingEvaluator.Mask(null).Should().Be("••••••");
    }
}