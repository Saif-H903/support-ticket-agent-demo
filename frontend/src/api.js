const basisUrl = "/api/ondersteuning";

async function leesJson(response) {
  if (!response.ok) {
    const tekst = await response.text();
    throw new Error(tekst || "De server gaf een fout terug.");
  }

  return response.json();
}

export async function stelVraag({ vraag, sessieId }) {
  const response = await fetch(`${basisUrl}/vraag`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify({ vraag, sessieId })
  });

  return leesJson(response);
}

export async function haalLogsOp() {
  const response = await fetch(`${basisUrl}/logs`);
  return leesJson(response);
}
