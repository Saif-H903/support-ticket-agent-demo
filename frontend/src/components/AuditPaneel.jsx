export function AuditPaneel({ logs }) {
  return (
    <section className="auditpaneel">
      <div className="sectiekop">
        <div>
          <p className="eyebrow">Audit trail</p>
          <h2>Laatste runs</h2>
        </div>
        <span>{logs.length} logs</span>
      </div>

      <div className="loglijst">
        {logs.length === 0 && (
          <p className="placeholder">Nog geen auditregels opgeslagen.</p>
        )}

        {logs.slice(0, 6).map((log) => (
          <article className="logitem" key={`${log.tijdstipUtc}-${log.vraag}`}>
            <div className="logkop">
              <strong>{log.vraag}</strong>
              <span className={log.escaleren ? "badge rood" : "badge groen"}>
                {log.eindActie}
              </span>
            </div>
            <p>{log.antwoord}</p>
            <dl className="logmeta">
              <div>
                <dt>Bron</dt>
                <dd>{log.bronTitel}</dd>
              </div>
              <div>
                <dt>Vertrouwen</dt>
                <dd>{Math.round(log.vertrouwen * 100)}%</dd>
              </div>
              <div>
                <dt>Antwoordbron</dt>
                <dd>{log.antwoordBron || "Onbekend"}</dd>
              </div>
              <div>
                <dt>Tijd</dt>
                <dd>{new Date(log.tijdstipUtc).toLocaleString("nl-NL")}</dd>
              </div>
            </dl>
          </article>
        ))}
      </div>
    </section>
  );
}
