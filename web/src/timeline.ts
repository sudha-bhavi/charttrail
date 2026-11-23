import type {
  CodeableConcept,
  Encounter,
  MedicationRequest,
  Observation,
} from "./fhir";

export type TimelineKind = "visit" | "medication" | "lab";

export interface TimelineItem {
  id: string;
  kind: TimelineKind;
  date: string | null;
  title: string;
  detail: string | null;
}

const firstText = (concept?: CodeableConcept) =>
  concept?.coding?.[0]?.display ?? concept?.text ?? null;

function labValue(lab: Observation): string | null {
  const quantity = lab.valueQuantity;
  if (quantity?.value !== undefined) {
    return quantity.unit
      ? `${quantity.value} ${quantity.unit}`
      : String(quantity.value);
  }
  if (lab.valueCodeableConcept) return firstText(lab.valueCodeableConcept);
  return lab.valueString ?? null;
}

const time = (item: TimelineItem) =>
  item.date ? Date.parse(item.date) : Number.NEGATIVE_INFINITY;

// Newest first. Items without a date go last.
function newestFirst(a: TimelineItem, b: TimelineItem): number {
  const ta = time(a);
  const tb = time(b);
  if (ta === tb) return 0;
  return tb > ta ? 1 : -1;
}

export function buildTimeline(
  encounters: Encounter[],
  medications: MedicationRequest[],
  labs: Observation[],
): TimelineItem[] {
  const items: TimelineItem[] = [
    ...encounters.map((e) => ({
      id: e.id,
      kind: "visit" as const,
      date: e.period?.start ?? null,
      title: firstText(e.type?.[0]) ?? "Visit",
      detail: e.class?.code ?? null,
    })),
    ...medications.map((m) => ({
      id: m.id,
      kind: "medication" as const,
      date: m.authoredOn ?? null,
      title: firstText(m.medicationCodeableConcept) ?? "Medication",
      detail: m.status ?? null,
    })),
    ...labs.map((l) => ({
      id: l.id,
      kind: "lab" as const,
      date: l.effectiveDateTime ?? null,
      title: firstText(l.code) ?? "Lab result",
      detail: labValue(l),
    })),
  ];
  return items.sort(newestFirst);
}
