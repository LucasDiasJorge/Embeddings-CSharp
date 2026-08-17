# Inventory — API de inventário de ativos com auditoria e busca semântica

API em .NET 10 para controle de **ativos serializados**: cada item é uma peça física única,
está em exatamente uma localização por vez, e toda mudança de estado deixa rastro.
Em cima desse rastro há duas coisas: uma **auditoria de inventário** (contagem) que corrige o
sistema a partir do mundo real, e uma **busca semântica** que responde perguntas em linguagem
natural sobre a vida dos itens, via Ollama + pgvector.

Arquitetura **ports and adapters** (hexagonal), com a regra de dependência garantida pelo
compilador, não por convenção.

---

## O nome da regra que você procurava

O conceito de "auditor/inventário" tem nome consagrado, e vale adotar o vocabulário —
ele evita discussões e casa com qualquer WMS/ERP que você venha a integrar:

| Conceito | Nome no código | Nome no mercado (PT / EN) |
|---|---|---|
| A rodada de auditoria numa localização | `InventoryCount` | Contagem de inventário / *Inventory Count*, *Cycle Count*, *Stock Take* |
| Quem executa | `Auditor` (campo) | Auditor / *Counter* |
| O que o sistema achava que estava lá | `ExpectedItem` | Saldo esperado / *Expected*, *Book quantity* |
| O que o auditor leu | `CountScan` | Leitura / *Scan*, *Count line* |
| O confronto entre os dois | `CountReconciliation` | Reconciliação / *Reconciliation*, *Variance* |
| O acerto gerado no fechamento | `MovementReason.CountReconciliation` | Acerto de inventário / *Adjustment* |
| A linha do tempo do item | `ItemLedgerEntry` | Razão do item / *Item Ledger Entry* |

Se for escolher **um** termo para nomear a feature inteira: **Contagem de Inventário**
(*Inventory Count*). "Inventário rotativo" (*cycle count*) é o caso específico de contar
uma localização por vez em ciclos, que é exatamente o que esta API faz.

---

## O modelo de domínio em cinco frases

1. Um **Item** é um ativo físico único, com etiqueta (`Sku`), e está em **uma** `Location`.
2. Toda transição de estado do item escreve uma linha no **razão** (`ItemLedgerEntry`), que é *append-only*.
3. Uma **contagem** abre numa localização e **congela** a lista de itens que o sistema acredita estarem lá.
4. Ao fechar, a contagem devolve o veredito: **confirmados**, **faltantes**, **inesperados**.
5. O caso de uso aplica o veredito: confirmados renovam a confiança, faltantes viram `Missing`,
   inesperados são **realocados** — e cada consequência vira mais uma linha no razão.

O passo 2 é o que torna o passo seguinte possível: **você não consegue movimentar um item
sem deixar rastro**, porque não existe setter público que mude estado sem gravar razão.

---

## Estrutura

```
Inventory/
├── src/
│   ├── Inventory.Domain/          # o hexágono — ZERO dependências
│   │   ├── Items/                 #   Item, Sku, ItemLedgerEntry, MovementReason
│   │   ├── Locations/             #   Location, LocationCode
│   │   └── Counting/              #   InventoryCount, CountReconciliation
│   ├── Inventory.Application/     # casos de uso + definição das portas dirigidas
│   │   ├── Contracts/             #   comandos e DTOs — servem também de contrato HTTP
│   │   ├── Ports/Driven/          #   o que a aplicação precisa (IItemRepository, ...)
│   │   ├── Services/              #   os casos de uso
│   │   └── Narration/             #   ItemNarrator — fatos → português
│   ├── Inventory.Infrastructure/  # adaptadores dirigidos
│   │   ├── Persistence/           #   EF Core + Postgres
│   │   ├── Embeddings/            #   Ollama + pgvector (e o adaptador no-op)
│   │   └── sql/                   #   schema.sql, schema_embeddings.sql
│   └── Inventory.Api/             # adaptador dirigente: HTTP
└── tests/Inventory.Tests/         # domínio, casos de uso (fakes) e mapeamento EF
```

A regra de dependência é **verificada pelo compilador**: `Inventory.Domain.csproj` não
referencia nada. Um `using Microsoft.EntityFrameworkCore` dentro do domínio quebra o build.

