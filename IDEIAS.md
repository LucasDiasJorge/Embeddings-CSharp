# Ideias de Projetos com Embeddings

Lista de ideias organizadas por nível de complexidade. Todas podem ser feitas em C# com Ollama + PostgreSQL/pgvector (a stack deste repositório).  
Cada ideia abaixo foi enriquecida com subtópicos para facilitar planejamento de MVP, dados, evolução e valor entregue.

## Como usar esta lista

Uma ideia de embeddings só se transforma em um bom projeto quando conecta **problema real,
dados confiáveis, busca mensurável e uma ação útil para alguém**. O objetivo desta lista é
ajudar a sair de uma demonstração que apenas calcula vetores e chegar a uma solução que possa
ser apresentada, testada e evoluída.

Antes de implementar qualquer ideia, registre:

| Decisão | Pergunta que precisa ser respondida | Evidência esperada |
|---|---|---|
| Problema | Que tarefa é lenta, cara ou difícil hoje? | Exemplo real do problema |
| Usuário | Quem toma uma decisão usando o resultado? | Persona ou equipe responsável |
| Entrada | Qual texto, imagem, áudio ou evento será indexado? | Dataset inicial documentado |
| Saída | O sistema retorna itens, resposta, classificação ou alerta? | Contrato de API ou wireframe |
| Ação | O que o usuário fará depois de receber o resultado? | Fluxo de uso completo |
| Qualidade | Como saberemos que o resultado é bom? | Dataset de avaliação e métricas |
| Valor | Qual custo, risco ou tempo será reduzido? | Indicador antes/depois |
| Limites | Quando o sistema deve recusar, pedir revisão ou não responder? | Fallback explícito |

## Arquitetura de referência

As ideias podem usar uma arquitetura simples no começo e evoluir sem trocar o núcleo:

1. **Ingestão**: recebe produtos, documentos, tickets, eventos, arquivos ou mídias.
2. **Normalização**: remove ruído, preserva metadados e gera um texto canônico.
3. **Segmentação**: divide conteúdo longo em unidades recuperáveis, mantendo origem e posição.
4. **Embedding**: gera vetores por meio de um contrato (`IEmbeddingGenerator` ou equivalente),
   permitindo trocar Ollama/modelo sem espalhar detalhes do provedor pela aplicação.
5. **Persistência**: guarda o dado original e uma projeção vetorial no PostgreSQL/pgvector.
6. **Recuperação**: aplica filtros estruturados antes/depois da busca e recupera o top-k.
7. **Ordenação**: usa similaridade, regras de negócio e, quando necessário, reranking.
8. **Entrega**: expõe API, tela, integração com agente ou processo operacional.
9. **Avaliação e observabilidade**: registra consulta, latência, resultados, feedback e falhas.

No projeto mais simples, esse fluxo pode ficar em uma API pequena como `MyPgVectorStore`.
Em uma solução maior, mantenha o domínio independente e coloque Ollama, pgvector e EF Core
em adaptadores de infraestrutura, como já ocorre no projeto `Inventory`. Isso permite rodar
casos de uso com fakes, desligar embeddings sem quebrar o domínio e reindexar a projeção sem
perder o dado original.

## Contrato mínimo de um registro vetorial

Cada item indexado deve ter, além do vetor:

- `source_id` e `source_type`: identificam o registro e sua origem.
- `text` ou `content_hash`: permitem reproduzir e detectar mudanças no conteúdo.
- `metadata`: categoria, autor, data, permissões, idioma e demais filtros.
- `model` e `model_version`: informam qual modelo gerou o vetor.
- `dimensions` e `distance_metric`: evitam comparar vetores incompatíveis.
- `created_at` e `updated_at`: sustentam auditoria e reprocessamento.
- `status`: distingue pendente, processado, falho e desatualizado.

O processo de indexação deve ser **idempotente**: repetir a mesma mensagem não pode criar
duplicatas. Se o modelo mudar, crie uma nova versão da projeção, compare os resultados e só
promova o novo índice depois da avaliação.

## Requisitos transversais de produção

Estes requisitos podem ser ajustados ao caso, mas devem ser discutidos desde o MVP:

- **Qualidade**: manter conjunto dourado de consultas e comparar com uma busca lexical baseline.
- **Desempenho**: medir p50/p95/p99 separando geração do embedding, consulta ao banco e reranking.
- **Confiabilidade**: usar timeout, retry com limite, backoff e fallback explícito quando Ollama ou
  PostgreSQL estiverem indisponíveis.
- **Consistência**: persistir o dado de negócio antes da projeção vetorial e permitir reindexação.
- **Segurança**: aplicar autorização por documento antes de devolver um resultado, nunca depois.
- **Privacidade**: remover ou mascarar PII, definir retenção e evitar enviar dados sensíveis ao modelo.
- **Observabilidade**: registrar `query_id`, modelo, dimensão, filtros, top-k, score e decisão final.
- **Custo**: acompanhar tokens, chamadas ao modelo, tamanho do índice e custo por consulta.
- **Explicabilidade**: devolver evidências, trechos ou atributos que justificam o resultado.
- **Operação**: disponibilizar health check, reindexação controlada e métricas de backlog.

## Caminho do MVP à produção

