# Ideias de Projetos com Embeddings

Lista de ideias organizadas por nível de complexidade. Todas podem ser feitas em C# com Ollama + PostgreSQL/pgvector (a stack deste repositório).  
Cada ideia abaixo foi enriquecida com subtópicos para facilitar planejamento de MVP, dados, evolução e valor entregue.

## Iniciante

1. **Busca semântica de produtos** – Encontrar produtos por significado ("algo para carregar o celular") e não só por palavra-chave.
   - Catálogo e atributos mínimos: nome, descrição curta, categoria e faixa de preço.
   - Consulta em linguagem natural com top-k resultados e filtro opcional por categoria.
   - Métricas iniciais: taxa de clique nos resultados e precisão percebida nos 5 primeiros itens.
   - Estratégia de melhoria contínua: registrar buscas sem resultado e alimentar backlog de catálogo.
   - **Valor entregue:** melhora a descoberta de produtos, aumenta conversão e reduz frustração em buscas vagas.

2. **FAQ inteligente** – Dada uma pergunta do usuário, retorna a resposta mais parecida de uma base de FAQs.
   - Curadoria da base de perguntas equivalentes (sinônimos e variações comuns).
   - Política de confiança: definir score mínimo para responder automaticamente.
   - Fallback para atendimento humano quando a similaridade for baixa.
   - Telemetria de lacunas: mapear perguntas recorrentes sem resposta adequada para atualizar o FAQ.
   - **Valor entregue:** reduz volume de chamados repetitivos e acelera autoatendimento com consistência.

3. **Detector de duplicatas** – Identificar cadastros, tickets ou notícias quase idênticos comparando similaridade de cosseno.
   - Definição de limiares: "possível duplicata" vs "duplicata forte".
   - Estratégia de revisão: fila de itens suspeitos para validação manual.
   - Prevenção de ruído: normalização de texto e remoção de boilerplate.
   - Regra operacional: bloquear criação automática apenas acima do limiar alto e sugerir merge assistido.
   - **Valor entregue:** diminui retrabalho, evita dados redundantes e melhora a qualidade da base.

4. **Recomendador de itens similares** – "Quem viu este produto também gostou": vizinhos mais próximos no espaço vetorial.
   - Construção de vitrine "similares" por item com atualização periódica.
   - Regras de diversidade para não recomendar apenas itens da mesma marca.
   - Medidas de qualidade: CTR da seção e taxa de adição ao carrinho.
   - Regras de negócio: excluir indisponíveis e priorizar itens com margem/estoque saudável.
   - **Valor entregue:** aumenta ticket médio e tempo de navegação útil no catálogo.

5. **Classificador de texto por exemplos** – Classificar mensagens (spam, suporte, vendas) comparando com exemplos rotulados (few-shot com kNN).
   - Curadoria de exemplos por classe com equilíbrio entre categorias.
   - Estratégia para classe "outros" quando nenhuma similaridade for suficiente.
   - Monitoramento de confusão entre classes e expansão contínua do conjunto de exemplos.
   - Operação assistida: exibir classes top-3 com confiança para revisão rápida por analistas.
   - **Valor entregue:** acelera triagem de mensagens e melhora SLA de atendimento.

6. **Busca em notas pessoais** – Indexar arquivos .md/.txt e pesquisar por assunto.
   - Pipeline de ingestão local (pastas monitoradas + atualização incremental).
   - Exibição de trechos com contexto para facilitar navegação da nota original.
   - Organização por tags/metadados (projeto, data, tema) para filtros rápidos.
   - UX de produtividade: atalho para abrir o arquivo exatamente no trecho retornado.
   - **Valor entregue:** reduz tempo de recuperação de conhecimento e evita retrabalho individual.

## Intermediário

7. **RAG (Retrieval-Augmented Generation)** – Chatbot que busca trechos relevantes de documentos (PDFs, manuais, wikis) e os envia como contexto a um LLM.
   - Pipeline de chunking + embeddings + armazenamento vetorial por fonte documental.
   - Estratégia de prompt com citações da origem e resposta "não sei" quando faltar contexto.
   - Avaliação com perguntas de referência e comparação entre top-k diferentes.
   - Governança: versionar base documental e invalidar respostas quando houver atualização crítica.
   - **Valor entregue:** respostas mais confiáveis e auditáveis, reduzindo dependência de especialistas.

