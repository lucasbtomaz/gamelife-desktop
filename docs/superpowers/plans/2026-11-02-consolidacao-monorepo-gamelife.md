# Consolidação do monorepositório GameLife - Plano de implementação

> **Para agentes executores:** SUB-HABILIDADE OBRIGATÓRIA: use `superpowers:subagent-driven-development` (recomendado) ou `superpowers:executing-plans` para executar este plano tarefa por tarefa. As etapas usam caixas de seleção (`- [ ]`) para acompanhamento.

**Objetivo:** Consolidar API, frontend React e desktop WPF/WebView2 em um repositório público `Gamelife`, preservando históricos, banco MySQL, testes e a experiência de uso por atalho no Windows.

**Arquitetura:** A branch que já contém API e desktop será a base. O histórico do frontend será incorporado sem squash em `src/GameLife.Web`; depois os projetos serão movidos para `src/` e `tests/`. O build do React alimentará a publicação autocontida do desktop, que será instalada em `%LOCALAPPDATA%\GameLife\App` e aberta por um atalho na Área de Trabalho.

**Tecnologias:** .NET 10, ASP.NET Core, Entity Framework Core, MySQL, WPF, WebView2, React 19, Vite 8, Vitest, PowerShell/Pester, GitHub Actions e Gitleaks.

**Especificação:** `docs/superpowers/specs/2026-11-02-consolidacao-monorepo-gamelife-design.md`

## Restrições globais

- Preservar funcionalidades, migrations, testes, dados e históricos antes de remover qualquer item.
- Não alterar o visual nem o comportamento das telas durante a consolidação.
- Não versionar credenciais, conexão MySQL, banco, backup, planilha pessoal, foto, log, build, artefato ou caminho absoluto do computador.
- Não excluir nem arquivar os repositórios anteriores antes do aceite do novo repositório.
- Manter o MySQL atual; nenhuma etapa pode recriar ou limpar o banco.
- Usar commits pequenos, no imperativo e em português.
- Criar a branch de trabalho com prefixo `codex/`.

## Foco da revisão

- Origem com alterações locais: a migração deve parar e registrar o estado antes de copiar, sem descartar arquivos não commitados.
- Segredo ou caminho pessoal em qualquer commit: o primeiro push público deve ser bloqueado até o histórico ser saneado.
- `dist` ausente ou desatualizado: a publicação deve sempre executar o build do React antes do `dotnet publish`.
- MySQL indisponível ou configuração ausente: o aplicativo deve mostrar erro claro sem alterar o banco nem perder arquivos locais.
- Atualização local interrompida ou executável aberto: a instalação deve preservar a versão utilizável e nunca apagar configuração ou dados.

---

### Task 1: Criar a base local e unir os históricos

**Arquivos:**
- Criar no repositório consolidado: `docs/superpowers/specs/2026-11-02-consolidacao-monorepo-gamelife-design.md`
- Criar no repositório consolidado: `docs/superpowers/plans/2026-11-02-consolidacao-monorepo-gamelife.md`
- Criar no repositório consolidado: `scripts/migration-preflight.ps1`
- Criar no repositório consolidado: `scripts/tests/migration-preflight.Tests.ps1`

**Interfaces:**
- Consome: branches `feature-mapeando-rotas`, `codex/gamelife-desktop` e `feature-inicio` dos repositórios atuais.
- Produz: branch local `codex/consolidar-monorepo` com os históricos de backend, desktop e frontend acessíveis pelo Git.

- [ ] **Etapa 1: Escrever o teste de origem com alterações locais**

Criar `Assert-GameLifeSourceState([string] $RepositoryPath)` em `scripts/migration-preflight.ps1`. Em um repositório temporário no `TestDrive`, o teste deve aceitar `git status --porcelain` vazio e lançar uma exceção com o caminho da origem quando houver arquivo modificado ou não rastreado.

- [ ] **Etapa 2: Executar o teste e confirmar a falha**

```powershell
Invoke-Pester scripts/tests/migration-preflight.Tests.ps1
```

Resultado esperado: falha porque a função ainda não existe.

- [ ] **Etapa 3: Implementar e usar a verificação das origens**

Implementar a função sem modificar a origem. Executar `git status --short --branch`, `git remote -v`, `git branch --all` e `git worktree list` nas duas origens. Se a função rejeitar uma origem, parar e preservar o diff antes de continuar.

Resultado esperado: estado das origens conhecido e nenhuma alteração local descartada.

- [ ] **Etapa 4: Confirmar a ancestralidade da branch desktop**

