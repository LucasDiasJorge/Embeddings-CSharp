-- Vincula recomendations -> products, permitindo seed idempotente e resultados mais ricos.
-- Rode este script uma vez contra o banco existente antes de subir a nova versão da aplicação.

ALTER TABLE recomendations ADD COLUMN IF NOT EXISTS product_id INT;

-- Backfill best-effort para linhas já existentes (casadas por título).
UPDATE recomendations r
SET product_id = p.id
FROM products p
WHERE r.title = p.title AND r.product_id IS NULL;

ALTER TABLE recomendations
    ADD CONSTRAINT fk_recomendations_product
    FOREIGN KEY (product_id) REFERENCES products(id);
