# GameLife Local pelo Navegador Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Publicar o GameLife como aplicativo local iniciado por atalho, aberto no navegador e persistente no MySQL.

**Architecture:** A API ASP.NET Core servirá os arquivos estáticos do build React e os endpoints na mesma origem em `127.0.0.1`. A publicação será autocontida para Windows e um inicializador abrirá o navegador após confirmar que a API está pronta.

**Tech Stack:** .NET 10, ASP.NET Core, EF Core, MySQL, React 19, Create React App, PowerShell.

**Spec:** `docs/superpowers/specs/2026-10-27-aplicativo-local-navegador-design.md`

## Global Constraints

- Manter MySQL e nunca incluir credenciais no Git ou no executável.
- Não alterar o escopo funcional além da biblioteca atual.
- Não exigir IDE, SDK ou Node.js para executar a versão publicada.
- Escutar somente em `127.0.0.1`.
- Usar TDD para mudanças de comportamento e validar build e testes completos.

## Review Focus

- MySQL indisponível deve produzir diagnóstico legível e não apagar dados.
- Porta local ocupada deve impedir segunda instância ou escolher comportamento determinístico.
- Credencial ausente deve ser tratada sem revelar senha ou detalhes internos.
- Rota do React atualizada diretamente deve retornar `index.html`.
- Fechar e reabrir deve recuperar o registro criado no MySQL.

---

### Task 1: Restaurar a API e proteger a configuração

**Files:**
- Modify: `gamelife-api/Program.cs`
- Modify: `gamelife-api/Models/ItemListaDesejos.cs`
- Create/Modify: `gamelife-api.Tests/**`
- Modify: `gamelife-api.slnx`

**Interfaces:**
- Produces: API compilável, `ConnectionStrings:GameLife` obrigatória e `public partial class Program` testável.
- Consumes: User Secrets atual e modelo EF existente.

- [ ] Criar testes que falham para configuração ausente e modelo EF inconsistente.
- [ ] Executar os testes focados e confirmar falhas pelas causas esperadas.
- [ ] Corrigir o fallback InMemory, a navegação `RegistrosPrecos` e a inicialização segura.
- [ ] Executar testes focados, `dotnet build gamelife-api.slnx` e `dotnet test gamelife-api.slnx`.
- [ ] Confirmar conexão real com MySQL por uma consulta somente leitura.

### Task 2: Alinhar a biblioteca do frontend à API

**Files:**
- Modify: `src/GameLife.Web/src/App.js`
- Modify: `src/GameLife.Web/src/App.test.js`

**Interfaces:**
- Consumes: `GET/POST /api/jogos` e contratos reais de `JogosController`.
- Produces: biblioteca atual carregada e cadastrada pela API existente.

- [ ] Criar testes React para carregamento, cadastro e erro HTTP usando os contratos reais.
- [ ] Confirmar RED por causa das rotas e payloads atuais.
- [ ] Ajustar rota, payload e projeção visual com a menor mudança possível.
- [ ] Executar `npm test -- --watchAll=false` e `npm run build`.

### Task 3: Hospedar o React na API publicada

**Files:**
- Modify: `gamelife-api/gamelife-api.csproj`
- Modify: `gamelife-api/Program.cs`
- Create: `gamelife-api.Tests/Hosting/StaticFrontendTests.cs`
- Create: `scripts/publish-local.ps1`

**Interfaces:**
- Consumes: build em `src/GameLife.Web/build`.
- Produces: `artifacts/GameLife/GameLife.exe` com `wwwroot` incorporado.

- [ ] Criar teste de integração para `/` e fallback de rota React.
- [ ] Confirmar RED sem arquivos estáticos configurados.
- [ ] Configurar arquivos estáticos, fallback e nome do executável.
- [ ] Criar publicação que compila React, testa as soluções e executa `dotnet publish --self-contained true --runtime win-x64`.
- [ ] Executar o script e validar a publicação em uma pasta limpa.

### Task 4: Inicialização, atalho e instância única

**Files:**
- Create: `scripts/start-local.ps1`
- Create: `scripts/create-shortcut.ps1`
- Create: `scripts/tests/start-local.Tests.ps1`

**Interfaces:**
- Consumes: executável publicado e configuração local.
- Produces: atalho `GameLife.lnk`, inicialização oculta e abertura do navegador padrão.

- [ ] Criar testes Pester para executável ausente, API pronta e segunda execução.
- [ ] Confirmar RED antes dos scripts existirem.
- [ ] Implementar espera por health check, instância única e abertura do navegador.
- [ ] Criar atalho com diretório de trabalho e ícone corretos.
- [ ] Executar os testes e abrir o aplicativo pelo atalho.

### Task 5: Backup e prova de persistência

**Files:**
- Create: `scripts/backup-mysql.ps1`
- Create: `scripts/tests/backup-mysql.Tests.ps1`
- Modify: `README.md`

**Interfaces:**
- Consumes: configuração MySQL local e ferramenta de dump instalada.
- Produces: backup SQL datado e procedimento documentado de restauração.

- [ ] Criar testes para ferramenta ausente, falha de dump e arquivo de saída válido.
- [ ] Confirmar RED antes do script existir.
- [ ] Implementar backup sem expor senha em argumentos ou logs.
- [ ] Cadastrar um registro de teste, encerrar, reabrir e comprovar que ele continua disponível.
- [ ] Executar build, testes de API, testes React e publicação completa; revisar o diff e verificar ausência de segredos.
