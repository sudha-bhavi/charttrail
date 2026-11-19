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
                            Interaction(CapabilityStatement.TypeRestfulInteraction.Read),
                            Interaction(CapabilityStatement.TypeRestfulInteraction.SearchType)
                        ],
                        SearchParam =
                        [
                            Param("name", SearchParamType.String, "Case-insensitive prefix match on family or given name")
                        ]
                    },
                    new CapabilityStatement.ResourceComponent
                    {
                        Type = "Observation",
                        Interaction = [Interaction(CapabilityStatement.TypeRestfulInteraction.SearchType)],
                        SearchParam =
                        [
                            Param("patient", SearchParamType.Reference, "Patient id, or Patient/<id>"),
                            Param("code", SearchParamType.Token, "LOINC code, or system|code"),
                            Param("category", SearchParamType.Token, "Observation category, for example laboratory"),
                            Param("_sort", SearchParamType.Special, "date (oldest first) or -date (newest first)")
                        ]
                    },
                    new CapabilityStatement.ResourceComponent
                    {
                        Type = "Encounter",
                        Interaction = [Interaction(CapabilityStatement.TypeRestfulInteraction.SearchType)],
                        SearchParam =
                        [
                            Param("patient", SearchParamType.Reference, "Patient id, or Patient/<id>"),
                            Param("_sort", SearchParamType.Special, "date (oldest first) or -date (newest first)")
                        ]
                    },
                    new CapabilityStatement.ResourceComponent
                    {
                        Type = "MedicationRequest",
                        Interaction = [Interaction(CapabilityStatement.TypeRestfulInteraction.SearchType)],
                        SearchParam =
                        [
                            Param("patient", SearchParamType.Reference, "Patient id, or Patient/<id>"),
                            Param("_sort", SearchParamType.Special, "date (oldest first) or -date (newest first)")
                        ]
                    }
                ]
            }
        ]
    };

    private static CapabilityStatement.ResourceInteractionComponent Interaction(
        CapabilityStatement.TypeRestfulInteraction code) => new() { Code = code };

    private static CapabilityStatement.SearchParamComponent Param(
        string name, SearchParamType type, string documentation) =>
        new() { Name = name, Type = type, Documentation = documentation };
}