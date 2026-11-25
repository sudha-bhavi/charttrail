import { describe, expect, it } from "vitest";
import type { Encounter, MedicationRequest, Observation } from "./fhir";
import { buildTimeline } from "./timeline";

const visit: Encounter = {
  resourceType: "Encounter",
  id: "e1",
  class: { code: "AMB" },
  type: [{ coding: [{ display: "Well child visit" }] }],
  period: { start: "2020-01-10T09:00:00-05:00" },
};

const medication: MedicationRequest = {
  resourceType: "MedicationRequest",
  id: "m1",
  status: "active",
  authoredOn: "2021-03-01T09:00:00-05:00",
  medicationCodeableConcept: {
    coding: [{ display: "Amlodipine 5 MG Oral Tablet" }],
  },
};

const lab: Observation = {
  resourceType: "Observation",
  id: "o1",
  code: { coding: [{ display: "Glucose" }] },
  effectiveDateTime: "2022-05-05T08:00:00-04:00",
  valueQuantity: { value: 95, unit: "mg/dL" },
};

describe("buildTimeline", () => {
  it("merges the three kinds newest first", () => {
    const timeline = buildTimeline([visit], [medication], [lab]);

    expect(timeline.map((item) => item.kind)).toEqual([
      "lab",
      "medication",
      "visit",
    ]);
  });

  it("describes each kind", () => {
    const timeline = buildTimeline([visit], [medication], [lab]);

    expect(timeline.find((i) => i.kind === "visit")).toMatchObject({
      title: "Well child visit",
      detail: "AMB",
    });
    expect(timeline.find((i) => i.kind === "medication")).toMatchObject({
      detail: "active",
    });
    expect(timeline.find((i) => i.kind === "lab")).toMatchObject({
      title: "Glucose",
      detail: "95 mg/dL",
    });
  });

  it("puts items without a date last", () => {
    const undated: Encounter = { resourceType: "Encounter", id: "e2" };

    const timeline = buildTimeline([undated, visit], [], []);

    expect(timeline.map((item) => item.id)).toEqual(["e1", "e2"]);
    expect(timeline[1].title).toBe("Visit");
  });

  it("shows a lab value of zero", () => {
    const zero: Observation = { ...lab, id: "o2", valueQuantity: { value: 0 } };

    expect(buildTimeline([], [], [zero])[0].detail).toBe("0");
  });
});
