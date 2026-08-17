-- Schema do inventário de ativos serializados.
-- Gerado a partir do modelo EF Core (Inventory.Infrastructure) e mantido à mão para
-- quem prefere aplicar SQL em produção a rodar EnsureCreated/migrations.
--
-- Ordem de leitura: locations -> items -> item_ledger_entries -> inventory_counts.

CREATE TABLE IF NOT EXISTS locations (
    id          uuid                     NOT NULL,
    code        character varying(40)    NOT NULL,
    name        character varying(160)   NOT NULL,
    parent_id   uuid,
    is_active   boolean                  NOT NULL,
    created_at  timestamp with time zone NOT NULL,
    CONSTRAINT pk_locations PRIMARY KEY (id),
    -- Restrict, não Cascade: apagar um prédio não pode fazer sumir as salas dele.
    CONSTRAINT fk_locations_parent FOREIGN KEY (parent_id) REFERENCES locations (id) ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_locations_code ON locations (code);
CREATE INDEX IF NOT EXISTS ix_locations_parent_id ON locations (parent_id);


-- Um ativo físico único. location_id é a localização atual; quando status <> 0,
-- leia como "última localização conhecida".
-- status: 0 = Active, 1 = Missing, 2 = Retired
CREATE TABLE IF NOT EXISTS items (
    id              uuid                     NOT NULL,
    sku             character varying(64)    NOT NULL,
    name            character varying(200)   NOT NULL,
    description     text,
    location_id     uuid                     NOT NULL,
    status          integer                  NOT NULL,
    registered_at   timestamp with time zone NOT NULL,
    last_seen_at    timestamp with time zone NOT NULL,
    ledger_sequence integer                  NOT NULL,
    CONSTRAINT pk_items PRIMARY KEY (id),
    CONSTRAINT fk_items_location FOREIGN KEY (location_id) REFERENCES locations (id) ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_items_sku ON items (sku);
CREATE INDEX IF NOT EXISTS ix_items_location ON items (location_id);


-- O razão do item: append-only. Nada aqui recebe UPDATE ou DELETE em operação normal —
-- é esta tabela que responde "o que aconteceu com o item X" e que alimentará os embeddings.
-- kind:   0 = Registered, 1 = Moved, 2 = CountConfirmed, 3 = CountMissing, 4 = Renamed, 5 = Retired
-- reason: 0 = Registration, 1 = Transfer, 2 = CountReconciliation, 3 = Correction
CREATE TABLE IF NOT EXISTS item_ledger_entries (
    id                  uuid                     NOT NULL,
    item_id             uuid                     NOT NULL,
    sequence            integer                  NOT NULL,
    kind                integer                  NOT NULL,
    occurred_at         timestamp with time zone NOT NULL,
    actor               character varying(160)   NOT NULL,
    from_location_id    uuid,
    to_location_id      uuid,
    reason              integer,
    inventory_count_id  uuid,
    note                text,
    CONSTRAINT pk_item_ledger_entries PRIMARY KEY (id),
    CONSTRAINT fk_ledger_item FOREIGN KEY (item_id) REFERENCES items (id) ON DELETE CASCADE
);

-- Impede, no banco, duas linhas na mesma posição da linha do tempo de um item.
CREATE UNIQUE INDEX IF NOT EXISTS ux_ledger_item_sequence ON item_ledger_entries (item_id, sequence);
CREATE INDEX IF NOT EXISTS ix_ledger_count ON item_ledger_entries (inventory_count_id);


-- A rodada de auditoria.
-- status: 0 = Open, 1 = Closed, 2 = Cancelled
CREATE TABLE IF NOT EXISTS inventory_counts (
    id                  uuid                     NOT NULL,
    location_id         uuid                     NOT NULL,
    auditor             character varying(120)   NOT NULL,
    status              integer                  NOT NULL,
    opened_at           timestamp with time zone NOT NULL,
    closed_at           timestamp with time zone,
    cancellation_reason text,
    CONSTRAINT pk_inventory_counts PRIMARY KEY (id),
    CONSTRAINT fk_inventory_counts_location FOREIGN KEY (location_id) REFERENCES locations (id) ON DELETE RESTRICT
);

-- Índice parcial: no máximo UMA contagem aberta por localização. Isso fecha a corrida
-- entre duas requisições simultâneas de abertura, que a checagem na aplicação sozinha não pega.
CREATE UNIQUE INDEX IF NOT EXISTS ux_inventory_counts_open_per_location
    ON inventory_counts (location_id) WHERE status = 0;


-- Fotografia congelada na abertura: o que o sistema acreditava estar na localização.
CREATE TABLE IF NOT EXISTS inventory_count_expected_items (
    inventory_count_id uuid NOT NULL,
    item_id            uuid NOT NULL,
    CONSTRAINT pk_inventory_count_expected_items PRIMARY KEY (inventory_count_id, item_id),
    CONSTRAINT fk_count_expected_count FOREIGN KEY (inventory_count_id)
        REFERENCES inventory_counts (id) ON DELETE CASCADE
);


-- O que o auditor efetivamente leu. A PK composta é a razão de bipar duas vezes ser inofensivo.
CREATE TABLE IF NOT EXISTS inventory_count_scans (
    inventory_count_id uuid                     NOT NULL,
    item_id            uuid                     NOT NULL,
    scanned_at         timestamp with time zone NOT NULL,
    CONSTRAINT pk_inventory_count_scans PRIMARY KEY (inventory_count_id, item_id),
    CONSTRAINT fk_count_scans_count FOREIGN KEY (inventory_count_id)
        REFERENCES inventory_counts (id) ON DELETE CASCADE
);


-- O índice semântico (tabela item_narratives, extensão pgvector) fica em
-- schema_embeddings.sql e só é necessário com Embeddings:Enabled = true.
-- O inventário inteiro — cadastro, movimentação, auditoria, histórico — funciona sem ele.
