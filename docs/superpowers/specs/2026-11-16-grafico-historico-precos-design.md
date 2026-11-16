# Gráfico do histórico de preços

## Intenção

Substituir a lista textual do histórico de preços da Lista de Desejos por um gráfico que permita comparar a evolução dos preços ao longo do tempo e identificar em qual plataforma e data ocorreu o menor valor.

## Comportamento

- O botão existente continua abrindo e ocultando o histórico.
- O histórico aberto exibe um gráfico SVG responsivo com todos os registros cadastrados, sem agrupamento ou descarte de pontos.
- Cada plataforma recebe uma linha e uma cor próprias, identificadas por uma legenda.
- O eixo horizontal representa a data da consulta e o vertical representa o preço em reais.
- Cada registro vira um ponto interativo. Ao passar o mouse ou navegar por teclado, o gráfico informa plataforma, preço e data completa.
- O menor preço histórico recebe destaque visual.
- Uma plataforma com apenas um registro aparece como um ponto, mesmo sem trecho de linha.
- Quando não houver registros, permanece uma mensagem de estado vazio.

## Restrições

- Usar React e SVG nativo, sem adicionar dependências.
- Reutilizar `historicoPrecos`, que já contém plataforma, preço e data.
- Não alterar API, SQLite, publicação, atalho ou dados existentes.
- Manter o visual e a interface em português.
- O gráfico deve funcionar em telas estreitas e permitir leitura por tecnologias assistivas.

## Abordagens consideradas

1. SVG nativo em React: escolhido por ser leve, responsivo e não aumentar as dependências do projeto.
2. Biblioteca de gráficos: descartada porque o conjunto de interações necessário é pequeno e não justifica uma dependência adicional.

## Fora do escopo

- Agrupar preços por dia, mês ou ano.
- Prever preços futuros.
- Buscar preços automaticamente.
- Alterar a forma de cadastrar registros.

## Validação

- Testar múltiplas plataformas, vários registros da mesma plataforma, ponto único, menor preço, tooltip e histórico vazio.
- Executar os testes completos do frontend e a publicação oficial antes da validação visual no aplicativo.
