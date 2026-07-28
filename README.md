# Embeddings-CSharp

Estudos e experimentos em C#/.NET sobre **embeddings de texto**, geração local de vetores com **Ollama** e busca semântica usando **pgvector** no PostgreSQL.

O repositório contém dois projetos:

| Projeto | Descrição |
|---|---|
| [`project-base/`](project-base) | Exemplo mínimo em console: gera embeddings de frases usando Ollama + Semantic Kernel e imprime os vetores resultantes. |
| [`MyPgVectorStore/`](MyPgVectorStore) | API mínima em ASP.NET Core que persiste produtos e recomendações (embeddings) no Postgres via pgvector, com busca por similaridade semântica. |

## Tecnologias

- .NET 10
- [Microsoft.SemanticKernel](https://learn.microsoft.com/semantic-kernel/) (conectores Ollama e PgVector)
- [OllamaSharp](https://github.com/awaescher/OllamaSharp) — cliente para o [Ollama](https://ollama.com/) rodando localmente
- Entity Framework Core + [Npgsql](https://www.npgsql.org/) + [Pgvector.EntityFrameworkCore](https://github.com/pgvector/pgvector-dotnet)
- PostgreSQL com a extensão [pgvector](https://github.com/pgvector/pgvector)

## Pré-requisitos

1. **Ollama** instalado e rodando em `http://localhost:11434`, com os modelos de embedding baixados:
   ```
   ollama pull nomic-embed-text
   ollama pull qwen3-embedding:4b
   ```
2. **PostgreSQL** com a extensão `vector` disponível (ex.: imagem `pgvector/pgvector`).
3. **.NET SDK 10.0**.

## project-base

Console app simples que demonstra a geração de embeddings com o modelo `nomic-embed-text`:

```bash
cd project-base
dotnet run
```

## MyPgVectorStore

API que embeda produtos (título, categoria, resumo e descrição) com o modelo `qwen3-embedding:4b` e permite buscar os mais relevantes a partir de uma pergunta em linguagem natural (similaridade de cosseno).

### Configuração

Ajuste a connection string em `MyPgVectorStore/appsettings.json` (ou `appsettings.Development.json`) apontando para o seu Postgres:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=EmbeddingEF;Username=postgres;Password=SUA_SENHA"
  }
}
```

> ⚠️ Evite manter credenciais reais versionadas em `appsettings.json`. Prefira [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) ou variáveis de ambiente em desenvolvimento.

Crie o banco/tabelas (via EF Core ou script SQL) e, se estiver atualizando um banco já existente, rode a migração:

```bash
psql -d EmbeddingEF -f add_product_id_to_recomendations.sql
```

Popule dados de exemplo (opcional):

```bash
psql -d EmbeddingEF -f insert_produtos.sql
```

### Executando

```bash
cd MyPgVectorStore
dotnet run
```

### Endpoints

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/` | Health check simples ("Hello World!"). |
| `GET` | `/v1/seed` | Gera embeddings para os produtos que ainda não possuem recomendação (idempotente). |
| `POST` | `/v1/products` | Cria um produto e já gera sua recomendação/embedding. |
| `POST` | `/v1/prompt?top=3` | Busca semântica: embeda o prompt e retorna os `top` produtos mais similares por distância de cosseno. |

Exemplo de criação de produto:

```json
POST /v1/products
{
  "title": "ASUS ROG Zephyrus G14",
  "category": "Notebook para programação e jogos",
  "summary": "Poder de um desktop em formato compacto.",
  "description": "Chassi em magnésio, tela OLED 120Hz, 32GB RAM."
}
```

Exemplo de busca semântica:

```json
POST /v1/prompt?top=3
{
  "prompt": "notebook bom para jogos e também para programar"
}
```

## Estrutura

```
MyPgVectorStore/
├── Data/            # AppDbContext (EF Core + mapeamento pgvector)
├── Models/          # Product, Recomendation
├── ViewModels/       # DTOs de entrada das rotas
├── Program.cs        # Configuração e endpoints da API
└── *.sql             # Scripts de seed e migração manual
```

## Referências

- [Vídeo de referência sobre embeddings com Ollama](https://www.youtube.com/watch?v=RSpZ9y8JkzA)
