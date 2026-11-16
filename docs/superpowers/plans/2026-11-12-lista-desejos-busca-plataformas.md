# Lista de Desejos: plataformas, busca e origem dos preços Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Permitir cotações em qualquer plataforma, localizar rapidamente jogos e lojas e identificar a origem dos menores preços.

**Architecture:** A API continuará reutilizando `Canal`, mas o fluxo específico da Lista de Desejos aceitará canais existentes mesmo quando a permissão global de preço estiver desativada, sem alterar essa permissão. O frontend filtrará os itens já carregados e derivará as lojas dos menores preços a partir das cotações e do histórico presentes na resposta atual.

**Tech Stack:** ASP.NET Core 10, Entity Framework Core, SQLite, React 19, Vitest e Testing Library.

**Spec:** `docs/superpowers/specs/2026-11-12-lista-desejos-busca-plataformas-design.md`

## Global Constraints

- Não alterar a estrutura do SQLite nem criar migração.
- Preservar todos os registros e históricos de preços existentes.
- A busca deve ser local, ignorar maiúsculas, minúsculas e acentos e considerar título, cotações atuais e histórico.
- Não criar nova entidade ou dependência.
- Publicar somente em `artifacts/GameLife`; o atalho deve apontar diretamente para `artifacts/GameLife/GameLife.exe`.
- Nunca apagar, substituir ou mover `data/gamelife.db`; verificar integridade e contagens antes e depois da publicação.
- Instalar e compilar dependências apenas no worktree fora do OneDrive; não gerar `node_modules`, `bin`, `obj` ou `dist` dentro de pastas sincronizadas.

## Review Focus

- Canal existente com `PermitePreco = false`: a cotação deve ser criada e a permissão global deve permanecer inalterada; coberto na Task 1.
- Canal livre ainda inexistente: deve continuar sendo criado e associado à cotação; coberto na regressão da Task 1.
- Busca com acentos e caixa diferente: deve encontrar título e loja equivalentes; coberto na Task 2.
- Cotação indisponível: não pode ser apresentada como origem do menor preço atual ou histórico; coberto na Task 2.
- Empate entre lojas e registros repetidos: deve produzir nomes únicos e uma etiqueta compacta; coberto na Task 2.

---

### Task 1: Aceitar plataformas existentes na cotação da Lista de Desejos

**Files:**
- Modify: `src/GameLife.Api/Controllers/ItensListaDesejosController.cs:60-77`
- Test: `tests/GameLife.Api.Tests/Api/ListaDesejosEndpointsTests.cs`

**Interfaces:**
- Consumes: `AdicionarPrecoDesejoRequest` com `NomeCanal`, `PrecoAtual`, `PrecoCheio` e `Disponivel`.
- Produces: `POST /api/lista-desejos/{id}/precos` aceita qualquer `Canal` válido neste fluxo e não modifica `Canal.PermitePreco`.

- [ ] **Step 1: Escrever o teste de API que reproduz o bloqueio do Xbox**

Adicionar `AdicionarPrecoAceitaCanalExistenteSemPermissaoGlobalDePreco`: criar `Canal { Nome = "Xbox One", PermitePreco = false }`, criar um desejo, enviar a cotação e verificar `201 Created`, `CanalNome == "Xbox One"` e `PermitePreco` ainda falso após recarregar o contexto.

- [ ] **Step 2: Executar o teste e confirmar a falha esperada**

Run: `dotnet test tests/GameLife.Api.Tests/GameLife.Api.Tests.csproj --filter "FullyQualifiedName~ListaDesejosEndpointsTests.AdicionarPrecoAceitaCanalExistenteSemPermissaoGlobalDePreco"`

Expected: FAIL porque o endpoint retorna `400 Bad Request` com “A loja informada não aceita registros de preço.”

- [ ] **Step 3: Remover somente a rejeição por `PermitePreco` do fluxo da Lista de Desejos**

Em `AdicionarPreco`, preservar a resolução por ID/nome, a criação de canais inexistentes e todas as validações de preço; retirar a condição que devolve `BadRequest` para um canal existente sem permissão global.

- [ ] **Step 4: Adicionar regressão para canal livre inexistente**

Garantir em teste que `Xbox Series S` inexistente continua sendo criado com a cotação e aparece em `PrecosAtuais`, sem afetar a regra do teste anterior.

- [ ] **Step 5: Executar os testes da API da Lista de Desejos**

Run: `dotnet test tests/GameLife.Api.Tests/GameLife.Api.Tests.csproj --filter "FullyQualifiedName~ListaDesejosEndpointsTests"`

Expected: todos os testes de `ListaDesejosEndpointsTests` aprovados.

- [ ] **Step 6: Commit**

```bash
git add src/GameLife.Api/Controllers/ItensListaDesejosController.cs tests/GameLife.Api.Tests/Api/ListaDesejosEndpointsTests.cs
git commit -m "fix: permita cotações em plataformas existentes"
```

### Task 2: Adicionar busca e origem dos menores preços

**Files:**
- Create: `src/GameLife.Web/src/utils/wishlist.js`
- Create: `src/GameLife.Web/src/utils/wishlist.test.js`
- Modify: `src/GameLife.Web/src/screens/WishlistScreen.jsx`
- Modify: `src/GameLife.Web/src/screens/WishlistScreen.test.jsx`
- Modify: `src/GameLife.Web/src/App.css`

