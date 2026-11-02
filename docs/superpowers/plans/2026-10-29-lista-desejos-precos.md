# Lista de Desejos e Histórico de Preços - Plano de Implementação

> **Para agentes executores:** SUB-HABILIDADE OBRIGATÓRIA: use `superpowers:executing-plans` para implementar este plano tarefa por tarefa. As etapas usam caixas de seleção para acompanhamento.

**Objetivo:** Adicionar ao GameLife uma Lista de Desejos em cartões, com flag automática de posse, preços manuais por loja e histórico imutável persistido no MySQL.

**Arquitetura:** Os modelos existentes `ItemListaDesejos`, `RegistroPreco`, `Canal`, `Jogo` e `PosseJogo` serão projetados por um controller agregado orientado à tela. O React receberá dados prontos para exibição, enquanto cada atualização de preço será um novo `POST`, preservando o histórico sem alterar o caminho desktop ou o uso futuro como site.

**Stack:** ASP.NET Core 10, EF Core/MySQL, React 19, Vite/Vitest e WPF/WebView2 para publicação desktop.

**Especificação:** `docs/superpowers/specs/2026-10-29-lista-desejos-precos-design.md`

## Restrições globais

- Todos os desejos e preços permanecem no MySQL atual.
- Nenhum `PUT` de preço será usado pela nova interface; cada atualização cria um novo registro.
- A flag de posse usa `JogoId` e `PossesJogos`, nunca comparação de título no frontend.
- A exclusão de um desejo não remove o jogo ou sua posse na biblioteca.
- A interface precisa ser testável pelo usuário sem IDE ou terminal.
- A Lista de Desejos funciona tanto no executável desktop quanto na futura hospedagem web.

## Foco da revisão

- Dois títulos iguais com diferenças de caixa ou espaços devem reutilizar o mesmo jogo, sem duplicar desejo.
- Dois registros de preço na mesma loja e mesmo instante devem escolher o maior `Id` como atual.
- Lojas indisponíveis não entram no menor preço atual, mas seus registros continuam no histórico.
- Excluir desejo com histórico deve preservar o jogo da biblioteca.
- Falha de rede ao salvar preço deve manter o formulário e mostrar erro, sem apresentar atualização falsa.

---

### Tarefa 1: Projetar os cartões da lista de desejos

**Arquivos:**
- Modificar: `gamelife-api/Controllers/ItensListaDesejosController.cs`
- Criar: `gamelife-api/Contracts/ListaDesejosContracts.cs`
- Criar: `gamelife-api.Tests/Api/ListaDesejosEndpointsTests.cs`

**Interfaces:**
- Produz: `GET /api/lista-desejos -> IReadOnlyList<ItemListaDesejosResponse>`.
- Produz: `POST /api/lista-desejos` com `CriarItemListaDesejosRequest(Titulo, PrecoDesejado, Observacao)`.
- Produz: `PUT /api/lista-desejos/{id}` com `AtualizarItemListaDesejosRequest(PrecoDesejado, Observacao)`.
- Produz: `DELETE /api/lista-desejos/{id}` preservando `Jogo` e `PosseJogo`.

- [ ] **Etapa 1: escrever testes de criação e posse automática**

Criar `CriarDesejoReutilizaJogoDaBibliotecaESinalizaPosse` e `CriarDesejoNormalizaTituloEImpedeDuplicidade`, cobrindo título com espaços/caixa diferentes, `JaTenho=true` e resposta `409 Conflict` para desejo repetido.

- [ ] **Etapa 2: executar e confirmar falha**

Executar: `dotnet test gamelife-api.Tests/gamelife-api.Tests.csproj --filter ListaDesejosEndpointsTests`

Esperado: falhas porque o contrato agregado ainda não aceita título nem retorna a projeção.

- [ ] **Etapa 3: criar contratos tipados**

Definir requests e responses com `Id`, `JogoId`, `Titulo`, `JaTenho`, `PrecoDesejado`, `Observacao`, `CriadoEm`, `PrecosAtuais`, `MenorPrecoAtual` e `HistoricoPrecos`.

