import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// In development the browser talks to Vite (port 5173), and Vite forwards
// the FHIR API paths to the .NET API, so the browser sees one origin and CORS is not needed.
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      "^/(Patient|Observation|Encounter|MedicationRequest|metadata|health)":
        "http://localhost:5200",
    },
  },
});
