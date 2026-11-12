# Melhorias de Painéis e Preços - Plano de Implementação

> **Para agentes executores:** SUB-SKILL OBRIGATÓRIA: use `superpowers:subagent-driven-development` (recomendado) ou `superpowers:executing-plans` para implementar este plano tarefa por tarefa. As etapas usam caixas de seleção (`- [ ]`) para acompanhamento.

**Objetivo:** Melhorar Início, Lista de Desejos, Jogos Concluídos e Retrospectiva com percentuais seguros, histórico de preços útil e layouts alinhados às referências aprovadas.

**Arquitetura:** Funções puras compartilhadas cuidarão da formatação e limitação de percentuais. As regras de oportunidade da Lista de Desejos serão derivadas no frontend a partir do contrato atual, sem migração. Os demais componentes preservarão suas APIs e comportamentos, alterando somente composição, estados reais e apresentação.

**Stack:** React 19, CSS, Vitest, Testing Library, API ASP.NET Core 10 e SQLite local.

**Especificação:** `docs/superpowers/specs/2026-11-12-melhorias-paineis-precos-design.md`

## Restrições globais

- Não adicionar dependências.
- Não apagar, mover, recriar ou substituir `data/gamelife.db`.
- Não alterar publicação, atalho, caminhos locais ou persistência.
- Não escrever na Área de Trabalho nem no `AppData` durante testes.
- Escrever e observar cada teste falhar antes da implementação correspondente.
- Preservar alterações preexistentes nos testes PowerShell.

## Foco da revisão

- Percentuais negativos, acima de 100 ou não numéricos devem produzir barras entre 0% e 100% e texto legível.
- Total zero não pode causar divisão por zero, `NaN%` nem preencher a barra.
- Cotação sem preço cheio, com preço cheio zero ou indisponível não pode receber selo de desconto.
- Histórico com várias lojas e várias cotações deve manter todas as entradas no grupo correto e em ordem decrescente.
- API vazia ou indisponível na Retrospectiva não pode exibir jogos demonstrativos como se fossem reais.

---

### Task 0: Registrar a linha de base do SQLite

**Arquivos:**
- Ler: `data/gamelife.db`
- Criar fora do repositório: `$env:TEMP\gamelife-ui-before.json`

**Interfaces:**
- Produz: snapshot somente leitura consumido pela Tarefa 5.

- [ ] **Etapa 1: capturar integridade, hash e quantidades antes da implementação**

Executar: `dotnet run --project tools/GameLife.DatabaseSafety -- snapshot data/gamelife.db "$env:TEMP\gamelife-ui-before.json"`
Esperado: “Snapshot seguro do banco criado.”, sem alteração no SQLite.

- [ ] **Etapa 2: confirmar que o banco continua fora do Git**

Executar: `git status --short -- data/gamelife.db "$env:TEMP\gamelife-ui-before.json"`
Esperado: nenhuma entrada para `data/gamelife.db`.

### Task 1: Percentuais compartilhados e painel de progresso do Início

**Arquivos:**
- Criar: `src/GameLife.Web/src/utils/percentual.js`
- Criar: `src/GameLife.Web/src/utils/percentual.test.js`
- Modificar: `src/GameLife.Web/src/components/HomeDashboard.jsx`
- Modificar: `src/GameLife.Web/src/components/HomeDashboard.test.jsx`
- Modificar: `src/GameLife.Web/src/App.css`

**Interfaces:**
- Produz: `limitarPercentual(valor): number` no intervalo de 0 a 100.
- Produz: `formatarPercentual(valor): string` em `pt-BR`, com zero ou uma casa decimal.
- Produz: painel `.overall-progress-card` baseado em `dashboard.resumo.totalJogos` e `dashboard.resumo.jogosZerados`.

- [ ] **Etapa 1: escrever testes falhos das funções de percentual**

Cobrir `67 -> "67%"`, `66.666 -> "66,7%"`, `150 -> 100`, `-5 -> 0` e valor inválido -> `0%`.

- [ ] **Etapa 2: executar o teste focado e confirmar a falha por módulo ausente**

Executar: `npm.cmd test -- --run src/utils/percentual.test.js`

- [ ] **Etapa 3: implementar `limitarPercentual` e `formatarPercentual`**

Normalizar com `Number`, limitar com `Math.min/Math.max` e formatar com `Intl.NumberFormat('pt-BR', { minimumFractionDigits: 0, maximumFractionDigits: 1 })`.

- [ ] **Etapa 4: executar o teste focado e confirmar aprovação**

Executar: `npm.cmd test -- --run src/utils/percentual.test.js`

- [ ] **Etapa 5: escrever teste falho do painel geral**

Em `HomeDashboard.test.jsx`, exigir título “Progresso geral da biblioteca”, contagens de zerados/em andamento, percentuais `100%` e `0%` para dados inconsistentes e larguras limitadas por `limitarPercentual`; cobrir também total zero.

- [ ] **Etapa 6: executar o teste de componente e confirmar a falha pela ausência do painel**

Executar: `npm.cmd test -- --run src/components/HomeDashboard.test.jsx`

