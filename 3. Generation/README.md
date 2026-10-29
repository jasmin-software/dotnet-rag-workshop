# Stage 3: Generation

For this stage, we will implementing `Relevant chunk` + `Prompt/Query` -> `Augmented prompt` -> `LLM`.

![RAG flowchart for dark theme](../assets/RAG_Generation_dark.png#gh-dark-mode-only)
![RAG flowchart for light theme](../assets/RAG_Generation_light.png#gh-light-mode-only)

---

## 9. Augment the original prompt

In the same while loop, combine the retrieved chunks and add it to the prompt:

```csharp
     var context = string.Join(
          "\n      ",
          topChunks.Select(c => c.Source + ": " + c.Text));

     var augmentedPrompt = $"""

          Instruction:
          Answer the question using the provided context.

          Context:
          {context}

          Question
          {prompt}
     """;
```

This step explains the term **Retrieval-Augmented Generation**:

```text
Retrieval
   ↓
Find relevant information

Augmentation
   ↓
Add that information to the prompt

Generation
   ↓
Ask the language model to produce an answer
```

The model is not being retrained on the handbook. The handbook information is supplied as context at query time.

---

## 10. Generate the final answer

In the same while loop, create and send a chat-completion request:

```csharp
     var req = new ChatCompletionCreateRequest
     {
     Messages = new List<ChatMessage>
     {
          ChatMessage.FromUser(augmentedPrompt),
     }
     };

     var completionResult =
     await openAiService.ChatCompletion.CreateCompletion(
          req,
          chatCompletionModel,
          ct);
```

Finally, the generated response is printed:

```csharp
     Console.WriteLine(
          completionResult.Choices[0].Message.Content);
```

Now, we've implemented the generation phase of RAG, and you can start interacting with the program.

Run `dotnet build` and `dotnet run`. You can start asking questions when you see the following in the console:

```text
Ready for user's questions!

> 
```
> [!TIP]
>
> Your questions will be embeded into a query vector, and will be compared to other embeddings in the in-memory "database" to retrieve the top 3 relevant chunks.
>
> The relevant chunks will be used to augment your original question.
>
> Lastly, the augmented prompt will be used to generate the response.

Here's what the response might look like:
```text
Ready for user's questions!

> When can I work from home?

Based on the provided context, you are allowed to work from home on Tuesday, unless an exception is approved by the company.
```

