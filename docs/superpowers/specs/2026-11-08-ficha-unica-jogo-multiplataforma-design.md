# Ficha única por jogo com múltiplas plataformas

## Intenção

O GameLife deve representar cada título uma única vez na Biblioteca. Um jogo pode estar disponível em várias plataformas, e cada vínculo com uma plataforma pode ter mídia física e acompanhamento próprios. O cadastro precisa permitir informar tudo em uma única operação, sem obrigar o usuário a repetir o título.

O caso real que expôs o problema é `Graveyard Keeper`: o banco já contém um único `Jogo` ligado a Steam e Xbox, mas a API e a interface devolvem uma linha e um card por posse, dando aparência de jogo duplicado.

## Objetivos

- Exibir uma única ficha por jogo na Biblioteca.
- Permitir selecionar uma ou mais plataformas no mesmo cadastro.
- Guardar mídia física separadamente para cada plataforma.
- Permitir, por plataforma, acompanhamento opcional com status, data, horas e conquistas.
- Reutilizar o jogo existente quando o título normalizado já estiver cadastrado.
- Atualizar um vínculo jogo/plataforma existente em vez de criar posse duplicada.
- Preservar todos os dados atuais e continuar usando somente `data/gamelife.db`.

## Não objetivos

- Unificar progresso entre plataformas diferentes.
- Buscar automaticamente metadados ou conquistas em serviços externos.
- Alterar a origem ou a localização do SQLite.
- Apagar jogos, posses ou acompanhamentos existentes durante a migração.

## Comportamento da interface

O formulário “Adicionar novo jogo” terá:

- nome do jogo;
- ano de lançamento opcional;
- uma lista de plataformas selecionáveis;
- para cada plataforma escolhida, opção independente de mídia física;
- para cada plataforma escolhida, seção opcional “Acompanhar progresso”;
- dentro do acompanhamento: status, data, horas, conquistas possíveis e conquistas obtidas, todos opcionais, com status padrão `NaoTerminado` quando a seção for ativada.

O envio será único. Se o título já existir, as novas plataformas serão anexadas à ficha existente. Se uma plataforma já estiver ligada ao jogo, os campos informados atualizarão essa posse e seu acompanhamento, sem criar outro registro.

Plataformas já ligadas ao jogo e ausentes do novo pedido serão preservadas. Em uma plataforma existente, omitir o acompanhamento preserva o acompanhamento atual; ausência não significa exclusão. Ano informado atualiza o jogo, enquanto ano omitido preserva o valor existente.

A ficha da Biblioteca mostrará o título e o ano apenas uma vez. Dentro dela haverá uma linha ou bloco por plataforma com mídia física/digital e os dados de acompanhamento disponíveis. Campos ausentes continuarão exibindo “Não informado” ou “Sem acompanhamento”.

Busca e filtros trabalharão sobre a ficha agrupada: pesquisar encontra o título; filtrar por plataforma mantém fichas que possuam aquela plataforma; “Apenas mídia física” mantém fichas com pelo menos uma posse física e destaca apenas as plataformas compatíveis com os filtros.

## Contrato da API

`GET /biblioteca` passará a retornar jogos agrupados:

- `jogoId`, `titulo`, `anoLancamento`;
- `plataformas[]`, contendo `posseId`, `canalId`, `nome`, `midiaFisica`;
- acompanhamento opcional da combinação jogo/plataforma: `id`, `status`, `dataStatus`, `tempoEmHoras`, `conquistasPossiveis` e `conquistasObtidas`.

`POST /biblioteca` receberá:

- `titulo` obrigatório;
- `anoLancamento` opcional;
- `plataformas[]` com pelo menos um item;
- em cada item: nome da plataforma, mídia física e acompanhamento opcional.

O endpoint executará toda a operação em uma transação. Títulos e plataformas serão comparados pela chave normalizada já usada no acompanhamento. Plataformas repetidas no mesmo pedido serão rejeitadas com erro de validação. Conquistas obtidas sem total possível, ou acima do total, também serão rejeitadas.

