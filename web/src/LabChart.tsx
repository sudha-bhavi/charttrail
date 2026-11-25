import { useRef, useState } from "react";
import type { ChangeEvent } from "react";
import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { errorMessage, searchObservations } from "./fhir";
import type { Observation } from "./fhir";
import { labOptions, labPoints } from "./labs";
import type { LabPoint } from "./labs";

const formatDay = (time: number) => new Date(time).toISOString().slice(0, 10);

interface Props {
  patientId: string;
  labs: Observation[];
}

export default function LabChart({ patientId, labs }: Props) {
  const options = labOptions(labs);
  const [code, setCode] = useState("");
  const [points, setPoints] = useState<LabPoint[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const latestRequest = useRef(0);

  async function handleChange(event: ChangeEvent<HTMLSelectElement>) {
    const nextCode = event.target.value;
    const request = ++latestRequest.current;
    setCode(nextCode);
    setPoints([]);
    setError(null);
    if (!nextCode) return;

    setLoading(true);
    try {
      const result = await searchObservations(patientId, nextCode);
      if (request !== latestRequest.current) return;
      setPoints(labPoints(result.items));
    } catch (e) {
      if (request === latestRequest.current) setError(errorMessage(e));
    } finally {
      if (request === latestRequest.current) setLoading(false);
    }
  }

  if (options.length === 0)
    return <p className="muted">No numeric lab results to chart.</p>;

  const option = options.find((o) => o.code === code);
  const unit = option?.unit ?? "";
  const first = points[0];
  const latest = points.at(-1);

  return (
    <div className="lab-chart">
      <label>
        Lab test{" "}
        <select value={code} onChange={handleChange}>
          <option value="">Choose a lab...</option>
          {options.map((o) => (
            <option key={o.code} value={o.code}>
              {o.display} ({o.count})
            </option>
          ))}
        </select>
      </label>

      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}
      {loading && <p className="muted">Loading results...</p>}

      {option && first && latest && !loading && (
        <>
          <p className="muted">
            {points.length} results from {formatDay(first.time)} to{" "}
            {formatDay(latest.time)}. Latest: {latest.value} {unit}
          </p>
          <ResponsiveContainer width="100%" height={320}>
            <LineChart
              data={points}
              margin={{ top: 8, right: 16, bottom: 8, left: 0 }}
            >
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis
                dataKey="time"
                type="number"
                scale="time"
                domain={["dataMin", "dataMax"]}
                tickFormatter={(time: number) =>
                  new Date(time).getUTCFullYear().toString()
                }
              />
              <YAxis
                domain={["auto", "auto"]}
                unit={unit ? ` ${unit}` : ""}
                width={90}
              />
              <Tooltip
                labelFormatter={(label) => formatDay(Number(label))}
                formatter={(value) => [`${value} ${unit}`, option.display]}
                contentStyle={{
                  background: "var(--chart-tooltip-bg)",
                  border: "1px solid var(--chart-grid)",
                }}
              />
              <Line
                type="monotone"
                dataKey="value"
                stroke="var(--chart-line)"
                strokeWidth={2}
                dot
              />
            </LineChart>
          </ResponsiveContainer>
        </>
      )}
    </div>
  );
}
