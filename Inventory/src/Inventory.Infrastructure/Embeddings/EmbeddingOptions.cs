namespace Inventory.Infrastructure.Embeddings;

public sealed class EmbeddingOptions
{
    public const string SectionName = "Embeddings";

    /// <summary>Desligado, a API roda inteira sem Ollama e sem pgvector.</summary>
    public bool Enabled { get; set; }

    public string Endpoint { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Modelo de embedding do Ollama. O padrão é forte e multilíngue — importa,
    /// porque as narrativas do razão são escritas em português.
    /// </summary>
    public string Model { get; set; } = "qwen3-embedding:4b";

    /// <summary>
    /// Dimensão do vetor, que precisa bater com o modelo:
    /// qwen3-embedding:4b = 2560, qwen3-embedding:0.6b = 1024, nomic-embed-text = 768.
    /// Errar aqui é erro de configuração, e o gerador reclama na primeira chamada
    /// em vez de gravar lixo no banco.
    /// </summary>
    public int Dimensions { get; set; } = 2560;

    /// <summary>
    /// Pede ao modelo para devolver o vetor já reduzido a <see cref="Dimensions"/>.
    /// </summary>
    /// <remarks>
    /// Só funciona em modelos treinados com Matryoshka (a família Qwen3-Embedding é),
    /// e exige um Ollama recente, que aceite o parâmetro <c>dimensions</c>.
    /// <para>
    /// Vale muito a pena ligar: os índices HNSW/IVFFlat do pgvector só aceitam até
    /// <b>2000 dimensões</b>. Nos 2560 nativos do qwen3-embedding:4b, toda busca vira
    /// scan sequencial da tabela inteira — aceitável em um acervo pequeno, inviável
    /// com centenas de milhares de linhas de razão. Reduzir para 1024 mantém quase
    /// toda a qualidade e destrava o índice.
    /// </para>
    /// </remarks>
    public bool TruncateDimensions { get; set; }

    /// <summary>
    /// Instrução colada apenas na PERGUNTA, nunca nos documentos.
    /// </summary>
    /// <remarks>
    /// Modelos da família Qwen3-Embedding são treinados com o formato
    /// <c>Instruct: {tarefa}\nQuery: {pergunta}</c> do lado da consulta, e texto cru do
    /// lado do documento. Descrever a tarefa em português, com o vocabulário do inventário,
    /// mede-se em pontos de recall — não é enfeite.
    /// </remarks>
    public string QueryInstruction { get; set; } =
        "Dado um histórico de movimentações, auditorias e baixas de itens de inventário, " +
        "recupere os eventos que respondem à pergunta.";

    /// <summary>Quantas narrativas vão por chamada ao Ollama durante a indexação.</summary>
    public int BatchSize { get; set; } = 16;

    /// <summary>
    /// Onde fica a tabela de vetores. Vazio usa a mesma connection string do inventário;
    /// preencha para apontar o índice semântico a outro banco.
    /// </summary>
    public string? ConnectionString { get; set; }
}
