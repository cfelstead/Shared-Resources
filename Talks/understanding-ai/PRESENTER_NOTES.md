# Presenter Notes - Understanding AI Talk

Per-step talking points to read from while recording voiceover. These expand
on `TALK_GUIDE.md`'s script/contingency notes for delivery aloud - they are
not a replacement for it. Run the commands and prompts from `TALK_GUIDE.md`;
use these bullets for what to say while each step plays out.

## Step 01 - The Echo

- What's happening: `dotnet run --project src\Step01Echo` reads a line of
  input and prints it straight back out - no model call at all.
- Say: "This is the loop every AI app is built on - read input, do
  something, print output. Right now the 'something' is nothing. We have
  the control flow and the state loop, but zero intelligence."
- Say: "Every step from here just replaces what happens in that middle
  step. Keep this shape in your head - it doesn't change."

## Step 02 - The Stateless LLM

- What's happening: `Step02StatelessLlm` sends each line of input straight
  to the model as a one-off call, with no history attached.
- Say: "Now let's swap 'do nothing' for an actual LLM call."
- Type `Hi, my name is Chris.` then `What is my name?`.
- Say: "First answer looks great - fully conversational. Second one -
  watch, it often has no idea what your name was."
- Say: "That's not the model being dumb. The model has no memory of its
  own. Every call is stateless unless the app resends the context."

## Step 03 - Adding Chat Memory

- What's happening: `Step03ChatMemory` keeps a running list of every
  message and resends the whole history on each turn.
- Say: "Same two prompts, same model - the only thing that changed is the
  app is now replaying the conversation back to it every time."
- Type `Hi, my name is Chris.` then `What is my name?`.
- Say: "Now it remembers. 'Memory' here isn't a model feature, it's
  just us re-sending the transcript."
- Say: "That also means memory costs tokens - the longer the chat, the more
  you resend every single turn."

## Step 04 - Native Skills (Unit Conversion)

- What's happening: `Step04NativeSkills` gives the model a real C# function
  it can call to do the Fahrenheit-to-Celsius conversion, instead of asking
  it to do the arithmetic itself.
- Type `If it is 100 degrees Fahrenheit in New York, what is that in
  Celsius?`
- Say: "The answer comes back as 37.78C - and it's exactly right, every
  time, because the model isn't doing the maths. It recognised what was
  being asked and called a plain C# function to compute it."
- Say: "This is the fix for hallucinated numbers: for anything
  deterministic - conversions, lookups, calculations - don't trust the
  model to compute it, give it a tool that computes it and let it just
  call that."

## Step 05 - Knowledge Cutoff

- What's happening: `Step05KnowledgeCutoff` asks the same kind of question
  as before, but this time for facts the model can't know: the current
  date and live weather.
- Type `What is today's date?` then `What is the weather in London right
  now?`
- Say: "Watch it hedge, guess, or flat-out admit it doesn't know."
- Say: "This is the model's knowledge cutoff showing through. Its weights
  are frozen at training time, and none of the tools we've built so far
  give it a window onto the live world. A native C# function only helps if
  the function itself has somewhere live to look - we haven't given it
  one yet."

## Step 06 - Retrieval-Augmented Generation

- What's happening: `Step06_Rag` embeds a small bundled corpus locally with
  an ONNX sentence-embedding model (all-MiniLM-L6-v2), then for each
  question retrieves the most relevant chunk and injects it into the
  prompt before asking the model to answer.
- Type `What is the staff car park entry code?`
- Say: "First, watch the console - it tells you how many chunks it just
  indexed from the corpus. That indexing happens once at startup, not per
  question - if the first run feels slow, that's the ONNX model loading,
  not the retrieval itself."
- Say: "Now look at the 'no retrieval' answer - the model hedges or
  guesses, because there's no way it was trained on our staff car park
  code."
- Say: "Then the retrieved context prints on screen - that's the
  `facilities` chunk, pulled straight out of our corpus because it's the
  closest match by embedding similarity."
- Say: "And now the 'with retrieval' answer gets it right - 4471 - because
  we handed the model the one paragraph it actually needed, instead of
  hoping it already knew it."
- Say: "This is RAG: give the model a private knowledge base as text, and
  it turns an unanswerable question into a grounded, correct one - no
  fine-tuning, no vendor lock-in. The embeddings run locally, so this works
  identically no matter which provider is behind `AI_PROVIDER`."

## Step 07 - Enter MCP

- What's happening: `Step07Mcp` connects to a separate MCP sidecar server
  over stdio, which exposes date/time and live weather as tools; the model
  discovers those tools at startup and decides when to call them.
- Type `What is today's date?`, then as a separate turn `What is the
  weather in London right now?`. Ask them one at a time, not combined -
  smaller local models like `llama3.1:8b` unreliably plan two tool calls
  from a single compound prompt (it'll answer the date and quietly skip
  the weather call), which is a model-capability limit worth calling out
  rather than a demo bug.
- Say: "Console shows the tool discovery happening first - the model is
  finding out what it's allowed to call before it answers anything."
- Say: "Each answer comes back grounded in a live tool call - today's
  real date, then live London weather - the exact two things Step 05
  couldn't do."
- Say: "The difference from Step 04's native skill is where the tool
  lives. That was a C# function baked into our app. This is a standard
  protocol - MCP - talking to a tool server that could be written in any
  language, running anywhere, and swapped out without touching our app
  code."
- Say: "The real pitch for MCP is that it's a standard connector, so tools
  become interchangeable, decoupled pieces the model can discover and
  orchestrate, rather than something you hardcode per app."
- If location resolution fails: retry with a major city spelling, e.g.
  `London`, and narrate that as a normal geocoding hiccup, not an MCP
  problem.

## Closing note for recording

- If a model's exact wording varies between takes, don't re-record for
  that alone - narrate that wording varies but the architecture and
  behavior being demonstrated stay the same.
- If network is unstable mid-recording, Step 04 is the fallback
  deterministic-reliability story - it needs no external calls beyond the
  LLM's own text response.
