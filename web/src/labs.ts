import type { Observation } from "./fhir";

export interface LabOption {
  code: string;
  display: string;
  unit: string | null;
  count: number;
}

export interface LabPoint {
  time: number;
  value: number;
}

// Labs worth charting: numeric, dated, and with at least two results (one point is not a trend).
export function labOptions(labs: Observation[]): LabOption[] {
  const byCode = new Map<string, LabOption>();
  for (const lab of labs) {
    const coding = lab.code?.coding?.[0];
    const value = lab.valueQuantity?.value;
    if (!coding?.code || value === undefined || !lab.effectiveDateTime)
      continue;

    const existing = byCode.get(coding.code);
    if (existing) {
      existing.count += 1;
    } else {
      byCode.set(coding.code, {
        code: coding.code,
        display: coding.display ?? coding.code,
        unit: lab.valueQuantity?.unit ?? null,
        count: 1,
      });
    }
  }
  return [...byCode.values()]
    .filter((option) => option.count >= 2)
    .sort((a, b) => b.count - a.count || a.display.localeCompare(b.display));
}

// Chart points, oldest first. Results without a number or a date are skipped.
export function labPoints(labs: Observation[]): LabPoint[] {
  return labs
    .flatMap((lab) => {
      const value = lab.valueQuantity?.value;
      const time = lab.effectiveDateTime
        ? Date.parse(lab.effectiveDateTime)
        : Number.NaN;
      return value === undefined || Number.isNaN(time) ? [] : [{ time, value }];
    })
    .sort((a, b) => a.time - b.time);
}
