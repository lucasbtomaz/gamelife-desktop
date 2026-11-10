# Proteção dos dados locais Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Impedir que atualizações ou executáveis antigos dividam os dados entre bancos diferentes e preservar todos os registros durante migrações e publicações.

**Architecture:** `data/gamelife.db` permanece como banco canônico. A inicialização rejeita executáveis fora da publicação oficial, cria backup antes de alterações de esquema ou mesclagem e importa dados legados de forma idempotente, inclusive acompanhamentos. A publicação valida o banco antes e depois e somente então recria o atalho.

**Tech Stack:** .NET 10, WPF, Entity Framework Core, SQLite, xUnit e PowerShell/Pester.

**Spec:** `docs/superpowers/specs/2026-11-06-protecao-dados-locais-design.md`

## Global Constraints

- O desktop usa somente `data/gamelife.db`, na raiz do repositório.
- Nunca apagar, recriar, substituir ou mover o banco canônico.
- Criar backup recuperável e comparar as quantidades por tabela antes de mudanças de persistência.
- Testes nunca escrevem na Área de Trabalho ou no `AppData` reais.
- `artifacts`, bancos, backups, logs e dados pessoais permanecem fora do Git.
- O atalho deve apontar diretamente para `artifacts/GameLife/GameLife.exe`.

## Review Focus

- Execução de uma cópia antiga em `AppData`: deve encerrar com orientação clara e sem criar outro banco.
- Banco legado sem a tabela nova de acompanhamentos: deve continuar sendo mesclado sem falha.
- Mesclagem executada repetidamente: não pode duplicar jogos, posses, registros ou acompanhamentos.
- Falha durante atualização de esquema ou mesclagem: o backup anterior deve continuar íntegro.
- Publicação com banco inconsistente ou testes falhando: não pode substituir o executável existente.

---

### Task 1: Bloquear bancos derivados do local do executável

**Files:**
- Modify: `src/GameLife.Desktop/LocalDatabase.cs`
- Test: `tests/GameLife.Api.Tests/Desktop/LocalDatabaseTests.cs`

**Interfaces:**
- Consumes: `LocalDatabase.ObterCaminhoPadrao(string baseDirectory)`
- Produces: `LocalDatabase.ValidarDiretorioPublicado(string baseDirectory)`

- [ ] **Step 1: Escrever testes falhos** para aceitar `artifacts/GameLife`, resolver a raiz corretamente e rejeitar uma instalação sob `AppData/Local/GameLife/App` sem criar diretórios ou bancos.
- [ ] **Step 2: Executar os testes focados** com `dotnet test tests/GameLife.Api.Tests/GameLife.Api.Tests.csproj --filter LocalDatabaseTests` e confirmar a falha pela ausência da validação.
- [ ] **Step 3: Implementar a validação mínima** em `LocalDatabase.cs`, mantendo a resolução atual para a publicação oficial e lançando uma mensagem orientando o uso do atalho oficial em qualquer instalação legada.
- [ ] **Step 4: Executar novamente os testes focados** e confirmar aprovação.

### Task 2: Criar backup antes de alterar o SQLite

**Files:**
- Modify: `src/GameLife.Desktop/LocalDatabase.cs`
- Test: `tests/GameLife.Api.Tests/Desktop/LocalDatabaseTests.cs`

**Interfaces:**
- Consumes: caminho do banco canônico e diretório `Backups` da raiz do projeto
- Produces: `LocalDatabase.CriarBackupAntesDeAlterar(string databasePath, string backupDirectory)` retornando o caminho criado ou `null`

- [ ] **Step 1: Escrever testes falhos** comprovando que um banco existente é copiado byte a byte para um caminho único, que banco ausente não gera backup e que nenhum arquivo original é movido ou substituído.
- [ ] **Step 2: Executar os testes focados** e confirmar falha pela função inexistente.
- [ ] **Step 3: Implementar a cópia recuperável** e chamá-la antes de `AtualizarEsquemaAsync` ou de uma mesclagem legada que possa escrever no destino.
- [ ] **Step 4: Executar os testes focados** e confirmar aprovação, incluindo integridade e contagens antes/depois.

### Task 3: Mesclar acompanhamentos legados sem duplicação

**Files:**
- Modify: `src/GameLife.Desktop/LegacyDatabaseMigration.cs`
- Test: `tests/GameLife.Api.Tests/Desktop/LocalDatabaseTests.cs`

