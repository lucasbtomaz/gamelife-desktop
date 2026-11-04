# Dashboard vivo da tela inicial - Plano de implementação

> **Para agentes executores:** SUB-HABILIDADE OBRIGATÓRIA: use `superpowers:subagent-driven-development` (recomendado) ou `superpowers:executing-plans` para executar este plano tarefa por tarefa. As etapas usam caixas de seleção (`- [ ]`) para acompanhamento.

**Objetivo:** Exibir na tela inicial todos os indicadores dos prints, calculados pelo banco e atualizados quando biblioteca, plataforma, finalização, horas ou conquistas mudarem.

**Arquitetura:** O endpoint `GET /api/dashboard/inicio` agregará os dados e entregará resumo, plataformas e anos prontos para apresentação. O React receberá esse contrato em um componente próprio da tela inicial, mantendo o padrão atual de cards e painéis. A data de entrada ficará persistida no perfil com migration compatível com o banco existente.

**Tecnologias:** .NET 10, ASP.NET Core, Entity Framework Core, MySQL, React 19, Vitest e Testing Library.

**Especificação:** `docs/superpowers/specs/2026-11-04-dashboard-inicio-vivo-design.md`

## Restrições globais

- Usar `29/09/2020` como data de entrada do perfil local existente.
- Calcular os valores no backend; o React somente formata e apresenta.
- Preservar dados, migrations e comportamento das outras telas.
- Manter o visual atual do GameLife, sem reproduzir literalmente as cores da planilha.
- Exibir as onze categorias principais do print mesmo com valor zero e acrescentar plataformas desconhecidas sem perder dados.
- Começar os anos em 2024, exibir pelo menos até 2043 e ampliar o intervalo quando necessário.
- Escrever e executar o teste que falha antes de cada mudança de comportamento.

## Foco da revisão

- Data atual anterior ao aniversário da entrada: contar somente anos completos.
- Registro sem `FinalizadoEm`: não contar como zerado nem somar suas horas em horas de zerados.
- Conquistas possíveis iguais a zero: devolver percentual zero, sem divisão inválida.
- Nome de plataforma com caixa, acento ou alias diferente: normalizar sem descartar a posse.
- Mais de 24 horas zeradas: manter horas totais na apresentação, sem reiniciar a contagem por dia.

---

### Task 1: Persistir a data de entrada e agregar o dashboard

**Arquivos:**
- Criar: `src/GameLife.Api/Services/DashboardResumoCalculator.cs`
- Criar: `tests/GameLife.Api.Tests/Services/DashboardResumoCalculatorTests.cs`
- Criar: `tests/GameLife.Api.Tests/Api/DashboardResumoEndpointsTests.cs`
- Modificar: `src/GameLife.Api/Models/PerfilLocal.cs`
- Modificar: `src/GameLife.Api/Controllers/PerfilController.cs`
- Modificar: `src/GameLife.Api/Controllers/DashboardController.cs`
- Criar: `src/GameLife.Api/Migrations/<timestamp>_AdicionarDataEntradaPerfil.cs`
- Criar: `src/GameLife.Api/Migrations/<timestamp>_AdicionarDataEntradaPerfil.Designer.cs`
- Modificar: `src/GameLife.Api/Migrations/GameLifeDbContextModelSnapshot.cs`

**Interfaces:**
- Consome: `PerfilLocal`, `PosseJogo`, `RegistroJogo`, `Canal` e o banco atual.
- Produz: `DashboardResumoCalculator.CalcularAnosCompletos(DateOnly inicio, DateOnly hoje) -> int`, `DashboardResumoCalculator.NormalizarPlataforma(string nome) -> string` e a resposta de `/api/dashboard/inicio` com `Resumo`, `BibliotecaPorPlataforma`, `ZeradosPorPlataforma` e `ZeradosPorAno`.

- [ ] **Etapa 1: Escrever testes unitários que falham para anos completos e aliases**

Criar testes que exijam `6` para `29/09/2020` até `04/11/2026`, `5` até `28/09/2026`, e normalizem PlayStation 5 para `PSN`, Xbox Series X|S para `XBOX` e Nintendo Switch para `NINTENDO`.

- [ ] **Etapa 2: Executar os testes unitários e confirmar a falha**

Executar `dotnet test tests/GameLife.Api.Tests/GameLife.Api.Tests.csproj --filter FullyQualifiedName~DashboardResumoCalculatorTests`.

Resultado esperado: falha porque `DashboardResumoCalculator` ainda não existe.

- [ ] **Etapa 3: Implementar o calculador mínimo**

Adicionar a classe estática e as onze categorias canônicas. Normalizar aliases conhecidos sem alterar nomes desconhecidos após limpeza de espaços.

- [ ] **Etapa 4: Executar os testes unitários e confirmar aprovação**

Executar o mesmo comando da Etapa 2.

Resultado esperado: todos os testes do filtro aprovados.

- [ ] **Etapa 5: Escrever o teste de API que falha para o resumo vivo**

No teste, cadastrar jogos em `STEAM` e `PlayStation 5`, criar um registro finalizado com `31.5` horas, `100` conquistas possíveis e `69` obtidas, e outro não finalizado. Exigir no dashboard: data `2020-09-29`, total da biblioteca `2`, zerados `1`, horas zeradas `31.5`, possíveis `100`, obtidas `69`, percentual `69.00`, STEAM `1`, PSN `1`, zerados STEAM `1`, zerados no ano da finalização `1` e categorias/anos sem dados com zero.