```
        HTTP (Inventory.Api)
              │  chama
              ▼
   ┌─────────────────────────┐
   │  ┌───────────────────┐  │   ItemService, InventoryCountService,
   │  │  Application      │  │   LocationService, ItemInsightService
   │  │  ┌─────────────┐  │  │
   │  │  │   Domain    │  │  │   Item, Location, InventoryCount — regra pura
   │  │  └─────────────┘  │  │
   │  └───────────────────┘  │
   │  Ports/Driven           │   IItemRepository, IClock, IItemNarrativeIndex
   └─────────────────────────┘
              │  implementado por
              ▼
   Postgres/EF · Ollama+pgvector · SystemClock   (Inventory.Infrastructure)
```

A inversão de dependência mora no lado **dirigido**, que é onde ela paga: a aplicação
declara `IItemRepository` e a Infrastructure é quem se encaixa. Do lado dirigente não há
interface — a API depende das classes de caso de uso direto, porque uma interface com uma
implementação só não desacopla nada quando ninguém pode depender do projeto da API mesmo.

---

## Rodando

### Pré-requisitos

- .NET SDK 10
- PostgreSQL (a extensão `vector` só é necessária se você ligar o embedding)
- Ollama, só para a busca semântica:
  ```bash
  ollama pull qwen3-embedding:4b
  ```

### Configuração

Ajuste a connection string em `src/Inventory.Api/appsettings.json`
(ou, melhor, use [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=Inventory;Username=postgres;Password=SUA_SENHA"
  }
}
```

Crie o schema:

```bash
psql -d Inventory -f src/Inventory.Infrastructure/sql/schema.sql
```

Em desenvolvimento, `Database:AutoCreate: true` já cria tudo via `EnsureCreated` no startup.

```bash
cd src/Inventory.Api
dotnet run
```

OpenAPI em `http://localhost:5180/openapi/v1.json`.

### Testes

```bash
dotnet test                                  # tudo
dotnet test --filter Category!=Integration   # só os que não precisam de infraestrutura
dotnet test --filter Category=Integration    # o caminho real: Postgres + Ollama + pgvector
```

**41 testes de unidade** cobrem domínio, casos de uso e mapeamento EF sem banco, sem Docker
e sem Ollama — o argumento prático da arquitetura: os casos de uso *reais* rodam contra
fakes de ~150 linhas (`tests/Inventory.Tests/Fakes/`).

**1 teste de integração** exercita a pilha inteira com as peças de verdade: cria um banco
temporário, roda uma auditoria completa e pergunta *"qual equipamento de informática
desapareceu do estoque?"* — texto que não compartilha nenhuma palavra com a narrativa
gravada ("Notebook Dell Latitude 5440 … não foi encontrado"). Se o notebook vier na frente
da cadeira, foi o embedding trabalhando. Sem Postgres ou Ollama no ar, ele sai sem falhar
e explica o motivo na saída.

---

## Endpoints

### Localizações

| Método | Rota | |
|---|---|---|
| `POST` | `/v1/locations` | Cadastra (código único, hierarquia opcional via `parentId`) |
| `GET` | `/v1/locations` | Lista |
| `GET` | `/v1/locations/{id}` | Detalha |
| `PATCH` | `/v1/locations/{id}` | Renomeia |
| `POST` | `/v1/locations/{id}/deactivate` | Desativa (409 se ainda houver itens) |
| `GET` | `/v1/locations/{id}/items` | O que o sistema acredita estar aqui |

### Itens

| Método | Rota | |
|---|---|---|
| `POST` | `/v1/items` | Registra |
| `GET` | `/v1/items?sku=&locationId=&status=&search=` | Busca por filtros |
| `GET` | `/v1/items/{id}` · `/v1/items/by-sku/{sku}` | Detalha |
| `POST` | `/v1/items/{id}/move` | Movimenta |
| `POST` | `/v1/items/{id}/retire` | Dá baixa |
| **`GET`** | **`/v1/items/{id}/history`** | **A vida inteira do item, narrada** |

### Contagem de inventário (a auditoria)

| Método | Rota | |
|---|---|---|
| `POST` | `/v1/inventory-counts` | Abre e **congela** os esperados |
| `GET` | `/v1/inventory-counts/{id}` | Estado ao vivo: pendentes / confirmados / inesperados |
| `POST` | `/v1/inventory-counts/{id}/scans` | Registra leitura (por `sku` ou `itemId`) — idempotente |
| `DELETE` | `/v1/inventory-counts/{id}/scans/{itemId}` | Desfaz leitura |
| `POST` | `/v1/inventory-counts/{id}/close` | **Fecha e aplica o veredito** |
| `POST` | `/v1/inventory-counts/{id}/cancel` | Cancela sem tocar em nada |
| `GET` | `/v1/inventory-counts/{id}/report` | Relatório (recalculável para sempre) |

### Insights e busca semântica

