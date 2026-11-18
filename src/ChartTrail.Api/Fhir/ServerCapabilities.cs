using Hl7.Fhir.Model;

namespace ChartTrail.Api.Fhir;

public static class ServerCapabilities
{
    public static void MapMetadataEndpoint(this WebApplication app) =>
        app.MapGet("/metadata", () => FhirHttp.Resource(Build()));

    public static CapabilityStatement Build() => new()
    {
        Status = PublicationStatus.Active,
        Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
        Kind = CapabilityStatementKind.Instance,
        FhirVersion = FHIRVersion.N4_0_1,
        Format = ["application/fhir+json"],
        Software = new CapabilityStatement.SoftwareComponent { Name = "ChartTrail", Version = "0.1.0" },
        Rest =
        [
            new CapabilityStatement.RestComponent
            {
                Mode = CapabilityStatement.RestfulCapabilityMode.Server,
                Documentation = "Read-only search over synthetic Synthea patient data. "
                    + "All searches also accept _count (page size).",
                Resource =
                [
                    new CapabilityStatement.ResourceComponent
                    {
                        Type = "Patient",
                        Interaction =
                        [
                            new CapabilityStatement.ResourceInteractionComponent
                                { Code = CapabilityStatement.TypeRestfulInteraction.Read },
                            new CapabilityStatement.ResourceInteractionComponent
                                { Code = CapabilityStatement.TypeRestfulInteraction.SearchType }
                        ],
                        SearchParam =
                        [
                            new CapabilityStatement.SearchParamComponent
                            {
                                Name = "name",
                                Type = SearchParamType.String,
                                Documentation = "Case-insensitive prefix match on family or given name"
                            }
                        ]
                    },
                    new CapabilityStatement.ResourceComponent
                    {
                        Type = "Observation",
                        Interaction =
                        [
                            new CapabilityStatement.ResourceInteractionComponent
                                { Code = CapabilityStatement.TypeRestfulInteraction.SearchType }
                        ],
                        SearchParam =
                        [
                            new CapabilityStatement.SearchParamComponent
                            {
                                Name = "patient",
                                Type = SearchParamType.Reference,
                                Documentation = "Patient id, or Patient/<id>"
                            },
                            new CapabilityStatement.SearchParamComponent
                            {
                                Name = "code",
                                Type = SearchParamType.Token,
                                Documentation = "LOINC code, or system|code"
                            },
                            new CapabilityStatement.SearchParamComponent
                            {
                                Name = "category",
                                Type = SearchParamType.Token,
                                Documentation = "Observation category, for example laboratory"
                            },
                            new CapabilityStatement.SearchParamComponent
                            {
                                Name = "_sort",
                                Type = SearchParamType.Special,
                                Documentation = "date (oldest first) or -date (newest first)"
                            }
                        ]
                    }
                ]
            }
        ]
    };
}