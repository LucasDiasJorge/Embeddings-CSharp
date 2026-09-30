# Ideias de Projetos com Embeddings

Lista de ideias organizadas por nível de complexidade. Todas podem ser feitas em C# com Ollama + PostgreSQL/pgvector (a stack deste repositório).

## Iniciante

1. **Busca semântica de produtos** – Encontrar produtos por significado ("algo para carregar o celular") e não só por palavra-chave.
2. **FAQ inteligente** – Dada uma pergunta do usuário, retorna a resposta mais parecida de uma base de FAQs.
3. **Detector de duplicatas** – Identificar cadastros, tickets ou notícias quase idênticos comparando similaridade de cosseno.
4. **Recomendador de itens similares** – "Quem viu este produto também gostou": vizinhos mais próximos no espaço vetorial.
5. **Classificador de texto por exemplos** – Classificar mensagens (spam, suporte, vendas) comparando com exemplos rotulados (few-shot com kNN).
6. **Busca em notas pessoais** – Indexar arquivos .md/.txt e pesquisar por assunto.

## Intermediário

7. **RAG (Retrieval-Augmented Generation)** – Chatbot que busca trechos relevantes de documentos (PDFs, manuais, wikis) e os envia como contexto a um LLM.
8. **Chat com a base de código** – Indexar repositórios por função/classe e responder "onde fica a lógica de X?".
9. **Triagem automática de tickets** – Roteamento para a equipe correta e sugestão de tickets/soluções anteriores parecidos.
10. **Busca de currículos x vagas** – Ranquear candidatos pela similaridade entre CV e descrição da vaga.
11. **Agrupamento de tópicos (clustering)** – K-Means/HDBSCAN sobre embeddings de feedbacks ou reviews para descobrir temas.
12. **Análise de logs e erros** – Agrupar stack traces/mensagens de erro parecidos e detectar novos tipos de incidente.
13. **Busca híbrida** – Combinar full-text (BM25/`tsvector`) com vetorial e fazer *reciprocal rank fusion*.
14. **Recomendador de músicas/filmes/livros** – Embeddings de sinopses/letras para "mais como este".
15. **Tradução/busca multilíngue** – Consultar em português e achar documentos em inglês usando modelo multilíngue.

## Avançado

16. **Busca multimodal** – Embeddings de imagem + texto (CLIP) para buscar fotos por descrição ou por imagem parecida.
17. **Memória de longo prazo para agentes** – Armazenar conversas/fatos e recuperar os mais relevantes a cada turno.
18. **Detecção de anomalias** – Identificar transações, textos ou eventos distantes do "normal" no espaço vetorial.
19. **Cache semântico para LLM** – Reutilizar respostas de perguntas semanticamente equivalentes para reduzir custo e latência.
20. **Reranking em duas etapas** – Recuperação rápida por embeddings + reranker (cross-encoder) para maior precisão.
21. **Detecção de plágio / conteúdo gerado** – Comparar trechos com um corpus em nível de sentença/parágrafo.
22. **Roteador de intenção para agentes** – Escolher a ferramenta/agente certo comparando a pergunta com descrições de ferramentas.
23. **Busca em áudio/vídeo** – Transcrever (Whisper), fatiar e indexar; buscar o momento exato de um assunto.
24. **Grafo de conhecimento assistido** – Ligar entidades/documentos por similaridade e navegar visualmente (UMAP/t-SNE).
25. **Fine-tuning de embeddings** – Ajustar um modelo ao seu domínio (jurídico, médico, etc.) e medir ganho em recall@k.

## Ideias de Extensão para Este Repositório

- Endpoint de busca por similaridade sobre `Recomendation`/produtos com filtro por categoria/preço.
- Índice HNSW/IVFFlat no pgvector e benchmark de latência vs. busca exata.
- Comparar modelos de embedding (dimensões, qualidade, velocidade) com um conjunto de avaliação.
- Pipeline de ingestão em lote com `BackgroundService` e reprocessamento quando o modelo mudar.
- Visualização 2D dos vetores (UMAP) em uma página simples.
- Testes de relevância (recall@k, MRR) automatizados no projeto de testes.

## Dicas Gerais

- **Chunking**: divida textos longos (200–500 tokens, com sobreposição) antes de gerar embeddings.
- **Normalização**: normalize vetores se usar produto interno; escolha a métrica (cosseno/L2/IP) conforme o modelo.
- **Metadados**: guarde fonte, data e categoria para filtrar antes/depois da busca vetorial.
- **Avaliação**: crie um pequeno conjunto de perguntas com respostas esperadas antes de otimizar.
- **Versionamento**: registre qual modelo gerou cada vetor; vetores de modelos diferentes não são comparáveis.
