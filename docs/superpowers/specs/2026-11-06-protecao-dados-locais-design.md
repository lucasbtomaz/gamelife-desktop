# Proteção dos dados locais do GameLife

## Intenção

Garantir que atualizações de código, testes e publicações nunca façam o GameLife abrir um banco diferente nem perder registros já persistidos.

## Objetivos

- Usar exclusivamente `data/gamelife.db`, na raiz do repositório, como banco ativo.
- Criar backup recuperável e validar integridade e contagens antes de migrações ou publicação.
- Mesclar bancos legados de modo idempotente, incluindo acompanhamentos de jogos, sem duplicar registros.
- Detectar a instalação antiga em `AppData` e impedir que ela continue causando gravações em outro banco.
- Verificar após a publicação o ciclo abrir, salvar, fechar e reabrir.

## Fora de escopo

- Inventar qual era o segundo jogo concluído que não chegou a nenhum banco ou backup.
- Voltar a usar `AppData` como local do banco ou do executável.
- Alterar dados pessoais sem backup e comparação prévia.

## Restrições

- O banco atual não pode ser apagado, substituído ou movido.
- Backups, bancos, logs e caminhos locais permanecem fora do Git.
- Testes usam somente diretórios temporários.
- A publicação deve falhar antes de substituir o executável quando qualquer validação falhar.

## Abordagens consideradas

1. **Proteção completa, escolhida:** banco canônico na raiz, backup e preflight, mesclagem completa dos legados e desativação recuperável da instalação antiga.
2. **Correção mínima:** somente desativar o executável antigo. Evita parte da confusão, mas não protege futuras migrações.
3. **Retornar o banco para `AppData`:** rejeitada porque recria o risco de virtualização e múltiplos bancos e viola as regras permanentes do projeto.

## Desenho

O fluxo de preparação do SQLite fará uma cópia de segurança antes de qualquer atualização de esquema ou mesclagem. A mesclagem legada passará a incluir todas as entidades persistentes, especialmente `AcompanhamentosJogos`, identificando registros por jogo e plataforma e preservando os dados mais completos sem duplicação. O fluxo de publicação executará um preflight do banco, registrará contagens, publicará somente depois de build e testes aprovados e comparará integridade e contagens no fim.

A instalação antiga em `AppData\Local\GameLife\App` será movida para uma pasta de backup recuperável, após nova conferência do destino. O atalho continuará apontando apenas para `artifacts\GameLife\GameLife.exe`.

## Critérios de aceite

- Teste reproduz a coexistência de banco canônico e legado e comprova mesclagem idempotente de acompanhamentos.
- Publicação aborta antes de alterar o executável se o banco estiver inconsistente ou se testes falharem.
- Contagens do banco atual não diminuem após migração ou publicação.
- Atalho aponta para `artifacts\GameLife\GameLife.exe`.
- Aplicativo abre, preserva uma gravação de teste após fechamento normal e reabertura e usa o mesmo banco.