1. **Descoberta**: escreva o problema, o usuário e o valor esperado em uma frase.
2. **Dataset**: reúna exemplos reais, remova dados sensíveis e defina casos positivos e negativos.
3. **Baseline**: implemente uma busca lexical ou regra simples para ter comparação honesta.
4. **MVP semântico**: faça ingestão, embedding, busca top-k e retorno com metadados.
5. **Avaliação**: meça recall@k, precision@k, MRR/nDCG ou a métrica específica do fluxo.
6. **Produto**: adicione filtros, feedback, fallback, autenticação, documentação e uma interface útil.
7. **Operação**: monitore latência, erros, custo, drift e qualidade ao longo do tempo.
8. **Escala**: só então escolha HNSW/IVFFlat, batching, cache, reranking ou fine-tuning.

## Definition of Done para qualquer ideia

Considere uma ideia pronta para apresentação quando ela tiver:

- problema e público-alvo descritos;
- dataset de demonstração reproduzível;
- fluxo ponta a ponta funcionando;
- API ou interface com exemplos de uso;
- baseline e métrica de qualidade;
- tratamento de baixa confiança e indisponibilidade;
- logs e métricas mínimas;
- testes para o caminho feliz e casos de borda;
- instruções de execução local;
- seção documentando limitações e próximos passos.

## Iniciante

### Objetivo do nível

O nível iniciante serve para entender o ciclo completo de embeddings sem esconder a lógica
atrás de uma plataforma pronta. O foco é construir uma solução pequena, explicável e
reproduzível: dado um conteúdo, gerar o vetor, armazená-lo, consultar vizinhos e devolver
um resultado que alguém consiga usar.

### Perfil e pré-requisitos

- Conhecimento básico de C#, ASP.NET Core, HTTP, SQL e manipulação de JSON.
- Noções de vetores, similaridade de cosseno, distância e diferença entre busca lexical e semântica.
- Capacidade de executar .NET, Ollama e PostgreSQL localmente.
- Não é necessário saber treinar modelos ou implementar machine learning do zero.

### Escopo técnico recomendado

- Um único tipo de dado, como produto, FAQ, mensagem ou nota.
- Ingestão síncrona ou por comando simples, com dataset pequeno e controlado.
- Um modelo de embedding, uma dimensão e uma métrica documentadas.
- Busca exata ou índice pequeno, sem otimizações prematuras.
- Filtros simples, score mínimo e resposta explícita para baixa confiança.
- Interface mínima: endpoint, arquivo `.http`, CLI ou página simples.

### Entregáveis do bloco

1. Dataset de exemplo com pelo menos 20 registros e consultas representativas.
2. Script de criação/seed e comando de indexação que possa ser repetido.
3. Endpoint de busca com `query`, `top`, filtros e score.
4. Exibição da origem e de um trecho/atributo que explique cada resultado.
5. Testes de normalização, geração/armazenamento e ordenação dos resultados.
6. README com pré-requisitos, configuração, exemplos e limitações conhecidas.

### Métricas e critérios de qualidade

- Avaliar manualmente as primeiras posições para um conjunto de consultas positivas e negativas.
- Medir precision@k ou taxa de resultados relevantes no top-5.
- Registrar latência da geração do embedding e da consulta ao banco separadamente.
- Testar consultas com sinônimos, erros de digitação, termos exatos e nenhuma correspondência.
- Comparar pelo menos uma vez com `LIKE` ou `tsvector` para demonstrar o ganho real.

### Riscos e como controlar

- **Resultado plausível, mas errado:** usar score mínimo, evidência e revisão humana.
- **Dados pobres:** enriquecer o texto canônico com descrição e metadados úteis.
- **Modelo indisponível:** retornar erro explícito ou modo de consulta documentado, nunca lista vazia.
- **Vetor incompatível:** persistir modelo, dimensão e métrica junto ao registro.
- **Escopo crescendo demais:** limitar o MVP a uma fonte e um fluxo de usuário.

### Valor entregue pelo nível

- Cria uma prova de conceito funcional que pode ser demonstrada em poucos minutos.
- Forma uma base para portfólio com API, banco, modelo local e testes.
- Mostra de maneira concreta quando a busca semântica é melhor que palavras-chave.
- Entrega uma primeira automação de descoberta, triagem ou recomendação sem depender de treinamento.

### Critério para avançar ao intermediário

Avance quando conseguir explicar e demonstrar o caminho completo, trocar o modelo sem reescrever
o domínio, reindexar os dados de forma segura, tratar baixa confiança e provar a qualidade com
um pequeno conjunto de avaliação.

1. **Busca semântica de produtos** – Encontrar produtos por significado ("algo para carregar o celular") e não só por palavra-chave.
   - Catálogo e atributos mínimos: nome, descrição curta, categoria e faixa de preço.
   - Consulta em linguagem natural com top-k resultados e filtro opcional por categoria.
   - Métricas iniciais: taxa de clique nos resultados e precisão percebida nos 5 primeiros itens.
   - Estratégia de melhoria contínua: registrar buscas sem resultado e alimentar backlog de catálogo.
   - MVP sugerido: endpoint `POST /search` com `query`, `top`, filtros e score, acompanhado de um seed com consultas reais.
   - Critério de sucesso: superar a busca lexical em um conjunto de consultas ambíguas sem aumentar excessivamente a latência.
   - **Valor entregue:** melhora a descoberta de produtos, aumenta conversão e reduz frustração em buscas vagas.

