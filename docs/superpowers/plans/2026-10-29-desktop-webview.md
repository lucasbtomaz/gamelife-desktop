# GameLife Desktop com WebView2 - Plano de Implementação

> **Para agentes executores:** SUB-HABILIDADE OBRIGATÓRIA: use `superpowers:executing-plans` para implementar este plano tarefa por tarefa. As etapas usam caixas de seleção para acompanhamento.

**Objetivo:** Entregar um `GameLife.exe` para Windows que abra uma janela própria, hospede a API localmente e preserve todos os dados no MySQL depois de fechar e reabrir.

**Arquitetura:** Um novo projeto WPF inicia a aplicação ASP.NET Core dentro do mesmo processo, aguarda o endpoint `/health` e navega um WebView2 para a origem local. A API continua servindo o React compilado e usando a mesma connection string do MySQL, preservando o caminho de publicação futura como site.

**Stack:** .NET 10, WPF, Microsoft Edge WebView2, ASP.NET Core/Kestrel, React/Vite, EF Core e MySQL.

**Especificação:** `docs/superpowers/specs/2026-10-29-desktop-webview-design.md`

## Restrições globais

- O aplicativo é exclusivo para Windows x64 nesta primeira versão desktop.
- O MySQL atual permanece como fonte persistente; não criar banco temporário nem SQLite.
- O executável não pode abrir o navegador padrão.
- O servidor deve escutar somente em `127.0.0.1` e ser encerrado junto com a janela.
- O front e os controllers HTTP permanecem compatíveis com futura hospedagem como site.
- O atalho deve iniciar diretamente `GameLife.exe`, sem PowerShell intermediário.

## Foco da revisão

- MySQL indisponível ao abrir: mostrar erro claro e não apresentar dados vazios como se fossem reais.
- Porta local ocupada: usar porta dinâmica em loopback, sem conflito com outras instâncias.
- WebView2 Runtime ausente: explicar o requisito e encerrar de forma controlada.
- Navegação externa, pop-ups e permissões: bloquear tudo que não pertença à origem local iniciada pelo aplicativo.
- Fechamento durante uma requisição: aguardar encerramento organizado da API sem apagar dados já confirmados no MySQL.

---

### Tarefa 1: Tornar a aplicação web hospedável pelo desktop

**Arquivos:**
- Criar: `gamelife-api/GameLifeApplication.cs`
- Modificar: `gamelife-api/Program.cs`
- Modificar: `gamelife-api/gamelife-api.csproj`
- Modificar: `gamelife-api.slnx`
- Testar: `gamelife-api.Tests/Hosting/GameLifeApplicationTests.cs`

**Interfaces:**
- Produz: `GameLifeApplication.Criar(string[] args, Action<WebApplicationBuilder>? configurarBuilder = null) -> WebApplication`.
- Produz: assembly da API `GameLife.Api`, evitando conflito com o executável desktop `GameLife.exe`.

- [ ] **Etapa 1: escrever teste que cria a aplicação pela fábrica**

O teste `CriarConfiguraControllersEHealth` deve fornecer configuração de teste, iniciar em loopback com porta dinâmica e confirmar HTTP 200 em `/health`.

- [ ] **Etapa 2: executar o teste e confirmar falha**

Executar: `dotnet test gamelife-api.Tests/gamelife-api.Tests.csproj --filter GameLifeApplicationTests`

Esperado: falha porque `GameLifeApplication` ainda não existe.

- [ ] **Etapa 3: extrair a criação/configuração de `Program.cs`**

Implementar `GameLifeApplication.Criar(...)`; manter `Program.cs` apenas como entrada que cria e executa a aplicação. Preservar controllers, CORS, arquivos estáticos e fallback do React.

- [ ] **Etapa 4: renomear o assembly da API e incluir o desktop na solução**

Alterar `<AssemblyName>` para `GameLife.Api`; a inclusão do projeto desktop será concluída na tarefa seguinte.

