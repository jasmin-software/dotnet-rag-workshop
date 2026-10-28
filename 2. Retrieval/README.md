
# Stage 2: Retrieval

For this stage, we will implementing `User prmopt/query` -> `Embedding model` -> ~~`Vector database`~~ `In-memory "database"` -> `Relevant chunks`.

![RAG flowchart for dark theme](../assets/RAG_dark.png#gh-dark-mode-only)
![RAG flowchart for light theme](../assets/RAG_light.png#gh-light-mode-only)

---

## 6. Get and embed the user's question

After ingestion, the program enters an interactive loop where the user can ask questions about the handbook.

Create a while loop in `Program.cs`, and we will build up what goes in it.

```csharp
while (true)
{

}
```

Add this in the while loop to get the question from the user and embed it:
```csharp
    Console.Write("\n> ");
    var prompt = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(prompt) || prompt == "exit")
        break;

    var queryEmbeddingResponse =
        await openAiService.Embeddings.CreateEmbedding(
            new EmbeddingCreateRequest
            {
                Input = prompt,
                Model = embeddingModel
            });

    var queryVector =
        queryEmbeddingResponse.Data.First().Embedding;
```

> [!TIP] 
>
> **Note**: the query does not have to contain exactly the same words as the document.
>
> For example:
> ```text
> Document: "Employees may work remotely..."
> Question: "How many days can I work from home?"
> ```
>
> Because embeddings represent semantic relationships, `"work from home"` and `"work remotely"` can still be considered similar.
>
> This is the major advantage over simple keyword matching.

---

## 7. Get the relevant chunks

Add this to the while loop to calculates the cosine similarity between the query embedding and each stored chunk embedding:

```csharp
    var topChunks = inMemoryDatabase
        .Select(d => (
            d.Source,
            d.Text,
            Score: CosineSimilarity(queryVector, d.Embedding)))
        .OrderByDescending(d => d.Score)
        .Take(3)
        .ToList();
```

This performs four operations:

```text
1. Compare query with every chunk
2. Calculate similarity score
3. Sort highest → lowest
4. Keep the top 3
```

---

## 8. Cosine similarity

Add the `CosineSimilarity` function outside of the while loop:

```csharp
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
```

Retrieval is done now that we have the relevant chunks.

The user's question is converted into a query vector, and it is used to compared with every stored chunk. The top 3 relevant chunks are retrieved.


![RAG flowchart for dark theme](../assets/RAG_dark.png#gh-dark-mode-only)
![RAG flowchart for light theme](../assets/RAG_light.png#gh-light-mode-only)

### Next step: [3. Generation](../3.%20Generation/README.md)