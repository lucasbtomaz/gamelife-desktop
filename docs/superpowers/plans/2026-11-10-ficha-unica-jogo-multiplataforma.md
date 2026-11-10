# Ficha Única por Jogo Multiplataforma Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cadastrar e exibir cada jogo uma única vez, com várias plataformas e dados opcionais independentes de mídia e acompanhamento.

**Architecture:** Manter o modelo `Jogo` + `PosseJogo` + `AcompanhamentoJogo`, acrescentando unicidade no vínculo jogo/plataforma. A API da Biblioteca passa a agrupar posses por jogo e a salvar várias plataformas atomicamente; a interface consome esse contrato agrupado e apresenta uma ficha única com subitens por plataforma.

**Tech Stack:** .NET 10, ASP.NET Core, Entity Framework Core, SQLite, React 19, Vite, Vitest, Testing Library, xUnit.

**Spec:** `docs/superpowers/specs/2026-11-08-ficha-unica-jogo-multiplataforma-design.md`

## Global Constraints

- Nunca apagar, recriar, substituir ou mover `data/gamelife.db`.
- O desktop usa somente `data/gamelife.db` na raiz do repositório; `AppData` é proibido como banco ativo.
- Criar backup SQLite consistente com WAL antes de alterar o esquema.
- Interromper sem excluir registros se houver posses históricas duplicadas.
- Testes nunca escrevem na Área de Trabalho real nem no `AppData` real.
- Preservar todas as alterações preexistentes no worktree e versionar somente arquivos da tarefa em cada commit.
- Não adicionar dependências externas.

## Review Focus

- Pedido com a mesma plataforma repetida deve retornar `400` e não persistir nenhuma parte.
- Atualização que omite uma plataforma existente deve preservar essa plataforma.
- Plataforma existente sem objeto de acompanhamento deve preservar o acompanhamento atual.
- Erro em uma das plataformas deve reverter jogo, posses, canais e acompanhamentos de toda a operação.
- Banco histórico com `PosseJogo(JogoId, CanalId)` duplicado deve ganhar backup e falhar sem reduzir contagens.

---

### Task 1: Proteger a unicidade jogo/plataforma no banco

**Files:**
- Modify: `src/GameLife.Api/Context/GameLifeDbContext.cs`
- Create: `src/GameLife.Api/Migrations/20261110_GarantirPosseUnicaPorPlataforma.cs`
- Modify: `src/GameLife.Api/Migrations/GameLifeDbContextModelSnapshot.cs`
- Modify: `src/GameLife.Desktop/LocalDatabase.cs:96-205`
- Modify: `tests/GameLife.Api.Tests/Desktop/LocalDatabaseTests.cs`

**Interfaces:**
- Produces: índice único `IX_PossesJogos_JogoId_CanalId` em bancos novos e existentes.
- Produces: `LocalDatabase.PrepararAsync(...)` cria backup antes do índice e rejeita duplicidades com mensagem clara.

- [ ] **Step 1: Escrever testes que falham para criação segura do índice**

Adicionar testes reais em `LocalDatabaseTests`:

```csharp
[Fact]
public async Task CriaIndiceUnicoDePosseSemAlterarDadosExistentes()

[Fact]
public async Task FazBackupERejeitaPossesDuplicadasSemApagarRegistros()
```

O primeiro deve criar banco antigo sem o índice, preparar e confirmar a preservação literal das contagens e a rejeição de uma segunda posse igual. O segundo deve inserir duplicatas antes da proteção, executar `PrepararAsync`, exigir `InvalidOperationException`, conferir o backup e confirmar que as duas linhas originais continuam presentes.

- [ ] **Step 2: Executar os testes e confirmar RED**

Run: `dotnet test tests/GameLife.Api.Tests/GameLife.Api.Tests.csproj --filter "FullyQualifiedName~LocalDatabaseTests"`

Expected: FAIL porque o índice não é criado e duplicidades não interrompem a atualização com backup.

- [ ] **Step 3: Mapear e criar o índice com preflight**

Adicionar em `GameLifeDbContext.OnModelCreating`:

```csharp
modelBuilder.Entity<PosseJogo>()
    .HasIndex(item => new { item.JogoId, item.CanalId })
    .IsUnique();
```