| Método | Rota | |
|---|---|---|
| `GET` | `/v1/items/{id}/insights` | Estatística do razão — **funciona sem embedding** |
| `POST` | `/v1/insights/items/search` | **Encontra itens por prompt** |
| `POST` | `/v1/insights/search` | Busca no nível do evento |
| `POST` | `/v1/insights/reindex` | Reconstrói o índice a partir do razão |

---

## Fluxo completo de uma auditoria

```bash
# 1. localização e itens
curl -X POST localhost:5180/v1/locations -H 'Content-Type: application/json' \
  -d '{"code":"ALM-A3","name":"Almoxarifado Central A3"}'

curl -X POST localhost:5180/v1/items -H 'Content-Type: application/json' \
  -d '{"sku":"PAT-001","name":"Notebook Dell Latitude 5440","locationId":"<id>","actor":"lucas"}'

# 2. o auditor abre a contagem — os esperados ficam congelados aqui
curl -X POST localhost:5180/v1/inventory-counts -H 'Content-Type: application/json' \
  -d '{"locationId":"<id>","auditor":"Ana, auditoria interna"}'

# 3. bipa o que encontra (idempotente; item inesperado é aceito de propósito)
curl -X POST localhost:5180/v1/inventory-counts/<count>/scans -H 'Content-Type: application/json' \
  -d '{"sku":"PAT-001"}'

# 4. fecha: aplica o veredito e escreve tudo no razão
curl -X POST localhost:5180/v1/inventory-counts/<count>/close
```

O fechamento devolve `accuracy` (confirmados ÷ esperados), a lista de faltantes e a de
realocados. E, para cada item envolvido, uma nova linha no razão explicando o porquê.

---

## Busca semântica

### Por que indexar o histórico, e não o cadastro

A intuição é embedar nome e descrição dos itens. Isso responde "notebook Dell", que um
`LIKE` já resolvia. O que um `LIKE` não resolve é o que está no **histórico**:

> "equipamentos que vivem sumindo do almoxarifado"
> "o que foi realocado por acerto de inventário no último trimestre"
> "itens parados na sala 204 que ninguém confere há meses"

Por isso o que vai para o índice é o razão, narrado em português pelo `ItemNarrator`:

```
02/03/2026 às 13:00 — Notebook Dell Latitude 5440 (etiqueta PAT-001) foi movido de
Almoxarifado Central A3 (ALM-A3) para Sala 204 — TI (SALA-204). Motivo: acerto de
inventário (item encontrado em local divergente). Responsável: Ana, auditoria interna.
Contagem de inventário 019… Observação: Encontrado em localização divergente durante a contagem.
```

Esse texto carrega nome, etiqueta, **os dois lugares por extenso**, o motivo, quem fez e
quando. Um par de GUIDs não carregaria nada disso — e é essa diferença que decide se a
busca funciona.

`POST /v1/insights/items/search` busca nesses eventos e **agrega o resultado por item**,
devolvendo cada item com localização atual, há quantos dias não é visto, e as narrativas
que justificaram o casamento:

```json
{ "prompt": "notebooks que vivem sumindo do almoxarifado", "top": 5 }
```

```json
[{
  "sku": "PAT-001",
  "name": "Notebook Dell Latitude 5440",
  "status": "Missing",
  "currentLocation": "Almoxarifado Central A3 (ALM-A3)",
  "daysSinceLastSeen": 128,
  "score": 0.83,
  "matchCount": 3,
  "evidence": [ { "text": "12/01/2026 às 09:14 — ... não foi encontrado ...", "score": 0.83 } ]
}]
```

A `evidence` não é enfeite: num sistema de auditoria, "o modelo achou que sim" não é
resposta aceitável. O usuário precisa ver **quais** eventos casaram para decidir se concorda.

### Ligando

```json
"Embeddings": {
  "Enabled": true,
  "Endpoint": "http://localhost:11434",
  "Model": "qwen3-embedding:4b",
  "Dimensions": 2560,
  "TruncateDimensions": false
}
```

```bash
psql -d Inventory -f src/Inventory.Infrastructure/sql/schema_embeddings.sql
curl -X POST localhost:5180/v1/insights/reindex   # indexa o histórico já acumulado
```

A partir daí, cada movimentação e cada fechamento de contagem alimentam o índice
automaticamente, **depois** do commit (índice atrasado se reconstrói; razão inconsistente, não).

### Três detalhes que decidem a qualidade

