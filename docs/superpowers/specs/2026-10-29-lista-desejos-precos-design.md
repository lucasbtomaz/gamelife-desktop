# Lista de Desejos e Histórico Manual de Preços

## Intenção

Adicionar ao GameLife uma aba de Lista de Desejos adequada ao uso em aplicativo. Cada jogo aparece em um cartão, pode ter preços manuais em várias lojas e informa automaticamente se já existe na biblioteca. O usuário poderá testar e ajustar o fluxo sozinho depois da entrega.

## Experiência da tela

- Novo item lateral `Lista de Desejos` entre Biblioteca e Coleções.
- Cabeçalho com quantidade de desejos, quantidade já possuída e melhor oportunidade atual.
- Formulário para adicionar um jogo pelo nome, preço desejado e observação opcional.
- Se o título já existir no cadastro de jogos, o desejo reutiliza esse jogo; caso contrário, cria o jogo e o desejo juntos.
- Cada cartão mostra nome, flag `Já tenho` quando houver uma posse na biblioteca, observação, preço desejado, menor preço atual e lojas atualizadas.
- A flag não remove nem oculta o item da lista.
- O cartão permite informar manualmente loja, preço atual, preço cheio opcional e disponibilidade.
- Cada atualização cria um novo registro com data e hora. Nenhum preço anterior é sobrescrito.
- Uma área expansível mostra o histórico por loja em ordem cronológica, destacando o menor preço histórico e se o valor atual iguala ou supera esse mínimo.
- Lojas podem ser selecionadas entre canais existentes que aceitam preço ou criadas durante o cadastro de preço.
- Estados de carregamento, erro e lista vazia terão mensagens específicas para permitir teste autônomo.

## Regras de preço

- O preço atual de uma loja é o registro mais recente por `ConsultadoEm` e, em caso de empate, maior `Id`.
- O menor preço atual compara apenas lojas marcadas como disponíveis.
- O menor preço histórico considera todos os registros disponíveis daquele jogo e da loja correspondente.
- Atualizar um preço sempre executa `POST`; o `PUT` existente deixa de ser usado pela nova interface para preservar histórico.
- Valores são armazenados em `decimal(18,2)` e exibidos em reais no formato `pt-BR`.
- Preços negativos são rejeitados; zero é permitido para jogos gratuitos.

## Detecção de jogo já possuído

- A fonte da verdade é o vínculo `JogoId` com `PossesJogos`, não uma comparação solta feita pelo frontend.
- Ao criar um desejo pelo nome, a API procura título normalizado, ignorando maiúsculas, minúsculas e espaços nas extremidades.
- Se houver correspondência, reutiliza o `JogoId` existente. Isso torna a flag `Já tenho` automática.
- Se o usuário adicionar o jogo à biblioteca depois, a flag aparece na próxima leitura sem modificar o desejo.
- Títulos parecidos, mas diferentes, permanecem jogos distintos; não haverá correspondência aproximada nesta versão.

## Backend

- Criar um endpoint agregado próprio para a tela, mantendo compatibilidade com os controllers atuais.
- `GET /api/lista-desejos` retorna cartões já projetados com posse, preços atuais, mínimos históricos e histórico completo.
- `POST /api/lista-desejos` recebe título, preço desejado e observação, reutiliza ou cria `Jogo` e cria o desejo de forma transacional.
- `POST /api/lista-desejos/{id}/precos` recebe loja existente ou novo nome de loja e acrescenta um `RegistroPreco`.
- `PUT /api/lista-desejos/{id}` atualiza apenas preço desejado e observação.
- `DELETE /api/lista-desejos/{id}` remove o desejo e seu histórico, sem remover o jogo nem sua posse na biblioteca.
- A exclusão continuará disponível, mas deve exigir confirmação na interface.

## Persistência

- Todos os desejos, lojas e preços permanecem no MySQL atual.
- Fechar ou atualizar o aplicativo não remove o histórico.
- Não haverá armazenamento temporário no frontend nem substituição por SQLite.
- A implementação incluirá teste automatizado de criação, nova cotação, fechamento lógico da requisição e nova leitura do histórico.

## Abordagens consideradas

1. Registros de preço imutáveis em uma única tabela, projetando o preço atual na leitura. Escolhida porque aproveita o modelo existente e preserva naturalmente todo o histórico.
2. Tabela de preço atual separada do histórico. Rejeitada por duplicar estado e permitir divergência entre o valor atual e os registros antigos.
3. Mapa livre de preços no frontend. Rejeitada porque prejudica histórico, validação, comparação e persistência relacional.

## Fora desta versão

- Consulta automática de preços em sites.
- Importação da planilha.
- Conversão de moedas.
- Alertas e notificações de queda de preço.
- Correspondência aproximada ou automática entre títulos escritos de formas diferentes.

## Critérios de aceite

- Adicionar um desejo pelo nome funciona mesmo quando o jogo ainda não existe.
- Um jogo presente na biblioteca aparece com flag `Já tenho` sem sair da lista.
- Informar novo preço mantém todos os registros anteriores.
- O cartão mostra preço atual por loja e menor preço histórico corretamente.
- Fechar e reabrir o GameLife mantém desejos e histórico no MySQL.
- A interface permite testar cadastro, atualização e consulta sem IDE ou terminal.