- [ ] **Etapa 5: executar testes da API**

Executar: `dotnet test gamelife-api.Tests/gamelife-api.Tests.csproj`

Esperado: todos os testes aprovados.

- [ ] **Etapa 6: criar commit**

`git commit -m "refactor: permitir hospedagem da API no desktop"`

### Tarefa 2: Criar a janela WPF e o ciclo de vida do servidor

**Arquivos:**
- Criar: `GameLife.Desktop/GameLife.Desktop.csproj`
- Criar: `GameLife.Desktop/App.xaml`
- Criar: `GameLife.Desktop/App.xaml.cs`
- Criar: `GameLife.Desktop/MainWindow.xaml`
- Criar: `GameLife.Desktop/MainWindow.xaml.cs`
- Criar: `GameLife.Desktop/LocalConfiguration.cs`
- Criar: `GameLife.Desktop/Assets/GameLife.ico`
- Modificar: `gamelife-api.slnx`
- Testar: `gamelife-api.Tests/Desktop/LocalConfigurationTests.cs`

**Interfaces:**
- Consome: `GameLifeApplication.Criar(...)` da tarefa 1.
- Produz: `LocalConfiguration.ObterConnectionString() -> string`.
- Produz: `MainWindow` que gerencia `WebApplication` e `WebView2` durante toda a sessão.

- [ ] **Etapa 1: escrever testes da configuração persistente**

Testar leitura de `%LOCALAPPDATA%/GameLife/appsettings.Local.json`, rejeição de configuração ausente e preservação literal da connection string existente.

- [ ] **Etapa 2: executar e confirmar falha**

Executar: `dotnet test gamelife-api.Tests/gamelife-api.Tests.csproj --filter LocalConfigurationTests`

Esperado: falha porque `LocalConfiguration` ainda não existe.

- [ ] **Etapa 3: criar o projeto WPF/WebView2**

Configurar `OutputType=WinExe`, `TargetFramework=net10.0-windows`, `UseWPF=true`, `AssemblyName=GameLife`, referência ao projeto da API e pacote `Microsoft.Web.WebView2` na mesma versão usada pelo FinBot.

- [ ] **Etapa 4: implementar leitura da configuração local**

Implementar `LocalConfiguration.ObterConnectionString()` usando o arquivo já criado pelo instalador atual, sem gravar, limpar ou substituir o banco.

- [ ] **Etapa 5: implementar inicialização da API em porta dinâmica**

No carregamento da janela, definir a connection string somente para o processo, criar a aplicação, configurar `http://127.0.0.1:0`, chamar `StartAsync`, obter a URL real em `IServerAddressesFeature` e validar `/health` antes de navegar.

- [ ] **Etapa 6: configurar e restringir o WebView2**

Usar cache em `%LOCALAPPDATA%/GameLife/webview`; desabilitar ferramentas de desenvolvedor, menus e barra de status; bloquear navegação fora da origem local, novas janelas e permissões.

- [ ] **Etapa 7: implementar fechamento organizado**

No fechamento, impedir reentrada, descartar o WebView2 e executar `StopAsync`/`DisposeAsync` da aplicação antes de liberar a janela.

- [ ] **Etapa 8: tratar falhas de inicialização**

Exibir caixa de diálogo em português para MySQL indisponível, assets ausentes ou WebView2 Runtime ausente; nunca abrir o navegador como fallback.

- [ ] **Etapa 9: executar testes e build desktop**

Executar: `dotnet test gamelife-api.slnx` e `dotnet build GameLife.Desktop/GameLife.Desktop.csproj -c Release`

Esperado: testes aprovados e build sem erros.

- [ ] **Etapa 10: criar commit**

`git commit -m "feat: adicionar aplicativo desktop com WebView2"`

### Tarefa 3: Publicar um único aplicativo e atualizar o atalho