Criar a migração/model snapshot correspondentes. Em `LocalDatabase`, detectar a ausência do índice como mudança de esquema, criar backup pelo mecanismo WAL-safe existente, consultar duplicidades antes do DDL e criar o índice somente quando seguro.

- [ ] **Step 4: Executar testes focados e confirmar GREEN**

Run: `dotnet test tests/GameLife.Api.Tests/GameLife.Api.Tests.csproj --filter "FullyQualifiedName~LocalDatabaseTests"`

Expected: PASS.

- [ ] **Step 5: Commit da proteção de persistência**

```powershell
git add src/GameLife.Api/Context/GameLifeDbContext.cs src/GameLife.Api/Migrations src/GameLife.Desktop/LocalDatabase.cs tests/GameLife.Api.Tests/Desktop/LocalDatabaseTests.cs
git commit -m "fix: impedir posses duplicadas por plataforma" -m "Protege a ficha única no SQLite e preserva bancos históricos antes da alteração de esquema."
```

---

### Task 2: Agrupar a Biblioteca e salvar várias plataformas atomicamente

**Files:**
- Create: `src/GameLife.Api/Contracts/BibliotecaContracts.cs`
- Create: `src/GameLife.Api/Services/BibliotecaService.cs`
- Modify: `src/GameLife.Api/Controllers/BibliotecaController.cs`
- Modify: `tests/GameLife.Api.Tests/Api/BibliotecaEndpointsTests.cs`
- Modify: `tests/GameLife.Api.Tests/Api/AcompanhamentosJogosEndpointsTests.cs`
- Modify: `tests/GameLife.Api.Tests/Api/AcompanhamentosResumoEndpointsTests.cs`
- Modify: `tests/GameLife.Api.Tests/Api/ColecoesEndpointsTests.cs`
- Modify: `tests/GameLife.Api.Tests/Api/ListaDesejosEndpointsTests.cs`

**Interfaces:**
- Produces: `JogoBibliotecaResponse(int JogoId, string Titulo, int? AnoLancamento, IReadOnlyList<PlataformaBibliotecaResponse> Plataformas)`.
- Produces: `SalvarJogoBibliotecaRequest(string Titulo, int? AnoLancamento, IReadOnlyList<SalvarPlataformaBibliotecaRequest> Plataformas)`.
- Produces: `SalvarPlataformaBibliotecaRequest(string Plataforma, bool MidiaFisica, SalvarAcompanhamentoBibliotecaRequest? Acompanhamento)`.
- Produces: `SalvarAcompanhamentoBibliotecaRequest(string? Status, DateTime? DataStatus, decimal? TempoEmHoras, int? ConquistasPossiveis, int? ConquistasObtidas)`.
- Produces: `PlataformaBibliotecaResponse(int PosseId, int CanalId, string Nome, bool MidiaFisica, AcompanhamentoBibliotecaResponse? Acompanhamento)`.
- Produces: `AcompanhamentoBibliotecaResponse(int Id, string Status, DateTime? DataStatus, decimal? TempoEmHoras, int? ConquistasPossiveis, int? ConquistasObtidas)`.
- Produces: `BibliotecaService.ObterAsync(CancellationToken)` e `BibliotecaService.SalvarAsync(SalvarJogoBibliotecaRequest, CancellationToken)`.
- Consumes: índice único da Task 1.

- [ ] **Step 1: Escrever testes de API para o contrato agrupado**

Adicionar testes independentes que comprovem:

```csharp
[Fact] public async Task CadastraUmJogoComDuasPlataformasEmUmaOperacao()
[Fact] public async Task ConsultaRetornaUmaFichaComDuasPlataformas()
[Fact] public async Task AtualizaPlataformaExistenteSemDuplicarEsemApagarAsDemais()
[Fact] public async Task OmitirAcompanhamentoPreservaOExistente()
[Fact] public async Task PlataformaRepetidaOuInvalidaReverteTodaAOperacao()
[Fact] public void ProjecaoPlanaDaBibliotecaContinuaTraduzivelPeloMySql()
```

Usar títulos únicos por `Guid`; conferir no banco uma linha em `Jogos`, duas em `PossesJogos` e somente os acompanhamentos enviados. Confirmar `201` na criação e `200` na atualização.

- [ ] **Step 2: Executar os testes da Biblioteca e confirmar RED**

Run: `dotnet test tests/GameLife.Api.Tests/GameLife.Api.Tests.csproj --filter "FullyQualifiedName~BibliotecaEndpointsTests"`