2. **FAQ inteligente** – Dada uma pergunta do usuário, retorna a resposta mais parecida de uma base de FAQs.
   - Curadoria da base de perguntas equivalentes (sinônimos e variações comuns).
   - Política de confiança: definir score mínimo para responder automaticamente.
   - Fallback para atendimento humano quando a similaridade for baixa.
   - Telemetria de lacunas: mapear perguntas recorrentes sem resposta adequada para atualizar o FAQ.
   - MVP sugerido: resposta com texto, categoria, fonte, score e indicação de confiança.
   - Critério de sucesso: responder corretamente perguntas conhecidas e encaminhar com segurança as desconhecidas.
   - **Valor entregue:** reduz volume de chamados repetitivos e acelera autoatendimento com consistência.

3. **Detector de duplicatas** – Identificar cadastros, tickets ou notícias quase idênticos comparando similaridade de cosseno.
   - Definição de limiares: "possível duplicata" vs "duplicata forte".
   - Estratégia de revisão: fila de itens suspeitos para validação manual.
   - Prevenção de ruído: normalização de texto e remoção de boilerplate.
   - Regra operacional: bloquear criação automática apenas acima do limiar alto e sugerir merge assistido.
   - MVP sugerido: tela ou endpoint que compare um novo registro com candidatos e mostre evidências lado a lado.
   - Critério de sucesso: alto recall para duplicatas conhecidas sem bloquear registros legítimos.
   - **Valor entregue:** diminui retrabalho, evita dados redundantes e melhora a qualidade da base.

4. **Recomendador de itens similares** – "Quem viu este produto também gostou": vizinhos mais próximos no espaço vetorial.
   - Construção de vitrine "similares" por item com atualização periódica.
   - Regras de diversidade para não recomendar apenas itens da mesma marca.
   - Medidas de qualidade: CTR da seção e taxa de adição ao carrinho.
   - Regras de negócio: excluir indisponíveis e priorizar itens com margem/estoque saudável.
   - MVP sugerido: rota de similares com filtros de disponibilidade e exclusão do próprio item.
   - Critério de sucesso: recomendações semanticamente relacionadas, diversas e melhores que uma lista aleatória ou apenas por categoria.
   - **Valor entregue:** aumenta ticket médio e tempo de navegação útil no catálogo.

5. **Classificador de texto por exemplos** – Classificar mensagens (spam, suporte, vendas) comparando com exemplos rotulados (few-shot com kNN).
   - Curadoria de exemplos por classe com equilíbrio entre categorias.
   - Estratégia para classe "outros" quando nenhuma similaridade for suficiente.
   - Monitoramento de confusão entre classes e expansão contínua do conjunto de exemplos.
   - Operação assistida: exibir classes top-3 com confiança para revisão rápida por analistas.
   - MVP sugerido: endpoint que devolve classe principal, alternativas, score e exemplos mais próximos.
   - Critério de sucesso: manter precisão mínima por classe e nunca forçar classificação quando a confiança for insuficiente.
   - **Valor entregue:** acelera triagem de mensagens e melhora SLA de atendimento.

6. **Busca em notas pessoais** – Indexar arquivos .md/.txt e pesquisar por assunto.
   - Pipeline de ingestão local (pastas monitoradas + atualização incremental).
   - Exibição de trechos com contexto para facilitar navegação da nota original.
   - Organização por tags/metadados (projeto, data, tema) para filtros rápidos.
   - UX de produtividade: atalho para abrir o arquivo exatamente no trecho retornado.
   - MVP sugerido: comando de indexação incremental e interface que retorne arquivo, linha aproximada e trecho.
   - Critério de sucesso: localizar notas relevantes mesmo quando a consulta não compartilha palavras com o arquivo.
   - **Valor entregue:** reduz tempo de recuperação de conhecimento e evita retrabalho individual.

## Intermediário

### Objetivo do nível

O nível intermediário transforma a busca vetorial em uma capacidade de produto. O desafio deixa
de ser apenas recuperar itens e passa a ser integrar documentos, feedback, geração de resposta,
processos assíncronos e regras de negócio sem perder rastreabilidade.

### Perfil e pré-requisitos

- Domínio do ciclo iniciante e experiência com APIs reais, EF Core e PostgreSQL.
- Conhecimento de cancelamento, concorrência, validação, testes de integração e configuração.
- Noções de chunking, recuperação top-k, RAG, busca híbrida e avaliação de relevância.
- Capacidade de definir quem pode consultar cada fonte e como o sistema deve falhar.

### Escopo técnico recomendado

- Múltiplas fontes ou documentos com metadados, versões e permissões.
- Pipeline incremental e idempotente, preferencialmente com `BackgroundService` ou fila.
- Chunking preservando documento, posição, título, idioma e origem.
- Busca vetorial combinada com filtros estruturados e, quando necessário, full-text.
- Resposta com evidências, feedback do usuário, logs e métricas de qualidade.
- Contratos de API documentados e testes com banco real ou infraestrutura equivalente.

### Entregáveis do bloco

