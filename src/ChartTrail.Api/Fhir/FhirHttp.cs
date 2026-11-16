using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace ChartTrail.Api.Fhir;

public static class FhirHttp
{
    public const string ContentType = "application/fhir+json";

    public static readonly FhirJsonParser Parser =
        new(new ParserSettings { AcceptUnknownMembers = true, AllowUnrecognizedEnums = true });

    private static readonly FhirJsonSerializer Serializer = new();

    public static IResult Resource(Base resource, int status = 200) =>
        Results.Text(Serializer.SerializeToString(resource), ContentType, statusCode: status);

    public static IResult Error(int status, OperationOutcome.IssueType type, string message) =>
        Resource(new OperationOutcome
        {
            Issue =
            [
                new OperationOutcome.IssueComponent
                {
                    Severity = OperationOutcome.IssueSeverity.Error,
                    Code = type,
                    Diagnostics = message
                }
            ]
        }, status);
}