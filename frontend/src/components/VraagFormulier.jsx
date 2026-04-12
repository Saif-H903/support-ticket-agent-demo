export function VraagFormulier({
  vraag,
  setVraag,
  verzendVraag,
  laden,
  voorbeeldVragen
}) {
  return (
    <form className="vraagblok" onSubmit={verzendVraag}>
      <div>
        <p className="eyebrow">Nieuwe vraag</p>
        <label htmlFor="supportvraag">Supportvraag</label>
      </div>

      <textarea
        id="supportvraag"
        value={vraag}
        onChange={(event) => setVraag(event.target.value)}
        rows={5}
        placeholder="Waar kan ik mee helpen?"
      />

      <div className="voorbeeldrij" aria-label="Voorbeelden">
        {voorbeeldVragen.map((voorbeeld) => (
          <button
            key={voorbeeld}
            type="button"
            onClick={() => setVraag(voorbeeld)}
          >
            {voorbeeld}
          </button>
        ))}
      </div>

      <button className="primaire-knop" type="submit" disabled={laden}>
        {laden ? "Bezig met beantwoorden" : "Vraag verwerken"}
      </button>
    </form>
  );
}
