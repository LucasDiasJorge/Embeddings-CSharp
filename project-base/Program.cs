using Microsoft.SemanticKernel.Embeddings;
using OllamaSharp;

namespace project;

class Program
{
    static async Task Main(string[] args)
    {
        using OllamaApiClient ollamaApiClient = new OllamaApiClient(
            uriString: "http://localhost:11434",
            defaultModel: "nomic-embed-text");

        ITextEmbeddingGenerationService service = ollamaApiClient.AsTextEmbeddingGenerationService();
        IList<ReadOnlyMemory<float>> embeddings = await service.GenerateEmbeddingsAsync([
            "Café Brasileiro",
            "Café Italiano",
            "Café Português"]);

        foreach (ReadOnlyMemory<float> embedding in embeddings)
            Console.WriteLine(embedding);

    }
}
