import { describe, expect, it } from "vitest";
import type { Observation } from "./fhir";
import { labOptions, labPoints } from "./labs";

function lab(
  id: string,
  code: string,
  display: string,
  value?: number,
  date?: string,
): Observation {
  return {
    resourceType: "Observation",
    id,
    code: { coding: [{ code, display }] },
    valueQuantity: value === undefined ? undefined : { value, unit: "mg/dL" },
    effectiveDateTime: date,
  };
}

describe("labOptions", () => {
  it("groups by code, counts results and puts the most frequent first", () => {
    const labs = [
      lab("1", "A", "Glucose", 90, "2020-01-01"),
      lab("2", "A", "Glucose", 95, "2021-01-01"),
      lab("3", "A", "Glucose", 99, "2022-01-01"),
      lab("4", "B", "Calcium", 9, "2020-01-01"),
      lab("5", "B", "Calcium", 10, "2021-01-01"),
    ];

    const options = labOptions(labs);

    expect(options.map((o) => [o.code, o.count])).toEqual([
      ["A", 3],
      ["B", 2],
    ]);
    expect(options[0].unit).toBe("mg/dL");
  });

  it("skips single results, undated results and non-numeric results", () => {
    const labs = [
      lab("1", "A", "Glucose", 90, "2020-01-01"),
      lab("2", "B", "Calcium", 9, "2020-01-01"),
      lab("3", "B", "Calcium", 10),
      lab("4", "C", "Smoking status", undefined, "2020-01-01"),
      lab("5", "C", "Smoking status", undefined, "2021-01-01"),
    ];

    expect(labOptions(labs)).toEqual([]);
  });
});

describe("labPoints", () => {
  it("returns points oldest first and skips results without a value or date", () => {
    const labs = [
      lab("1", "A", "Glucose", 110, "2021-02-15T08:00:00-05:00"),
      lab("2", "A", "Glucose", 95, "2020-01-10T09:15:00-05:00"),
      lab("3", "A", "Glucose", undefined, "2020-06-01T00:00:00Z"),
      lab("4", "A", "Glucose", 100),
    ];

    expect(labPoints(labs).map((p) => p.value)).toEqual([95, 110]);
  });

  it("keeps a result of zero", () => {
    expect(labPoints([lab("1", "A", "Glucose", 0, "2020-01-01")])).toHaveLength(
      1,
    );
  });
});
