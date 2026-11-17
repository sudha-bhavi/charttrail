using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Model;
namespace ChartTrail.Api.Fhir;

public static class FhirHttp
{
    public const string ContentType = "application/fhir+json";

    public static readonly FhirJsonDeserializer Parser =
        new(new DeserializerSettings { AcceptUnknownMembers = true, AllowUnrecognizedEnums = true });

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

            public static Bundle SearchSet(IEnumerable<Resource> resources, int total, string baseUrl)
    {
        var bundle = new Bundle { Type = Bundle.BundleType.Searchset, Total = total };
        foreach (var resource in resources)
        {
            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"{baseUrl}/{resource.TypeName}/{resource.Id}",
                Resource = resource,
                Search = new Bundle.SearchComponent { Mode = Bundle.SearchEntryMode.Match }
            });
        }
        return bundle;
    }
}