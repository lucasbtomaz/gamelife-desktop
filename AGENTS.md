# Regras permanentes do GameLife

Estas regras protegem o funcionamento local e os dados do usuário. Não altere nenhuma delas sem autorização explícita.

## Executável e atalho

- Replicar o modelo já usado pelo FinBot.
- `scripts/publish-local.ps1` publica em `artifacts/GameLife`.
- O atalho `GameLife.lnk` aponta diretamente para `artifacts/GameLife/GameLife.exe`.
- Não criar uma segunda instalação do executável dentro de `AppData`.
- Não usar caminhos de pacotes do Codex, `LocalCache`, pastas `sandbox` ou caminhos virtualizados.
- Ao recriar o atalho, remover o arquivo `.lnk` anterior antes de salvar o novo.

## Persistência

- O desktop usa somente o SQLite local em `%USERPROFILE%/AppData/Local/GameLife/data/gamelife.db`.
- Nunca apagar, recriar, substituir ou mover esse banco durante build, teste, publicação ou atualização.
- Os arquivos do executável e o banco devem permanecer em pastas separadas.
- O MySQL é apenas uma origem legada para importação única; não é dependência para abrir o desktop.
- Antes de qualquer mudança de persistência, criar backup recuperável e comparar as quantidades por tabela.

## Testes obrigatórios

- Testes automatizados nunca podem escrever na Área de Trabalho real nem no `AppData` real.
- A publicação deve falhar antes de alterar o executável quando build ou testes falharem.
- Após alterar publicação, atalho ou persistência, validar o ciclo real: abrir, conferir os dados, fechar normalmente e abrir novamente.
- Confirmar que o atalho continua apontando para `artifacts/GameLife/GameLife.exe`.
- Confirmar a integridade do SQLite e a preservação das quantidades antes de declarar conclusão.

## Segurança do repositório público

- Nunca versionar bancos, backups, planilhas pessoais, logs, configurações locais, credenciais ou caminhos absolutos do computador.
- Manter `artifacts`, `*.db`, `*.sql`, `appsettings.Local.json` e dados pessoais fora do Git.