- [ ] **Etapa 6: Executar o teste de API e confirmar a falha**

Executar `dotnet test tests/GameLife.Api.Tests/GameLife.Api.Tests.csproj --filter FullyQualifiedName~DashboardResumoEndpointsTests`.

Resultado esperado: falha porque o contrato atual não contém os novos campos.

- [ ] **Etapa 7: Adicionar `DataEntrada` e gerar a migration**

Adicionar `DateTime DataEntrada` ao perfil com valor inicial `2020-09-29`; expor a data em `PerfilResponse` sem permitir que o PUT atual a apague. Gerar `AdicionarDataEntradaPerfil` com default SQL compatível com MySQL para preservar o perfil existente.

- [ ] **Etapa 8: Ampliar `ObterInicio` com agregações do banco**

Consultar posses e registros sem rastreamento, aplicar a normalização após materializar somente os campos necessários e montar listas completas para plataformas e anos. O resumo deve devolver `TempoNaPlataformaAnos`, `JogosZerados`, `HorasZeradas`, `ConquistasPossiveis`, `ConquistasObtidas` e `PercentualConquistas`.

- [ ] **Etapa 9: Executar testes focados e completos do backend**

Executar os dois filtros anteriores e depois `dotnet test GameLife.slnx`.

Resultado esperado: todos os testes aprovados.

- [ ] **Etapa 10: Commit do backend**

```powershell
git add src/GameLife.Api tests/GameLife.Api.Tests docs/superpowers/specs/2026-11-04-dashboard-inicio-vivo-design.md docs/superpowers/plans/2026-11-04-dashboard-inicio-vivo.md
git commit -m "feat: calcular resumo vivo da tela inicial"
```

---

### Task 2: Apresentar todos os blocos no React

**Arquivos:**
- Criar: `src/GameLife.Web/src/components/HomeDashboard.jsx`
- Criar: `src/GameLife.Web/src/components/HomeDashboard.test.jsx`
- Modificar: `src/GameLife.Web/src/App.jsx`
- Modificar: `src/GameLife.Web/src/App.css`

**Interfaces:**
- Consome: o contrato produzido pela Tarefa 1 e `perfil` já carregado pelo `App`.
- Produz: `HomeDashboard({ dashboard, perfil })`, com cards de resumo, dois painéis por plataforma, grade anual e os painéis existentes.

- [ ] **Etapa 1: Escrever o teste React que falha para os indicadores**

Renderizar o componente com fixture literal e exigir os rótulos e valores: `6 anos`, `82`, `31:30:00`, `2`, `4299`, `2971`, `69,11%`, além de STEAM/PSN e 2024/2025.

- [ ] **Etapa 2: Executar o teste e confirmar a falha**

Executar `npm.cmd test -- src/components/HomeDashboard.test.jsx` em `src/GameLife.Web`.

Resultado esperado: falha porque o componente não existe.

- [ ] **Etapa 3: Implementar `HomeDashboard` e formatação de horas**

Formatar horas decimais como `HH:mm:ss`, preservando totais acima de 24 horas, e percentual com duas casas em `pt-BR`. Renderizar semanticamente os cards e tabelas responsivas, reutilizando classes visuais existentes.

- [ ] **Etapa 4: Integrar o componente ao `App` e ampliar opções de plataforma**

Substituir somente o JSX da tela inicial pelo componente e adicionar AMAZON PRIME, BLIZZARD, EA, NINTENDO, PSN, UBISOFT, XBOX e PLAY STORE às opções de cadastro, sem remover as atuais.

- [ ] **Etapa 5: Executar testes React focados e completos**

Executar `npm.cmd test -- src/components/HomeDashboard.test.jsx`, `npm.cmd test` e `npm.cmd run build` em `src/GameLife.Web`.

Resultado esperado: testes aprovados e build Vite concluído.

- [ ] **Etapa 6: Commit do frontend**

```powershell
git add src/GameLife.Web
git commit -m "feat: exibir indicadores vivos no início"
```

---

### Task 3: Validar no aplicativo instalado

**Arquivos:**
- Modificar: nenhum arquivo de produto, salvo correção exigida por falha comprovada.

**Interfaces:**
- Consome: backend e frontend aprovados nas tarefas anteriores.
- Produz: publicação instalada em `%LOCALAPPDATA%\GameLife\App` e atalho atualizado.

- [ ] **Etapa 1: Executar todas as verificações do repositório**

Executar Pester, Vitest, build Vite, `dotnet test GameLife.slnx`, `git diff --check` e a varredura Gitleaks.

- [ ] **Etapa 2: Publicar e instalar**

Executar `./scripts/publish-local.ps1`.

Resultado esperado: testes aprovados, publicação criada, instalação atualizada e atalho recriado.

- [ ] **Etapa 3: Fazer teste real de atualização**

Abrir pelo atalho, conferir os blocos e comparar os totais retornados por `/api/dashboard/inicio`. Fechar e reabrir para confirmar persistência sem criar nem excluir dados reais.

- [ ] **Etapa 4: Revisar o diff e o histórico**

Confirmar que nenhuma configuração local, planilha, credencial, build ou caminho pessoal foi rastreado e que a árvore está limpa.
