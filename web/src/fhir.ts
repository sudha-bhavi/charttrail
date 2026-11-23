// Only the parts of FHIR R4 that this app reads. Real resources have many more fields.
export interface Coding {
  system?: string;
  code?: string;
  display?: string;
}

export interface CodeableConcept {
  coding?: Coding[];
  text?: string;
}

export interface HumanName {
  family?: string;
  given?: string[];
}

export interface Patient {
  resourceType: "Patient";
  id: string;
  name?: HumanName[];
  gender?: string;
  birthDate?: string;
}

export interface Encounter {
  resourceType: "Encounter";
  id: string;
  class?: Coding;
  type?: CodeableConcept[];
  period?: { start?: string; end?: string };
}

export interface MedicationRequest {
  resourceType: "MedicationRequest";
  id: string;
  status?: string;
  authoredOn?: string;
  medicationCodeableConcept?: CodeableConcept;
}

export interface Observation {
  resourceType: "Observation";
  id: string;
  code?: CodeableConcept;
  effectiveDateTime?: string;
  valueQuantity?: { value?: number; unit?: string };
  valueCodeableConcept?: CodeableConcept;
  valueString?: string;
}

interface Bundle<T> {
  total?: number;
  entry?: { resource: T }[];
}

interface SearchResult<T> {
  total: number;
  items: T[];
}

async function search<T>(path: string): Promise<SearchResult<T>> {
  const response = await fetch(path, {
    headers: { Accept: "application/fhir+json" },
  });
  if (!response.ok)
    throw new Error(
      `The server answered ${response.status} for ${path.split("?")[0]}`,
    );
  const bundle = (await response.json()) as Bundle<T>;
  return {
    total: bundle.total ?? 0,
    items: (bundle.entry ?? []).map((entry) => entry.resource),
  };
}

export const searchPatients = (name: string) =>
  search<Patient>(`/Patient?name=${encodeURIComponent(name)}&_count=20`);

export async function loadPatientHistory(patientId: string) {
  const id = encodeURIComponent(patientId);
  const [encounters, medications, labs] = await Promise.all([
    search<Encounter>(`/Encounter?patient=${id}&_count=1000`),
    search<MedicationRequest>(`/MedicationRequest?patient=${id}&_count=1000`),
    search<Observation>(
      `/Observation?patient=${id}&category=laboratory&_count=1000`,
    ),
  ]);

  const truncated = [encounters, medications, labs].some(
    (result) => result.total > result.items.length,
  );
  return {
    encounters: encounters.items,
    medications: medications.items,
    labs: labs.items,
    truncated,
  };
}
