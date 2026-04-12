import { useEffect, useMemo, useState } from "react";
import { haalLogsOp, stelVraag } from "./api";
import { VraagFormulier } from "./components/VraagFormulier";
import { AntwoordPaneel } from "./components/AntwoordPaneel";
import { AuditPaneel } from "./components/AuditPaneel";

const voorbeeldVragen = [
  "Waar is bestelling 12345?",
  "Hoe lang duurt een terugbetaling?",
  "Mijn resetmail komt niet binnen."
];

export default function App() {
  const [vraag, setVraag] = useState(voorbeeldVragen[0]);
  const [antwoord, setAntwoord] = useState(null);
  const [logs, setLogs] = useState([]);
  const [laden, setLaden] = useState(false);
  const [fout, setFout] = useState("");

  const laatsteActie = useMemo(() => {
    if (!antwoord) {
      return "Klaar voor een supportvraag";
    }

    return antwoord.escaleren ? "Human-in-the-loop" : "Automatisch beantwoord";
  }, [antwoord]);

  async function logsVerversen() {
    const nieuweLogs = await haalLogsOp();
    setLogs(nieuweLogs);
  }

  async function verzendVraag(event) {
    event.preventDefault();
    if (!vraag.trim()) {
      setFout("Vul eerst een vraag in.");
      return;
    }

    setLaden(true);
    setFout("");

    try {
      const resultaat = await stelVraag({
        vraag,
        sessieId: "demo-sessie-1"
      });
      setAntwoord(resultaat);
      await logsVerversen();
    } catch (error) {
      setFout(error.message);
    } finally {
      setLaden(false);
    }
  }

  useEffect(() => {
    logsVerversen().catch((error) => setFout(error.message));
  }, []);

  return (
    <main className="app-shell">
      <section className="werkruimte" aria-label="Support agent werkruimte">
        <aside className="zijpaneel">
          <div className="werkbeeld" aria-hidden="true">
            <div className="ticketlijnen">
              <span></span>
              <span></span>
              <span></span>
            </div>
            <div className="visueel-label">RAG / Tool / Human Review</div>
          </div>
          <div className="merkblok">
            <p className="eyebrow">Demo voor support</p>
            <h1>Support Ticket Agent</h1>
            <p>
              Veilige antwoorden met kennisbankcontext, een besteltool en een
              audit trail.
            </p>
          </div>
          <div className="statusrij">
            <span>Status</span>
            <strong>{laatsteActie}</strong>
          </div>
        </aside>

        <section className="hoofdpaneel">
          <VraagFormulier
            vraag={vraag}
            setVraag={setVraag}
            verzendVraag={verzendVraag}
            laden={laden}
            voorbeeldVragen={voorbeeldVragen}
          />

          {fout && <p className="foutmelding">{fout}</p>}

          <AntwoordPaneel antwoord={antwoord} laden={laden} />
          <AuditPaneel logs={logs} />
        </section>
      </section>
    </main>
  );
}