**Arquivos:**
- Modificar: `scripts/publish-local.ps1`
- Modificar: `scripts/create-shortcut.ps1`
- Remover do fluxo: uso de `scripts/start-local.ps1` pelo atalho
- Testar: `scripts/tests/publish-local.Tests.ps1`
- Testar: `scripts/tests/create-shortcut.Tests.ps1`
- Modificar: `README.md`

**Interfaces:**
- Consome: `GameLife.Desktop.csproj` da tarefa 2.
- Produz: `artifacts/GameLife/GameLife.exe` e atalho `GameLife.lnk` apontando diretamente para ele.

- [ ] **Etapa 1: ajustar testes dos scripts**

Exigir que a publicação use o projeto desktop e que o atalho tenha `TargetPath=.../GameLife.exe`, sem `powershell.exe`, argumentos de script ou abertura de navegador.

- [ ] **Etapa 2: executar os testes e confirmar falha**

Executar: `Invoke-Pester scripts/tests/publish-local.Tests.ps1,scripts/tests/create-shortcut.Tests.ps1`

Esperado: falha porque os scripts ainda usam o inicializador web.

- [ ] **Etapa 3: atualizar publicação**

Manter `npm ci`, testes e build do Vite; restaurar/testar a solução; publicar `GameLife.Desktop` como `win-x64`, autocontido, em `artifacts/GameLife` e copiar os assets React necessários pela referência da API.

- [ ] **Etapa 4: atualizar criação do atalho**

Preservar a criação do arquivo de configuração local e mudar o atalho para executar diretamente `GameLife.exe`, com diretório de trabalho no artefato e ícone do aplicativo.

- [ ] **Etapa 5: atualizar documentação**

Explicar que o GameLife abre em janela própria, depende do MySQL local e do WebView2 Runtime, e que os dados permanecem no MySQL depois do fechamento.

- [ ] **Etapa 6: executar testes dos scripts**

Executar: `Invoke-Pester scripts/tests/publish-local.Tests.ps1,scripts/tests/create-shortcut.Tests.ps1`

Esperado: todos aprovados.

- [ ] **Etapa 7: criar commit**

`git commit -m "build: publicar GameLife como aplicativo desktop"`

### Tarefa 4: Validar persistência e experiência final

**Arquivos:**
- Modificar apenas se a validação revelar defeito: arquivos das tarefas anteriores.

**Interfaces:**
- Consome: executável e atalho das tarefas anteriores.
- Produz: versão local validada e pronta para uso diário.

- [ ] **Etapa 1: executar verificação automatizada completa**

Executar: `npm.cmd test -- --run`, `npm.cmd run build`, `dotnet test gamelife-api.slnx -c Release` e `dotnet publish GameLife.Desktop/GameLife.Desktop.csproj -c Release -r win-x64 --self-contained true`.

- [ ] **Etapa 2: criar backup antes do teste real**

Executar `scripts/backup-mysql.ps1` e registrar o arquivo produzido.

- [ ] **Etapa 3: atualizar o atalho e abrir o aplicativo**

Executar `scripts/create-shortcut.ps1`; confirmar janela com título GameLife e ausência de uma nova aba no navegador padrão.

- [ ] **Etapa 4: validar persistência real**

Criar um registro de teste pela interface, fechar a janela, reabrir pelo atalho e confirmar que o registro continua visível. Remover somente o registro de teste pela própria interface se houver suporte; caso contrário, mantê-lo claramente identificado para o usuário remover.

- [ ] **Etapa 5: revisar processos e segurança local**

Confirmar que fechar a janela encerra o processo e a porta local, que apenas loopback foi usado e que navegações externas foram bloqueadas.

- [ ] **Etapa 6: revisar diff e estado dos repositórios**

Executar `git diff --check`, revisar mudanças incidentais e preservar `.gemini/`, `AGENTS.md` e `GEMINI.md` não rastreados.

- [ ] **Etapa 7: criar commit final somente se houver correções**

`git commit -m "fix: estabilizar inicialização do aplicativo desktop"`