**Interfaces:**
- Consumes: `LegacyDatabaseMigration.MesclarAsync(source, destination, cancellationToken)`
- Produces: mesclagem de `AcompanhamentoJogo` por título normalizado e plataforma normalizada, preservando status, data, horas e conquistas

- [ ] **Step 1: Escrever testes falhos** com um acompanhamento presente apenas no legado, executar a mesclagem duas vezes e exigir um único registro completo no destino.
- [ ] **Step 2: Adicionar teste falho** para um legado sem tabela `AcompanhamentosJogos`, exigindo que as tabelas antigas ainda sejam mescladas.
- [ ] **Step 3: Executar os testes focados** e confirmar as falhas esperadas.
- [ ] **Step 4: Implementar a detecção de tabela e a mesclagem idempotente**, vinculando a posse correspondente quando existir.
- [ ] **Step 5: Executar os testes focados** e confirmar aprovação.

### Task 4: Proteger a publicação com preflight do banco

**Files:**
- Create: `scripts/database-safety.ps1`
- Modify: `scripts/publish-local.ps1`
- Test: `scripts/tests/database-safety.Tests.ps1`
- Test: `scripts/tests/publish-local.Tests.ps1`

**Interfaces:**
- Consumes: `data/gamelife.db`, executável publicado e etapas existentes da publicação
- Produces: snapshot de integridade, tamanho, hash e contagens por tabela; comparação que proíbe redução inesperada de contagens

- [ ] **Step 1: Escrever testes Pester falhos** para snapshot íntegro, banco inconsistente, redução de contagem e preservação do executável quando o preflight falha.
- [ ] **Step 2: Executar os testes de scripts** e confirmar as falhas esperadas.
- [ ] **Step 3: Implementar `database-safety.ps1`** usando o provedor SQLite já restaurado pelo projeto, sem depender de instalação global e sem imprimir dados pessoais.
- [ ] **Step 4: Integrar o preflight ao início e ao fim de `publish-local.ps1`**, antes de qualquer substituição do executável.
- [ ] **Step 5: Executar novamente os testes Pester** e confirmar aprovação.

### Task 5: Desativar recuperavelmente a instalação antiga e validar o ciclo real

**Files:**
- Modify: somente arquivos locais ignorados pelo Git em `AppData/Local/GameLife/App` e `Backups`

**Interfaces:**
- Consumes: executável legado já identificado e atalho `GameLife.lnk`
- Produces: cópia recuperável do executável legado fora do caminho ativo; atalho único para a publicação oficial

- [ ] **Step 1: Criar backups atuais** dos bancos canônico e legado e registrar integridade e contagens de todas as tabelas.
- [ ] **Step 2: Confirmar o destino absoluto** do executável legado e do atalho antes de qualquer movimentação.
- [ ] **Step 3: Mover somente a pasta de executável legado** para um diretório de backup recuperável; preservar integralmente `AppData/Local/GameLife/data`.
- [ ] **Step 4: Executar `scripts/publish-local.ps1`** e confirmar frontend, .NET, publicação e criação do atalho.
- [ ] **Step 5: Abrir pelo atalho, criar um registro de teste identificável, fechar normalmente e reabrir**, comprovando persistência no mesmo banco.
- [ ] **Step 6: Remover somente o registro de teste pela API/interface após confirmação**, sem tocar nos dados pessoais existentes.
- [ ] **Step 7: Comparar integridade e contagens finais**, permitindo apenas as alterações esperadas do teste temporário já revertido.
- [ ] **Step 8: Revisar `git diff --check`, `git status` e todos os arquivos alterados** para garantir que nenhum banco, backup, caminho absoluto ou dado pessoal entrou no Git.

### Task 6: Verificação completa

**Files:**
- Verify only

**Interfaces:**
- Consumes: solução, frontend, scripts e publicação atualizados
- Produces: evidência fresca de conclusão

- [ ] **Step 1: Executar `npm.cmd test` e `npm.cmd run build`** em `src/GameLife.Web`.
- [ ] **Step 2: Executar `dotnet test GameLife.slnx --configuration Release`** e confirmar zero falhas.
- [ ] **Step 3: Executar a suíte Pester dos scripts** e confirmar zero falhas.
- [ ] **Step 4: Executar a publicação oficial** e confirmar que o atalho aponta para `artifacts/GameLife/GameLife.exe`.
- [ ] **Step 5: Conferir o SQLite final** com `PRAGMA integrity_check`, contagens por tabela e comparação com o snapshot inicial.
