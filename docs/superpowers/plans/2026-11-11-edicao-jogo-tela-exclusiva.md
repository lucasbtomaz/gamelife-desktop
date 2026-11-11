# Edição de jogo em tela exclusiva Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Corrigir o erro 500 do SQLite, abrir cadastro e edição em telas exclusivas e manter totais e adicionar no topo da Biblioteca.

**Architecture:** `BibliotecaService` deve projetar a resposta antes de confirmar a transação relacional. `App` deve alternar entre biblioteca e uma nova tela de formulário que reutiliza `LibraryGameForm`, sem renderizar a grade ao mesmo tempo. A faixa de progresso e o botão de adicionar ficam antes da grade.

**Tech Stack:** .NET 10, Entity Framework Core, SQLite, React, Vitest, Testing Library.

**Spec:** `docs/superpowers/specs/2026-11-11-edicao-jogo-tela-exclusiva-design.md`

## Global Constraints

- Usar somente `data/gamelife.db` no executável oficial e nunca alterá-lo em testes.
- Preservar uma ficha por jogo e todas as plataformas já cadastradas.
- Publicar somente após build e testes aprovados.
- Manter o atalho apontando para `artifacts/GameLife/GameLife.exe`.

## Review Focus

- O SQLite deve responder 200 depois de realmente gravar a nova plataforma.
- Uma falha antes do commit deve continuar revertendo toda a operação.
- A tela de edição não pode renderizar cards, busca ou filtros da biblioteca.
- A faixa de totais e o botão de adicionar devem preceder busca, filtros e cards.
- Cancelar ou voltar não pode enviar requisição nem alterar dados.
- Salvar deve voltar à biblioteca mostrando uma única ficha atualizada.

---

### Task 1: Corrigir o salvamento transacional no SQLite

**Files:**
- Create: `tests/GameLife.Api.Tests/Api/BibliotecaSqliteTests.cs`
- Modify: `src/GameLife.Api/Services/BibliotecaService.cs`

**Interfaces:**
- Consumes: `BibliotecaService.SalvarAsync(SalvarJogoBibliotecaRequest, CancellationToken)`.
- Produces: resposta agrupada válida depois de gravar uma nova plataforma em SQLite.

- [ ] **Step 1: Escrever o teste SQLite que salva uma segunda plataforma e exige retorno válido e duas posses.**
- [ ] **Step 2: Executar o teste e confirmar falha com transação SQLite encerrada.**
- [ ] **Step 3: Consultar `ObterJogoAsync` antes do commit e evitar rollback de transação já confirmada.**
- [ ] **Step 4: Executar o teste focado e toda a suíte .NET.**
- [ ] **Step 5: Commitar a correção transacional.**

### Task 2: Mover cadastro e edição para telas exclusivas

**Files:**
- Create: `src/GameLife.Web/src/screens/LibraryGameScreen.jsx`
- Create: `src/GameLife.Web/src/screens/LibraryGameScreen.test.jsx`
- Modify: `src/GameLife.Web/src/App.jsx`
- Modify: `src/GameLife.Web/src/App.test.jsx`
- Modify: `src/GameLife.Web/src/App.css`

**Interfaces:**
- Consumes: ficha agrupada opcional, `LibraryGameForm`, callback de salvar e callback de voltar.
- Produces: `LibraryGameScreen` para adicionar ou editar, com formulário e retorno à biblioteca.

- [ ] **Step 1: Escrever testes que exigem telas exclusivas ao adicionar e editar, totais e adicionar no topo, retorno sem salvar e volta após sucesso.**
- [ ] **Step 2: Executar os testes e confirmar que a grade continua visível indevidamente.**
- [ ] **Step 3: Implementar `LibraryGameScreen`, a navegação interna e a faixa superior em `App`.**
- [ ] **Step 4: Executar testes focados, suíte React e build Vite.**
- [ ] **Step 5: Commitar a tela exclusiva.**

### Task 3: Validar e publicar sem perda de dados

**Files:**
- Verify: `scripts/publish-local.ps1`
- Verify: `data/gamelife.db`
- Verify: `artifacts/GameLife/GameLife.exe`

**Interfaces:**
- Consumes: commits das Tasks 1 e 2.
- Produces: executável oficial atualizado e banco preservado.

- [ ] **Step 1: Rodar 30 testes React, build Vite, testes .NET e 5 testes Pester.**
- [ ] **Step 2: Fechar o GameLife normalmente se estiver aberto.**
- [ ] **Step 3: Publicar com `scripts/publish-local.ps1`.**
- [ ] **Step 4: Abrir o executável e validar a edição em tela exclusiva sem salvar dados de teste.**
- [ ] **Step 5: Confirmar integridade, contagens e destino do atalho.**
