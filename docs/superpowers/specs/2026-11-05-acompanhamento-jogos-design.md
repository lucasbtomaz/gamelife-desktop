# Acompanhamento de jogos e resumo da Biblioteca

## Intenção

Permitir que o usuário acompanhe jogos zerados, abandonados ou ainda não terminados, inclusive títulos acessados por assinatura e que não fazem parte da Biblioteca. Exibir na Biblioteca uma barra resumida com total acompanhado e a distribuição entre esses três estados.

## Objetivos

- Criar uma nova aba de acompanhamento de jogos.
- Cadastrar nome, plataforma, status, data, tempo jogado, conquistas possíveis e conquistas obtidas.
- Permitir editar e excluir acompanhamentos já cadastrados.
- Calcular e exibir automaticamente o percentual de conquistas obtidas.
- Oferecer a opção `Adicionar à Biblioteca`; quando marcada, criar também a posse do jogo na Biblioteca.
- Mostrar na Biblioteca uma barra com total acompanhado, zerados, dropados e não terminados.
- Preservar integralmente o banco SQLite local e os registros existentes.

## Fora do escopo

- Consultar automaticamente catálogos de assinaturas ou serviços externos.
- Importar horas ou conquistas de Steam, PlayStation, Xbox ou outras plataformas.
- Manter uma lista individual com o nome de cada conquista.
- Alterar o conceito atual de posse usado pela Biblioteca.

## Restrições

- O desktop continuará usando exclusivamente `data/gamelife.db`.
- A mudança de esquema será aditiva, por migração do Entity Framework, sem apagar ou recriar tabelas.
- Antes da validação real, será criado backup recuperável do banco e serão comparadas as quantidades por tabela.
- Testes não poderão escrever no banco real, na Área de Trabalho real ou no `AppData` real.

## Abordagens consideradas

### Escolhida: acompanhamento independente da Biblioteca

Criar uma entidade própria de acompanhamento ligada a jogo e plataforma, com vínculo opcional a uma posse da Biblioteca. Um jogo de assinatura pode ser acompanhado sem ser tratado como posse. Quando `Adicionar à Biblioteca` estiver marcado, a API cria ou reutiliza jogo e plataforma, cria a posse e vincula o acompanhamento a ela.

Vantagens: mantém o significado correto dos dados, permite jogos de assinatura e evita esconder posses artificiais. Custo: exige uma nova tabela, novos endpoints e uma nova tela.

### Descartada: usar posse com uma flag de visibilidade

Todo jogo acompanhado seria uma posse, mas os não marcados ficariam ocultos na Biblioteca. Exige menos mudanças, porém mistura jogos possuídos com jogos apenas acessados por assinatura e torna consultas futuras ambíguas.

## Modelo e regras

Cada acompanhamento terá:

- jogo;
- plataforma;
- status: `NaoTerminado`, `Zerado` ou `Dropado`;
- data do status, opcional;
- tempo em horas, opcional e não negativo;
- conquistas possíveis, opcional e não negativo;
- conquistas obtidas, opcional, não negativo e nunca maior que as possíveis;
- posse da Biblioteca, opcional.

O percentual será calculado pela API e novamente apresentado pela interface como `(obtidas / possíveis) × 100`, arredondado para o inteiro mais próximo. Quando o total possível for zero ou não estiver preenchido, o percentual será `0%`.

Se conquistas obtidas forem informadas, conquistas possíveis também serão obrigatórias. O valor obtido nunca poderá ser maior que o possível.

Poderá existir um acompanhamento para cada combinação de jogo e plataforma. O mesmo jogo poderá aparecer em várias plataformas, inclusive como zerado em mais de uma delas, mas não haverá dois acompanhamentos do mesmo jogo na mesma plataforma. A edição será usada para atualizar status, data, horas e conquistas desse registro.

A data representará quando o jogo recebeu o status atual. A interface usará o rótulo `Data da conclusão` para zerados, `Data do abandono` para dropados e `Data de atualização` para não terminados.

Ao marcar `Adicionar à Biblioteca`, a API criará uma posse digital, pois o formulário de acompanhamento não solicita tipo de mídia. Se já houver na Biblioteca uma posse do mesmo jogo e plataforma, ela será reutilizada para evitar duplicidade.

## Interface

A navegação lateral ganhará a aba `Jogos concluídos`, reunindo também os dropados e não terminados. A tela terá um formulário e uma lista dos acompanhamentos cadastrados, seguindo o padrão visual atual da Biblioteca e da Lista de Desejos. Cada item terá ações para editar e excluir.

O formulário conterá nome do jogo, plataforma, status, data, tempo em horas, conquistas possíveis, conquistas obtidas e a opção `Adicionar à Biblioteca`. A lista exibirá os dados principais, um selo de status e uma barra de progresso das conquistas.

Na Biblioteca, acima da grade principal, será exibida uma barra segmentada semelhante à referência enviada. O primeiro bloco mostrará o total acompanhado; os demais representarão proporcionalmente zerados, dropados e não terminados, sempre com números e rótulos para não depender apenas das cores. Em telas estreitas, os blocos poderão quebrar em linhas preservando a leitura.

## API e persistência

- Nova tabela de acompanhamentos com chaves estrangeiras para jogo, canal e posse opcional.
- Restrição única para a combinação de jogo e plataforma.
- Endpoints para listar, cadastrar, editar e excluir acompanhamentos.
- Endpoint de resumo com as quatro contagens usadas pela barra da Biblioteca.
- Criação de acompanhamento e eventual posse ocorrerão na mesma operação de banco, evitando gravação parcial.
- As consultas serão sem rastreamento quando forem somente leitura.

## Validação e testes

- Testes da API para cadastro independente, cadastro com inclusão na Biblioteca, reutilização de posse existente, edição, exclusão, duplicidade por jogo e plataforma e rejeição de conquistas inválidas.
- Testes do resumo para todas as categorias e para base vazia.
- Testes da interface para cálculo/exibição do percentual, envio da flag e renderização da barra.
- Build e suítes completas de .NET e React.
- Após a migração e publicação, validar abertura, leitura dos dados, fechamento normal e segunda abertura; confirmar o destino do atalho e a integridade/contagens do SQLite.

## Decisões deixadas ao implementador

- Nomes internos de classes, contratos e arquivos, respeitando as convenções atuais.
- Organização dos componentes React para evitar ampliar ainda mais o `App.jsx`.
- Paleta exata da barra, mantendo contraste acessível e coerência com o tema atual.