**Interfaces:**
- Consumes: `ItemListaDesejosResponse` com `titulo`, `precosAtuais`, `historicoPrecos`, `menorPrecoAtual` e `menorPrecoHistorico`.
- Produces: `normalizarBusca(texto: unknown): string`, `filtrarDesejos(itens: Array, termo: string): Array`, `obterOrigensMenores(item: object): { atual: string[], historico: string[] }` e `formatarOrigens(nomes: string[]): string`.

- [ ] **Step 1: Escrever testes unitários dos helpers de busca e origem**

Cobrir: `Pokémon` localizado por `pokemon`; item localizado por loja presente somente no histórico; cotação indisponível excluída; empate entre duas lojas deduplicado; `formatarOrigens(["Xbox One", "Xbox Series S"])` retorna `Xbox One +1`.

- [ ] **Step 2: Executar os helpers e confirmar a falha esperada**

Run: `npm.cmd test -- --run src/utils/wishlist.test.js`

Expected: FAIL porque `src/utils/wishlist.js` ainda não existe.

- [ ] **Step 3: Implementar os quatro helpers em `src/utils/wishlist.js`**

Usar normalização Unicode NFD, remoção de marcas diacríticas, minúsculas e `trim`; considerar apenas preços disponíveis ao calcular origens; comparar valores numericamente; deduplicar e ordenar nomes com locale `pt-BR`.

- [ ] **Step 4: Executar os testes unitários e confirmar aprovação**

Run: `npm.cmd test -- --run src/utils/wishlist.test.js`

Expected: todos os testes do arquivo aprovados.

- [ ] **Step 5: Escrever testes de componente para a nova interface**

Adicionar casos que verifiquem: ausência do texto global “melhor preço”; campo `Buscar por jogo ou loja`; filtro por título e loja com estado “Nenhum desejo encontrado”; etiquetas `Loja do menor atual` e `Loja do menor histórico`; sugestões incluem `Xbox One` mesmo com `permitePreco: false`.

- [ ] **Step 6: Executar o componente e confirmar as falhas esperadas**

Run: `npm.cmd test -- --run src/screens/WishlistScreen.test.jsx`

Expected: FAIL nos novos elementos e comportamentos ainda ausentes.

- [ ] **Step 7: Integrar busca, origens e canais na `WishlistScreen`**

Adicionar estado `busca`, derivar `itensFiltrados` com `filtrarDesejos`, renderizar o campo de busca acima dos cartões, substituir `itens.map` por `itensFiltrados.map`, mostrar estado vazio específico e usar `obterOrigensMenores`/`formatarOrigens` nas duas métricas. Carregar todos os canais no `datalist`, sem filtrar por `permitePreco`, e remover o terceiro indicador do cabeçalho.

- [ ] **Step 8: Estilizar busca, etiquetas e estado vazio**

Em `App.css`, manter o padrão visual atual, permitir quebra segura das etiquetas de origem e garantir que busca e cartões continuem responsivos em 850 px e 560 px.

- [ ] **Step 9: Executar os testes focados do frontend**

Run: `npm.cmd test -- --run src/utils/wishlist.test.js src/screens/WishlistScreen.test.jsx`

Expected: todos os testes dos dois arquivos aprovados.

- [ ] **Step 10: Commit**

```bash
git add src/GameLife.Web/src/utils/wishlist.js src/GameLife.Web/src/utils/wishlist.test.js src/GameLife.Web/src/screens/WishlistScreen.jsx src/GameLife.Web/src/screens/WishlistScreen.test.jsx src/GameLife.Web/src/App.css
git commit -m "feat: facilite consultas na lista de desejos"
```

### Task 3: Verificação e publicação local

**Files:**
- Verify only: `data/gamelife.db`
- Publish output: `artifacts/GameLife`
- Shortcut: `GameLife.lnk`

**Interfaces:**
- Consumes: commits das Tasks 1 e 2.
- Produces: executável local atualizado, atalho oficial validado e banco preservado.

- [ ] **Step 1: Executar toda a suíte frontend e o build**

Run: `npm.cmd test -- --run` e `npm.cmd run build`

Expected: todos os testes aprovados e build Vite concluído.

- [ ] **Step 2: Executar toda a suíte .NET**

Run: `dotnet test GameLife.slnx --no-restore`

Expected: todos os testes aprovados.

- [ ] **Step 3: Criar snapshot somente leitura do banco original**

Usar `tools/GameLife.DatabaseSafety` contra `data/gamelife.db` da raiz oficial e gravar o snapshot apenas em `%TEMP%`.

- [ ] **Step 4: Publicar e recriar o atalho oficial**

Publicar `GameLife.Desktop.csproj` para `artifacts/GameLife` da raiz oficial e executar `scripts/create-shortcut.ps1` com o caminho explícito do executável publicado.

- [ ] **Step 5: Verificar SQLite e atalho**

Comparar integridade e contagens com o snapshot, confirmar que o snapshot temporário foi removido e que `GameLife.lnk` aponta para `artifacts/GameLife/GameLife.exe`.

- [ ] **Step 6: Validar o ciclo real**

Abrir pelo executável oficial, verificar Xbox, busca, etiquetas e cabeçalho com os dados existentes, fechar normalmente e abrir novamente; não criar nem alterar cotações reais durante a validação.
