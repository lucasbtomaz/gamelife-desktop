# Acompanhamento de jogos e resumo da Biblioteca - Plano de implementação

> **Para agentes executores:** SUB-SKILL OBRIGATÓRIA: usar `superpowers:subagent-driven-development` (recomendado) ou `superpowers:executing-plans` para executar este plano tarefa por tarefa. Os passos usam caixas de seleção (`- [ ]`) para acompanhamento.

**Objetivo:** Criar uma aba para acompanhar jogos por plataforma e status, calcular conquistas e exibir na Biblioteca uma barra com total, zerados, dropados e não terminados.

**Arquitetura:** Uma nova entidade `AcompanhamentoJogo` será ligada a `Jogo` e `Canal`, com vínculo opcional a `PosseJogo`. A API concentrará validação, percentual, unicidade e criação opcional da posse; a interface React usará um componente de tela próprio e um componente reutilizável para o resumo.

**Stack:** .NET 10, ASP.NET Core, Entity Framework Core, SQLite, React 19, Vite, Vitest e Testing Library.

**Especificação:** `docs/superpowers/specs/2026-11-05-acompanhamento-jogos-design.md`

## Restrições globais

- Usar somente o SQLite local `data/gamelife.db`; não apagar, recriar, substituir ou mover esse arquivo.
- A migração deve ser aditiva e manter intactas as tabelas e contagens existentes.
- Testes não podem escrever na Área de Trabalho real nem no `AppData` real.
- Jogos acompanhados só entram na Biblioteca quando `adicionarBiblioteca` estiver marcado.
- O mesmo jogo pode ser acompanhado em plataformas diferentes, mas a combinação jogo + plataforma deve ser única.
- Se `conquistasObtidas` for informado, `conquistasPossiveis` é obrigatório e deve ser maior ou igual ao valor obtido.
- Não adicionar dependências externas.

## Foco da revisão

- Cadastro repetido com diferenças de maiúsculas ou espaços deve ser reconhecido como a mesma combinação jogo + plataforma.
- Marcar Biblioteca quando já existe posse equivalente deve reutilizá-la sem duplicação.
- Desmarcar Biblioteca durante uma edição não deve apagar uma posse existente nem dados do usuário.
- Percentual com zero, campos vazios e valores fracionários deve permanecer previsível e sem divisão por zero.
- Excluir acompanhamento não deve excluir jogo, plataforma ou posse associados.

---

### Tarefa 1: Modelo persistente e migração aditiva

**Arquivos:**
- Criar: `src/GameLife.Api/Models/AcompanhamentoJogo.cs`
- Modificar: `src/GameLife.Api/Context/GameLifeDbContext.cs`
- Criar: `src/GameLife.Api/Migrations/<timestamp>_AdicionarAcompanhamentosJogos.cs`
- Criar: `src/GameLife.Api/Migrations/<timestamp>_AdicionarAcompanhamentosJogos.Designer.cs`
- Modificar: `src/GameLife.Api/Migrations/GameLifeDbContextModelSnapshot.cs`
- Criar: `tests/GameLife.Api.Tests/Models/AcompanhamentoJogoTests.cs`

**Interfaces:**
- Produz: entidade `AcompanhamentoJogo`, enum `StatusAcompanhamentoJogo` e `DbSet<AcompanhamentoJogo> AcompanhamentosJogos`.
- Produz: índice único normalizado por `JogoId` e `CanalId`; `PosseJogoId` anulável com exclusão restrita.

- [ ] **Passo 1: escrever testes de modelo que falham**

Criar testes para os três valores do enum, campos obrigatórios, valores não negativos, vínculo opcional com posse e exclusão do acompanhamento sem cascata para jogo, canal ou posse.

- [ ] **Passo 2: executar os testes focados e confirmar a falha**

Executar: `dotnet test tests/GameLife.Api.Tests/GameLife.Api.Tests.csproj --filter FullyQualifiedName~AcompanhamentoJogoTests`