- [ ] **Etapa 7: implementar o painel e aplicar a formatação compartilhada**

Calcular `zerados = min(max(jogosZerados, 0), totalJogos)`, `emAndamento = totalJogos - zerados` e percentuais protegidos. Substituir a formatação local de conquistas pelo utilitário compartilhado.

- [ ] **Etapa 8: estilizar o painel conforme a referência**

Adicionar cartão, cabeçalho, trilho segmentado, legenda e regras responsivas em `App.css`, sempre com `overflow: hidden`.

- [ ] **Etapa 9: executar os testes focados e confirmar aprovação**

Executar: `npm.cmd test -- --run src/utils/percentual.test.js src/components/HomeDashboard.test.jsx`

- [ ] **Etapa 10: registrar a tarefa**

Commit: `feat: adicione progresso seguro ao início`

### Task 2: Lista de Desejos sem preço alvo e com oportunidades por loja

**Arquivos:**
- Modificar: `src/GameLife.Web/src/screens/WishlistScreen.jsx`
- Modificar: `src/GameLife.Web/src/screens/WishlistScreen.test.jsx`
- Modificar: `src/GameLife.Web/src/App.css`

**Interfaces:**
- Consome: contrato atual `precosAtuais[]` e `historicoPrecos[]`.
- Produz: `classificarCotacao(preco): string[]` com `desconto-80`, `menor-historico` e `abaixo-20`.
- Produz: histórico agrupado visualmente por `canalId`, mais recente primeiro.

- [ ] **Etapa 1: escrever testes falhos do novo fluxo**

Exigir ausência dos campos e rótulos de preço desejado; criação com `{ titulo, precoDesejado: null, observacao }`; três selos simultâneos para preço atual R$ 19,00, cheio R$ 100,00 e mínimo histórico R$ 19,00; nenhum selo para cotação indisponível.

- [ ] **Etapa 2: escrever teste falho do histórico agrupado**

Fornecer duas lojas e três registros fora de ordem; exigir duas seções nomeadas e datas/preços em ordem decrescente dentro da loja correta.

- [ ] **Etapa 3: executar o teste focado e confirmar falhas pelo comportamento antigo**

Executar: `npm.cmd test -- --run src/screens/WishlistScreen.test.jsx`

- [ ] **Etapa 4: remover preço desejado do estado e da apresentação**

Manter `precoDesejado: null` apenas no payload de criação para compatibilidade; ao editar a observação, reenviar silenciosamente `item.precoDesejado` para preservar eventual valor legado, sem exibi-lo.

- [ ] **Etapa 5: implementar classificação e agrupamento derivados**

Calcular desconto somente com `disponivel && precoCheio > 0`; menor histórico com igualdade numérica; abaixo de R$ 20 com comparação estrita. Agrupar o histórico por `canalId` e ordenar por `consultadoEm` e `id` decrescentes.

- [ ] **Etapa 6: reconstruir as linhas de loja e o histórico expansível**

Exibir preço atual, preço cheio, desconto e selos acumuláveis por loja. O histórico deve manter disponibilidade e data de cada registro.

- [ ] **Etapa 7: aplicar cores e responsividade**

Usar verde para `desconto-80`, violeta para `menor-historico` e âmbar para `abaixo-20`, com texto além da cor.

- [ ] **Etapa 8: executar o teste focado e confirmar aprovação**

Executar: `npm.cmd test -- --run src/screens/WishlistScreen.test.jsx`

- [ ] **Etapa 9: executar os testes da API de Lista de Desejos para confirmar preservação do histórico**

Executar: `dotnet test tests/GameLife.Api.Tests/GameLife.Api.Tests.csproj --filter FullyQualifiedName~ListaDesejosEndpointsTests`

- [ ] **Etapa 10: registrar a tarefa**

Commit: `feat: destaque oportunidades na lista de desejos`

### Task 3: Fichas renovadas em Jogos Concluídos

**Arquivos:**
- Modificar: `src/GameLife.Web/src/screens/GameTrackingScreen.jsx`
- Modificar: `src/GameLife.Web/src/screens/GameTrackingScreen.test.jsx`
- Modificar: `src/GameLife.Web/src/App.css`

**Interfaces:**
- Consome: `formatarPercentual` e `limitarPercentual` da Tarefa 1.
- Preserva: agrupamento por `jogoId`, edição e exclusão por acompanhamento/plataforma.
- Produz: ficha `.tracking-game-card` responsiva com seções semânticas de plataforma.

- [ ] **Etapa 1: escrever testes falhos da ficha renovada**

Exigir plataforma, selo com data de conclusão, horas, conquistas, percentual com uma casa no máximo e ações acessíveis. Para `percentualConquistas: 125`, exigir texto `100%` e largura `100%`.

- [ ] **Etapa 2: executar o teste focado e confirmar falhas pela apresentação antiga**

Executar: `npm.cmd test -- --run src/screens/GameTrackingScreen.test.jsx`

- [ ] **Etapa 3: reorganizar a marcação da ficha preservando handlers**