Executar:

```powershell
git merge-base --is-ancestor feature-mapeando-rotas codex/gamelife-desktop
```

Resultado esperado: código de saída `0`. Se falhar, incorporar primeiro a branch do backend na branch de consolidação e revisar conflitos.

- [ ] **Etapa 5: Criar a cópia de trabalho de migração**

Clonar localmente o repositório da API para uma nova pasta `Gamelife`, selecionar `codex/gamelife-desktop` e criar `codex/consolidar-monorepo`. Não reutilizar nem mover as pastas de origem.

Resultado esperado: `git status --short --branch` mostra uma branch limpa e isolada.

- [ ] **Etapa 6: Incorporar o frontend com o histórico completo**

Adicionar o repositório web como remoto temporário, buscar `feature-inicio` e executar `git subtree add` sem `--squash` com prefixo `src/GameLife.Web`.

Resultado esperado: `git log --all --oneline -- src/GameLife.Web` contém o commit `22b1822` e seus ancestrais.

- [ ] **Etapa 7: Adicionar a especificação e este plano ao repositório consolidado**

Copiar os dois documentos para os caminhos declarados em **Arquivos**, sem incluir caminhos pessoais em seu conteúdo.

- [ ] **Etapa 8: Confirmar que as três linhas de histórico estão presentes**

Executar:

```powershell
git log --oneline --all -- src/GameLife.Web
git log --oneline --all -- GameLife.Desktop
git log --oneline --all -- gamelife-api
```

Resultado esperado: cada comando apresenta commits da origem correspondente.

- [ ] **Etapa 9: Executar o teste de preflight e fazer o commit da consolidação**

```powershell
Invoke-Pester scripts/tests/migration-preflight.Tests.ps1
```

Resultado esperado: teste aprovado.

```powershell
git add docs scripts/migration-preflight.ps1 scripts/tests/migration-preflight.Tests.ps1
git commit -m "docs: registrar consolidação do GameLife"
```

---

### Task 2: Organizar os projetos em `src` e `tests`

**Arquivos:**
- Mover: `gamelife-api/**` → `src/GameLife.Api/**`
- Renomear: `src/GameLife.Api/gamelife-api.csproj` → `src/GameLife.Api/GameLife.Api.csproj`
- Mover: `GameLife.Desktop/**` → `src/GameLife.Desktop/**`
- Mover: `gamelife-api.Tests/**` → `tests/GameLife.Api.Tests/**`
- Renomear: `tests/GameLife.Api.Tests/gamelife-api.Tests.csproj` → `tests/GameLife.Api.Tests/GameLife.Api.Tests.csproj`
- Renomear: `gamelife-api.slnx` → `GameLife.slnx`
- Modificar: `GameLife.slnx`
- Modificar: `src/GameLife.Api/GameLife.Api.csproj`
- Modificar: `src/GameLife.Desktop/GameLife.Desktop.csproj`
- Modificar: `tests/GameLife.Api.Tests/GameLife.Api.Tests.csproj`
- Modificar: testes ou scripts que usam os caminhos anteriores.

**Interfaces:**
- Consome: árvore combinada da Tarefa 1.
- Produz: `GameLife.slnx` com referências válidas para `src/GameLife.Api`, `src/GameLife.Desktop` e `tests/GameLife.Api.Tests`.

- [ ] **Etapa 1: Registrar um teste de estrutura que falha nos caminhos antigos**

Criar `scripts/tests/repository-layout.Tests.ps1` com testes que exigem os três projetos nos novos caminhos, rejeitam as pastas antigas e validam as referências listadas em `GameLife.slnx`.

- [ ] **Etapa 2: Executar o teste estrutural e confirmar a falha**

Executar:

```powershell
Invoke-Pester scripts/tests/repository-layout.Tests.ps1
```

Resultado esperado: falha porque API, desktop, testes e solução ainda usam caminhos antigos.

- [ ] **Etapa 3: Mover arquivos com `git mv`**

Mover somente as pastas rastreadas e preservar os nomes de namespaces existentes nesta etapa para evitar uma refatoração funcional misturada à mudança estrutural.

- [ ] **Etapa 4: Atualizar solução e referências de projeto**

Definir em `GameLife.slnx`:

```xml
<Project Path="src/GameLife.Api/GameLife.Api.csproj" />
<Project Path="src/GameLife.Desktop/GameLife.Desktop.csproj" />
<Project Path="tests/GameLife.Api.Tests/GameLife.Api.Tests.csproj" />
```

