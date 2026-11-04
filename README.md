# GameLife

Aplicativo desktop para organizar uma biblioteca de jogos, com janela WPF/WebView2, frontend React, API .NET 10 e banco SQLite local.

## Usar no Windows

1. Execute `scripts\publish-local.ps1` para compilar, testar, instalar e criar o atalho `GameLife` na Área de Trabalho.
2. Abra o GameLife pelo atalho. O aplicativo abrirá em uma janela própria, sem iniciar o navegador padrão.

Os dados ficam em `data\gamelife.db`, na raiz do projeto, e permanecem salvos depois de fechar o aplicativo ou reiniciar o computador. Fechar a janela encerra somente o GameLife; não apaga nem recria o banco.

Os arquivos do aplicativo ficam em `artifacts\GameLife`, seguindo o mesmo modelo do FinBot. O atalho aponta diretamente para `artifacts\GameLife\GameLife.exe`, enquanto o banco permanece separado em `data`.

Quando encontra uma instalação antiga sem SQLite, o desktop copia uma única vez os dados do MySQL configurado para o banco local. Depois da cópia, o executável não depende do MySQL.

O Microsoft Edge WebView2 Runtime deve estar instalado. Ele já acompanha normalmente o Windows 10/11 e é usado apenas para renderizar a interface dentro da janela do GameLife.

## Backup

Feche o GameLife e copie `data\gamelife.db` para uma pasta segura. Para restaurar, recoloque a cópia na pasta `data` com o nome `gamelife.db`.

## Desenvolvimento

Depois de clonar o repositório, restaure as dependências com `dotnet restore GameLife.slnx` e `npm ci`, executado dentro de `src\GameLife.Web`.

- API: `dotnet run --project src\GameLife.Api\GameLife.Api.csproj` usando a configuração de banco desejada
- Testes .NET: `dotnet test GameLife.slnx`
- Frontend: execute `npm run dev` dentro de `src\GameLife.Web`
- Testes do frontend: execute `npm test` dentro de `src\GameLife.Web`
- Publicação completa: `scripts\publish-local.ps1`
