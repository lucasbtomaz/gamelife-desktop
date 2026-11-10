# Restaurar a biblioteca visual do GameLife

## Intenção

Recuperar a experiência visual mostrada na referência do usuário sem criar dados fictícios nem esconder registros reais.

## Objetivos

- Exibir os 26 itens da biblioteca em cards responsivos.
- Adicionar busca por título, filtros por plataforma e opção “Apenas mídia física”.
- Mostrar plataforma, mídia, ano, horas, conquistas e status quando existirem no SQLite.
- Exibir “Não informado” quando um dado de acompanhamento ainda não tiver sido cadastrado.
- Substituir caracteres e emojis do menu por ícones SVG consistentes.
- Manter formulários de cadastro e demais abas funcionando.

## Dados

`data/gamelife.db` continua sendo a única fonte. O endpoint da biblioteca passará a combinar posse, jogo, canal e acompanhamento pelo par jogo/plataforma. Itens da lista de desejos sem posse continuam na aba Lista de Desejos, sem serem apresentados falsamente como biblioteca.

## Abordagens consideradas

1. **Cards com dados reais, escolhida:** ampliar a resposta da biblioteca e renderizar cards e filtros no frontend.
2. **Cards apenas visuais com valores simulados:** rejeitada porque repetiria a aparência da referência com informações falsas.
3. **Reverter somente o CSS:** rejeitada porque não recuperaria busca, filtros nem estrutura de cards.

## Comportamento visual

Os cards seguem a referência: cabeçalho colorido por plataforma, etiquetas de plataforma e mídia, título em destaque, métricas abaixo e status no rodapé. A busca e os filtros ficam acima da grade. Todos os ícones de navegação usam o componente SVG existente, sem símbolos dependentes da fonte do Windows.

## Critérios de aceite

- Busca filtra títulos sem diferenciar maiúsculas e minúsculas.
- Filtro de plataforma e mídia física funcionam em conjunto.
- Todos os itens retornados pela biblioteca aparecem sem filtro.
- Dados ausentes não viram zero ou informação inventada.
- O menu não contém símbolos Unicode usados como ícones.
- Testes frontend e .NET, build, publicação protegida e validação do SQLite passam.
