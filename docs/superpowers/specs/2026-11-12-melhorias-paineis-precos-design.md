# Melhorias de Início, Lista de Desejos, Jogos Concluídos e Retrospectiva

## Intenção

Deixar as quatro telas mais legíveis e úteis no uso diário, seguindo as referências visuais fornecidas, sem alterar a localização do banco SQLite nem perder dados existentes. A Lista de Desejos deve orientar a compra por fatos observáveis nas cotações, sem exigir que o usuário declare quanto gostaria de pagar.

## Objetivos

- Exibir percentuais com zero ou, quando necessário, uma casa decimal no formato `pt-BR`.
- Impedir que qualquer preenchimento visual ultrapasse os limites da barra de progresso.
- Remover o preço desejado do fluxo da Lista de Desejos.
- Preservar e apresentar o histórico de todas as cotações adicionadas, separado por loja.
- Sinalizar oportunidades por desconto, menor preço histórico e preço abaixo de R$ 20.
- Aproximar Jogos Concluídos e Retrospectiva da hierarquia visual das referências anexadas.

## Fora do escopo

- Consulta automática de preços em lojas.
- Notificações fora do aplicativo.
- Exclusão da coluna antiga `PrecoDesejado` ou migração destrutiva dos dados já salvos.
- Mudanças nos caminhos de publicação, atalho ou banco local.
- Alteração das regras de cadastro e edição dos acompanhamentos.

## Restrições

- `data/gamelife.db` continua sendo a única fonte de persistência do desktop.
- Nenhum teste pode escrever na Área de Trabalho ou no `AppData` real.
- A implementação deve usar os componentes React, CSS e APIs já existentes, sem dependência visual nova.
- Mudanças de comportamento serão cobertas por testes escritos antes da implementação.

## Abordagens consideradas

1. **Calcular as sinalizações no frontend a partir das cotações existentes.** Escolhida porque a API já retorna preço atual, preço cheio e menor preço histórico por loja. Evita migração, mantém o histórico imutável e concentra a mudança na apresentação.
2. **Adicionar sinalizações prontas ao contrato da API.** Centralizaria as regras, mas aumentaria o contrato e os testes de integração sem oferecer benefício para regras exclusivamente visuais nesta versão.
3. **Persistir o estado dos alertas no banco.** Rejeitada porque duplicaria informações derivadas dos preços e poderia deixar um alerta desatualizado após uma nova cotação.

## Experiência da tela Início

- Adicionar um painel de progresso geral com a mesma hierarquia da referência: título, contagens, barra segmentada, legenda e total.
- O percentual usa `pt-BR`, sem casa decimal para valores inteiros e com no máximo uma casa quando houver fração, por exemplo `67%` e `66,7%`.
- Valores usados em largura visual são limitados ao intervalo de 0% a 100%, mesmo que a origem contenha dados inconsistentes.
- A divisão considera jogos zerados e os demais jogos da biblioteca como em andamento. Quando não houver jogos, a barra permanece vazia e os percentuais são `0%`.
- A mesma formatação será aplicada aos percentuais visíveis de conquistas para manter consistência.

## Experiência da Lista de Desejos

- O formulário de novo desejo terá nome e observação; o campo “Preço que gostaria de pagar” será removido.
- A edição do cartão permitirá atualizar a observação, sem apresentar preço desejado.
- O campo antigo permanecerá no modelo e no contrato para compatibilidade, mas novos desejos serão enviados com valor nulo e valores antigos não serão apagados do banco.
- Cada nova cotação continuará criando um registro, nunca sobrescrevendo os anteriores.
- As lojas atuais serão apresentadas em linhas próprias, com preço atual, preço cheio quando informado, desconto calculado e acesso ao histórico daquela loja.
- O histórico será agrupado por loja e ordenado do registro mais recente para o mais antigo, incluindo data, disponibilidade, preço atual e preço cheio.
- Uma cotação disponível poderá exibir simultaneamente:
  - selo verde para desconto maior ou igual a 80%;
  - selo violeta para preço atual igual ao menor preço histórico da loja;
  - selo âmbar para preço estritamente menor que R$ 20.
- Cotações indisponíveis não recebem selos de oportunidade.
- O cartão e o resumo superior deixam de usar preço desejado como critério.

## Experiência de Jogos Concluídos

- Manter o formulário e todas as ações existentes, mas separar visualmente cadastro e consulta com melhor hierarquia e espaçamento.
- A listagem usará uma grade responsiva de fichas, inspirada na referência: ícone, título, plataforma, status/data, horas, conquistas e barra de troféus.
- Jogos com várias plataformas continuam agrupados em uma única ficha, preservando a decisão existente de não repetir o mesmo título; cada plataforma terá sua seção e ações próprias.
- Cores de status permanecem semânticas: concluído em verde, em andamento em violeta/azul e dropado em vermelho.
- As barras de conquistas terão largura limitada a 100% e percentual formatado com no máximo uma casa decimal.

## Experiência da Retrospectiva

- Reorganizar o cabeçalho em um painel único com identificação, título, explicação, ações e seleção de período, seguindo a referência.
- Manter os modos mensal e anual e as ações de copiar e baixar PNG.
- Apresentar quatro indicadores compactos: tempo, conquistas, biblioteca ativa e destaque do período.
- Dar prioridade ao ranking de jogos mais jogados, com posição, status, plataforma, conquistas, horas e participação no tempo total.
- Manter as demais seções anuais existentes abaixo do conteúdo principal, reduzindo ruído visual e repetição.
- Não usar dados demonstrativos quando a API retornar vazio ou falhar; a tela deve mostrar estado vazio ou erro real para não confundir dados fictícios com dados do usuário.
- Períodos e rótulos devem ser derivados da data atual e dos registros disponíveis, sem ficar presos a novembro de 2026.

## Testes e validação

- Testes de componente para formatação inteira e fracionária de percentuais e limitação das barras.
- Testes da Lista de Desejos garantindo ausência do preço desejado, envio nulo, histórico agrupado e os três selos acumuláveis.
- Testes de Jogos Concluídos para a nova ficha, agrupamento multiplataforma e percentual limitado.
- Testes da Retrospectiva para dados reais, estado vazio, períodos dinâmicos e ranking.
- Testes completos do frontend e da solução .NET, além do build do frontend.
- Como não haverá mudança em persistência, publicação ou atalho, não será executada publicação local nem modificado o banco. A integridade do SQLite será conferida em modo somente leitura antes e depois da implementação.

## Critérios de aceite

- Nenhum percentual visível possui mais de uma casa decimal.
- Nenhuma barra de progresso ultrapassa visualmente seu trilho.
- É possível cadastrar um desejo sem preço desejado e esse campo não aparece na interface.
- Todas as cotações permanecem consultáveis por loja depois de novas inclusões.
- Uma mesma cotação pode mostrar os três selos quando satisfizer as três condições.
- Jogos Concluídos preserva cadastro, edição, exclusão e agrupamento multiplataforma com o novo layout.
- Retrospectiva usa apenas dados reais e mantém os modos mensal, anual, cópia e download.
- O banco local não é apagado, movido, recriado nem substituído.