Esperado: falha de compilação porque `AcompanhamentoJogo` ainda não existe.

- [ ] **Passo 3: implementar o modelo mínimo**

Criar `StatusAcompanhamentoJogo { NaoTerminado, Zerado, Dropado }` e `AcompanhamentoJogo` com `Id`, `JogoId`, `CanalId`, `PosseJogoId`, `Status`, `DataStatus`, `TempoEmHoras`, `ConquistasPossiveis` e `ConquistasObtidas`.

- [ ] **Passo 4: configurar relações e gerar migração**

Adicionar o `DbSet`, relações com `DeleteBehavior.Restrict` e índice único `(JogoId, CanalId)`; gerar a migração com `dotnet ef migrations add AdicionarAcompanhamentosJogos --project src/GameLife.Api --startup-project src/GameLife.Api`.

- [ ] **Passo 5: executar os testes focados e confirmar aprovação**

Executar o comando do Passo 2.

Esperado: todos os testes de `AcompanhamentoJogoTests` aprovados.

- [ ] **Passo 6: revisar a migração**

Confirmar que contém somente criação da nova tabela, índices e chaves estrangeiras; não pode conter `DropTable`, remoção de coluna ou alteração destrutiva.

- [ ] **Passo 7: commit pequeno**

Commit: `feat: adicionar modelo de acompanhamento de jogos`

### Tarefa 2: API de cadastro, edição, exclusão e resumo

**Arquivos:**
- Criar: `src/GameLife.Api/Contracts/AcompanhamentoJogoContracts.cs`
- Criar: `src/GameLife.Api/Controllers/AcompanhamentosJogosController.cs`
- Criar: `tests/GameLife.Api.Tests/Api/AcompanhamentosJogosEndpointsTests.cs`

**Interfaces:**
- Consome: `AcompanhamentoJogo`, `StatusAcompanhamentoJogo` e `GameLifeDbContext.AcompanhamentosJogos` da Tarefa 1.
- Produz: `GET /api/acompanhamentos-jogos`, `POST /api/acompanhamentos-jogos`, `PUT /api/acompanhamentos-jogos/{id}`, `DELETE /api/acompanhamentos-jogos/{id}` e `GET /api/acompanhamentos-jogos/resumo`.
- Produz: resposta com `id`, `jogoId`, `titulo`, `canalId`, `plataforma`, `status`, `dataStatus`, `tempoEmHoras`, `conquistasPossiveis`, `conquistasObtidas`, `percentualConquistas` e `estaNaBiblioteca`.

- [ ] **Passo 1: escrever testes de endpoints que falham**

Cobrir listagem vazia, cadastro sem posse, cadastro com criação de posse digital, reutilização de posse, mesmo jogo em plataformas diferentes, rejeição da mesma combinação com caixa/espaço diferentes, validações de conquistas, edição dos campos, inclusão posterior na Biblioteca, desmarcação sem apagar posse, exclusão sem cascata e resumo das três categorias.

- [ ] **Passo 2: executar os testes focados e confirmar a falha**

Executar: `dotnet test tests/GameLife.Api.Tests/GameLife.Api.Tests.csproj --filter FullyQualifiedName~AcompanhamentosJogosEndpointsTests`

Esperado: respostas 404 porque os endpoints não existem.

- [ ] **Passo 3: definir contratos exatos**

Criar `SalvarAcompanhamentoJogoRequest` com `Titulo`, `Plataforma`, `Status`, `DataStatus`, `TempoEmHoras`, `ConquistasPossiveis`, `ConquistasObtidas` e `AdicionarBiblioteca`; criar `AcompanhamentoJogoResponse` e `ResumoAcompanhamentosResponse`.

- [ ] **Passo 4: implementar leitura e cálculo**

Listar ordenando por título e plataforma. Calcular `PercentualConquistas` como zero quando possíveis for nulo/zero e, nos demais casos, `Math.Round(obtidas * 100m / possíveis, 0, MidpointRounding.AwayFromZero)`.

