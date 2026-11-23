import { useRef, useState } from "react";
import type { FormEvent } from "react";
import { loadPatientHistory, searchPatients } from "./fhir";
import type { Patient } from "./fhir";
import { buildTimeline } from "./timeline";
import type { TimelineItem, TimelineKind } from "./timeline";
import "./App.css";

const KIND_LABEL: Record<TimelineKind, string> = {
  visit: "Visit",
  medication: "Medication",
  lab: "Lab",
};

function patientName(patient: Patient): string {
  const name = patient.name?.[0];
  const full = [name?.given?.join(" "), name?.family].filter(Boolean).join(" ");
  return full || patient.id;
}

const errorMessage = (error: unknown) =>
  error instanceof Error ? error.message : "Something went wrong";

function App() {
  const [query, setQuery] = useState("");
  const [patients, setPatients] = useState<Patient[] | null>(null);
  const [selected, setSelected] = useState<Patient | null>(null);
  const [timeline, setTimeline] = useState<TimelineItem[]>([]);
  const [truncated, setTruncated] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const latestRequest = useRef(0);

  async function handleSearch(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSelected(null);
    setTimeline([]);
    try {
      const result = await searchPatients(query.trim());
      setPatients(result.items);
    } catch (e) {
      setError(errorMessage(e));
    }
  }

  async function handleSelect(patient: Patient) {
    const request = ++latestRequest.current;
    setSelected(patient);
    setTimeline([]);
    setError(null);
    setLoading(true);
    try {
      const history = await loadPatientHistory(patient.id);
      if (request !== latestRequest.current) return; // a newer click superseded this one
      setTimeline(
        buildTimeline(history.encounters, history.medications, history.labs),
      );
      setTruncated(history.truncated);
    } catch (e) {
      if (request === latestRequest.current) setError(errorMessage(e));
    } finally {
      if (request === latestRequest.current) setLoading(false);
    }
  }

  const count = (kind: TimelineKind) =>
    timeline.filter((item) => item.kind === kind).length;

  return (
    <main className="app">
      <h1>ChartTrail</h1>
      <p className="subtitle">Patient timeline over synthetic FHIR data</p>

      <form className="search" onSubmit={handleSearch}>
        <input
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          placeholder="Search patients by name"
          aria-label="Patient name"
        />
        <button type="submit">Search</button>
      </form>

      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}

      {patients && (
        <section aria-label="Search results">
          {patients.length === 0 ? (
            <p className="muted">No patients found.</p>
          ) : (
            <ul className="patients">
              {patients.map((patient) => (
                <li key={patient.id}>
                  <button
                    type="button"
                    className={
                      patient.id === selected?.id
                        ? "patient selected"
                        : "patient"
                    }
                    onClick={() => handleSelect(patient)}
                  >
                    {patientName(patient)}
                    <span className="muted">
                      {[
                        patient.gender,
                        patient.birthDate && `born ${patient.birthDate}`,
                      ]
                        .filter(Boolean)
                        .join(" · ")}
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </section>
      )}

      {selected && (
        <section aria-label="Timeline">
          <h2>{patientName(selected)}</h2>
          {loading ? (
            <p className="muted">Loading history...</p>
          ) : (
            <>
              <p className="muted">
                {count("visit")} visits · {count("medication")} medications ·{" "}
                {count("lab")} lab results
              </p>
              {truncated && (
                <p className="muted">
                  Showing the first 1000 items of at least one list.
                </p>
              )}
              <ol className="timeline">
                {timeline.map((item) => (
                  <li
                    key={`${item.kind}-${item.id}`}
                    className={`entry kind-${item.kind}`}
                  >
                    <span className="date">
                      {item.date ? item.date.slice(0, 10) : "no date"}
                    </span>
                    <span className="badge">{KIND_LABEL[item.kind]}</span>
                    <span className="title">{item.title}</span>
                    {item.detail && (
                      <span className="detail">{item.detail}</span>
                    )}
                  </li>
                ))}
              </ol>
            </>
          )}
        </section>
      )}
    </main>
  );
}

export default App;