1. Pipeline de ingestão, atualização e remoção lógica de conteúdo.
2. Modelo de dados que separe fonte original, chunks e projeção de embeddings.
3. Busca com autorização, filtros, score mínimo, paginação e fallback.
4. Dataset dourado com perguntas, respostas esperadas e casos sem resposta.
5. Avaliação automatizada com recall@k, MRR, nDCG ou métrica adequada ao fluxo.
6. Processo de feedback que registre aceitação, correção e motivo da falha.
7. Observabilidade com `query_id`, latência por etapa, erros e backlog de indexação.

### Métricas e critérios de qualidade

- Comparar busca lexical, vetorial e híbrida no mesmo conjunto de consultas.
- Medir qualidade por categoria, idioma, fonte e nível de dificuldade.
- Acompanhar p50/p95/p99, taxa de erro, tempo de indexação e tamanho do backlog.
- Medir taxa de respostas com evidência, taxa de fallback e satisfação do usuário.
- Definir limiar de regressão no CI antes de trocar chunking ou modelo.

### Riscos e como controlar

- **Alucinação em RAG:** restringir resposta ao contexto, exigir citações e permitir "não sei".
- **Vazamento de informação:** filtrar autorização antes da recuperação e testar usuários distintos.
- **Conteúdo desatualizado:** versionar documentos e invalidar/reindexar mudanças.
- **Pipeline inconsistente:** usar idempotência, retry limitado, dead-letter e reconciliação.
- **Custo inesperado:** cache, batch, métricas de consumo e limites por usuário/fonte.
- **Drift de consultas:** acompanhar buscas sem resultado e revisar o dataset dourado periodicamente.

### Valor entregue pelo nível

- Converte a técnica em ferramenta de trabalho para suporte, engenharia, produto ou operações.
- Reduz tempo de busca de conhecimento e encaminhamento manual.
- Permite decisões baseadas em feedback e evidências, em vez de apenas percepção.
- Produz um projeto de portfólio mais próximo de uma aplicação que poderia operar em equipe.

### Critério para avançar ao avançado

Avance quando o sistema tiver avaliação automatizada, ingestão reprocessável, autorização,
observabilidade, controle de custo e uma estratégia clara para troca de modelo sem regressão
silenciosa ou perda do conteúdo original.

7. **RAG (Retrieval-Augmented Generation)** – Chatbot que busca trechos relevantes de documentos (PDFs, manuais, wikis) e os envia como contexto a um LLM.
   - Pipeline de chunking + embeddings + armazenamento vetorial por fonte documental.
   - Estratégia de prompt com citações da origem e resposta "não sei" quando faltar contexto.
   - Avaliação com perguntas de referência e comparação entre top-k diferentes.
   - Governança: versionar base documental e invalidar respostas quando houver atualização crítica.
   - MVP sugerido: upload de documentos, consulta, resposta com citações e botão de feedback correto/incorreto.
   - Critério de sucesso: toda afirmação factual importante deve ter evidência recuperada ou ser explicitamente marcada como desconhecida.
   - **Valor entregue:** respostas mais confiáveis e auditáveis, reduzindo dependência de especialistas.

8. **Chat com a base de código** – Indexar repositórios por função/classe e responder "onde fica a lógica de X?".
   - Segmentação por símbolos (classe, método, arquivo) com metadados de linguagem.
   - Busca com enriquecimento por caminho de arquivo e comentários/documentação.
   - Casos de uso: onboarding de devs, troubleshooting e descoberta de responsabilidades.
   - Segurança e escopo: aplicar controle por repositório/time para evitar exposição indevida.
   - MVP sugerido: indexação por símbolo com link para o arquivo/linha e resposta composta apenas por trechos recuperados.
   - Critério de sucesso: encontrar o símbolo responsável por uma tarefa em repositórios de teste e informar quando não houver evidência suficiente.
   - **Valor entregue:** acelera onboarding técnico e reduz tempo de investigação de mudanças.

9. **Triagem automática de tickets** – Roteamento para a equipe correta e sugestão de tickets/soluções anteriores parecidos.
   - Classificação por time/produto com base em histórico resolvido.
   - Sugestão de artigos/runbooks junto com tickets similares.
   - Feedback loop: aceitar/corrigir roteamento para retreinar regras.
   - Modo híbrido: autoencaminhar apenas alta confiança e manter revisão humana nos demais casos.
   - MVP sugerido: fila de tickets com destino recomendado, justificativa, tickets parecidos e ação de aceitar/corrigir.
   - Critério de sucesso: reduzir encaminhamentos manuais sem piorar o tempo de resolução ou a taxa de reabertura.
   - **Valor entregue:** diminui tempo de primeira resposta e evita encaminhamentos incorretos.

10. **Busca de currículos x vagas** – Ranquear candidatos pela similaridade entre CV e descrição da vaga.
    - Extração de competências (hard/soft skills, senioridade, domínio).
    - Regras de elegibilidade antes da busca semântica (idioma, localização, faixa salarial).
    - Transparência no score: explicar quais trechos sustentam o ranqueamento.
    - Mitigação de viés: remover atributos sensíveis e revisar critérios de ranqueamento periodicamente.
    - MVP sugerido: ranking com filtros eliminatórios, competências encontradas e trechos que sustentam cada correspondência.
    - Critério de sucesso: aumentar a taxa de candidatos aprovados na triagem sem transformar similaridade em decisão automática de contratação.
    - **Valor entregue:** encurta o funil de seleção e aumenta aderência técnica dos candidatos pré-selecionados.