- [ ] **Etapa 4: implementar criação transacional por título**

Normalizar com `Trim()` e comparação sem diferenciar caixa; reutilizar `Jogo` existente ou criar um novo; impedir segundo desejo para o mesmo `JogoId`; salvar jogo e desejo em uma transação.

- [ ] **Etapa 5: implementar projeção da lista**

Calcular `JaTenho` por `PossesJogos.Any`, ordenar cartões por título e preparar preços atuais/histórico; o cálculo final de mínimos será concluído na tarefa 2.

- [ ] **Etapa 6: proteger exclusão**

Excluir apenas `ItemListaDesejos`; confirmar por teste que `GET /api/jogos/{jogoId}` e a biblioteca permanecem disponíveis.

- [ ] **Etapa 7: executar suíte da API**

Executar: `dotnet test gamelife-api.slnx --verbosity minimal`

Esperado: todos os testes aprovados.

- [ ] **Etapa 8: criar commit**

`git commit -m "feat: criar cartões da lista de desejos"`

### Tarefa 2: Acrescentar preços imutáveis e comparações históricas

**Arquivos:**
- Modificar: `gamelife-api/Controllers/ItensListaDesejosController.cs`
- Modificar: `gamelife-api/Contracts/ListaDesejosContracts.cs`
- Modificar: `gamelife-api.Tests/Api/ListaDesejosEndpointsTests.cs`

**Interfaces:**
- Consome: contratos e itens agregados da tarefa 1.
- Produz: `POST /api/lista-desejos/{id}/precos` com `AdicionarPrecoDesejoRequest(CanalId, NomeCanal, PrecoAtual, PrecoCheio, Disponivel)`.
- Produz: preço atual por loja, menor atual disponível e menor histórico por loja.

- [ ] **Etapa 1: escrever testes de histórico imutável**

Criar `AdicionarPrecoMantemRegistrosAnterioresEDestacaMenorHistorico`, com três cotações na mesma loja e assertivas de três itens no histórico, última cotação como atual e menor valor histórico preservado.

- [ ] **Etapa 2: escrever testes de disponibilidade e desempate**

Criar cenários em que loja indisponível não participa do menor atual e registros com mesmo `ConsultadoEm` usam maior `Id` como atual.

- [ ] **Etapa 3: executar e confirmar falhas**

Executar: `dotnet test gamelife-api.Tests/gamelife-api.Tests.csproj --filter ListaDesejosEndpointsTests`

Esperado: falha porque a rota agregada de preço e os cálculos ainda não existem.

- [ ] **Etapa 4: implementar criação ou reutilização de loja**

Se `CanalId` existir, validar `PermitePreco`; se `NomeCanal` for informado, reutilizar nome normalizado ou criar `Canal` com `PermitePreco=true` e `PermitePosse=false`.

- [ ] **Etapa 5: implementar registro somente por acréscimo**

Criar sempre novo `RegistroPreco` com `ConsultadoEm=DateTime.UtcNow`; nunca atualizar ou remover cotações anteriores nesse fluxo.

- [ ] **Etapa 6: implementar cálculos e ordenação**

Agrupar por `CanalId`; escolher atual por `ConsultadoEm desc, Id desc`; calcular mínimo histórico apenas entre registros disponíveis; calcular menor atual apenas entre lojas atualmente disponíveis; retornar histórico global em ordem decrescente.

- [ ] **Etapa 7: validar SQL e suíte completa**

Executar: `dotnet test gamelife-api.slnx --verbosity minimal`

Esperado: todos os testes aprovados e nenhuma avaliação client-side inesperada nas consultas principais.

- [ ] **Etapa 8: criar commit**

`git commit -m "feat: preservar histórico manual de preços"`

### Tarefa 3: Criar a tela de cartões no React

**Arquivos:**
- Criar: `D:/Projetos/gamelife-web/src/screens/WishlistScreen.jsx`
- Criar: `D:/Projetos/gamelife-web/src/screens/WishlistScreen.test.jsx`
- Criar: `D:/Projetos/gamelife-web/src/api/wishlist.js`
- Modificar: `D:/Projetos/gamelife-web/src/App.jsx`
- Modificar: `D:/Projetos/gamelife-web/src/App.css`
- Modificar: `D:/Projetos/gamelife-web/src/App.test.jsx`