Atualizar o teste para referenciar `../../src/GameLife.Api/GameLife.Api.csproj` e `../../src/GameLife.Desktop/GameLife.Desktop.csproj`; atualizar o desktop para `../GameLife.Api/GameLife.Api.csproj`.

- [ ] **Etapa 5: Atualizar a origem dos recursos web**

Nos projetos da API e do desktop, usar `../GameLife.Web/dist/**/*` como origem de `wwwroot`, mantendo `CopyToOutputDirectory` e `CopyToPublishDirectory` como `PreserveNewest`.

- [ ] **Etapa 6: Compilar o frontend e executar as verificações estruturais**

```powershell
Push-Location src/GameLife.Web
npm.cmd ci
npm.cmd test
npm.cmd run build
Pop-Location
Invoke-Pester scripts/tests/repository-layout.Tests.ps1
dotnet restore GameLife.slnx
dotnet test GameLife.slnx --no-restore
```

Resultado esperado: testes do React, Pester e .NET aprovados.

- [ ] **Etapa 7: Commit da estrutura**

```powershell
git add GameLife.slnx src tests scripts/tests/repository-layout.Tests.ps1
git commit -m "refactor: organizar projetos no monorepositório"
```

---

### Task 3: Tornar build e publicação independentes de pastas externas

**Arquivos:**
- Modificar: `scripts/publish-local.ps1`
- Criar: `scripts/tests/publish-local.Tests.ps1`
- Modificar: `src/GameLife.Api/GameLife.Api.csproj`
- Modificar: `src/GameLife.Desktop/GameLife.Desktop.csproj`
- Modificar: `README.md`

**Interfaces:**
- Consome: `src/GameLife.Web/package.json`, `GameLife.slnx` e `src/GameLife.Desktop/GameLife.Desktop.csproj`.
- Produz: `Get-GameLifePublishPaths([string] $ProjectRoot) -> PSCustomObject` com `WebRoot`, `SolutionPath`, `DesktopProject` e `OutputPath`; `scripts/publish-local.ps1` gera `artifacts/GameLife/GameLife.exe`.

- [ ] **Etapa 1: Escrever testes dos caminhos de publicação**

Em `scripts/tests/publish-local.Tests.ps1`, importar o script com `-LibraryOnly` e afirmar que `Get-GameLifePublishPaths` resolve todos os caminhos dentro da raiz recebida, sem `gamelife-web` como pasta irmã e sem caminho absoluto fixo. Adicionar um teste que simula falha no build do React e comprova que testes .NET, publicação e instalação não são iniciados; adicionar outro que registra as chamadas e exige a ordem `npm ci`, `npm test`, `npm run build`, `dotnet restore`, `dotnet test`, `dotnet publish`.

- [ ] **Etapa 2: Executar o teste e confirmar a falha**

```powershell
Invoke-Pester scripts/tests/publish-local.Tests.ps1
```

Resultado esperado: falha porque a função e o parâmetro `LibraryOnly` ainda não existem.

- [ ] **Etapa 3: Implementar os caminhos internos**

Adicionar `param([switch] $LibraryOnly)` e `Get-GameLifePublishPaths`. Usar `src/GameLife.Web`, `GameLife.slnx`, `src/GameLife.Desktop/GameLife.Desktop.csproj` e `artifacts/GameLife` relativos à raiz.

- [ ] **Etapa 4: Preservar a ordem obrigatória da publicação**

Executar `npm ci`, `npm test`, `npm run build`, `dotnet restore`, `dotnet test` e `dotnet publish`, nessa ordem. Interromper no primeiro código de saída diferente de zero.

- [ ] **Etapa 5: Atualizar o README**

Documentar clone, User Secrets, MySQL, desenvolvimento, publicação e atalho usando somente caminhos relativos ao repositório.

- [ ] **Etapa 6: Executar testes e publicação completa**

```powershell
Invoke-Pester scripts/tests/publish-local.Tests.ps1
dotnet test GameLife.slnx
./scripts/publish-local.ps1
```

Resultado esperado: todos os testes aprovados e `artifacts/GameLife/GameLife.exe`, `wwwroot/index.html` e o ícone presentes.

- [ ] **Etapa 7: Commit da publicação**

```powershell
git add scripts src README.md
git commit -m "build: publicar todas as camadas pelo monorepositório"
```

---

### Task 4: Instalar localmente e preservar dados entre execuções