11. **Agrupamento de tópicos (clustering)** – K-Means/HDBSCAN sobre embeddings de feedbacks ou reviews para descobrir temas.
    - Escolha de algoritmo por formato de dado (clusters fixos vs densidade).
    - Rotulagem automática/semi-automática dos clusters para leitura executiva.
    - Acompanhamento temporal dos tópicos emergentes por semana/mês.
    - Fechamento de ciclo: vincular cada cluster a ação de produto/atendimento e dono responsável.
    - MVP sugerido: painel com cluster, rótulo, volume, exemplos representativos, evolução e itens sem grupo.
    - Critério de sucesso: especialistas reconhecem os temas e conseguem tomar uma decisão usando o painel.
    - **Valor entregue:** transforma feedback disperso em agenda clara de melhorias.

12. **Análise de logs e erros** – Agrupar stack traces/mensagens de erro parecidos e detectar novos tipos de incidente.
    - Normalização de logs (remoção de IDs dinâmicos, timestamps e ruído).
    - Clusterização de incidentes para reduzir duplicidade em alertas.
    - Detecção de novidade: alertar quando surgir padrão sem cluster conhecido.
    - Integração operacional: abrir incidente automaticamente com contexto quando padrão crítico aparecer.
    - MVP sugerido: ingestão de logs, agrupamento por assinatura semântica, contagem temporal e alerta de novidade.
    - Critério de sucesso: reduzir alertas equivalentes e preservar os incidentes críticos em um replay controlado.
    - **Valor entregue:** reduz MTTR e fadiga de alertas em operações.

13. **Busca híbrida** – Combinar full-text (BM25/`tsvector`) com vetorial e fazer *reciprocal rank fusion*.
    - Estratégia de pesos entre lexical e semântico por tipo de consulta.
    - Experimentos A/B para definir fusão e ordenação final.
    - Observabilidade: latência por etapa (textual, vetorial, rerank).
    - Política de fallback: degradar para full-text quando serviço vetorial estiver indisponível.
    - MVP sugerido: duas buscas independentes, fusão configurável e endpoint que exponha score de cada etapa.
    - Critério de sucesso: melhorar recall e relevância em consultas com termos exatos e consultas conceituais.
    - **Valor entregue:** melhora precisão sem perder robustez operacional.

14. **Recomendador de músicas/filmes/livros** – Embeddings de sinopses/letras para "mais como este".
    - Modelagem por conteúdo (sinopse, gênero, tags, elenco/artista).
    - Regras de personalização leve (histórico recente e exclusões do usuário).
    - Métricas de descoberta: diversidade e novidade das recomendações.
    - Curva de exploração: combinar itens familiares e novos para evitar bolha de conteúdo.
    - MVP sugerido: página de detalhe com itens similares, filtros e explicação dos atributos compartilhados.
    - Critério de sucesso: equilibrar relevância, diversidade e novidade em um conjunto de usuários ou perfis simulados.
    - **Valor entregue:** aumenta retenção e satisfação por descoberta personalizada.

15. **Tradução/busca multilíngue** – Consultar em português e achar documentos em inglês usando modelo multilíngue.
    - Escolha de modelo multilíngue e testes com pares PT-EN-ES.
    - Avaliação de perda semântica em termos técnicos e siglas.
    - UX bilíngue: mostrar trecho original e resumo no idioma do usuário.
    - Monitoramento por idioma: acompanhar precisão por língua e ajustar corpora de apoio.
    - MVP sugerido: corpus paralelo pequeno, consultas cruzadas e resultado com idioma, fonte e trecho original.
    - Critério de sucesso: manter qualidade comparável entre idiomas e não perder entidades, números ou termos técnicos.
    - **Valor entregue:** democratiza acesso ao conhecimento global sem barreira linguística.

## Avançado

### Objetivo do nível

O nível avançado trata embeddings como uma capacidade crítica de produto ou plataforma. O foco é
resolver problemas que exigem escala, multimodalidade, memória, detecção, otimização de ranking,
adaptação de modelo ou decisões com impacto operacional.

### Perfil e pré-requisitos

- Experiência com os níveis anteriores, arquitetura de software e operação de serviços.
- Conhecimento de índices ANN, benchmark, estatística básica e experimentação controlada.
- Domínio de segurança, privacidade, governança de dados e revisão humana.
- Capacidade de analisar trade-offs entre qualidade, latência, custo, explicabilidade e risco.

### Escopo técnico recomendado

- Volume suficiente para justificar HNSW/IVFFlat, cache, batching, reranking ou processamento distribuído.
- Mais de uma modalidade ou fonte, com contratos estáveis e pipeline assíncrono.
- Registro de modelos, dimensões, prompts/instruções, datasets e versões de índice.
- Experimentos offline e online com baseline, grupo de controle e possibilidade de rollback.
- Human-in-the-loop para anomalias, plágio, decisões sensíveis e relações inferidas.
- SLOs de latência/disponibilidade, limites de consumo e plano de recuperação.

### Entregáveis do bloco

