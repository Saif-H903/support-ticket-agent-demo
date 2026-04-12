# Support Ticket Agent Demo

Een minimale full-stack demo voor een uitlegbare support-agent.

- Frontend: React + Vite
- Backend: .NET 8 Web API
- Storage: lokale JSON-bestanden
- LLM: Ollama lokaal of OpenAI Responses API via `OPENAI_API_KEY`
- Tool: mock bestelstatus lookup

## Wat de demo doet

1. Een gebruiker stelt een supportvraag in de React UI.
2. De backend zoekt de beste match in `backend/Data/knowledge-base.json`.
3. Als de vraag over een bestelling gaat, gebruikt de backend de mock bestelstatus-tool.
4. De backend berekent zelf een confidence score.
5. Bij lage confidence wordt het ticket doorgezet naar een menselijke medewerker.
6. Bij voldoende confidence maakt de LLM een antwoord met alleen de opgehaalde context.
7. Elke run wordt opgeslagen in `backend/Data/logs.json`.

## Projectstructuur

```text
backend/
  Controllers/
  Data/
  Models/
  Services/
frontend/
  src/
    components/
```

## Starten

Backend met Ollama:

```powershell
ollama pull qwen3:1.7b
ollama serve

cd backend
$env:AI_PROVIDER="ollama"
$env:OLLAMA_BASE_URL="http://localhost:11434"
$env:OLLAMA_MODEL="qwen3:1.7b"
dotnet run --launch-profile http
```

In development staat `AI:Provider` al op `ollama` in `backend/appsettings.Development.json`, dus de environment variables zijn vooral handig als je tijdelijk een ander model of URL wilt gebruiken.

Backend met OpenAI:

```powershell
cd backend
$env:AI_PROVIDER="openai"
$env:OPENAI_API_KEY="jouw-api-key"
$env:OPENAI_MODEL="gpt-4.1-mini"
dotnet run --launch-profile http
```

Als `OPENAI_API_KEY` ontbreekt wanneer `AI_PROVIDER=openai` actief is, gebruikt de demo een veilige fallback op basis van de gevonden bron. Dat maakt lokaal testen mogelijk zonder API-kosten.

Frontend:

```powershell
cd frontend
npm install
npm run dev
```

Open daarna `http://127.0.0.1:5173`.

## API

```http
POST /api/ondersteuning/vraag
GET  /api/ondersteuning/logs
GET  /api/tools/bestelstatus/{bestellingId}
```

Er zijn ook Engelstalige aliasroutes uit het plan beschikbaar:

```http
POST /api/support/ask
GET  /api/support/logs
GET  /api/tool/order-status/{bestellingId}
```

Voorbeeldrequest:

```json
{
  "vraag": "Waar is bestelling 12345?",
  "sessieId": "demo-sessie-1"
}
```

Voorbeeldresponse:

```json
{
  "antwoord": "Volgens 'Verzend FAQ' geldt: ...",
  "antwoordBron": "ollama:qwen3:1.7b",
  "vertrouwen": 0.95,
  "escaleren": false,
  "bronTitel": "Verzend FAQ",
  "bronFragment": "Bestellingen met status verzonden ...",
  "toolGebruikt": "bestelstatus",
  "toolUitvoer": "Bestelling 12345: verzonden, verwacht binnen 2 werkdagen",
  "besluitReden": "kennisbankmatch 'Verzend FAQ' gevonden; ..."
}
```

## RAG

De demo gebruikt een lichte RAG-flow: eerst zoekt de backend relevante kennisbankcontext, daarna krijgt de LLM alleen die context en eventuele tooluitvoer. Er is bewust geen vector database toegevoegd, omdat keyword overlap genoeg is voor een kleine sollicitatiedemo.

## Memory

`SessieGeheugenService` bewaart per `sessieId` de laatste interacties in geheugen. Die korte sessiecontext gaat mee naar de prompt, zodat de agent weet wat er net in dezelfde demo-sessie gebeurde. Dit is lightweight memory, geen permanente klantgeschiedenis.

## Tool Calling

`ToolRouterService` herkent bestelvragen met een bestelnummer. Daarna haalt `BestelStatusService` een mock-status op, bijvoorbeeld voor bestelling `12345`. De tool is expres simpel gehouden, zodat het agentpatroon duidelijk blijft zonder echte CRM- of orderkoppeling.

## Human-In-The-Loop

`VertrouwenService` bepaalt of de agent genoeg bewijs heeft. Als de score te laag is, geeft de backend geen verzonnen antwoord, maar markeert het ticket als escalatie naar een menselijke medewerker.

## Confidence Threshold

De drempel staat op `0.75`.

- `>= 0.75`: automatisch beantwoorden
- `< 0.75`: escaleren naar human-in-the-loop

De LLM bepaalt deze score niet zelf. De backend berekent de score op basis van kennisbankmatch, toolgebruik, toolresultaat en vraagspecificiteit.

## Audit Trail

Elke request wordt opgeslagen in `backend/Data/logs.json` met:

- tijdstip
- sessie
- vraag
- antwoord
- antwoordbron
- gebruikte bron
- confidence score
- escalatieflag
- tooluitvoer
- besluitreden
- eindactie

Dit maakt de agent-flow uitlegbaar en achteraf controleerbaar.

## Guardrails

- Antwoorden alleen met opgehaalde kennisbankcontext en toolresultaten.
- Geen giswerk zonder betrouwbare bron.
- Lage confidence gaat naar een menselijke medewerker.
- Elke run komt in de audit trail.

## Known limitations

- Uses keyword retrieval instead of embeddings or vector search.
- Uses a mock order-status tool instead of a real commerce or CRM integration.
- Uses lightweight in-memory session memory, so memory resets when the API restarts.
- Uses heuristic confidence scoring rather than calibrated model evaluation.

## LLM providers

De backend gebruikt standaard Ollama in development. Met `AI_PROVIDER=ollama` roept de app lokaal `http://localhost:11434/api/chat` aan en gebruikt hij `OLLAMA_MODEL`, standaard `qwen3:1.7b`. In de UI en audit trail zie je dan bijvoorbeeld `ollama:qwen3:1.7b` als `Antwoordbron`.

Met `AI_PROVIDER=openai` gebruikt de backend de OpenAI Responses API met `HttpClient`. De API-key komt uit `OPENAI_API_KEY`; het model kan met `OPENAI_MODEL` worden aangepast. In de UI en audit trail zie je dan bijvoorbeeld `openai:gpt-4.1-mini`.

OpenAI documentatie: https://platform.openai.com/docs/api-reference/responses
