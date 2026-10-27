# GameLife

Aplicação local para organizar uma biblioteca de jogos, com frontend React, API .NET 10 e persistência no MySQL.

## Usar no Windows

1. Confirme que o MySQL está instalado e iniciado.
2. Configure `ConnectionStrings:GameLife` nos User Secrets do projeto da API.
3. Execute `scripts\publish-local.ps1` para compilar, testar e publicar o aplicativo.
4. Execute `scripts\create-shortcut.ps1` para criar o atalho `GameLife` na Área de Trabalho.
5. Abra o GameLife pelo atalho. A aplicação iniciará em segundo plano e abrirá no navegador padrão.

Os dados ficam no MySQL e permanecem salvos depois de fechar o navegador ou reiniciar o computador. A configuração local fica em `%LOCALAPPDATA%\GameLife\appsettings.Local.json` e não deve ser adicionada ao Git.

## Backup

Execute `scripts\backup-mysql.ps1`. O arquivo SQL será criado em `%LOCALAPPDATA%\GameLife\Backups` com data e hora no nome.

Para restaurar, use o MySQL Workbench ou o cliente `mysql` apontando para o banco configurado. Faça um novo backup antes de qualquer restauração.

## Desenvolvimento

- API: `dotnet run --project gamelife-api\gamelife-api.csproj`
- Testes da API: `dotnet test gamelife-api.slnx`
- Frontend: execute `npm start` em `D:\Projetos\gamelife-web`
- Testes do frontend: `npm test`