1. Arquitetura de referência com limites claros entre domínio, aplicação e adaptadores de modelo/banco.
2. Pipeline resiliente com filas, checkpoints, retries, dead-letter e reprocessamento seletivo.
3. Benchmark de escala com volume, concorrência, memória, custo e p95/p99 documentados.
4. Registry de modelo/índice e estratégia de migração ou reconstrução sem indisponibilidade indevida.
5. Suíte de avaliação estatisticamente defensável e comparação com baseline congelado.
6. Dashboard operacional com qualidade, drift, custo, backlog, erro e uso por cliente.
7. Threat model, política de retenção, auditoria e procedimento de intervenção/rollback.

### Métricas e critérios de qualidade

- Medir recall@k, MRR/nDCG ou métrica de negócio junto com latência e custo.
- Reportar intervalos de confiança ou repetição suficiente para não promover um ganho casual.
- Avaliar qualidade por segmento, idioma, modalidade, usuário e casos extremos.
- Medir taxa de falso positivo/negativo, calibração de confiança e quantidade de revisões humanas.
- Definir SLOs e orçamento de erro para consulta, ingestão e atualização do índice.

### Riscos e como controlar

- **Escala sem ganho real:** só adotar ANN/reranking após benchmark contra busca exata.
- **Modelo novo degradando um segmento:** rollout gradual, shadow traffic, A/B e rollback.
- **Cache desatualizado:** invalidar por modelo, prompt, contexto e versão da base.
- **Falso positivo de alto impacto:** exigir evidência, confirmação e revisão humana.
- **Prompt injection ou conteúdo malicioso:** separar instruções de dados recuperados e sanitizar fontes.
- **Privacidade e compliance:** minimizar dados, controlar acesso, auditar uso e testar deleção.
- **Dependência operacional do modelo:** manter fallback, health checks e modo degradado explícito.

### Valor entregue pelo nível

- Permite operar busca e inteligência semântica em maior volume e com previsibilidade.
- Reduz custos de infraestrutura ou melhora qualidade onde soluções genéricas falham.
- Cria diferenciação por domínio, dados, avaliação e integração ao processo real.
- Gera conhecimento técnico reutilizável: benchmarks, modelos versionados, pipelines e critérios de promoção.

### Critério de conclusão

Um projeto avançado só deve ser considerado concluído quando houver ganho comprovado contra
baseline, dados e modelos reproduzíveis, monitoramento em produção, segurança revisada, plano
de rollback e uma explicação clara de quais decisões continuam sob responsabilidade humana.

16. **Busca multimodal** – Embeddings de imagem + texto (CLIP) para buscar fotos por descrição ou por imagem parecida.
    - Pipeline de ingestão de imagens (thumbnail, OCR opcional, metadados EXIF).
    - Busca texto→imagem e imagem→imagem com interface unificada.
    - Regras de privacidade e moderação para conteúdo sensível.
    - Otimização de custo: cache de embeddings e reprocessamento apenas de ativos alterados.
    - MVP sugerido: galeria indexada com consulta textual, upload de imagem, filtros e link para o ativo original.
    - Critério de sucesso: recuperar imagens relevantes em consultas textuais, visuais e combinadas com latência aceitável.
    - **Valor entregue:** acelera workflows visuais (catálogo, mídia, suporte) com recuperação muito mais natural.

17. **Memória de longo prazo para agentes** – Armazenar conversas/fatos e recuperar os mais relevantes a cada turno.
    - Estrutura de memória episódica (eventos) e semântica (fatos duradouros).
    - Estratégia de esquecimento/atualização para evitar memória obsoleta.
    - Auditoria: rastrear quais memórias foram usadas em cada resposta.
    - Política de consentimento e retenção de dados por perfil de usuário.
    - MVP sugerido: gravação explícita de memórias, recuperação por consulta e tela para editar ou apagar fatos.
    - Critério de sucesso: recuperar preferências relevantes sem inventar fatos nem expor memórias fora do escopo do usuário.
    - **Valor entregue:** interações mais personalizadas e menos repetitivas ao longo do tempo.

18. **Detecção de anomalias** – Identificar transações, textos ou eventos distantes do "normal" no espaço vetorial.
    - Definição de baseline por segmento (cliente, região, tipo de evento).
    - Combinação de distância vetorial com regras de negócio para reduzir falso positivo.
    - Playbook de resposta: classificação de severidade e escalonamento automático.
    - Calibração contínua: revisão semanal de falsos positivos/negativos para ajuste de thresholds.
    - MVP sugerido: painel de anomalias com score, vizinhos normais, evidências e estado da investigação.
    - Critério de sucesso: detectar casos conhecidos em replay histórico e oferecer explicação suficiente para a triagem.
    - **Valor entregue:** prevenção proativa de fraude, falhas e incidentes críticos.

19. **Cache semântico para LLM** – Reutilizar respostas de perguntas semanticamente equivalentes para reduzir custo e latência.
    - Chave semântica baseada em embedding + limiar de equivalência.
    - Estratégia de invalidação por versão de prompt/modelo/base de conhecimento.
    - Métricas de impacto: hit-rate, economia de tokens e tempo médio de resposta.
    - Controle de risco: bypass automático para perguntas sensíveis ou regulatórias.
    - MVP sugerido: middleware com chave composta por prompt, contexto, modelo e versão da base, além de comparação de cache hit/miss.
    - Critério de sucesso: reduzir custo e latência sem devolver resposta desatualizada ou de outro contexto.
    - **Valor entregue:** queda de custo operacional e respostas mais rápidas em alta escala.