8. **Chat com a base de código** – Indexar repositórios por função/classe e responder "onde fica a lógica de X?".
   - Segmentação por símbolos (classe, método, arquivo) com metadados de linguagem.
   - Busca com enriquecimento por caminho de arquivo e comentários/documentação.
   - Casos de uso: onboarding de devs, troubleshooting e descoberta de responsabilidades.
   - Segurança e escopo: aplicar controle por repositório/time para evitar exposição indevida.
   - **Valor entregue:** acelera onboarding técnico e reduz tempo de investigação de mudanças.

9. **Triagem automática de tickets** – Roteamento para a equipe correta e sugestão de tickets/soluções anteriores parecidos.
   - Classificação por time/produto com base em histórico resolvido.
   - Sugestão de artigos/runbooks junto com tickets similares.
   - Feedback loop: aceitar/corrigir roteamento para retreinar regras.
   - Modo híbrido: autoencaminhar apenas alta confiança e manter revisão humana nos demais casos.
   - **Valor entregue:** diminui tempo de primeira resposta e evita encaminhamentos incorretos.

10. **Busca de currículos x vagas** – Ranquear candidatos pela similaridade entre CV e descrição da vaga.
    - Extração de competências (hard/soft skills, senioridade, domínio).
    - Regras de elegibilidade antes da busca semântica (idioma, localização, faixa salarial).
    - Transparência no score: explicar quais trechos sustentam o ranqueamento.
    - Mitigação de viés: remover atributos sensíveis e revisar critérios de ranqueamento periodicamente.
    - **Valor entregue:** encurta o funil de seleção e aumenta aderência técnica dos candidatos pré-selecionados.

11. **Agrupamento de tópicos (clustering)** – K-Means/HDBSCAN sobre embeddings de feedbacks ou reviews para descobrir temas.
    - Escolha de algoritmo por formato de dado (clusters fixos vs densidade).
    - Rotulagem automática/semi-automática dos clusters para leitura executiva.
    - Acompanhamento temporal dos tópicos emergentes por semana/mês.
    - Fechamento de ciclo: vincular cada cluster a ação de produto/atendimento e dono responsável.
    - **Valor entregue:** transforma feedback disperso em agenda clara de melhorias.

12. **Análise de logs e erros** – Agrupar stack traces/mensagens de erro parecidos e detectar novos tipos de incidente.
    - Normalização de logs (remoção de IDs dinâmicos, timestamps e ruído).
    - Clusterização de incidentes para reduzir duplicidade em alertas.
    - Detecção de novidade: alertar quando surgir padrão sem cluster conhecido.
    - Integração operacional: abrir incidente automaticamente com contexto quando padrão crítico aparecer.
    - **Valor entregue:** reduz MTTR e fadiga de alertas em operações.

13. **Busca híbrida** – Combinar full-text (BM25/`tsvector`) com vetorial e fazer *reciprocal rank fusion*.
    - Estratégia de pesos entre lexical e semântico por tipo de consulta.
    - Experimentos A/B para definir fusão e ordenação final.
    - Observabilidade: latência por etapa (textual, vetorial, rerank).
    - Política de fallback: degradar para full-text quando serviço vetorial estiver indisponível.
    - **Valor entregue:** melhora precisão sem perder robustez operacional.

14. **Recomendador de músicas/filmes/livros** – Embeddings de sinopses/letras para "mais como este".
    - Modelagem por conteúdo (sinopse, gênero, tags, elenco/artista).
    - Regras de personalização leve (histórico recente e exclusões do usuário).
    - Métricas de descoberta: diversidade e novidade das recomendações.
    - Curva de exploração: combinar itens familiares e novos para evitar bolha de conteúdo.
    - **Valor entregue:** aumenta retenção e satisfação por descoberta personalizada.

