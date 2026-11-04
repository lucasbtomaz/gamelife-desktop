# Dashboard vivo da tela inicial

## Intenção

Transformar a tela inicial em um resumo vivo da biblioteca. Os indicadores devem refletir imediatamente os dados salvos quando um jogo for adicionado, receber uma plataforma, for marcado como zerado ou tiver horas e conquistas atualizadas.

## Escopo

- Salvar no perfil a data de entrada na plataforma. Para o perfil local existente, usar `29/09/2020`.
- Exibir o tempo desde a entrada em anos completos.
- Exibir jogos zerados, horas dos jogos zerados, total da biblioteca, conquistas possíveis, conquistas obtidas e percentual de conquistas.
- Exibir jogos zerados por plataforma e jogos da biblioteca por plataforma.
- Exibir o total de jogos zerados por ano, começando em 2024 e mantendo vinte anos visíveis; o intervalo cresce quando o ano atual ultrapassar 2043.
- Manter os cards, painéis, cores e responsividade já usados pelo GameLife. Os prints definem as informações e a organização, não a aparência literal da planilha.
- Preservar o perfil, jogos recentes e conquistas recentes que já existem na tela inicial.

## Fonte e cálculos

O endpoint `GET /api/dashboard/inicio` será a única fonte do resumo:

- `Tempo passado`: diferença em anos completos entre a data de entrada e a data atual.
- `Jogos zerados`: registros com `FinalizadoEm` preenchido.
- `Horas de zerados`: soma de `TempoEmHoras` apenas dos registros finalizados, exibida como horas totais, minutos e segundos, sem reiniciar após 24 horas.
- `Jogos na biblioteca`: jogos distintos que possuem uma posse cadastrada.
- `Conquistas possíveis/obtidas`: soma dos campos dos registros de jogos.
- `Percentual`: obtidas dividido por possíveis, com duas casas decimais; zero quando não houver conquistas possíveis.
- `Por plataforma`: biblioteca agrupada pelas posses e zerados agrupados pelos registros finalizados e pelo canal da posse.
- `Por ano`: registros finalizados agrupados pelo ano de `FinalizadoEm`.

As categorias principais do print serão sempre mostradas: AMAZON PRIME, BLIZZARD, EA, EPIC, GOG, NINTENDO, PSN, STEAM, UBISOFT, XBOX e PLAY STORE. Nomes atuais serão normalizados para essas categorias quando houver equivalência, como PlayStation para PSN, Xbox para XBOX e Nintendo Switch para NINTENDO. Plataformas sem equivalência, como PC ou Outra, aparecerão como linhas adicionais para não perder dados. O formulário da biblioteca receberá também as opções que ainda não existem.

## Abordagens consideradas

1. **Agregação no backend e renderização no React, escolhida:** mantém uma fonte única e facilita manutenção e testes.
2. Agregação parcial no React: exigiria várias chamadas e duplicaria regras de cálculo.
3. Valores fixos reproduzindo a planilha: não acompanharia as alterações da aplicação.

## Alterações técnicas

- Adicionar `DataEntrada` ao perfil e criar migration preservando o banco atual.
- Ampliar a resposta do dashboard com resumo, plataformas e anos.
- Criar testes de API para todos os cálculos e agrupamentos.
- Atualizar os testes React antes de criar os novos blocos visuais.
- Reorganizar a seção inicial em componentes próprios se isso reduzir o tamanho do `App.jsx`, sem alterar as outras telas.

## Fora do escopo

- Importar a planilha.
- Criar uma tela completa para editar registros de jogo.
- Alterar o visual das telas de biblioteca, desejos, coleções ou configurações.
