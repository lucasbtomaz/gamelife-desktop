# Lista de Desejos: plataformas, busca e origem dos preços

## Objetivo

Tornar uma Lista de Desejos com dezenas de jogos rápida de consultar e permitir registrar preços em qualquer loja ou plataforma utilizada pelo jogador, inclusive Xbox, Xbox One e Xbox Series S/X.

## Comportamento

- O cadastro de cotação aceitará qualquer canal informado. Se o canal já existir com `PermitePreco = false`, ele será aceito neste fluxo sem alterar suas permissões globais.
- A tela continuará sugerindo canais conhecidos, mas não limitará o texto às sugestões.
- Uma busca no topo filtrará imediatamente os cartões por título do jogo e pelos nomes das lojas/plataformas presentes nas cotações e no histórico.
- A busca ignorará maiúsculas, minúsculas e acentos. Sem resultados, a tela mostrará um estado vazio específico sem esconder o formulário de cadastro.
- “Menor atual” exibirá o valor e uma etiqueta com a loja ou plataforma da cotação disponível correspondente.
- “Menor histórico” exibirá o valor e uma etiqueta com a loja ou plataforma do registro correspondente.
- Em caso de empate no menor valor, a etiqueta mostrará os nomes empatados de forma compacta.
- O indicador global “melhor preço” será removido do cabeçalho. Permanecerão o total de desejos e a quantidade de jogos já adquiridos.
- Os dados e históricos existentes serão preservados; não haverá migração de banco.

## Abordagens consideradas

1. **Reutilizar os canais existentes e habilitar preços quando usados**: escolhida por preservar o modelo atual, aceitar variações de Xbox e evitar migração.
2. **Criar uma entidade separada de lojas**: separaria melhor loja e plataforma, mas exigiria migração e conversão do histórico atual.
3. **Salvar o nome da loja diretamente em cada preço**: permitiria texto livre, mas duplicaria nomes e enfraqueceria o agrupamento do histórico.

## Limites

- Não haverá alteração da estrutura do SQLite.
- A busca será local sobre os itens já carregados, adequada ao volume atual e sem novas chamadas à API.
- Esta mudança não incluirá ordenação avançada ou paginação.

## Critérios de aceite

- Cotações de Xbox, Xbox One e Xbox Series S/X são salvas mesmo quando o canal existia sem permissão de preço.
- A busca encontra jogos pelo título e pela loja/plataforma, inclusive sem acentos.
- Cada menor preço informa sua origem.
- O cabeçalho não exibe mais um preço global sem contexto.
- Testes de frontend e API cobrem os novos comportamentos e todo o conjunto existente continua aprovado.
