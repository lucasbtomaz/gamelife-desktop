# GameLife local pelo navegador

## Objetivo

Permitir que o GameLife seja iniciado por um atalho na Área de Trabalho, sem IDE ou terminal visível, com interface aberta no navegador e dados persistidos no MySQL local.

## Escopo

- Corrigir o build atual da API.
- Manter a credencial `ConnectionStrings:GameLife` fora do Git.
- Fazer a API servir o build do frontend React e seus endpoints na mesma origem local.
- Alinhar a tela atual de biblioteca aos contratos existentes da API.
- Publicar a API como executável Windows autocontido.
- Criar scripts de publicação, atalho e backup do banco.
- Validar que um registro persiste após encerrar e abrir novamente o aplicativo.

## Fora do escopo

- Lista de desejos e importação do Excel.
- Janela WPF/WebView2.
- Instalação automática do MySQL.
- Autenticação ou publicação na internet.

## Decisão

A API ASP.NET Core hospedará os arquivos estáticos produzidos pelo React e abrirá a URL local no navegador padrão. O executável trabalhará apenas em `127.0.0.1`, usará uma configuração local de conexão copiada para a pasta de dados do usuário e manterá o MySQL como fonte persistente.

## Alternativas consideradas

- WPF com WebView2: aparência de aplicativo tradicional, mas adiciona uma camada descartável para a futura versão web.
- Processos separados para API e servidor React: mais componentes em execução e maior risco de portas, origens e encerramento divergirem.

## Segurança e operação

- Nenhuma senha será versionada ou incorporada no executável.
- O instalador local solicitará ou reutilizará a conexão atual e armazenará a configuração somente no perfil do usuário.
- O aplicativo falhará com uma mensagem clara se o MySQL estiver indisponível.
- O backup será explícito e produzirá um arquivo datado fora do repositório.
