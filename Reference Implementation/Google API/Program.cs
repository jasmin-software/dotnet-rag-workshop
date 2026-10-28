using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using System.Text.RegularExpressions;

CancellationToken ct = new();

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
    .Build();

string? token = config["Token"] ?? throw new Exception("Token not found in appsettings.Development.json");
string? endpoint = config["Endpoint"] ?? throw new Exception("Endpoint not found in appsettings.Development.json");
string? chatCompletionModel = config["ChatCompletionModel"] ?? throw new Exception("ChatCompletionModel not found in appsettings.Development.json");
string? embeddingModel = config["EmbeddingModel"] ?? throw new Exception("EmbeddingModel not found in appsettings.Development.json");

var openAiService = new OpenAIService(new OpenAiOptions()
{
    ApiKey = token,
    BaseDomain = endpoint
});

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("===============\n1. Ingestion\n===============");

Console.ForegroundColor = ConsoleColor.Blue;
Console.WriteLine("--> Raw document is split into chunks...");

string markdown = File.ReadAllText("../../sample_company_handbook.md");

// var chunks = new List<MarkdownChunk>();
var chunks = new List<(string Heading, string Text)>();

// Match each ### heading and capture everything until
// the next ### heading or the end of the document.
var pattern =
    @"(?ms)^###\s+(.+?)\r?\n(.*?)(?=^#{1,3}\s+|\z)";

foreach (Match match in Regex.Matches(markdown, pattern))
{
    string heading = match.Groups[1].Value.Trim();
    string text = match.Groups[2].Value.Trim();

    chunks.Add((heading, text));
}

Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("    Example of chunks:");
for (int i = 0; i < Math.Min(3, chunks.Count); i++)
{
    Console.WriteLine($"      ----- Chunk {i + 1} -----");
    Console.WriteLine($"      {chunks[i].Text}");
}
Console.WriteLine($"       ...\n");
Console.WriteLine($"    Total chunks: {chunks.Count}\n");

var inMemoryDatabase = new List<(string Source, string Text, List<double> Embedding)>();
foreach (var chunk in chunks)
{
    var resp = await openAiService.Embeddings.CreateEmbedding(new EmbeddingCreateRequest
    {
        Input = chunk.Text,
        Model = embeddingModel
    });
    var chunkEmbedding = resp.Data.First().Embedding;
    inMemoryDatabase.Add((chunk.Heading, chunk.Text, chunkEmbedding));
}

Console.ForegroundColor = ConsoleColor.Blue;
Console.WriteLine("--> Each chunk is embedded into a vector embedding and stored in a in-memory \"database\"...");

Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("    Example of embeddings:");
for (int i = 0; i < Math.Min(3, inMemoryDatabase.Count); i++)
{
    Console.WriteLine($"      ----- Chunk {i + 1} -----");
    Console.WriteLine($"      Text: {inMemoryDatabase[i].Text}");
    Console.WriteLine($"      Embedding: {string.Join(", ", inMemoryDatabase[i].Embedding.Take(3))}...");
    Console.WriteLine($"      Embedding dimension: {inMemoryDatabase[i].Embedding.Count}");
}
Console.WriteLine($"      ...\n");
Console.WriteLine($"   Total embedding stored: {inMemoryDatabase.Count}\n");

Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine("Ready for user's questions!");

while (true)
{
    Console.Write("\n> ");
    var prompt = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(prompt) || prompt == "exit") break;

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("\n===============\n2. Retrieval\n===============");
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.WriteLine("--> Query is embedded into a vector...");
    var queryEmbeddingResponse = await openAiService.Embeddings.CreateEmbedding(new EmbeddingCreateRequest
    {
        Input = prompt,
        Model = embeddingModel
    });
    var queryVector = queryEmbeddingResponse.Data.First().Embedding;
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"    Query vector:");
    Console.WriteLine($"      {string.Join(", ", queryVector.Take(3))}...");
    Console.WriteLine($"      Embedding dimension: {queryVector.Count}\n");

    Console.ForegroundColor = ConsoleColor.Blue;
    Console.WriteLine("--> The query vector is compared with the in-memory chunk embeddings to find the top 3 relevant chunks...");
    var topChunks = inMemoryDatabase
        .Select(d => (d.Source, d.Text, Score: CosineSimilarity(queryVector, d.Embedding)))
        .OrderByDescending(d => d.Score)
        .Take(3)
        .ToList();

    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"    Top 3 retrieved chunks with their respective scores:");
    foreach (var c in topChunks)
    {
        Console.WriteLine($"      {c.Source}: {c.Text}    [{c.Score:F3}]");
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("\n===============\n3. Generation\n===============");
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.WriteLine("--> The retrieved chunks are added to the original prompt...");
    var context = string.Join("\n      ", topChunks.Select(c => c.Source + ": " + c.Text));
    var augmentedPrompt = $"""

          Instruction:
          Answer the question using the provided context.

          Context:
          {context}

          Question
          {prompt}
    """;

    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"    Augmented Prompt:\n      {augmentedPrompt}\n");

    Console.ForegroundColor = ConsoleColor.Blue;
    Console.WriteLine("--> The augmented prompt is passed to the LLM to generate a response...");
    var req = new ChatCompletionCreateRequest
    {
        Messages = new List<ChatMessage>
        {
            ChatMessage.FromUser(augmentedPrompt),
        }
    };

    var completionResult = await openAiService.ChatCompletion.CreateCompletion(req, chatCompletionModel, ct);
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine(completionResult.Choices[0].Message.Content);
}

double CosineSimilarity(List<double> a, List<double> b)
{
    double dot = 0, na = 0, nb = 0;
    for (int i = 0; i < a.Count; i++)
    {
        dot += a[i] * b[i];
        na += a[i] * a[i];
        nb += b[i] * b[i];
    }
    return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
}