- [ ] **Passo 5: implementar gravação transacional**

Normalizar título e plataforma com `Trim()` e comparação sem distinção de caixa; reutilizar ou criar `Jogo` e `Canal`; rejeitar combinação existente no POST; no PUT preservar o próprio registro; criar/reutilizar `PosseJogo` digital apenas quando solicitado; nunca apagar posse ao desmarcar.

- [ ] **Passo 6: implementar exclusão e resumo**

Excluir somente o acompanhamento. Retornar `Total`, `Zerados`, `Dropados` e `NaoTerminados` usando uma única consulta agregada ou contagens assíncronas sem rastreamento.

- [ ] **Passo 7: executar os testes focados e confirmar aprovação**

Executar o comando do Passo 2.

Esperado: todos os testes de endpoints aprovados.

- [ ] **Passo 8: commit pequeno**

Commit: `feat: criar API de acompanhamento de jogos`

### Tarefa 3: Tela de acompanhamento

**Arquivos:**
- Criar: `src/GameLife.Web/src/api/gameTracking.js`
- Criar: `src/GameLife.Web/src/screens/GameTrackingScreen.jsx`
- Criar: `src/GameLife.Web/src/screens/GameTrackingScreen.test.jsx`
- Modificar: `src/GameLife.Web/src/App.jsx`
- Modificar: `src/GameLife.Web/src/App.css`

**Interfaces:**
- Consome: endpoints e contratos da Tarefa 2.
- Produz: componente `GameTrackingScreen` sem propriedades obrigatórias e funções de API `listGameTrackings`, `createGameTracking`, `updateGameTracking` e `deleteGameTracking`.

- [ ] **Passo 1: escrever testes da tela que falham**

Testar carregamento e estado vazio, preenchimento de todos os campos, envio da flag de Biblioteca, rótulo de data conforme status, exibição do percentual e barra, edição, exclusão com confirmação, erro da API e validação de conquistas possíveis obrigatórias.

- [ ] **Passo 2: executar os testes focados e confirmar a falha**

Executar: `npm test -- --run src/screens/GameTrackingScreen.test.jsx` em `src/GameLife.Web`.

Esperado: falha porque o componente não existe.

- [ ] **Passo 3: implementar cliente HTTP**

Adicionar as quatro funções usando `/api/acompanhamentos-jogos`, tratamento uniforme de respostas sem conteúdo e propagação da mensagem retornada pela API.

- [ ] **Passo 4: implementar formulário e lista**

Usar estados controlados, opções de plataforma existentes, status em português, campos numéricos com mínimo zero e ações de salvar, cancelar edição e excluir. Mostrar `0%` quando aplicável e largura da barra limitada entre 0 e 100.

- [ ] **Passo 5: integrar à navegação**

Adicionar a opção `Jogos concluídos` à barra lateral e renderizar `GameTrackingScreen` quando selecionada. Ajustar o menu responsivo para seis itens.

- [ ] **Passo 6: executar os testes focados e confirmar aprovação**

Executar o comando do Passo 2.

Esperado: todos os testes da tela aprovados.

- [ ] **Passo 7: commit pequeno**

Commit: `feat: adicionar tela de acompanhamento de jogos`

### Tarefa 4: Barra de resumo na Biblioteca

**Arquivos:**
- Criar: `src/GameLife.Web/src/components/LibraryProgressSummary.jsx`
- Criar: `src/GameLife.Web/src/components/LibraryProgressSummary.test.jsx`
- Modificar: `src/GameLife.Web/src/App.jsx`
- Modificar: `src/GameLife.Web/src/App.css`

**Interfaces:**
- Consome: `GET /api/acompanhamentos-jogos/resumo` da Tarefa 2.
- Produz: `LibraryProgressSummary({ summary })` com quatro blocos rotulados e segmentos proporcionais.