A resposta será `201 Created` quando uma nova ficha for criada e `200 OK` quando uma ficha existente for apenas ampliada ou atualizada.

## Modelo e integridade

O modelo atual já tem as relações necessárias:

- `Jogo`: identidade única conceitual do título;
- `PosseJogo`: vínculo do jogo com uma plataforma e informação de mídia física;
- `AcompanhamentoJogo`: progresso da combinação jogo/plataforma.

Não será criada uma nova tabela. Será acrescentada proteção de unicidade para `PosseJogo(JogoId, CanalId)`, equivalente à proteção já existente em `AcompanhamentoJogo`, tanto no mapeamento do Entity Framework quanto no atualizador manual do SQLite usado pelo desktop. Antes de qualquer alteração de esquema, o fluxo atual criará backup SQLite consistente com WAL. Se forem detectadas posses históricas repetidas, a atualização deve parar com mensagem clara e manter o banco intacto; não haverá exclusão automática.

Os dados existentes serão apenas reprojetados pelo novo contrato agrupado. `Graveyard Keeper`, por exemplo, continuará sendo o jogo 34, agora com Steam e Xbox dentro da mesma ficha.

## Totais e telas relacionadas

Todos os totais rotulados como “jogos”, incluindo Biblioteca, dashboard e barra lateral, contarão `JogoId` distinto, não posses. Distribuições por plataforma continuam contando vínculos jogo/plataforma. Resumos de acompanhamento continuam sendo calculados por plataforma, pois status e conquistas podem diferir entre elas. O status interno `NaoTerminado` será apresentado como “Em andamento”.

A tela “Jogos concluídos” continuará editando acompanhamentos por plataforma, mas agrupará visualmente as plataformas sob um único título para manter a regra de uma ficha por jogo em toda a aplicação.

## Tratamento de falhas

- Nenhuma plataforma selecionada: rejeitar sem gravar.
- Plataforma repetida no mesmo pedido: rejeitar sem gravar.
- Mesmo título e mesma plataforma já existentes: atualizar, não duplicar.
- Falha em qualquer plataforma ou acompanhamento: reverter toda a transação.
- Valores de conquistas inconsistentes: rejeitar sem gravar.
- Banco com posses duplicadas históricas: criar backup, interromper a atualização e informar o problema sem apagar registros.

## Testes e validação

- Teste de API cadastrando um jogo com duas plataformas em uma única chamada.
- Teste de API repetindo título e plataforma e confirmando atualização sem duplicidade.
- Teste de API garantindo rollback integral quando um item é inválido.
- Teste de projeção agrupada com mídia e progresso diferentes por plataforma.
- Testes de interface para seleção múltipla, mídia física individual e acompanhamento opcional.
- Testes da ficha única, busca e filtros sobre plataformas agrupadas.
- Teste do agrupamento da tela de acompanhamentos.
- Suíte completa, publicação protegida, integridade do SQLite e comparação das contagens antes/depois.
- Ciclo real: abrir, conferir `Graveyard Keeper` em uma única ficha com Steam e Xbox, fechar normalmente e reabrir.

## Abordagens consideradas

1. **Agrupar apenas no frontend.** Menor alteração, mas mantém a API ambígua, exige vários POSTs e permite operações parciais. Rejeitada.
2. **Agrupar no contrato da Biblioteca e salvar todas as plataformas em uma transação.** Escolhida porque expressa a regra no backend, evita duplicidade e permite cadastro atômico.
3. **Substituir `PosseJogo` por uma nova estrutura.** Rejeitada porque o modelo atual já representa corretamente a relação e uma migração ampla aumentaria o risco para os dados locais.

## Restrições permanentes

- Nunca apagar, recriar, substituir ou mover `data/gamelife.db`.
- Nunca usar `AppData` como banco ativo.
- Publicação e testes não podem escrever em dados pessoais fora do repositório.
- Alterações de persistência exigem backup recuperável, integridade SQLite e comparação de quantidades.
