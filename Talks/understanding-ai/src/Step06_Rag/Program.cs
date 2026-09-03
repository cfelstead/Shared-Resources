using Step06_Rag;
using Talk.Core;
using Talk.Core.Rag;

ConsoleHelpers.WriteHeader(
    "Step 06 - Retrieval-Augmented Generation",
    "Grounding the model in a private corpus turns a guess into a fact.");

using var factory = TalkHost.CreateChatClientFactory();
using var chatClient = factory.CreateBaseClient();

var baseDirectory = AppContext.BaseDirectory;
using var embeddingGenerator = new OnnxEmbeddingGenerator(
    Path.Combine(baseDirectory, "model", "model.onnx"),
    Path.Combine(baseDirectory, "model", "vocab.txt"));

var corpusDirectory = Path.Combine(baseDirectory, "corpus");
var chunks = Directory.GetFiles(corpusDirectory, "*.md")
    .SelectMany(path => CorpusChunker.SplitIntoChunks(
        Path.GetFileNameWithoutExtension(path),
        File.ReadAllText(path)))
    .Select(chunk => new DocumentChunk(chunk.Id, chunk.Text, embeddingGenerator.GenerateEmbedding(chunk.Text)))
    .ToList();

Console.WriteLine($"Indexed {chunks.Count} chunks from {chunks.Select(c => c.Id.Split('#')[0]).Distinct().Count()} documents.");

var vectorStore = new InMemoryVectorStore(chunks);

await Step06_RagApp.RunAsync(chatClient, embeddingGenerator, vectorStore, Console.In, Console.Out);
