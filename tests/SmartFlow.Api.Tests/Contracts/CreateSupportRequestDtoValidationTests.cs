using System.ComponentModel.DataAnnotations;
using SmartFlow.Api.Contracts;
using SmartFlow.Api.Models;

namespace SmartFlow.Api.Tests.Contracts;

/// <summary>
/// Verifies the validation rules of the payload that n8n sends to POST /api/requests.
/// The LLM output is untrusted, so these rules are the last gate before the database.
/// </summary>
public class CreateSupportRequestDtoValidationTests
{
    // A payload that satisfies every rule. Each test changes exactly one field.
    private static CreateSupportRequestDto ValidDto() => new()
    {
        OriginalText = "I was charged twice for my November invoice.",
        SenderEmail = "customer@example.com",
        Category = RequestCategory.Billing,
        Priority = RequestPriority.High,
        Summary = "Customer was charged twice for the November invoice.",
        SuggestedAction = "Check the payment provider and refund the duplicate charge.",
        ModelName = "llama3.1:8b"
    };

    // Runs the same DataAnnotations validation that ASP.NET Core uses
    // and returns the names of the fields that failed.
    private static List<string> InvalidMembers(CreateSupportRequestDto dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        return results.SelectMany(r => r.MemberNames).ToList();
    }

    [Fact]
    public void Valid_payload_has_no_errors()
    {
        Assert.Empty(InvalidMembers(ValidDto()));
    }

    [Fact]
    public void Payload_without_sender_email_is_valid()
    {
        var dto = ValidDto() with { SenderEmail = null };

        Assert.Empty(InvalidMembers(dto));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_original_text_is_rejected(string? value)
    {
        var dto = ValidDto() with { OriginalText = value };

        Assert.Equal(["OriginalText"], InvalidMembers(dto));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_summary_is_rejected(string? value)
    {
        var dto = ValidDto() with { Summary = value };

        Assert.Equal(["Summary"], InvalidMembers(dto));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_suggested_action_is_rejected(string? value)
    {
        var dto = ValidDto() with { SuggestedAction = value };

        Assert.Equal(["SuggestedAction"], InvalidMembers(dto));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Missing_model_name_is_rejected(string? value)
    {
        var dto = ValidDto() with { ModelName = value };

        Assert.Equal(["ModelName"], InvalidMembers(dto));
    }

    // Boundary tests: the maximum length is accepted, one character more is rejected.
    [Theory]
    [InlineData(5000, true)]
    [InlineData(5001, false)]
    public void Original_text_length_limit_is_5000(int length, bool expectedValid)
    {
        var dto = ValidDto() with { OriginalText = new string('a', length) };

        Assert.Equal(expectedValid, InvalidMembers(dto).Count == 0);
    }

    [Theory]
    [InlineData(300, true)]
    [InlineData(301, false)]
    public void Summary_length_limit_is_300(int length, bool expectedValid)
    {
        var dto = ValidDto() with { Summary = new string('a', length) };

        Assert.Equal(expectedValid, InvalidMembers(dto).Count == 0);
    }

    [Theory]
    [InlineData(500, true)]
    [InlineData(501, false)]
    public void Suggested_action_length_limit_is_500(int length, bool expectedValid)
    {
        var dto = ValidDto() with { SuggestedAction = new string('a', length) };

        Assert.Equal(expectedValid, InvalidMembers(dto).Count == 0);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    public void Malformed_sender_email_is_rejected(string value)
    {
        var dto = ValidDto() with { SenderEmail = value };

        Assert.Equal(["SenderEmail"], InvalidMembers(dto));
    }

    [Fact]
    public void Missing_category_is_rejected()
    {
        var dto = ValidDto() with { Category = null };

        Assert.Equal(["Category"], InvalidMembers(dto));
    }

    [Fact]
    public void Undefined_category_value_is_rejected()
    {
        // JSON like {"category": 99} would otherwise slip through as a number.
        var dto = ValidDto() with { Category = (RequestCategory)99 };

        Assert.Equal(["Category"], InvalidMembers(dto));
    }

    [Fact]
    public void Missing_priority_is_rejected()
    {
        var dto = ValidDto() with { Priority = null };

        Assert.Equal(["Priority"], InvalidMembers(dto));
    }

    [Fact]
    public void Undefined_priority_value_is_rejected()
    {
        var dto = ValidDto() with { Priority = (RequestPriority)99 };

        Assert.Equal(["Priority"], InvalidMembers(dto));
    }
}
