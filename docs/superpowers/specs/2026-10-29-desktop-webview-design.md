# GameLife Desktop com WebView2

## Intenção

Transformar o uso local do GameLife em um aplicativo Windows com janela própria, aberto por um executável e sem depender de uma aba do navegador.

## Requisitos

- O usuário abre o GameLife por um atalho e vê somente uma janela do aplicativo.
- A janela WPF usa WebView2 para renderizar o front React já existente.
- O executável inicia a API ASP.NET Core local de forma invisível e aguarda sua disponibilidade antes de carregar a interface.
- Fechar a janela encerra o servidor local iniciado pelo aplicativo.
- Todos os dados continuam persistidos no MySQL atual. Abrir, fechar, atualizar ou reiniciar o computador não pode limpar nem recriar as informações cadastradas.
- O front e o back continuam utilizáveis para a futura publicação como site.

## Abordagens consideradas

1. WPF/WebView2 com a API local embutida no mesmo aplicativo. Escolhida por preservar a arquitetura atual, o MySQL e o caminho futuro para o site com menor risco.
2. Ponte direta entre React e .NET, como a arquitetura interna do FinBot. Rejeitada nesta etapa porque exigiria substituir boa parte dos controllers HTTP.
3. Electron ou Tauri. Rejeitada porque acrescentaria outra plataforma e outro processo de empacotamento sem benefício relevante para o projeto atual em .NET.

## Arquitetura

- Novo projeto Windows `GameLife.Desktop`, WPF, .NET 10 e WebView2.
- A API expõe uma forma reutilizável de criação/configuração do `WebApplication` para ser hospedada pelo desktop.
- O desktop inicia a API apenas em `127.0.0.1`, usando uma porta local controlada.
- O WebView2 navega para essa origem local depois que `/health` responder como pronto.
- Navegação externa, pop-ups e permissões do navegador são bloqueados.
- A configuração do MySQL continua carregada do arquivo local já usado pelo inicializador.

## Persistência e segurança dos dados

- Nenhum banco temporário ou SQLite será introduzido.
- O desktop usa a mesma connection string do MySQL existente.
- Migrações não serão executadas para cada abertura sem necessidade.
- O fechamento aguarda o encerramento organizado da API; gravações concluídas permanecem no MySQL.
- Falhas de conexão exibem uma mensagem clara e não substituem dados por valores vazios.

## Critérios de aceite

- `GameLife.exe` abre uma janela desktop sem abrir o navegador padrão.
- O aplicativo carrega Início, Biblioteca, Coleções e Configurações.
- Um dado salvo permanece após fechar e reabrir o aplicativo.
- O atalho da área de trabalho aponta para o novo executável.
- Testes da API e do front continuam passando, e o projeto desktop compila e publica para Windows x64.
