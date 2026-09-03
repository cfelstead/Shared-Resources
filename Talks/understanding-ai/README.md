# Understanding AI Talk (.NET 10)

_**Please note** this has only been live tested with the Docker (Ollama setup) and Gemini_

Live-coding repository for a talk that builds an AI agent from scratch in seven steps:

1. Echo loop
2. Stateless LLM call
3. Chat memory replay
4. Native C# skills (unit conversion)
5. Knowledge cutoff limitations
6. Retrieval-augmented generation over a bundled corpus
7. MCP protocol tools (date/time + live weather)

## Repository Layout

Each step is its own independently-runnable console project - there is no menu
or mode-switcher. A shared `Talk.Core` library holds config loading and
provider client construction. The talk app and the MCP sidecar always run
natively via `dotnet run`; only Ollama itself has an opt-in containerized path.

- `src/Talk.Core`: Shared library - config loading (`AppConfigLoader`) and
  chat client construction, behind an `IChatClientFactory` interface with one
  implementation per provider (`OpenAiChatClientFactory`,
  `AzureOpenAiChatClientFactory`, `AnthropicChatClientFactory`,
  `GeminiChatClientFactory`, `OllamaChatClientFactory`), selected at runtime
  by `ChatClientFactoryResolver` based on `AI_PROVIDER`. Every step project
  consumes only the resulting `IChatClient` - no step-level code changes are
  needed to support a new provider.
- `src/Talk.Core.Tests`: Unit tests for `Talk.Core`.
- `src/Step01Echo` .. `src/Step07Mcp`: One project per talk step, each with
  its own `Program.cs` entry point and a testable `RunAsync`/`Run` method.
- `src/Step06_Rag`: Also bundles a small sample document corpus (`corpus/`)
  and a local ONNX sentence-embedding model (`model/`, all-MiniLM-L6-v2) used
  to retrieve context uniformly regardless of `AI_PROVIDER`.
- `src/Talk.Steps.Tests`: Smoke tests proving each step's loop runs
  end-to-end against a fake `IChatClient` (no live network calls).
- `src/McpSidecarServer`: MCP server exposing date/time and weather tools,
  used by Step 07.
- `docker/Dockerfile.ollama`, `docker-compose.yml`: Container assets for the
  opt-in Ollama-in-Docker path.
- `TALK_GUIDE.md`: Presentation script and prompt-by-prompt flow.
- `understanding-ai.mp4`: A sample video of the presentation.

## Prerequisites

1. .NET SDK 10.0 (GA) or later.
2. An API key for your chosen provider (OpenAI, Azure OpenAI, Anthropic, or
   Gemini), or a local Ollama instance with a model pulled (see
   `.env.example` for the defaults), or Docker Desktop for the opt-in
   containerized Ollama path.

## Environment Variables

Copy `.env.example` values into your shell or environment.

- `AI_PROVIDER`: Required. One of `OpenAI`, `AzureOpenAI`, `Anthropic`,
  `Gemini`, `Ollama`.
- `AI_ENDPOINT`: Base URL for the provider. Required for `AzureOpenAI`;
  optional for the others (defaults to the provider's own endpoint, or
  `http://localhost:11434` for `Ollama`).
- `AI_MODEL`: Model name (or Azure deployment name).
- `AI_API_KEY`: API key. Not used by `Ollama`.
- `MCP_SERVER_COMMAND`: Command used to launch Step 07's MCP server over stdio.
- `MCP_SERVER_ARGS`: Arguments passed to the server command.

## Build and Run

```powershell
dotnet build .\UnderstandingAiTalk.slnx
dotnet test .\UnderstandingAiTalk.slnx
```

Opt-in local/offline Ollama via Docker (no local Ollama install required):

```powershell
.\Start-Demo.ps1
```

This starts an `ollama` container and pulls the configured model, then leaves
you to run any step natively against it. Shut it down with `.\Stop-Demo.ps1`.

Run any step standalone:

```powershell
dotnet run --project src\Step01Echo
dotnet run --project src\Step02StatelessLlm
dotnet run --project src\Step03ChatMemory
dotnet run --project src\Step04NativeSkills
dotnet run --project src\Step05KnowledgeCutoff
dotnet run --project src\Step06_Rag
dotnet run --project src\Step07Mcp
```

## Step Mapping to Code

- Step 01: `src/Step01Echo/Program.cs`
- Step 02: `src/Step02StatelessLlm/Program.cs`
- Step 03: `src/Step03ChatMemory/Program.cs`
- Step 04: `src/Step04NativeSkills/Program.cs`
- Step 05: `src/Step05KnowledgeCutoff/Program.cs`
- Step 06: `src/Step06_Rag/Program.cs`
- Step 07: `src/Step07Mcp/Program.cs`

## Troubleshooting

1. If a step fails to reach the configured AI provider, verify `AI_PROVIDER`,
   `AI_ENDPOINT`, `AI_MODEL`, and `AI_API_KEY` are correct. For `Ollama`,
   verify it's running and the configured model is pulled (`ollama serve`,
   `ollama pull llama3.1:8b`).
2. If Step 07 cannot discover tools, ensure `McpSidecarServer` builds and
   `MCP_SERVER_COMMAND`/`MCP_SERVER_ARGS` point to a valid launch command.
3. If weather lookups fail, verify outbound internet connectivity to
   Open-Meteo and geocoding endpoints.
