export function AntwoordPaneel({ antwoord, laden }) {
  if (laden) {
    return (
      <section className="antwoordpaneel">
        <p className="eyebrow">Antwoord</p>
        <p className="placeholder">De agent zoekt context en bepaalt vertrouwen.</p>
      </section>
    );
  }

  if (!antwoord) {
    return (
      <section className="antwoordpaneel leeg">
        <p className="eyebrow">Antwoord</p>
        <p className="placeholder">
          Stel een vraag om het antwoord, de bron en de actie te zien.
        </p>
      </section>
    );
  }

  return (
    <section className="antwoordpaneel">
      <div className="antwoordkop">
        <div>
          <p className="eyebrow">Antwoord</p>
          <h2>{antwoord.escaleren ? "Doorzetten naar mens" : "Klantantwoord"}</h2>
        </div>
        <span className={antwoord.escaleren ? "badge rood" : "badge groen"}>
          {antwoord.escaleren ? "Escaleren" : "Afhandelen"}
        </span>
      </div>

      <p className="antwoordtekst">{antwoord.antwoord}</p>

      <dl className="metaraster">
        <div>
          <dt>Vertrouwen</dt>
          <dd>{Math.round(antwoord.vertrouwen * 100)}%</dd>
        </div>
        <div>
          <dt>Bron</dt>
          <dd>{antwoord.bronTitel}</dd>
        </div>
        <div>
          <dt>Antwoordbron</dt>
          <dd>{antwoord.antwoordBron || "Onbekend"}</dd>
        </div>
        <div>
          <dt>Tool</dt>
          <dd>{antwoord.toolGebruikt || "Geen tool"}</dd>
        </div>
        <div>
          <dt>Besluit</dt>
          <dd>{antwoord.besluitReden}</dd>
        </div>
      </dl>

      {antwoord.bronFragment && (
        <div className="bronfragment">
          <strong>Bronfragment</strong>
          <p>{antwoord.bronFragment}</p>
        </div>
      )}

      {antwoord.toolUitvoer && (
        <p className="toolregel">
          <strong>Tooluitvoer:</strong> {antwoord.toolUitvoer}
        </p>
      )}
    </section>
  );
}
