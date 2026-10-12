# Estabilização da API GameLife Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tornar a API GameLife utilizável localmente com MySQL, contratos HTTP seguros, histórico de preços, posses por formato, testes e CI.

**Architecture:** Manter uma única Web API ASP.NET Core com controllers e EF Core/MySQL. DTOs delimitam a API, configurações Fluent API definem os relacionamentos e índices, e testes de integração substituem o banco por EF Core InMemory apenas durante os testes.

**Tech Stack:** .NET 10, ASP.NET Core, EF Core, MySQL, xUnit, `Microsoft.AspNetCore.Mvc.Testing`, GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-10-12-estabilizacao-api-gamelife-design.md`

## Global Constraints

- Manter a aplicação simples e adequada ao conteúdo estudado.
- Não adicionar autenticação, integração externa, frontend ou importação automática da planilha.
- Guardar credenciais somente em User Secrets; nenhum segredo entra no repositório.
- Aceitar múltiplos preços por item da lista de desejos e múltiplas posses por jogo em canais ou formatos diferentes.
- Executar cada alteração de comportamento em ciclo RED, GREEN e refatoração.

## Review Focus

- Connection string ausente deve impedir a inicialização com mensagem segura e clara.
- O mesmo jogo não pode aparecer duas vezes na lista de desejos.
- O mesmo jogo pode ter vários registros de preço e posses em canais ou formatos distintos.
- Entradas inválidas não podem alterar o banco e devem retornar `400`, `404` ou `409` adequadamente.
- Exceções e respostas não podem revelar credenciais, stack traces ou detalhes internos.

---

### Task 1: Build e configuração segura

**Files:**
- Modify: `gamelife-api/Program.cs`
- Modify: `gamelife-api/appsettings.json`
- Create: `gamelife-api.Tests/gamelife-api.Tests.csproj`
- Create: `gamelife-api.Tests/Configuration/StartupConfigurationTests.cs`
- Modify: `gamelife-api.slnx`

**Interfaces:**
- Produces: inicialização que exige `ConnectionStrings:GameLife`; política CORS `Frontend`; `public partial class Program` para testes de integração.
- Consumes: nenhuma interface nova.

- [ ] **Step 1: Criar projeto e teste de configuração**

Criar `StartupConfigurationTests.InicializacaoSemConnectionStringFalhaComMensagemSegura`, removendo a connection string e verificando `InvalidOperationException` com a chave `ConnectionStrings:GameLife`, sem senha.

- [ ] **Step 2: Confirmar RED**

Run: `dotnet test gamelife-api.Tests/gamelife-api.Tests.csproj --filter StartupConfigurationTests`

Expected: FAIL porque a aplicação ainda usa fallback para banco em memória ou não expõe `Program`.

- [ ] **Step 3: Implementar inicialização segura**

Em `Program.cs`, exigir a connection string, registrar MySQL diretamente, adicionar `ProblemDetails`, tratamento de exceção, CORS com origens de `Cors:AllowedOrigins` e `public partial class Program`.

- [ ] **Step 4: Confirmar GREEN e build**

Run: `dotnet test gamelife-api.Tests/gamelife-api.Tests.csproj --filter StartupConfigurationTests`

Expected: PASS.

Run: `dotnet build gamelife-api.slnx --no-restore`

Expected: exit code 0.

- [ ] **Step 5: Commit**

Commit: `fix: corrigir inicializacao segura da API`

### Task 2: Modelo relacional e migration corretiva

**Files:**
- Create: `gamelife-api/Models/FormatoPosse.cs`
- Modify: `gamelife-api/Models/ItemListaDesejos.cs`
- Modify: `gamelife-api/Models/PosseJogo.cs`
- Modify: `gamelife-api/Context/GameLifeDbContext.cs`
- Create: `gamelife-api.Tests/Context/GameLifeDbContextModelTests.cs`
- Create: `gamelife-api/Migrations/<timestamp>_CorrigirRelacionamentosEAdicionarFormato.cs`
- Create: `gamelife-api/Migrations/<timestamp>_CorrigirRelacionamentosEAdicionarFormato.Designer.cs`
- Modify: `gamelife-api/Migrations/GameLifeDbContextModelSnapshot.cs`

**Interfaces:**
- Produces: `FormatoPosse { Digital, MidiaFisica, Assinatura }`; `ItemListaDesejos.RegistrosPrecos`; índice único `ItemListaDesejos.JogoId`; índice único `PosseJogo(JogoId, CanalId, Formato)`.
- Consumes: `GameLifeDbContext` registrado pela Task 1.

- [ ] **Step 1: Criar testes do modelo EF Core**

Testar que `ItemListaDesejos` se relaciona com `RegistroPreco`, que `RegistroJogo` não possui FK para item da lista e que os dois índices únicos existem com as propriedades exatas.

- [ ] **Step 2: Confirmar RED**

Run: `dotnet test gamelife-api.Tests/gamelife-api.Tests.csproj --filter GameLifeDbContextModelTests`

Expected: FAIL devido à navegação incorreta, à ausência de `Formato` e dos índices.

- [ ] **Step 3: Corrigir entidades e Fluent API**

Substituir `Registros` por `ICollection<RegistroPreco> RegistrosPrecos`; adicionar `FormatoPosse Formato`; configurar relações, precisão decimal e índices em `OnModelCreating`.

- [ ] **Step 4: Confirmar GREEN**

Run: `dotnet test gamelife-api.Tests/gamelife-api.Tests.csproj --filter GameLifeDbContextModelTests`

Expected: PASS.

- [ ] **Step 5: Gerar e validar migration**

Run: `dotnet tool run dotnet-ef migrations add CorrigirRelacionamentosEAdicionarFormato --project gamelife-api/gamelife-api.csproj --startup-project gamelife-api/gamelife-api.csproj --no-build`

Expected: remover `ItemListaDesejosId` de `RegistrosJogos`, adicionar `Formato` e os índices únicos.

Run: `dotnet tool run dotnet-ef migrations has-pending-model-changes --project gamelife-api/gamelife-api.csproj --startup-project gamelife-api/gamelife-api.csproj --no-build`

Expected: exit code 0 e nenhuma alteração pendente.

- [ ] **Step 6: Commit**

Commit: `fix: corrigir relacionamentos e posses de jogos`

### Task 3: DTOs, validações e endpoints

**Files:**
- Create: `gamelife-api/Dtos/**/*.cs`
- Modify: `gamelife-api/Controllers/*.cs`
- Create: `gamelife-api.Tests/Api/GameLifeApiFactory.cs`
- Create: `gamelife-api.Tests/Api/JogosEndpointsTests.cs`
- Create: `gamelife-api.Tests/Api/ListaDesejosEndpointsTests.cs`
- Create: `gamelife-api.Tests/Api/PossesEndpointsTests.cs`
- Create: `gamelife-api.Tests/Api/RegistrosJogosEndpointsTests.cs`

**Interfaces:**
- Produces: DTOs de criação, atualização, resumo e detalhe; endpoints assíncronos com `CancellationToken`; erros `ProblemDetails`.
- Consumes: `FormatoPosse`, relacionamentos e índices da Task 2; política de erros da Task 1.

- [ ] **Step 1: Criar factory e testes HTTP**

Testar criação e consulta de jogo; título vazio; inclusão e duplicidade na lista; vários preços; posses em canais ou formatos distintos; duplicidade da mesma combinação; conquistas obtidas maiores que possíveis.

- [ ] **Step 2: Confirmar RED**

Run: `dotnet test gamelife-api.Tests/gamelife-api.Tests.csproj --filter FullyQualifiedName~EndpointsTests`

Expected: FAIL porque controllers ainda recebem entidades e não aplicam todos os contratos.

- [ ] **Step 3: Implementar DTOs e controllers**

Criar DTOs focados por operação, mapear campos permitidos, usar projeções de saída, adicionar `CancellationToken`, validar referências e converter duplicidades em `409 Conflict`.

- [ ] **Step 4: Implementar validação de registro**

Criar DTO de registro que implemente `IValidatableObject` e rejeite `ConquistasObtidas > ConquistasPossiveis`.

- [ ] **Step 5: Confirmar GREEN**

Run: `dotnet test gamelife-api.Tests/gamelife-api.Tests.csproj --filter FullyQualifiedName~EndpointsTests`

Expected: PASS.

Run: `dotnet test gamelife-api.slnx`

Expected: todos os testes passam.

- [ ] **Step 6: Commit**

Commit: `feat: adicionar contratos seguros aos endpoints`

### Task 4: Fluxo principal e integração contínua

**Files:**
- Create: `gamelife-api.Tests/Api/FluxoListaDesejosTests.cs`
- Create: `.github/workflows/ci.yml`

**Interfaces:**
- Produces: teste do fluxo principal; verificação automatizada de restore, build e test.
- Consumes: endpoints e DTOs da Task 3.

- [ ] **Step 1: Criar teste de ponta a ponta**

Criar `CadastrarJogoListaDesejosPrecosEPossesMantemTodoOHistorico`: cadastrar jogo e canais, adicionar item, registrar dois preços, registrar duas posses e consultar o resultado.

- [ ] **Step 2: Confirmar RED**

Run: `dotnet test gamelife-api.Tests/gamelife-api.Tests.csproj --filter FluxoListaDesejosTests`

Expected: FAIL se qualquer integração estiver incompleta.

- [ ] **Step 3: Fazer ajuste mínimo e confirmar GREEN**

Run: `dotnet test gamelife-api.Tests/gamelife-api.Tests.csproj --filter FluxoListaDesejosTests`

Expected: PASS.

- [ ] **Step 4: Criar CI**

Configurar checkout, .NET 10, restore da ferramenta, restore, build sem restore e testes sem build.

- [ ] **Step 5: Validar solução**

Run: `dotnet restore gamelife-api.slnx`

Expected: exit code 0.

Run: `dotnet build gamelife-api.slnx --no-restore`

Expected: exit code 0 e zero erros.

Run: `dotnet test gamelife-api.slnx --no-build`

Expected: todos os testes passam.

- [ ] **Step 6: Commit**

Commit: `test: validar fluxo principal e configurar CI`

### Task 5: Colaboração e proteção

**Files:**
- Create: `CONTRIBUTING.md`
- Create: `.github/pull_request_template.md`
- Create: `.github/CODEOWNERS`
- Modify: `README.md`

**Interfaces:**
- Produces: instruções para executar, contribuir e proteger segredos; revisão do proprietário.
- Consumes: comandos comprovados pelas Tasks 1 a 4.

- [ ] **Step 1: Documentar execução e contribuição**

Explicar User Secrets, migrations, build, testes, branch e pull request. Avisar que a API permanece local enquanto não houver autenticação.

- [ ] **Step 2: Adicionar template e CODEOWNERS**

Usar checklist para testes, migration, ausência de segredos e escopo; definir `* @lucasbtomaz`.

- [ ] **Step 3: Validar segurança**

Run: `git grep -n -I -E "(Password|Pwd|ClientSecret|ApiKey|BEGIN (RSA|OPENSSH|EC) PRIVATE KEY)" -- ':!docs/superpowers/**'`

Expected: nenhuma credencial real; revisar manualmente chaves sem valor e exemplos.

Run: `dotnet list gamelife-api.slnx package --vulnerable --include-transitive`

Expected: nenhum pacote vulnerável conhecido.

- [ ] **Step 4: Verificação final e commit**

Run: `dotnet build gamelife-api.slnx --no-restore`

Expected: exit code 0.

Run: `dotnet test gamelife-api.slnx --no-build`

Expected: todos os testes passam.

Commit: `docs: orientar execucao e contribuicoes`

- [ ] **Step 5: Configurar a `main` após autorização externa**

Exigir pull request, CI, conversas resolvidas, uma aprovação do `CODEOWNER`, e bloquear force push e exclusão. Esta chamada altera o GitHub e requer autorização específica.