Expected: FAIL porque o endpoint atual aceita uma única plataforma e retorna linhas planas.

- [ ] **Step 3: Criar contratos agrupados e serviço transacional**

Implementar os records em `BibliotecaContracts.cs`. Manter uma projeção plana interna traduzível pelo provedor MySQL e agrupar somente depois de materializar os resultados. Em `BibliotecaService`:

- normalizar título e plataforma com `Trim()`, `NormalizationForm.FormKC` e `ToUpperInvariant()`;
- validar todas as plataformas antes de escrever;
- usar uma transação do EF Core para jogo, canais, posses e acompanhamentos;
- reutilizar o jogo existente;
- preservar ano quando omitido e atualizar quando informado;
- preservar plataformas ausentes e acompanhamento omitido;
- atualizar `MidiaFisica` da plataforma enviada;
- criar/atualizar acompanhamento somente quando o objeto opcional existir;
- usar `NaoTerminado` quando o acompanhamento não informar status;
- validar conquistas antes da transação;
- retornar a ficha agrupada final e indicador de criação/atualização.

- [ ] **Step 4: Tornar o controller fino e atualizar consumidores de teste**

`BibliotecaController` deve delegar GET/POST ao serviço, devolver `Created` ou `Ok` e manter mensagens de erro em `ProblemDetails`. Atualizar os tipos de resposta nos testes de acompanhamentos, coleções e lista de desejos para ler `plataformas[]`, sem mudar o comportamento desses recursos.

- [ ] **Step 5: Executar testes de API e confirmar GREEN**

Run: `dotnet test tests/GameLife.Api.Tests/GameLife.Api.Tests.csproj --filter "FullyQualifiedName~BibliotecaEndpointsTests|FullyQualifiedName~AcompanhamentosJogosEndpointsTests|FullyQualifiedName~AcompanhamentosResumoEndpointsTests|FullyQualifiedName~ColecoesEndpointsTests|FullyQualifiedName~ListaDesejosEndpointsTests"`

Expected: PASS.

- [ ] **Step 6: Commit do contrato e salvamento atômico**

```powershell
git add src/GameLife.Api/Contracts/BibliotecaContracts.cs src/GameLife.Api/Services/BibliotecaService.cs src/GameLife.Api/Controllers/BibliotecaController.cs tests/GameLife.Api.Tests/Api
git commit -m "feat: salvar jogo em várias plataformas" -m "Agrupa a Biblioteca por jogo e grava posses e acompanhamentos em uma única transação."
```

---

### Task 3: Criar formulário multiplataforma com acompanhamento opcional

**Files:**
- Create: `src/GameLife.Web/src/components/LibraryGameForm.jsx`
- Create: `src/GameLife.Web/src/components/LibraryGameForm.test.jsx`
- Modify: `src/GameLife.Web/src/App.jsx:18-111,202-254`
- Modify: `src/GameLife.Web/src/App.test.jsx`
- Modify: `src/GameLife.Web/src/App.css`

**Interfaces:**
- Produces: `<LibraryGameForm platforms saving onSubmit />`.
- Produces: `onSubmit(payload)` com o formato `SalvarJogoBibliotecaRequest` da Task 2.
- Consumes: resposta agrupada `JogoBibliotecaResponse`; `App` substitui por `jogoId` ou acrescenta a ficha salva.

- [ ] **Step 1: Escrever testes do formulário antes do componente**

Cobrir:

```jsx
test('seleciona várias plataformas e envia uma única ficha')
test('permite mídia física diferente em cada plataforma')
test('mantém acompanhamento completamente opcional')
test('envia status horas e conquistas somente na plataforma acompanhada')
test('bloqueia envio sem plataforma e conquistas inconsistentes')
```

Asserir o payload literal entregue a `onSubmit`, sem testar detalhes privados de estado.

- [ ] **Step 2: Executar os testes do componente e confirmar RED**

Run: `npm.cmd test -- src/components/LibraryGameForm.test.jsx`

Working directory: `src/GameLife.Web`

Expected: FAIL porque o componente ainda não existe.

- [ ] **Step 3: Implementar o formulário acessível**

O componente deve mostrar título, ano opcional, checkboxes de plataforma e, para cada selecionada, mídia física e toggle “Acompanhar progresso”. Ao ativar o toggle, mostrar status, data, horas e conquistas. Enviar uma única vez e limpar o formulário somente após `onSubmit` resolver.