**Arquivos:**
- Criar: `scripts/install-local.ps1`
- Criar: `scripts/tests/install-local.Tests.ps1`
- Modificar: `scripts/create-shortcut.ps1`
- Modificar: `scripts/tests/create-shortcut.Tests.ps1`
- Modificar: `scripts/publish-local.ps1`
- Modificar: `src/GameLife.Desktop/LocalConfiguration.cs` apenas se a validação revelar incompatibilidade.

**Interfaces:**
- Consome: `artifacts/GameLife` e `%LOCALAPPDATA%\GameLife\appsettings.Local.json`.
- Produz: `Get-GameLifeInstallRoot() -> string`, `Install-GameLifeFiles([string] $Source, [string] $Destination)`, executável instalado em `%LOCALAPPDATA%\GameLife\App\GameLife.exe` e atalho `GameLife.lnk`.

- [ ] **Etapa 1: Escrever testes da instalação local**

Cobrir em `install-local.Tests.ps1`: destino dentro de `LocalApplicationData\GameLife\App`, cópia do executável e `wwwroot`, preservação de `appsettings.Local.json`, rejeição de origem sem `GameLife.exe` e falha segura quando o destino está bloqueado.

- [ ] **Etapa 2: Atualizar o teste do atalho**

Exigir que o atalho aponte diretamente para o executável instalado, sem PowerShell, argumentos ou navegador, com diretório de trabalho e ícone do próprio executável.

- [ ] **Etapa 3: Executar os testes e confirmar a falha**

```powershell
Invoke-Pester scripts/tests/install-local.Tests.ps1,scripts/tests/create-shortcut.Tests.ps1
```

Resultado esperado: falha porque o instalador local e o novo destino ainda não existem.

- [ ] **Etapa 4: Implementar a instalação recuperável**

Copiar a publicação primeiro para uma pasta temporária dentro de `%LOCALAPPDATA%\GameLife`, validar `GameLife.exe` e `wwwroot/index.html`, e somente então substituir `App`. Nunca incluir `appsettings.Local.json`, logs ou dados na pasta substituída. Se o aplicativo estiver aberto ou a troca falhar, manter a instalação anterior e mostrar instrução para fechá-lo.

- [ ] **Etapa 5: Criar ou atualizar o atalho**

Reutilizar `New-GameLifeShortcut`, apontando para `%LOCALAPPDATA%\GameLife\App\GameLife.exe`. Manter a conexão somente em `%LOCALAPPDATA%\GameLife\appsettings.Local.json`.

- [ ] **Etapa 6: Integrar publicação e instalação**

Após uma publicação bem-sucedida, chamar `install-local.ps1` e `create-shortcut.ps1`. Um build ou teste com falha não pode alterar a instalação atual.

- [ ] **Etapa 7: Executar testes focados e completos**

```powershell
Invoke-Pester scripts/tests/*.Tests.ps1
dotnet test GameLife.slnx
./scripts/publish-local.ps1
```

Resultado esperado: todos os testes aprovados, executável instalado e atalho criado.

Executar também o teste focado da configuração ausente:

```powershell
dotnet test GameLife.slnx --filter "FullyQualifiedName~LocalConfigurationTests.ObterConnectionStringRejeitaArquivoAusente"
```

Resultado esperado: aprovado, confirmando que a ausência do MySQL/configuração produz erro controlado antes de iniciar a aplicação.

- [ ] **Etapa 8: Validar persistência com o MySQL existente**

Executar `scripts/backup-mysql.ps1`, registrar as contagens atuais pelas APIs, abrir pelo atalho, fechar, abrir novamente e confirmar as mesmas contagens. Não criar nem excluir registro real durante o teste.

- [ ] **Etapa 9: Commit da instalação local**

```powershell
git add scripts src/GameLife.Desktop README.md
git commit -m "feat: instalar GameLife com atalho local"
```

---

### Task 5: Bloquear informações sensíveis e automatizar a integração contínua

**Arquivos:**
- Modificar: `.gitignore`
- Criar: `.gitleaks.toml`
- Criar: `.github/workflows/ci.yml`
- Criar: `scripts/tests/repository-safety.Tests.ps1`
- Modificar: arquivos históricos somente se a varredura encontrar informação real sensível.

**Interfaces:**
- Consome: arquivos rastreados e todo o histórico Git consolidado.
- Produz: verificações locais e CI que bloqueiam segredos, caminhos pessoais e artefatos locais.

- [ ] **Etapa 1: Escrever testes de segurança do repositório**

Em `repository-safety.Tests.ps1`, executar `git ls-files` e rejeitar: `appsettings.Local.json`, `.env*` com valores, `*.xlsx`, `*.xls`, `*.csv` pessoais, `*.sql`, `*.db`, `Backups/`, `artifacts/`, `dist/`, `build/`, `bin/`, `obj/`, logs e caminhos absolutos de perfis de usuário, projetos ou downloads locais.

