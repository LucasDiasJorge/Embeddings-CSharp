

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
    options.UseNpgsql(connectionString));
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString, o => o.UseVector());
});

builder.Services.AddTransient<OllamaApiClient>(x => new OllamaApiClient(
    uriString: "http://localhost:11434",
    defaultModel: "mxbai-embed-large"
));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("v1/seed", async (AppDbContext context, OllamaApiClient ollama) =>
{
    var products = await context.Products.ToListAsync();


    foreach (var product in products)
    {
        var service = ollama.AsTextEmbeddingGenerationService();
        var embeddings = await service.GenerateEmbeddingAsync(product.Category);

        var recomendation = new Recomendation
        {
            Title = product.Title,
            Category = product.Category,
            Embedding = new Vector(embeddings)
        };

        await context.Recomendations.AddAsync(recomendation);
    }
    await context.SaveChangesAsync();
    return Results.Ok(products);
});

app.MapPost("v1/products", async (AppDbContext context, OllamaApiClient ollama, CreateProductViewModel product) =>
{

    var service = ollama.AsTextEmbeddingGenerationService();
    var embeddings = await service.GenerateEmbeddingAsync(product.Category);

    var recomendation = new Recomendation
    {
        Title = product.Title,
        Category = product.Category,
        Embedding = new Vector(embeddings)
    };

    var newProduct = new Product
    {
        Title = product.Title,
        Category = product.Category,
        Summary = product.Summary,
        Description = product.Description
    };

    await context.Products.AddAsync(newProduct);
    await context.Recomendations.AddAsync(recomendation);
    await context.SaveChangesAsync();
    return Results.Ok(recomendation);
});

app.MapPost("v1/prompt", async (QuestionViewModel question, AppDbContext context, OllamaApiClient ollama) =>
{
    var service = ollama.AsTextEmbeddingGenerationService();
    string promptOtimizado = $"Represent this sentence for searching relevant passages: {question.Prompt}";
    var embeddings = await service.GenerateEmbeddingAsync(promptOtimizado);

    // oq eh o AsNoTracking ???
    var recomendations = await context.Recomendations.AsNoTracking().OrderBy(x => x.Embedding.CosineDistance(new Vector(embeddings.ToArray()))).Take(3).Select(x => new { x.Title, x.Category } ).ToListAsync();

    return Results.Ok(recomendations);
});

app.Run();