- [ ] **Step 4: Integrar o formulário no App**

Trocar os estados simples `plataforma`/`midiaFisica` pelo componente. `adicionarJogo(payload)` faz um único POST, substitui a ficha de mesmo `jogoId` ou acrescenta uma nova, e mantém os outros jogos. Ajustar CSS sem alterar a navegação ou os ícones atuais.

- [ ] **Step 5: Executar testes focados e confirmar GREEN**

Run: `npm.cmd test -- src/components/LibraryGameForm.test.jsx src/App.test.jsx`

Working directory: `src/GameLife.Web`

Expected: PASS.

- [ ] **Step 6: Commit do cadastro multiplataforma**

```powershell
git add src/GameLife.Web/src/components/LibraryGameForm.jsx src/GameLife.Web/src/components/LibraryGameForm.test.jsx src/GameLife.Web/src/App.jsx src/GameLife.Web/src/App.test.jsx src/GameLife.Web/src/App.css
git commit -m "feat: cadastrar jogo em várias plataformas" -m "Permite mídia e acompanhamento opcionais por plataforma em um único envio."
```

---

### Task 4: Exibir uma ficha única na Biblioteca com filtros agrupados

**Files:**
- Modify: `src/GameLife.Web/src/screens/LibraryScreen.jsx`
- Modify: `src/GameLife.Web/src/screens/LibraryScreen.test.jsx`
- Modify: `src/GameLife.Web/src/App.css`

**Interfaces:**
- Consumes: `JogoBibliotecaResponse[]` da Task 2.
- Produces: um `<article>` por jogo e subitens por `item.plataformas`.

- [ ] **Step 1: Reescrever fixtures e testes para fichas agrupadas**

Cobrir:

```jsx
test('exibe uma ficha para jogo com duas plataformas')
test('mostra mídia e progresso independentes por plataforma')
test('filtra a ficha quando qualquer plataforma corresponde')
test('filtro físico mantém somente plataformas físicas visíveis')
test('busca continua ignorando caixa e acentos')
```

O teste de `Graveyard Keeper` deve exigir exatamente um `article` principal e rótulos Steam e Xbox dentro dele.

- [ ] **Step 2: Executar teste da tela e confirmar RED**

Run: `npm.cmd test -- src/screens/LibraryScreen.test.jsx`

Working directory: `src/GameLife.Web`

Expected: FAIL porque a tela atual espera uma posse por item.

- [ ] **Step 3: Implementar agrupamento visual e filtros**

Renderizar título/ano uma vez e uma linha de plataforma para cada posse compatível com os filtros. Não inventar métricas ausentes. Manter cards responsivos e a linguagem visual aprovada.

- [ ] **Step 4: Executar teste focado e confirmar GREEN**

Run: `npm.cmd test -- src/screens/LibraryScreen.test.jsx`

Working directory: `src/GameLife.Web`

Expected: PASS.

- [ ] **Step 5: Commit da ficha única**

```powershell
git add src/GameLife.Web/src/screens/LibraryScreen.jsx src/GameLife.Web/src/screens/LibraryScreen.test.jsx src/GameLife.Web/src/App.css
git commit -m "fix: exibir uma ficha por jogo" -m "Agrupa plataformas, mídia e progresso sem repetir o título na Biblioteca."
```

---

### Task 5: Agrupar visualmente acompanhamentos e alinhar totais

**Files:**
- Modify: `src/GameLife.Web/src/screens/GameTrackingScreen.jsx`
- Modify: `src/GameLife.Web/src/screens/GameTrackingScreen.test.jsx`
- Modify: `src/GameLife.Web/src/App.test.jsx`
- Modify: `tests/GameLife.Api.Tests/Api/DashboardResumoEndpointsTests.cs`
- Modify: `src/GameLife.Web/src/App.css`

**Interfaces:**
- Consumes: resposta plana existente de `/api/acompanhamentos-jogos` para preservar CRUD por plataforma.
- Produces: um grupo visual por `jogoId`, contendo controles de edição/exclusão para cada acompanhamento de plataforma.
- Confirma: totais rotulados como jogos usam `JogoId` distinto; resumos de progresso permanecem por plataforma.

- [ ] **Step 1: Escrever testes para agrupamento e totais**