**Interfaces:**
- Consome: endpoints agregados das tarefas 1 e 2.
- Produz: menu `Lista de Desejos`, formulário de desejo, cartões, flag `Já tenho`, formulário de preço e histórico expansível.

- [ ] **Etapa 1: escrever testes da navegação e estado vazio**

Testar que o menu abre `Lista de Desejos`, chama `GET /api/lista-desejos` e mostra orientação quando a lista está vazia.

- [ ] **Etapa 2: escrever teste do cartão e flag de posse**

Usar resposta completa da API e confirmar título, `Já tenho`, preço desejado, menor atual, menor histórico e nomes das lojas.

- [ ] **Etapa 3: escrever testes de cadastro e nova cotação**

Confirmar payloads dos dois `POST`, atualização do cartão após sucesso e manutenção do formulário com mensagem após falha de rede.

- [ ] **Etapa 4: executar e confirmar falhas**

Executar: `npm.cmd test -- --run src/screens/WishlistScreen.test.jsx src/App.test.jsx`

Esperado: falha porque tela, API client e menu ainda não existem.

- [ ] **Etapa 5: implementar cliente de API**

Criar funções `listarDesejos`, `criarDesejo`, `adicionarPreco`, `atualizarDesejo` e `excluirDesejo`, preservando mensagens de erro retornadas pela API.

- [ ] **Etapa 6: implementar `WishlistScreen`**

Separar estados de carregamento, erro, vazio e conteúdo; formatar moeda com `Intl.NumberFormat('pt-BR', { currency: 'BRL' })`; manter histórico fechado por padrão e acessível por botão.

- [ ] **Etapa 7: integrar ao menu e responsividade**

Adicionar a nova tela entre Biblioteca e Coleções; estilizar cartões para desktop e uma coluna em largura reduzida; garantir labels e botões acessíveis por nome.

- [ ] **Etapa 8: executar testes e build do front**

Executar: `npm.cmd test -- --run` e `npm.cmd run build`.

Esperado: todos os testes aprovados e build Vite concluído.

- [ ] **Etapa 9: criar commit no repositório web**

`git commit -m "feat: adicionar tela de lista de desejos"`

### Tarefa 4: Publicar e validar no aplicativo desktop

**Arquivos:**
- Modificar apenas se validações revelarem defeito: arquivos das tarefas anteriores.

**Interfaces:**
- Consome: API e front das tarefas 1 a 3.
- Produz: `GameLife.exe` atualizado, atalho direto e dados persistentes no MySQL.

- [ ] **Etapa 1: executar backup do MySQL**

Executar: `scripts/backup-mysql.ps1` e registrar o caminho do SQL criado.

- [ ] **Etapa 2: executar publicação completa**

Executar: `scripts/publish-local.ps1`.

Esperado: testes .NET, Vitest, build Vite e publicação desktop aprovados.

- [ ] **Etapa 3: atualizar atalho**

Executar: `scripts/create-shortcut.ps1`; confirmar que continua apontando diretamente para `GameLife.exe`.

- [ ] **Etapa 4: validar fluxo real**

Abrir o executável, criar desejo de teste, adicionar duas cotações na mesma loja, fechar e reabrir; confirmar flag de posse quando aplicável, duas linhas no histórico e menor preço correto.

- [ ] **Etapa 5: limpar somente dados de teste**

Remover o desejo de teste pela interface após confirmação do usuário ou usar um banco isolado de teste; nunca apagar dados reais existentes.

- [ ] **Etapa 6: executar verificação final**

Executar `dotnet test gamelife-api.slnx -c Release`, `npm.cmd test -- --run`, `npm.cmd run build`, `Invoke-Pester scripts/tests` e `git diff --check` nos respectivos repositórios.

- [ ] **Etapa 7: solicitar revisão independente**

Revisar flag de posse, normalização de título, cálculos de mínimo, preservação do histórico, erros de rede e persistência após reabrir.