20. **Reranking em duas etapas** – Recuperação rápida por embeddings + reranker (cross-encoder) para maior precisão.
    - Fase 1: recall alto (top-50/top-100) com ANN no pgvector.
    - Fase 2: reordenação fina com cross-encoder em subconjunto reduzido.
    - Trade-off latência x qualidade com perfilamento por carga.
    - Perfil por segmento: aplicar rerank apenas em consultas onde o ganho de qualidade compensa.
    - MVP sugerido: benchmark reproduzível comparando busca exata, ANN e ANN + reranker.
    - Critério de sucesso: ganho de nDCG/MRR que justifique a latência e o consumo adicional do segundo estágio.
    - **Valor entregue:** melhor relevância percebida pelo usuário final sem explodir custo.

21. **Detecção de plágio / conteúdo gerado** – Comparar trechos com um corpus em nível de sentença/parágrafo.
    - Segmentação por sentenças e janelas deslizantes para comparação robusta.
    - Evidências de similaridade com links e destaques de trechos alinhados.
    - Regras de decisão por contexto (citação legítima vs cópia indevida).
    - Trilhas de auditoria: guardar evidências e scores para suporte a revisão humana.
    - MVP sugerido: relatório com percentual de sobreposição, fontes candidatas, trechos alinhados e revisão manual.
    - Critério de sucesso: detectar casos conhecidos sem tratar o score como prova isolada ou decisão automática.
    - **Valor entregue:** reforça integridade acadêmica/editorial com análise escalável.

22. **Roteador de intenção para agentes** – Escolher a ferramenta/agente certo comparando a pergunta com descrições de ferramentas.
    - Catálogo de ferramentas com embeddings de descrição e exemplos de uso.
    - Política de roteamento com fallback e desambiguação quando houver empate.
    - Telemetria de acerto de roteamento e custo por tipo de tarefa.
    - Guarda de segurança: bloquear roteamento para ferramentas sensíveis sem confirmação contextual.
    - MVP sugerido: catálogo versionado, top-3 intenções, confirmação em casos ambíguos e registro da ferramenta escolhida.
    - Critério de sucesso: elevar a taxa de execução correta e falhar de forma segura quando nenhuma ferramenta for adequada.
    - **Valor entregue:** aumenta taxa de resolução no primeiro passo e reduz chamadas erradas.

23. **Busca em áudio/vídeo** – Transcrever (Whisper), fatiar e indexar; buscar o momento exato de um assunto.
    - Pipeline mídia→transcrição→chunks temporais com timestamps.
    - Busca semântica com retorno do minuto/segundo exato e snippet transcrito.
    - Pós-processamento: diarização, remoção de ruído e capítulos automáticos.
    - Navegação prática: deep link para o timestamp no player e exportação de highlights.
    - MVP sugerido: upload de mídia, transcrição assíncrona, busca e player aberto no trecho encontrado.
    - Critério de sucesso: localizar tópicos em gravações com diferentes sotaques, ruído e durações.
    - **Valor entregue:** economiza horas de revisão manual de reuniões, aulas e entrevistas.

24. **Grafo de conhecimento assistido** – Ligar entidades/documentos por similaridade e navegar visualmente (UMAP/t-SNE).
    - Extração de entidades e relações candidatas por proximidade vetorial.
    - Navegação exploratória com filtros por tipo de entidade e confiança.
    - Casos de uso: investigação, descoberta de lacunas e análise de impacto.
    - Curadoria incremental: validação humana das relações críticas para elevar confiança do grafo.
    - MVP sugerido: grafo com entidades, arestas, score, fonte da evidência e expansão por vizinhança.
    - Critério de sucesso: permitir encontrar relações úteis sem esconder a diferença entre inferência e fato confirmado.
    - **Valor entregue:** acelera análise estratégica e descoberta de conexões não óbvias.

25. **Fine-tuning de embeddings** – Ajustar um modelo ao seu domínio (jurídico, médico, etc.) e medir ganho em recall@k.
    - Construção de dataset de pares positivos/negativos com revisão de especialistas.
    - Treino contrastivo e validação offline (recall@k, nDCG, MRR).
    - Estratégia de rollout gradual com comparação online contra modelo base.
    - Critério de adoção: só promover versão nova se houver ganho estatisticamente consistente.
    - MVP sugerido: baseline congelado, dataset versionado, script de treino/avaliação e relatório comparativo.
    - Critério de sucesso: ganho consistente em dados fora do treino sem degradar idiomas, classes ou consultas críticas.
    - **Valor entregue:** melhora significativa de relevância em domínio específico, criando vantagem competitiva.

## Ideias de Extensão para Este Repositório

- Endpoint de busca por similaridade sobre `Recomendation`/produtos com filtro por categoria/preço.
  - Definir contrato de API (filtros, paginação, score mínimo).
  - Incluir payload com item, score e justificativa textual curta.
  - Adicionar validação de `top`, timeout de embedding e resposta explícita para índice desabilitado.
  - **Entregável:** rota documentada, exemplos HTTP, seed reproduzível e testes de filtro/ordenação.
  - **Valor entregue:** viabiliza feature pronta para consumo por frontend e integrações externas.