Adicionar:

```jsx
test('agrupa dois acompanhamentos do mesmo jogo em uma ficha')
test('edita somente a plataforma selecionada dentro da ficha')
```

No backend, acrescentar caso com duas posses do mesmo jogo e confirmar `TotalJogos == 1`, mantendo duas entradas na distribuição por plataforma.

- [ ] **Step 2: Executar testes e confirmar RED**

Run frontend: `npm.cmd test -- src/screens/GameTrackingScreen.test.jsx src/App.test.jsx`

Working directory: `src/GameLife.Web`

Run backend: `dotnet test tests/GameLife.Api.Tests/GameLife.Api.Tests.csproj --filter "FullyQualifiedName~DashboardResumoEndpointsTests"`

Expected: frontend FAIL porque cada acompanhamento ainda vira card separado; backend deve caracterizar e confirmar a contagem distinta já existente.

- [ ] **Step 3: Agrupar os cards de acompanhamento sem alterar o CRUD**

Agrupar `items` por `jogoId`; renderizar título uma vez e uma seção por plataforma. Cada botão Editar/Excluir continua usando o `id` do acompanhamento correspondente. Manter status `NaoTerminado` com rótulo “Em andamento”.

- [ ] **Step 4: Executar testes focados e confirmar GREEN**

Run: `npm.cmd test -- src/screens/GameTrackingScreen.test.jsx src/App.test.jsx`

Working directory: `src/GameLife.Web`

Run: `dotnet test tests/GameLife.Api.Tests/GameLife.Api.Tests.csproj --filter "FullyQualifiedName~DashboardResumoEndpointsTests"`

Expected: PASS.

- [ ] **Step 5: Commit do agrupamento de acompanhamento**

```powershell
git add src/GameLife.Web/src/screens/GameTrackingScreen.jsx src/GameLife.Web/src/screens/GameTrackingScreen.test.jsx src/GameLife.Web/src/App.test.jsx src/GameLife.Web/src/App.css tests/GameLife.Api.Tests/Api/DashboardResumoEndpointsTests.cs
git commit -m "fix: agrupar progresso por jogo" -m "Mantém edição por plataforma e apresenta o título uma única vez."
```

---

### Task 6: Verificação integral, publicação e ciclo real

**Files:**
- Modify if necessary: `scripts/tests/publish-local.Tests.ps1`
- Verify: `scripts/publish-local.ps1`
- Verify: `data/gamelife.db` somente leitura durante conferências

**Interfaces:**
- Consumes: todas as tasks anteriores.
- Produces: executável em `artifacts/GameLife/GameLife.exe` e atalho oficial preservando o SQLite.

- [ ] **Step 1: Capturar baseline seguro do banco**

Executar `PRAGMA integrity_check`, contagens de todas as tabelas e consulta específica de `Graveyard Keeper`. Esperado antes da atualização: um único `Jogo` com Steam e Xbox; nenhuma posse repetida por `(JogoId, CanalId)`.

- [ ] **Step 2: Rodar todas as suítes**

Run: `npm.cmd test`

Working directory: `src/GameLife.Web`

Run: `npm.cmd run build`

Working directory: `src/GameLife.Web`

Run: `dotnet test GameLife.slnx`

Run: `Invoke-Pester scripts/tests/publish-local.Tests.ps1`

Expected: todas as verificações passam sem reduzir contagens.

- [ ] **Step 3: Revisar o diff**

Run: `git diff --check`

Revisar contratos, transações, ausência de exclusões, caminhos do banco, responsividade e textos em português.

- [ ] **Step 4: Publicar pelo fluxo protegido**

Run: `.\scripts\publish-local.ps1`

Expected: snapshot inicial, testes, build, publicação, atalho e verificação final de integridade/contagens.

- [ ] **Step 5: Validar o ciclo real no executável**

Abrir `artifacts/GameLife/GameLife.exe`, conferir uma única ficha de `Graveyard Keeper` com Steam e Xbox, testar filtros, fechar normalmente e reabrir. Confirmar que os mesmos dados permanecem e que o atalho aponta exatamente para o executável oficial.

- [ ] **Step 6: Comparar banco e encerrar**

Repetir integridade e contagens. Nenhuma tabela pode perder registros; a única alteração esperada no primeiro início é o índice único. Não criar dados pessoais de teste no banco real.

