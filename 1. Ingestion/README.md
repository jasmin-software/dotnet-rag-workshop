# 1. Ingestion

For this stage, we will implementing `Raw document` -> `Chunks` -> `Embedding model` -> ~~`Vector database`~~ `In-memory "database"`.

![RAG flowchart for dark theme](../assets/RAG_dark.png#gh-dark-mode-only)
![RAG flowchart for light theme](../assets/RAG_light.png#gh-light-mode-only)

---

## 1. Project setup

The comments at the beginning show the basic .NET commands:

```bash
dotnet new console -n dotnet_rag
cd dotnet_rag
dotnet add package Betalgo.OpenAI
```

Create a file called `appsettings.Development.json` in the project root directory, paste this in the file:

```json
{
    "Token": "put-your-google-ai-studio-api-key-here",
    "Endpoint": "https://generativelanguage.googleapis.com/v1beta/openai/",
    "ChatCompletionModel": "gemini-3.5-flash-lite",
    "EmbeddingModel": "gemini-embedding-2"
}
```
Replace the token with what you got in [0. Google AI Studio Key]().

## 2. Load configuration

Open `Program.cs` and add these:

```csharp
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using System.Text.RegularExpressions;

CancellationToken ct = new();
```

The cancellation token is later passed into the chat-completion request.

The application reads its settings from `appsettings.Development.json`:

```csharp
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.Development.json",
                 optional: true,
                 reloadOnChange: true)
    .Build();
```

It then retrieves four configuration values:

```csharp
string? token = config["Token"]
    ?? throw new Exception("Token not found in appsettings.Development.json");

string? endpoint = config["Endpoint"]
    ?? throw new Exception("Endpoint not found in appsettings.Development.json");

string? chatCompletionModel = config["ChatCompletionModel"]
    ?? throw new Exception("ChatCompletionModel not found in appsettings.Development.json");

string? embeddingModel = config["EmbeddingModel"]
    ?? throw new Exception("EmbeddingModel not found in appsettings.Development.json");
```

These represent:

- `Token` - API authentication credential.
- `Endpoint` - API base endpoint.
- `ChatCompletionModel` - model used to generate the final answer.
- `EmbeddingModel` - model used to convert text into vectors.

If a required value is missing, the program stops immediately with an exception.

The API client is then initialized:

```csharp
var openAiService = new OpenAIService(new OpenAiOptions()
{
    ApiKey = token,
    BaseDomain = endpoint
});
```

---

## 3. Read the source document

Download and save `sample_company_handbook.md` in your project, and add this line to read it:

```csharp
string markdown =
    File.ReadAllText("../sample_company_handbook.md");
```

Instead of embedding the entire handbook as one large piece of text, the application divides it into smaller **chunks**.

---

## 4. Split the document into chunks

Chunks are stored as tuples and the Markdown sections are identified with a regular expression:

```csharp
var chunks = new List<(string Heading, string Text)>();

var pattern =
    @"(?ms)^###\s+(.+?)\r?\n(.*?)(?=^#{1,3}\s+|\z)";

foreach (Match match in Regex.Matches(markdown, pattern))
{
    string heading = match.Groups[1].Value.Trim();
    string text = match.Groups[2].Value.Trim();

    chunks.Add((heading, text));
}
```

About the regular expression:

- `(.+?)` captures the heading.
- `(.*?)` captures the text following the heading.
- The final lookahead stops the chunk when another level 1–3 Markdown heading or the end of the document is encountered.

The result is approximately:

```text
Chunk 1
Heading: Vacation Policy
Text: Employees receive 20 vacation days per year.

Chunk 2
Heading: Remote Work
Text: Employees may work remotely three days per week.

Chunk 3
Heading: Sick Leave
Text: Employees receive 10 paid sick days per year.
```

---

## 5. Create embeddings

The next step converts every chunk into an **embedding**.

An embedding is a numeric vector representing semantic information about text. Texts with similar meanings tend to have vectors that are closer together in the embedding space.

The application creates an in-memory data store and embeds every chunk:

```csharp
var inMemoryDatabase =
    new List<(string Source,
              string Text,
              List<double> Embedding)>();

foreach (var chunk in chunks)
{
    var resp =
        await openAiService.Embeddings.CreateEmbedding(
            new EmbeddingCreateRequest
            {
                Input = chunk.Text,
                Model = embeddingModel
            });

    var chunkEmbedding = resp.Data.First().Embedding;

    inMemoryDatabase.Add(
        (chunk.Heading, chunk.Text, chunkEmbedding));
}
```

Ingestion is done. 

The `sample_company_handbook.md` is broken down into chunks. Each chunk is embedded and stored in the in-memory "database".

![RAG flowchart for dark theme](../assets/RAG_dark.png#gh-dark-mode-only)
![RAG flowchart for light theme](../assets/RAG_light.png#gh-light-mode-only)

### Next step: [2. Retrieval](../2.%20Retrieval/README.md)