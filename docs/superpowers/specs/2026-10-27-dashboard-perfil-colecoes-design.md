# Dashboard, perfil e coleções do GameLife

## Objetivo

Transformar a tela única em uma aplicação local com navegação lateral entre Início, Biblioteca, Coleções e Configurações, usando um perfil único persistido no MySQL.

## Escopo

- Perfil local único com nome, foto e preferências.
- Dashboard com totais, jogos recentes, troféus e conquistas.
- Conquistas persistidas e classificadas como `Trofeu` ou `Conquista`.
- Coleções personalizadas contendo jogos já cadastrados.
- Navegação interna React sem recarregar a página.
- Preservar biblioteca, executável, atalho e dados existentes.

## Fora do escopo

- Capturas de jogos, autenticação, múltiplos usuários e importação do Excel.

## Decisões

- O perfil terá ID fixo `1` e será criado automaticamente com valores iniciais.
- A foto será enviada como data URL, limitada a 2 MB, e persistida no MySQL.
- Coleções usam relação muitos-para-muitos com jogos.
- O dashboard será uma projeção somente leitura dos dados existentes.