Manter uma ficha por jogo e uma seção por plataforma. Colocar status/data no cabeçalho da seção, métricas em cartões e barra de troféus abaixo.

- [ ] **Etapa 4: aplicar os utilitários de percentual**

Usar `formatarPercentual(item.percentualConquistas)` no texto e `limitarPercentual` na largura.

- [ ] **Etapa 5: ajustar grade, métricas, ícones e estados no CSS**

Criar grade responsiva de duas colunas em telas largas e uma coluna em telas estreitas, sem reduzir áreas clicáveis.

- [ ] **Etapa 6: executar o teste focado e confirmar aprovação**

Executar: `npm.cmd test -- --run src/screens/GameTrackingScreen.test.jsx`

- [ ] **Etapa 7: registrar a tarefa**

Commit: `feat: renove as fichas de jogos concluídos`

### Task 4: Retrospectiva dinâmica baseada apenas nos dados reais

**Arquivos:**
- Modificar: `src/GameLife.Web/src/components/PainelRetrospectiva.jsx`
- Modificar: `src/GameLife.Web/src/components/PainelRetrospectiva.test.jsx`
- Modificar: `src/GameLife.Web/src/App.css`

**Interfaces:**
- Consome: `formatarPercentual` e `limitarPercentual` da Tarefa 1.
- Produz: períodos atual, anterior e anual derivados do relógio local.
- Preserva: modos `mensal` e `wrapped`, cópia de texto e geração PNG.

- [ ] **Etapa 1: escrever testes falhos de períodos dinâmicos**

Fixar o relógio em janeiro de 2027 e exigir “Janeiro/2027 (Atual)”, “Dezembro/2026” e “Retrospectiva Anual 2027”.

- [ ] **Etapa 2: escrever testes falhos de estado vazio e erro**

Com resposta `[]`, exigir mensagem de ausência e nenhum título demonstrativo. Com rejeição, exigir alerta de carregamento e nenhum dado fictício.

- [ ] **Etapa 3: escrever teste falho do ranking principal**

Fornecer três jogos reais e exigir ordem por horas, posição, status, plataforma, conquistas e participação formatada/limitada no tempo total.

- [ ] **Etapa 4: executar o teste focado e confirmar falhas pelos períodos fixos e dados de demonstração**

Executar: `npm.cmd test -- --run src/components/PainelRetrospectiva.test.jsx`

- [ ] **Etapa 5: remover `DADOS_DEMO` e modelar estados reais**

Guardar erro de carregamento separadamente; lista vazia permanece vazia e não cai em dados substitutos.

- [ ] **Etapa 6: derivar períodos do relógio e ajustar filtros**

Calcular mês/ano atual, mês anterior inclusive na virada do ano e opção anual. Quando um período não tiver registros, mostrar estado vazio desse período sem reutilizar registros de outro mês.

- [ ] **Etapa 7: reorganizar cabeçalho, indicadores e ranking**

Unir ações e seletores no painel superior; renderizar quatro indicadores e painel de ranking segundo a referência. Aplicar os utilitários aos percentuais do ranking.

- [ ] **Etapa 8: preservar e adaptar os modos mensal/anual, cópia e PNG**

Manter `cartaoWrappedRef`, `copiarResumo` e `baixarCartaoImagem`, agora alimentados somente por dados reais.

- [ ] **Etapa 9: executar o teste focado e confirmar aprovação**

Executar: `npm.cmd test -- --run src/components/PainelRetrospectiva.test.jsx`

- [ ] **Etapa 10: registrar a tarefa**

Commit: `feat: torne a retrospectiva dinâmica e real`

### Task 5: Verificação integrada e segurança do banco local

**Arquivos:**
- Revisar: todos os arquivos modificados nas Tarefas 1 a 4.
- Não modificar: `data/gamelife.db`, scripts de publicação e atalho.

**Interfaces:**
- Consome: todas as entregas anteriores.
- Produz: evidência fresca de testes, build e integridade somente leitura.

- [ ] **Etapa 1: executar toda a suíte do frontend**

Executar: `npm.cmd test -- --run`
Esperado: todos os testes aprovados, sem falhas.

- [ ] **Etapa 2: compilar o frontend**

Executar: `npm.cmd run build`
Esperado: código de saída 0.

- [ ] **Etapa 3: executar toda a suíte .NET**

Executar: `dotnet test GameLife.slnx --no-restore`
Esperado: todos os testes aprovados, sem falhas.

- [ ] **Etapa 4: conferir o SQLite em modo somente leitura contra a linha de base**

Executar: `dotnet run --project tools/GameLife.DatabaseSafety -- verify data/gamelife.db "$env:TEMP\gamelife-ui-before.json"`
Esperado: “Integridade e contagens do banco foram preservadas.” O comando remove somente o snapshot temporário após validar o banco em modo somente leitura.

- [ ] **Etapa 5: revisar diff e estado do repositório**

Confirmar que somente código, testes, CSS e documentação planejados mudaram; preservar as nove alterações PowerShell preexistentes.

- [ ] **Etapa 6: registrar a verificação final**

Commit: `test: valide melhorias dos painéis e preços`