- [ ] **Passo 1: escrever testes do resumo que falham**

Testar os valores `89/19/8/62` da referência, base vazia, rótulos acessíveis, proporções calculadas sobre o total e renderização responsiva sem depender apenas da cor.

- [ ] **Passo 2: executar os testes focados e confirmar a falha**

Executar: `npm test -- --run src/components/LibraryProgressSummary.test.jsx` em `src/GameLife.Web`.

Esperado: falha porque o componente não existe.

- [ ] **Passo 3: implementar o componente**

Renderizar total em bloco fixo e os três estados com `flex-grow` proporcional, mantendo números e rótulos visíveis. Para total zero, usar proporções iguais sem valores inválidos de CSS.

- [ ] **Passo 4: carregar e integrar o resumo**

Ao abrir a Biblioteca, buscar o resumo junto com os itens; renderizar a barra entre o cabeçalho e o grid e exibir estado de erro sem bloquear a coleção.

- [ ] **Passo 5: executar os testes focados e confirmar aprovação**

Executar o comando do Passo 2.

Esperado: todos os testes do resumo aprovados.

- [ ] **Passo 6: executar testes React relacionados**

Executar: `npm test -- --run src/App.test.jsx src/screens/GameTrackingScreen.test.jsx src/components/LibraryProgressSummary.test.jsx` em `src/GameLife.Web`.

Esperado: todos aprovados.

- [ ] **Passo 7: commit pequeno**

Commit: `feat: exibir resumo de progresso na biblioteca`

### Tarefa 5: Segurança do banco, regressão e ciclo real

**Arquivos:**
- Modificar somente se um teste revelar necessidade: `scripts/publish-local.ps1`
- Modificar somente se um teste revelar necessidade: testes existentes em `scripts/tests/`

**Interfaces:**
- Consome: toda a funcionalidade das Tarefas 1 a 4.
- Produz: evidência de preservação do SQLite, build, publicação, atalho e ciclo real do desktop.

- [ ] **Passo 1: registrar estado inicial do banco real sem alterá-lo**

Executar consulta somente leitura para `PRAGMA integrity_check`, listar tabelas e contar cada tabela. Salvar a saída fora do Git em arquivo temporário.

- [ ] **Passo 2: criar backup recuperável**

Copiar `data/gamelife.db` para um arquivo temporário com data/hora, fora do repositório ou ignorado pelo Git, e confirmar tamanho e hash. Não substituir o banco original.

- [ ] **Passo 3: executar suítes completas antes da publicação**

Executar: `dotnet test GameLife.slnx`.

Executar em `src/GameLife.Web`: `npm test -- --run` e `npm run build`.

Esperado: zero falhas e build concluído.

- [ ] **Passo 4: publicar pelo fluxo oficial**

Executar: `./scripts/publish-local.ps1`.

Esperado: publicação em `artifacts/GameLife` somente após testes e build aprovados.

- [ ] **Passo 5: validar migração e dados no ciclo real**

Abrir `GameLife.lnk`, confirmar que aponta para `artifacts/GameLife/GameLife.exe`, cadastrar um acompanhamento de teste sem Biblioteca e outro com Biblioteca, conferir barra/percentual, fechar normalmente e abrir novamente. Excluir somente os dados de teste pela própria interface.

- [ ] **Passo 6: comparar integridade e contagens**

Executar novamente `PRAGMA integrity_check` e contagens. Confirmar que tabelas preexistentes mantêm as quantidades originais, exceto alterações deliberadas feitas e revertidas no ciclo de teste, e que a nova tabela existe.

- [ ] **Passo 7: revisar repositório público**

Executar `git status --short` e `git diff --check`; confirmar que nenhum `.db`, backup, log, configuração local, credencial ou caminho absoluto foi adicionado.

- [ ] **Passo 8: commit final somente se houver ajustes de integração**

Commit: `test: validar ciclo local do acompanhamento de jogos`
