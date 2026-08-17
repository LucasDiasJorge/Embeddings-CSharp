using Inventory.Api.Endpoints;
using Inventory.Api.Http;
using Inventory.Application;
using Inventory.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// As duas linhas que montam o hexágono: o miolo, e depois os adaptadores dele.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Conveniência de desenvolvimento. Em produção, aplique sql/schema.sql.
    if (app.Configuration.GetValue<bool>("Database:AutoCreate"))
    {
        await app.Services.EnsureSchemaAsync();
    }
}

app.MapGet("/", () => Results.Ok(new { service = "inventory-api", status = "ok" }))
    .ExcludeFromDescription();

app.MapLocationEndpoints();
app.MapItemEndpoints();
app.MapInventoryCountEndpoints();
app.MapInsightEndpoints();

app.Run();

/// <summary>Exposto para os testes de integração poderem instanciar a API com WebApplicationFactory.</summary>
public partial class Program;