- [ ] **Etapa 2: Executar o teste e registrar falhas reais**

```powershell
Invoke-Pester scripts/tests/repository-safety.Tests.ps1
```

Resultado esperado: falhar apenas para itens que precisam ser removidos ou ignorados; não alterar arquivos automaticamente.

- [ ] **Etapa 3: Completar o `.gitignore`**

Ignorar configurações locais, dados, dumps, planilhas, logs, caches, dependências, builds e artefatos. Manter apenas exemplos sem valores reais.

- [ ] **Etapa 4: Configurar varredura de segredos**

Adicionar configuração mínima do Gitleaks, permitindo somente valores comprovadamente fictícios dos testes. Executar:

```powershell
gitleaks git --redact --log-opts="--all"
```

Resultado esperado: código `0`. Se houver segredo real, revogar quando aplicável, removê-lo e reescrever somente o histórico necessário antes de continuar.

- [ ] **Etapa 5: Criar o workflow de CI**

Em `windows-latest`, usar Node 22 e .NET 10 para `npm ci`, `npm test`, `npm run build`, `dotnet restore`, `dotnet test` e `dotnet publish`. Em job separado, executar Gitleaks em todo o histórico disponível.

- [ ] **Etapa 6: Executar todas as verificações localmente**

```powershell
Invoke-Pester scripts/tests/*.Tests.ps1
Push-Location src/GameLife.Web
npm.cmd test
npm.cmd run build
Pop-Location
dotnet test GameLife.slnx
gitleaks git --redact --log-opts="--all"
```

Resultado esperado: todos os comandos aprovados e nenhum caminho ou segredo pessoal detectado.

- [ ] **Etapa 7: Commit da proteção pública**

```powershell
git add .gitignore .gitleaks.toml .github scripts/tests/repository-safety.Tests.ps1
git commit -m "ci: proteger publicação pública do GameLife"
```

---

### Task 6: Criar o repositório público e validar a entrega

**Arquivos:**
- Modificar: nenhum arquivo de produto; somente configuração de remoto Git e estado externo do GitHub.

**Interfaces:**
- Consome: branch saneada `codex/consolidar-monorepo` com todas as verificações aprovadas.
- Produz: repositório público `lucasbtomaz/Gamelife`, branch `main`, CI aprovada e aplicativo local validado.

- [ ] **Etapa 1: Executar a validação final antes de qualquer push público**

Executar novamente Pester, Vitest, build Vite, testes .NET, publicação Release e Gitleaks. Conferir `git status --short` e `git diff --check`.

Resultado esperado: árvore limpa, nenhum erro e nenhuma descoberta de segurança.

- [ ] **Etapa 2: Criar o repositório público vazio**

Criar `lucasbtomaz/Gamelife` como público, sem README, licença ou `.gitignore` gerados pelo GitHub, para evitar conflito de históricos.

- [ ] **Etapa 3: Preservar o remoto antigo e configurar o novo**

Renomear o remoto atual para `legacy-api`, remover o remoto temporário do frontend e definir `origin` como o novo repositório.

- [ ] **Etapa 4: Publicar a branch principal**

Renomear a branch consolidada para `main` e enviar somente depois da aprovação das verificações locais.

Resultado esperado: o GitHub mostra os históricos das três origens e a estrutura consolidada.

- [ ] **Etapa 5: Aguardar e verificar a CI**

Resultado esperado: jobs de frontend, .NET, publicação Windows e segurança aprovados.

- [ ] **Etapa 6: Fazer o teste de uso real**

Abrir pelo atalho da Área de Trabalho, conferir biblioteca e lista de desejos atuais, fechar, abrir novamente e confirmar que o conteúdo permanece. Conferir que nenhum navegador ou terminal fica aberto.

- [ ] **Etapa 7: Entregar sem excluir os legados**

Apresentar URL do novo repositório, resultado dos testes, local do executável, local do backup e contagens preservadas. Solicitar confirmação separada antes de arquivar ou excluir `gamelife-api` e `gamelife-web`.

---

## Planos posteriores

Depois desta entrega estar validada, criar planos independentes para:

1. separar `App.jsx` e `App.css` em telas, layouts, componentes e estilos sem alterar o visual;
2. implementar a importação transacional da planilha com pré-visualização, mapeamento e deduplicação;
3. pesquisar e projetar conectores oficiais para bibliotecas externas.