- Índice HNSW/IVFFlat no pgvector e benchmark de latência vs. busca exata.
  - Criar cenários de benchmark por volume (10k, 100k, 1M vetores).
  - Comparar recall e latência P95 para cada configuração de índice.
  - Medir custo de construção, memória, tempo de manutenção e comportamento após inserções.
  - **Entregável:** script de benchmark com dataset fixo, comandos de reprodução e recomendação de configuração.
  - **Valor entregue:** decisão técnica orientada por dados para escalar com previsibilidade.

- Comparar modelos de embedding (dimensões, qualidade, velocidade) com um conjunto de avaliação.
  - Montar suíte de consultas com gabarito e critérios objetivos.
  - Publicar relatório comparativo por custo, qualidade e throughput.
  - Fixar versão, prompt/instrução, métrica, idioma e hardware para que a comparação seja justa.
  - **Entregável:** tabela de trade-offs e decisão registrada, não apenas uma lista de scores.
  - **Valor entregue:** escolha de modelo com melhor custo-benefício para o contexto real.

- Pipeline de ingestão em lote com `BackgroundService` e reprocessamento quando o modelo mudar.
  - Implementar fila idempotente com checkpoint de progresso.
  - Adicionar versionamento de embeddings por modelo e data de geração.
  - Incluir retry limitado, dead-letter, cancelamento gracioso e retomada após reinício.
  - **Entregável:** comando de reindexação, dashboard de progresso e relatório de falhas por item.
  - **Valor entregue:** operação estável de dados em produção com menor risco de inconsistência.

- Visualização 2D dos vetores (UMAP) em uma página simples.
  - Exportar projeções com rótulos e metadados para inspeção visual.
  - Permitir filtro interativo por categoria e busca textual.
  - Separar visualização exploratória de decisão automática: proximidade em 2D pode distorcer a distância original.
  - **Entregável:** exportação reproduzível, legenda, filtros e amostras representativas por grupo.
  - **Valor entregue:** facilita interpretação de qualidade semântica por perfis não técnicos.

- Testes de relevância (recall@k, MRR) automatizados no projeto de testes.
  - Criar dataset mínimo reprodutível para CI.
  - Definir limiares de qualidade para evitar regressões semânticas.
  - Cobrir consultas sem resultado, empates, filtros, conteúdo novo e troca de modelo.
  - **Entregável:** relatório no CI com baseline, resultado atual e motivo explícito para qualquer quebra.
  - **Valor entregue:** protege a qualidade de busca ao longo da evolução do código e dos modelos.

## Dicas Gerais

- **Chunking**: divida textos longos (200–500 tokens, com sobreposição) antes de gerar embeddings;
  preserve `document_id`, posição, título e limites para reconstruir o contexto.
- **Normalização**: normalize vetores se usar produto interno; escolha a métrica (cosseno/L2/IP)
  conforme o modelo e use a mesma convenção na indexação e na consulta.
- **Dimensão**: confirme o limite do índice escolhido; no pgvector, dimensões altas podem forçar
  scan sequencial. Avalie truncamento Matryoshka somente com medição de qualidade.
- **Texto canônico**: não embedde apenas IDs ou campos técnicos; componha uma descrição legível
  com os sinais que realmente aparecerão nas perguntas.
- **Metadados**: guarde fonte, data, categoria, idioma e permissões para filtrar antes/depois da
  busca vetorial e preservar segurança no resultado.
- **Filtros**: aplique autorização e filtros obrigatórios no banco; não recupere documentos que o
  usuário não poderia ver para depois tentar escondê-los na aplicação.
- **Avaliação**: crie um pequeno conjunto de perguntas com respostas esperadas antes de otimizar;
  separe treino, validação e teste para não medir memorização.
- **Baseline**: compare contra `LIKE`, `tsvector`, regras ou recomendação popular; sem baseline,
  um score alto não prova que embeddings trouxeram valor.
- **Confiança**: defina faixas de alta confiança, revisão humana e recusa; top-1 sempre existente
  não significa top-1 correto.
- **Evidência**: retorne trecho, origem, score e metadados suficientes para o usuário verificar
  o motivo do casamento.
- **Versionamento**: registre qual modelo, instrução, dimensão e métrica geraram cada vetor;
  vetores de modelos diferentes não são comparáveis.
- **Reindexação**: trate o índice como projeção descartável e reconstrua-o de forma idempotente
  quando trocar modelo, chunking ou texto canônico.
- **Falhas**: não converta timeout do Ollama em lista vazia; diferencie indisponibilidade,
  índice desabilitado, consulta inválida e ausência real de resultados.
- **Observabilidade**: registre latência por etapa, quantidade de candidatos, score mínimo,
  cache hit/miss, modelo utilizado e feedback do usuário.
- **Segurança**: embeddings podem permitir inferências sobre o conteúdo original; limite acesso,
  proteja backups e avalie se dados sensíveis podem ser indexados.
- **Custo**: use batch, cache e reprocessamento incremental; meça custo por documento e por consulta,
  não apenas o tempo de uma execução local.
- **Reprodutibilidade**: fixe seeds, versões, dataset, configuração e comandos de execução para
  que outra pessoa consiga confirmar o resultado.
