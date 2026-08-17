-- Índice semântico do histórico dos itens.
-- Só é necessário quando Embeddings:Enabled = true. O inventário funciona inteiro sem isto.
--
-- Esta tabela é 100% descartável: ela é derivada de item_ledger_entries e pode ser
-- reconstruída a qualquer momento com POST /v1/insights/reindex. Se você trocar de modelo
-- de embedding, o procedimento é DROP TABLE, recriar com a nova dimensão e reindexar.

CREATE EXTENSION IF NOT EXISTS vector;

-- IMPORTANTE: a dimensão abaixo precisa bater com Embeddings:Dimensions.
--   qwen3-embedding:4b   -> 2560 nativo (ou 1024 com TruncateDimensions)
--   qwen3-embedding:0.6b -> 1024 nativo
--   nomic-embed-text     ->  768 nativo
CREATE TABLE IF NOT EXISTS item_narratives (
    ledger_entry_id uuid                     NOT NULL,
    item_id         uuid                     NOT NULL,
    sku             character varying(64)    NOT NULL,
    occurred_at     timestamp with time zone NOT NULL,
    text            text                     NOT NULL,
    embedding       vector(2560)             NOT NULL,
    -- A chave é o id da linha do razão: reindexar sobrescreve em vez de duplicar.
    CONSTRAINT pk_item_narratives PRIMARY KEY (ledger_entry_id)
);

-- Usado para restringir a busca ao histórico de um item específico.
CREATE INDEX IF NOT EXISTS ix_item_narratives_item ON item_narratives (item_id);


-- ---------------------------------------------------------------------------
-- Índice vetorial (recomendado a partir de ~50k linhas de razão)
-- ---------------------------------------------------------------------------
-- Os índices HNSW e IVFFlat do pgvector aceitam no máximo 2000 dimensões.
-- Com os 2560 nativos do qwen3-embedding:4b, o comando abaixo FALHA e toda busca
-- é resolvida por scan sequencial.
--
-- Para destravar o índice, reduza a dimensão via Matryoshka no appsettings:
--
--   "Embeddings": { "Dimensions": 1024, "TruncateDimensions": true }
--
-- recrie esta tabela com vector(1024), rode o reindex e então crie o índice:
--
-- CREATE INDEX IF NOT EXISTS ix_item_narratives_embedding
--     ON item_narratives USING hnsw (embedding vector_cosine_ops);
