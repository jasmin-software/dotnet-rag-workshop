# Stage 3: Generation

For this stage, we will implementing `Relevant chunk` -> `Augmented prompt` -> `LLM`.

![RAG flowchart for dark theme](../assets/RAG_dark.png#gh-dark-mode-only)
![RAG flowchart for light theme](../assets/RAG_light.png#gh-light-mode-only)

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

Generation is now done.

The relevant chunks are used to augment the prompt. The augmented prompt is used to generate the response.


![RAG flowchart for dark theme](../assets/RAG_dark.png#gh-dark-mode-only)
![RAG flowchart for light theme](../assets/RAG_light.png#gh-light-mode-only)
