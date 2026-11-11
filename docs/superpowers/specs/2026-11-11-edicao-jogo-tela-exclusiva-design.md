# Edição de jogo em tela exclusiva

## Intenção

Ao clicar em **Editar ficha** na biblioteca, o GameLife deve substituir a grade por uma tela dedicada ao jogo escolhido. A tela exibe somente o título, o formulário completo e a ação **Voltar para a biblioteca**, evitando rolagem mesmo com milhares de jogos.

Na Biblioteca, a faixa de totais e a ação **Adicionar jogo** devem aparecer acima da busca, dos filtros e dos cards. Adicionar também abre uma tela exclusiva, mantendo a grade apenas para consulta.

O salvamento deve acrescentar plataformas à mesma ficha e retornar sucesso. O SQLite real não pode confirmar a gravação e depois responder com erro.

## Restrições

- Uma ficha por jogo, com várias plataformas.
- Nenhuma alteração, remoção ou recriação de `data/gamelife.db` durante testes e publicação.
- Plataformas existentes e acompanhamentos devem ser preservados.
- A biblioteca mantém cards, busca e filtros quando não está em edição.
- A faixa de progresso e o botão de adicionar ficam no topo da Biblioteca.
- A edição é uma tela interna do aplicativo; não será modal e não exigirá nova dependência de roteamento.

## Abordagens consideradas

- **Formulário abaixo da grade:** implementação atual, rejeitada porque exige rolagem proporcional ao tamanho da biblioteca.
- **Modal sobre a grade:** reduz a rolagem, mas não atende ao pedido de uma nova tela e limita formulários extensos.
- **Tela interna exclusiva:** escolhida. Reutiliza o estado de navegação atual, mostra somente o formulário de adicionar ou o jogo selecionado e oferece retorno explícito à biblioteca.

## Correção do salvamento

A resposta do jogo será consultada ainda dentro da transação SQLite ativa. Somente depois a transação será confirmada. Um teste com SQLite real impedirá a regressão que os testes em memória não detectaram.

