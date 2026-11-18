namespace ChartTrail.Api.Tests;

// Hand-written synthetic data. No real person.
public static class TestBundle
{
    public const string Json = """
        {
          "resourceType": "Bundle",
          "type": "transaction",
          "entry": [
            { "resource": {
                "resourceType": "Patient", "id": "test-patient-1",
                "name": [ { "family": "Testerson", "given": [ "Ada" ] } ],
                "gender": "female", "birthDate": "1985-04-12" } },
            { "resource": {
                "resourceType": "Encounter", "id": "test-enc-1", "status": "finished",
                "class": { "system": "http://terminology.hl7.org/CodeSystem/v3-ActCode", "code": "AMB" },
                "type": [ { "coding": [ { "display": "Well child visit" } ] } ],
                "subject": { "reference": "urn:uuid:test-patient-1" },
                "period": { "start": "2020-01-10T09:00:00-05:00", "end": "2020-01-10T09:30:00-05:00" } } },
            { "resource": {
                "resourceType": "MedicationRequest", "id": "test-med-1", "status": "active", "intent": "order",
                "medicationCodeableConcept": { "coding": [ { "code": "197361", "display": "Amlodipine 5 MG Oral Tablet" } ] },
                "subject": { "reference": "urn:uuid:test-patient-1" },
                "authoredOn": "2020-01-10T09:10:00-05:00" } },
            { "resource": {
                "resourceType": "Observation", "id": "test-obs-1", "status": "final",
                "category": [ { "coding": [ { "code": "laboratory" } ] } ],
                "code": { "coding": [ { "system": "http://loinc.org", "code": "2339-0", "display": "Glucose" } ] },
                "subject": { "reference": "urn:uuid:test-patient-1" },
                "effectiveDateTime": "2020-01-10T09:15:00-05:00",
                "valueQuantity": { "value": 95, "unit": "mg/dL" } } },
            { "resource": {
                "resourceType": "Observation", "id": "test-obs-2", "status": "final",
                "category": [ { "coding": [ { "code": "laboratory" } ] } ],
                "code": { "coding": [ { "system": "http://loinc.org", "code": "2339-0", "display": "Glucose" } ] },
                "subject": { "reference": "urn:uuid:test-patient-1" },
                "effectiveDateTime": "2021-02-15T08:00:00-05:00",
                "valueQuantity": { "value": 110, "unit": "mg/dL" } } }
          ]
        }
        """;
}