15. **Tradução/busca multilíngue** – Consultar em português e achar documentos em inglês usando modelo multilíngue.
    - Escolha de modelo multilíngue e testes com pares PT-EN-ES.
    - Avaliação de perda semântica em termos técnicos e siglas.
    - UX bilíngue: mostrar trecho original e resumo no idioma do usuário.
    - Monitoramento por idioma: acompanhar precisão por língua e ajustar corpora de apoio.
    - **Valor entregue:** democratiza acesso ao conhecimento global sem barreira linguística.

## Avançado

16. **Busca multimodal** – Embeddings de imagem + texto (CLIP) para buscar fotos por descrição ou por imagem parecida.
    - Pipeline de ingestão de imagens (thumbnail, OCR opcional, metadados EXIF).
    - Busca texto→imagem e imagem→imagem com interface unificada.
    - Regras de privacidade e moderação para conteúdo sensível.
    - Otimização de custo: cache de embeddings e reprocessamento apenas de ativos alterados.
    - **Valor entregue:** acelera workflows visuais (catálogo, mídia, suporte) com recuperação muito mais natural.

17. **Memória de longo prazo para agentes** – Armazenar conversas/fatos e recuperar os mais relevantes a cada turno.
    - Estrutura de memória episódica (eventos) e semântica (fatos duradouros).
    - Estratégia de esquecimento/atualização para evitar memória obsoleta.
    - Auditoria: rastrear quais memórias foram usadas em cada resposta.
    - Política de consentimento e retenção de dados por perfil de usuário.
    - **Valor entregue:** interações mais personalizadas e menos repetitivas ao longo do tempo.

18. **Detecção de anomalias** – Identificar transações, textos ou eventos distantes do "normal" no espaço vetorial.
    - Definição de baseline por segmento (cliente, região, tipo de evento).
    - Combinação de distância vetorial com regras de negócio para reduzir falso positivo.
    - Playbook de resposta: classificação de severidade e escalonamento automático.
    - Calibração contínua: revisão semanal de falsos positivos/negativos para ajuste de thresholds.
    - **Valor entregue:** prevenção proativa de fraude, falhas e incidentes críticos.

19. **Cache semântico para LLM** – Reutilizar respostas de perguntas semanticamente equivalentes para reduzir custo e latência.
    - Chave semântica baseada em embedding + limiar de equivalência.
    - Estratégia de invalidação por versão de prompt/modelo/base de conhecimento.
    - Métricas de impacto: hit-rate, economia de tokens e tempo médio de resposta.
    - Controle de risco: bypass automático para perguntas sensíveis ou regulatórias.
    - **Valor entregue:** queda de custo operacional e respostas mais rápidas em alta escala.

20. **Reranking em duas etapas** – Recuperação rápida por embeddings + reranker (cross-encoder) para maior precisão.
    - Fase 1: recall alto (top-50/top-100) com ANN no pgvector.
    - Fase 2: reordenação fina com cross-encoder em subconjunto reduzido.
    - Trade-off latência x qualidade com perfilamento por carga.
    - Perfil por segmento: aplicar rerank apenas em consultas onde o ganho de qualidade compensa.
    - **Valor entregue:** melhor relevância percebida pelo usuário final sem explodir custo.

21. **Detecção de plágio / conteúdo gerado** – Comparar trechos com um corpus em nível de sentença/parágrafo.
    - Segmentação por sentenças e janelas deslizantes para comparação robusta.
    - Evidências de similaridade com links e destaques de trechos alinhados.
    - Regras de decisão por contexto (citação legítima vs cópia indevida).
    - Trilhas de auditoria: guardar evidências e scores para suporte a revisão humana.
    - **Valor entregue:** reforça integridade acadêmica/editorial com análise escalável.

22. **Roteador de intenção para agentes** – Escolher a ferramenta/agente certo comparando a pergunta com descrições de ferramentas.
    - Catálogo de ferramentas com embeddings de descrição e exemplos de uso.
    - Política de roteamento com fallback e desambiguação quando houver empate.
    - Telemetria de acerto de roteamento e custo por tipo de tarefa.
    - Guarda de segurança: bloquear roteamento para ferramentas sensíveis sem confirmação contextual.
    - **Valor entregue:** aumenta taxa de resolução no primeiro passo e reduz chamadas erradas.

