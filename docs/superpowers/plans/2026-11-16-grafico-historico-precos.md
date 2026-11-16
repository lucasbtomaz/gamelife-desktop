# Gráfico do Histórico de Preços Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Substituir a lista textual do histórico por um gráfico SVG responsivo com todos os registros e uma linha por plataforma.

**Architecture:** Um componente React isolado transforma `historicoPrecos` em séries ordenadas por plataforma, calcula uma escala temporal e uma escala de preços e renderiza eixos, linhas, pontos e tooltip em SVG. `WishlistScreen` mantém apenas o controle de abrir/fechar e fornece os dados já retornados pela API.

**Tech Stack:** React 19, SVG nativo, CSS, Vitest e Testing Library.

**Spec:** `docs/superpowers/specs/2026-11-16-grafico-historico-precos-design.md`

## Global Constraints

- Renderizar todos os registros cadastrados, sem agrupamento ou descarte de pontos válidos.
- Não adicionar dependências.
- Não alterar API, SQLite, publicação, atalho ou dados existentes.
- Manter textos em português e o estilo visual atual do GameLife.
- Garantir uso por mouse, teclado e tecnologias assistivas.

## Review Focus

- Uma plataforma com um único registro deve continuar visível como ponto.
- Preços iguais ou consultas no mesmo instante não podem produzir coordenadas inválidas.
- Muitos pontos devem permanecer dentro do `viewBox` sem ampliar o card horizontalmente.
- O menor preço deve ser determinado entre todas as plataformas e destacado uma única vez quando houver empate pelo registro mais antigo.
- O histórico vazio deve mostrar uma mensagem, sem SVG vazio ou erro de execução.

---

### Task 1: Componente do gráfico de preços

**Files:**
- Create: `src/GameLife.Web/src/components/PriceHistoryChart.jsx`
- Create: `src/GameLife.Web/src/components/PriceHistoryChart.test.jsx`

**Interfaces:**
- Consumes: `registros: Array<{ id, canalId, canalNome, precoAtual, consultadoEm }>` e `tituloJogo: string`.
- Produces: `PriceHistoryChart({ registros, tituloJogo })`, com SVG acessível, legenda, pontos interativos e estado vazio.

- [ ] **Step 1: Escrever os testes falhos do gráfico**

Criar testes que afirmem:

- Duas plataformas geram duas séries identificadas e todos os quatro registros viram pontos acessíveis.
- Os pontos são ordenados pela data dentro de cada série.
- O foco ou hover em um ponto exibe plataforma, preço em BRL e data completa.
- O menor registro recebe a indicação “Menor preço histórico”.
- Uma série com um ponto é renderizada; preços/datas iguais não geram `NaN` ou `Infinity`.
- Uma lista vazia mostra `Nenhum histórico de preço cadastrado.`.

- [ ] **Step 2: Executar o teste e confirmar RED**

Run: `npm.cmd test -- PriceHistoryChart.test.jsx`

Expected: FAIL porque `PriceHistoryChart.jsx` ainda não existe.

- [ ] **Step 3: Implementar o componente**

Em `PriceHistoryChart.jsx`, criar `PriceHistoryChart({ registros = [], tituloJogo })` com `viewBox="0 0 720 320"`. Agrupar por `canalId` com fallback para `canalNome`, ordenar por `consultadoEm`, usar paleta fixa e escalas com margens seguras quando os domínios forem constantes. Renderizar eixos, rótulos de datas/preços, `polyline` para séries com dois ou mais pontos, `circle` focalizável para cada registro, legenda e tooltip React controlado por hover/foco.

- [ ] **Step 4: Executar o teste e confirmar GREEN**

Run: `npm.cmd test -- PriceHistoryChart.test.jsx`

Expected: todos os testes do componente aprovados.

- [ ] **Step 5: Commit**

```bash
git add src/GameLife.Web/src/components/PriceHistoryChart.jsx src/GameLife.Web/src/components/PriceHistoryChart.test.jsx
git commit -m "feat: criar gráfico do histórico de preços"
```

### Task 2: Integrar o gráfico à Lista de Desejos

**Files:**
- Modify: `src/GameLife.Web/src/screens/WishlistScreen.jsx:1-260`
- Modify: `src/GameLife.Web/src/screens/WishlistScreen.test.jsx`
- Modify: `src/GameLife.Web/src/App.css:1106-1152`

**Interfaces:**
- Consumes: `PriceHistoryChart({ registros, tituloJogo })` da Task 1.
- Produces: histórico expansível renderizado como gráfico dentro de cada card.

- [ ] **Step 1: Escrever o teste falho de integração**

Adicionar ao teste da tela um desejo com registros de STEAM e PlayStation em datas diferentes. Abrir `Ver histórico (3)` e afirmar que existe o gráfico nomeado `Histórico de preços de <jogo>`, as duas plataformas na legenda e os três pontos; afirmar também que a antiga lista textual não é renderizada.

- [ ] **Step 2: Executar o teste e confirmar RED**

Run: `npm.cmd test -- WishlistScreen.test.jsx`

Expected: FAIL porque a tela ainda usa `.price-history` com linhas textuais.

- [ ] **Step 3: Integrar o componente e aplicar os estilos**

Importar `PriceHistoryChart` e substituir a lista inline pelo componente. Em `App.css`, criar os estilos `.price-history-chart`, `.price-chart-svg`, `.price-chart-grid`, `.price-chart-axis`, `.price-chart-line`, `.price-chart-point`, `.price-chart-point-min`, `.price-chart-tooltip` e `.price-chart-legend`, incluindo ajuste para telas de até 560px e rolagem somente se o conteúdo textual da legenda exigir.

- [ ] **Step 4: Executar os testes focados e o frontend completo**

Run: `npm.cmd test -- WishlistScreen.test.jsx PriceHistoryChart.test.jsx`

Expected: testes focados aprovados.

Run: `npm.cmd test`

Expected: 100% dos testes do frontend aprovados.

- [ ] **Step 5: Commit**

```bash
git add src/GameLife.Web/src/screens/WishlistScreen.jsx src/GameLife.Web/src/screens/WishlistScreen.test.jsx src/GameLife.Web/src/App.css
git commit -m "feat: exibir evolução dos preços por plataforma"
```

### Task 3: Publicação e validação real

**Files:**
- Verify only: `scripts/publish-local.ps1`
- Verify only: `data/gamelife.db`
- Verify only: `artifacts/GameLife/GameLife.exe`

**Interfaces:**
- Consumes: frontend concluído nas Tasks 1 e 2.
- Produces: executável publicado e validado sem alteração de dados.

- [ ] **Step 1: Revisar o diff e confirmar que API, migrações e persistência não mudaram**

Run: `git diff --check` e `git status --short`.

Expected: somente arquivos planejados, além das alterações preexistentes preservadas.

- [ ] **Step 2: Executar a publicação oficial**

Run: `.\scripts\publish-local.ps1`

Expected: frontend, build e testes .NET aprovados; integridade e contagens do SQLite preservadas; atalho recriado para `artifacts/GameLife/GameLife.exe`.

- [ ] **Step 3: Validar visualmente o aplicativo**

Abrir o executável publicado, acessar Lista de Desejos, expandir um jogo com múltiplas plataformas e confirmar linhas, pontos, legenda, tooltip e responsividade. Fechar normalmente e confirmar que não ficou processo residual.

- [ ] **Step 4: Conferir o banco depois do ciclo real**

Comparar o SQLite com o snapshot anterior à publicação usando `GameLife.DatabaseSafety`.

Expected: integridade `ok` e nenhuma redução nas contagens.
