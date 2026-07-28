

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel.Embeddings;
using MyPgVectorStore.Data;
using MyPgVectorStore.Models;
using MyPgVectorStore.ViewModels;
using OllamaSharp;
using Pgvector;
using Pgvector.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, o => o.UseVector()));

builder.Services.AddTransient<OllamaApiClient>(x => new OllamaApiClient(
    uriString: "http://localhost:11434",
    // qualidade de output depende do modelo, n inventa de modelo 300m
    defaultModel: "qwen3-embedding:4b"
));

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "Hello World!");

app.MapGet("/v1/seed", async (AppDbContext context, OllamaApiClient ollama) =>
{
    // Só embeda produtos que ainda não têm recomendação, pra rodar o seed várias vezes sem duplicar.
    var pendingProducts = await context.Products
        .Where(p => !context.Recomendations.Any(r => r.ProductId == p.Id))
        .ToListAsync();

    foreach (var product in pendingProducts)
    {
        var recomendation = await CreateRecomendationAsync(product, ollama);
        await context.Recomendations.AddAsync(recomendation);
    }
    await context.SaveChangesAsync();

    return Results.Ok(new { seeded = pendingProducts.Count });
});

app.MapPost("/v1/products", async (AppDbContext context, OllamaApiClient ollama, CreateProductViewModel product) =>
{
    if (string.IsNullOrWhiteSpace(product.Title) || string.IsNullOrWhiteSpace(product.Category))
    {
        return Results.BadRequest("Title e Category são obrigatórios.");
    }

    var newProduct = new Product
    {
        Title = product.Title,
        Category = product.Category,
        Summary = product.Summary,
        Description = product.Description
    };

    await context.Products.AddAsync(newProduct);
    await context.SaveChangesAsync(); // precisa do Id gerado antes de vincular a recomendação

    var recomendation = await CreateRecomendationAsync(newProduct, ollama);
    await context.Recomendations.AddAsync(recomendation);
    await context.SaveChangesAsync();

    return Results.Ok(recomendation);
});

app.MapPost("/v1/prompt", async (QuestionViewModel question, AppDbContext context, OllamaApiClient ollama, int top = 3) =>
{
    if (string.IsNullOrWhiteSpace(question.Prompt))
    {
        return Results.BadRequest("Prompt é obrigatório.");
    }

    var service = ollama.AsTextEmbeddingGenerationService();
    var promptOtimizado = $"Represent this sentence for searching relevant passages: {question.Prompt}";
    var embeddings = await service.GenerateEmbeddingAsync(promptOtimizado);
    var queryVector = new Vector(embeddings.ToArray());

    var recomendations = await context.Recomendations
        .AsNoTracking()
        .Join(context.Products, r => r.ProductId, p => p.Id, (r, p) => new { r, p })
        .OrderBy(x => x.r.Embedding.CosineDistance(queryVector))
        .Take(top)
        .Select(x => new
        {
            x.r.Title,
            x.r.Category,
            x.p.Summary,
            x.p.Description,
            Score = 1 - x.r.Embedding.CosineDistance(queryVector)
        })
        .ToListAsync();

    return Results.Ok(recomendations);
});

app.Run();

// Texto usado para gerar o embedding: combina título, categoria, resumo e descrição
// pra dar mais sinal semântico do que só a categoria (várias categorias se repetem entre produtos).
static string BuildEmbeddingText(Product product) =>
    $"{product.Title}\nCategoria: {product.Category}\n{product.Summary}\n{product.Description}";

static async Task<Recomendation> CreateRecomendationAsync(Product product, OllamaApiClient ollama)
{
    var service = ollama.AsTextEmbeddingGenerationService();
    var embeddings = await service.GenerateEmbeddingAsync(BuildEmbeddingText(product));

    return new Recomendation
    {
        ProductId = product.Id,
        Title = product.Title,
        Category = product.Category,
        Embedding = new Vector(embeddings)
    };
}