23. **Busca em áudio/vídeo** – Transcrever (Whisper), fatiar e indexar; buscar o momento exato de um assunto.
    - Pipeline mídia→transcrição→chunks temporais com timestamps.
    - Busca semântica com retorno do minuto/segundo exato e snippet transcrito.
    - Pós-processamento: diarização, remoção de ruído e capítulos automáticos.
    - Navegação prática: deep link para o timestamp no player e exportação de highlights.
    - **Valor entregue:** economiza horas de revisão manual de reuniões, aulas e entrevistas.

24. **Grafo de conhecimento assistido** – Ligar entidades/documentos por similaridade e navegar visualmente (UMAP/t-SNE).
    - Extração de entidades e relações candidatas por proximidade vetorial.
    - Navegação exploratória com filtros por tipo de entidade e confiança.
    - Casos de uso: investigação, descoberta de lacunas e análise de impacto.
    - Curadoria incremental: validação humana das relações críticas para elevar confiança do grafo.
    - **Valor entregue:** acelera análise estratégica e descoberta de conexões não óbvias.

25. **Fine-tuning de embeddings** – Ajustar um modelo ao seu domínio (jurídico, médico, etc.) e medir ganho em recall@k.
    - Construção de dataset de pares positivos/negativos com revisão de especialistas.
    - Treino contrastivo e validação offline (recall@k, nDCG, MRR).
    - Estratégia de rollout gradual com comparação online contra modelo base.
    - Critério de adoção: só promover versão nova se houver ganho estatisticamente consistente.
    - **Valor entregue:** melhora significativa de relevância em domínio específico, criando vantagem competitiva.

## Ideias de Extensão para Este Repositório

- Endpoint de busca por similaridade sobre `Recomendation`/produtos com filtro por categoria/preço.
  - Definir contrato de API (filtros, paginação, score mínimo).
  - Incluir payload com item, score e justificativa textual curta.
  - **Valor entregue:** viabiliza feature pronta para consumo por frontend e integrações externas.

- Índice HNSW/IVFFlat no pgvector e benchmark de latência vs. busca exata.
  - Criar cenários de benchmark por volume (10k, 100k, 1M vetores).
  - Comparar recall e latência P95 para cada configuração de índice.
  - **Valor entregue:** decisão técnica orientada por dados para escalar com previsibilidade.

- Comparar modelos de embedding (dimensões, qualidade, velocidade) com um conjunto de avaliação.
  - Montar suíte de consultas com gabarito e critérios objetivos.
  - Publicar relatório comparativo por custo, qualidade e throughput.
  - **Valor entregue:** escolha de modelo com melhor custo-benefício para o contexto real.

- Pipeline de ingestão em lote com `BackgroundService` e reprocessamento quando o modelo mudar.
  - Implementar fila idempotente com checkpoint de progresso.
  - Adicionar versionamento de embeddings por modelo e data de geração.
  - **Valor entregue:** operação estável de dados em produção com menor risco de inconsistência.

- Visualização 2D dos vetores (UMAP) em uma página simples.
  - Exportar projeções com rótulos e metadados para inspeção visual.
  - Permitir filtro interativo por categoria e busca textual.
  - **Valor entregue:** facilita interpretação de qualidade semântica por perfis não técnicos.

- Testes de relevância (recall@k, MRR) automatizados no projeto de testes.
  - Criar dataset mínimo reprodutível para CI.
  - Definir limiares de qualidade para evitar regressões semânticas.
  - **Valor entregue:** protege a qualidade de busca ao longo da evolução do código e dos modelos.

## Dicas Gerais

- **Chunking**: divida textos longos (200–500 tokens, com sobreposição) antes de gerar embeddings.
- **Normalização**: normalize vetores se usar produto interno; escolha a métrica (cosseno/L2/IP) conforme o modelo.
- **Metadados**: guarde fonte, data e categoria para filtrar antes/depois da busca vetorial.
- **Avaliação**: crie um pequeno conjunto de perguntas com respostas esperadas antes de otimizar.
- **Versionamento**: registre qual modelo gerou cada vetor; vetores de modelos diferentes não são comparáveis.
