-- Schema base do MyPgVectorStore (PostgreSQL + pgvector).
-- Equivalente ao mapeamento de AppDbContext.OnModelCreating.

CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE IF NOT EXISTS products (
    id          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    title       TEXT NOT NULL,
    category    TEXT NOT NULL,
    summary     TEXT NOT NULL,
    description TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS recomendations (
    id         INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    product_id INT NULL REFERENCES products (id),
    title      TEXT NOT NULL,
    category   TEXT NOT NULL,
    embedding  vector(2560) NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_recomendations_product_id
    ON recomendations (product_id);

-- Sem índice ANN no embedding: HNSW/IVFFlat do pgvector só aceitam até 2000 dimensões
-- e o qwen3-embedding:4b gera 2560. A busca por cosseno (<=>) roda como seq scan exato,
-- o que é adequado para o volume atual de produtos.
--
-- Se o número de linhas crescer a ponto de incomodar, as saídas são:
--   1) usar um modelo de embedding com <= 2000 dimensões (ajustar vector(N) acima
--      e o HasColumnType em AppDbContext), ou
--   2) indexar via halfvec, que suporta até 4000 dimensões:
--        CREATE INDEX ix_recomendations_embedding ON recomendations
--            USING hnsw ((embedding::halfvec(2560)) halfvec_cosine_ops);
--      Atenção: esse índice só é usado se a consulta repetir o mesmo cast, o que o
--      LINQ atual (Embedding.CosineDistance) não gera — exigiria SQL manual.
