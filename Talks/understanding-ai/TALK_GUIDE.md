# TALK GUIDE - Building an AI Agent from Scratch in .NET 10

This script is designed for live demo flow, running each step as its own standalone project.

## Setup Before Going On Stage

1. Start Ollama and make sure model exists:
   - `ollama pull llama3.1:8b`
   - Set `AI_PROVIDER=Ollama` (see `.env.example`; defaults to
     `http://localhost:11434` / `llama3.1:8b` if `AI_ENDPOINT`/`AI_MODEL` are unset).
2. Build once:
   - `dotnet build .\UnderstandingAiTalk.slnx`
3. Run each step from its own project as you reach it (see below).

## Step 01 - The Echo

### What to run
- `dotnet run --project src\Step01Echo`

### What to type
1. `Hello world`

### Expected shape
- App returns: `Echo: Hello world`

### Concept takeaway
- We have control flow and state loop, but no intelligence.

## Step 02 - The Stateless LLM

### What to run
- `dotnet run --project src\Step02StatelessLlm`

### What to type
1. `Hi, my name is Chris.`
2. `What is my name?`

### Expected shape
- First response is conversational.
- Second response often fails to remember your name.

### Concept takeaway
- Raw LLM calls are stateless unless you resend context.

## Step 03 - Adding Chat Memory

### What to run
- `dotnet run --project src\Step03ChatMemory`

### What to type
1. `Hi, my name is Chris.`
2. `What is my name?`

### Expected shape
- Model now usually recalls `Chris`.

### Concept takeaway
- Memory is engineered by replaying chat history each turn.

## Step 04 - Native Skills (Unit Conversion)

### What to run
- `dotnet run --project src\Step04NativeSkills`

### What to type
1. `If it is 100 degrees Fahrenheit in New York, what is that in Celsius?`

### Expected shape
- LLM returns approximately `37.78 C`.

### Concept takeaway
- Deterministic C# functions reduce hallucinations for exact math.

## Step 05 - Knowledge Cutoff

### What to run
- `dotnet run --project src\Step05KnowledgeCutoff`

### What to type
1. `What is today's date?`
2. `What is the weather in London right now?`

### Expected shape
- Model may hedge, guess, or disclaim real-time certainty.

### Concept takeaway
- Local-only skills and model weights are not live infrastructure truth.

## Step 06 - Retrieval-Augmented Generation

### What to run
- `dotnet run --project src\Step06_Rag`

### What to type
1. `What is the staff car park entry code?`

### Expected shape
- The app reports how many chunks it indexed from the bundled corpus.
- "LLM (no retrieval)" answer hedges, guesses, or admits it doesn't know.
- Retrieved context is printed, showing the `facilities` chunk containing the code.
- "LLM (with retrieval)" answer correctly states the code (4471).

### Concept takeaway
- Embedding a private corpus and injecting the retrieved text into the prompt turns an
  unanswerable question into a grounded, correct one - without fine-tuning or a second
  vendor's credentials, since embeddings run locally via ONNX Runtime for every provider.

## Step 07 - Enter MCP

### What to run
- `dotnet run --project src\Step07Mcp`

### What to type
1. `What is today's date?`
2. `What is the weather in London right now?`

### Expected shape
- The app reports MCP tool discovery.
- First response includes the live date/time from the MCP tool.
- Second response includes live weather for London from the MCP tool.

### Note
- Ask these as two separate turns, not one compound question. Smaller local
  models (e.g. `llama3.1:8b`) are unreliable at planning two tool calls from a
  single prompt - it will often answer the date and silently skip the weather
  call. This is a model-capability limit, not a demo bug.

### Concept takeaway
- MCP acts as a standard connector so the model can discover and orchestrate decoupled tools.

## Contingency Notes

1. If network is unstable, pivot to Step 04 as the deterministic reliability story.
2. If Step 06's first ONNX run is slow to load, narrate that the model loads once at
   startup, not per-query.
3. If Step 07 fails to resolve location, retry with a major city spelling, e.g. `London`.
4. If model output varies, narrate that wording varies but architecture behavior remains the same.