**1. Embedding assimétrico.** A pergunta recebe a instrução que a família Qwen3-Embedding
espera (`Instruct: {tarefa}\nQuery: {pergunta}`); o documento vai cru. Embedar os dois do
mesmo jeito é o erro mais comum e derruba o recall silenciosamente. A instrução está em
`Embeddings:QueryInstruction` e vale ajustá-la ao seu vocabulário.

**2. Limite de 2000 dimensões do pgvector.** Índices HNSW e IVFFlat não aceitam mais que
isso, e `qwen3-embedding:4b` gera **2560** — ou seja, com a configuração padrão toda busca
é *scan sequencial*. Tudo bem em um acervo pequeno; inviável com centenas de milhares de
linhas de razão. A saída é Matryoshka:

```json
"Embeddings": { "Dimensions": 1024, "TruncateDimensions": true }
```

Recrie `item_narratives` com `vector(1024)`, rode o reindex e crie o índice:

```sql
CREATE INDEX ix_item_narratives_embedding ON item_narratives USING hnsw (embedding vector_cosine_ops);
```

Requer um Ollama recente (que aceite o parâmetro `dimensions`); se o modelo não suportar,
o gerador falha na primeira chamada com uma mensagem dizendo exatamente isso, em vez de
gravar vetores errados no banco.

**3. O índice é descartável.** Ele é derivado do razão. Trocou de modelo? `DROP TABLE`,
recria com a nova dimensão, `POST /v1/insights/reindex`. Nada de histórico se perde,
porque histórico nunca esteve lá.

### Sem Ollama

Com `Embeddings:Enabled: false`, entra o `NoOpItemNarrativeIndex` e a aplicação inteira
funciona — cadastro, movimentação, auditoria, `GET /history`, `GET /insights`. Só as rotas
de busca semântica respondem `409 insight.index_disabled`. É deliberado: lista vazia
passaria por "não existe nada assim no acervo", que é uma mentira diferente.

---

## Decisões de projeto

**Ledger append-only em vez de log de auditoria opcional.** O razão *é* o modelo, não um
efeito colateral. `IItemLedgerRepository` não tem `Update` nem `Delete` — a ausência é o contrato.

**Esperados congelados na abertura da contagem.** Se fossem avaliados no fechamento, alguém
movimentando itens durante a auditoria mudaria o que o auditor deveria ter encontrado.
Existe teste cobrindo exatamente isso.

**O comando do caso de uso é o corpo da requisição.** Não existe um `RegisterItemRequest`
no adaptador HTTP que copie campo a campo para um `RegisterItemCommand`: o endpoint recebe
o comando direto. A regra que mantém isso honesto é que **comando carrega só o que vem no
corpo** — `Guid` cru, nada de identificador tipado — enquanto o id do recurso alvo vem da
rota e entra como parâmetro do método: `MoveAsync(ItemId id, MoveItemCommand command)`.
O preço é que mudar o JSON passa a mexer no projeto da aplicação; em troca somem dez
records e o mapeamento inteiro entre eles.

**`DomainException` → 422, `Result<T>` → 400/404/409.** Violação de invariante (movimentar
item baixado) é 422: a requisição foi entendida, a operação é que contradiz o estado do
mundo. Falha esperada de caso de uso (item não existe) volta como `Result` tipado. Todo
erro traz um `code` estável no corpo.

**Identificadores tipados (`ItemId`, `LocationId`).** `Guid` e `Guid` são intercambiáveis
para o compilador; `ItemId` e `LocationId` não. Num domínio que passa os dois lado a lado
o tempo todo, isso pega bugs em tempo de compilação. O custo é um conversor por tipo,
registrado uma vez em `ConfigureConventions`.

**Índice semântico em `DbContext` separado.** Assim o inventário não passa a exigir a
extensão `vector`, a dimensão do vetor pode vir de configuração sem contaminar o modelo
principal, e o índice pode morar em outro banco. Como consequência, o `IModelCacheKeyFactory`
inclui a dimensão — sem isso, o primeiro contexto criado no processo ditaria a dimensão de
todos os outros.

**Contagem única por localização, garantida no banco.** A checagem na aplicação não resolve
duas requisições simultâneas; o índice parcial `WHERE status = 0` resolve.

---

## Próximos passos naturais

- **Autenticação**: hoje `actor` e `auditor` são strings vindas do cliente. Em produção
  devem vir do token, não do corpo da requisição.
- **Contagem por múltiplas localizações** numa rodada só (hoje é uma por vez).
- **Alertas** em cima do que `GET /insights` já calcula: item sem confirmação há N dias,
  item que sumiu e reapareceu mais de N vezes.
- **Reranking** dos resultados semânticos com um cross-encoder, se o volume justificar.
