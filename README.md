# GameLife

Aplicativo desktop para organizar uma biblioteca de jogos, com janela WPF/WebView2, frontend React, API .NET 10 e persistência no MySQL.

## Usar no Windows

1. Confirme que o MySQL está instalado e iniciado.
2. Configure `ConnectionStrings:GameLife` nos User Secrets de `src\GameLife.Api\GameLife.Api.csproj`.
3. Execute `scripts\publish-local.ps1` para compilar, testar, instalar e criar o atalho `GameLife` na Área de Trabalho.
4. Abra o GameLife pelo atalho. O aplicativo abrirá em uma janela própria, sem iniciar o navegador padrão.

Os dados ficam no MySQL e permanecem salvos depois de fechar o aplicativo ou reiniciar o computador. Fechar a janela encerra somente o GameLife; não apaga nem recria o banco. A configuração local fica em `%LOCALAPPDATA%\GameLife\appsettings.Local.json` e não deve ser adicionada ao Git.

Os arquivos do aplicativo ficam em `%LOCALAPPDATA%\GameLife\App`. Atualizar o aplicativo substitui apenas essa pasta e preserva a configuração e os dados.

O Microsoft Edge WebView2 Runtime deve estar instalado. Ele já acompanha normalmente o Windows 10/11 e é usado apenas para renderizar a interface dentro da janela do GameLife.

## Backup

Execute `scripts\backup-mysql.ps1`. O arquivo SQL será criado em `%LOCALAPPDATA%\GameLife\Backups` com data e hora no nome.

Para restaurar, use o MySQL Workbench ou o cliente `mysql` apontando para o banco configurado. Faça um novo backup antes de qualquer restauração.

## Desenvolvimento

Depois de clonar o repositório, restaure as dependências com `dotnet restore GameLife.slnx` e `npm ci`, executado dentro de `src\GameLife.Web`.

- API: `dotnet run --project src\GameLife.Api\GameLife.Api.csproj`
- Testes .NET: `dotnet test GameLife.slnx`
- Frontend: execute `npm run dev` dentro de `src\GameLife.Web`
- Testes do frontend: execute `npm test` dentro de `src\GameLife.Web`
- Publicação completa: `scripts\publish-local.ps1`
