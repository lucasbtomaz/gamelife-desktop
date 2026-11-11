# Restaurar Biblioteca Visual Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restaurar a biblioteca em cards pesquisáveis e filtráveis, usando somente dados reais, e substituir símbolos do menu por SVGs consistentes.

**Architecture:** O endpoint `/biblioteca` combina posse, jogo, plataforma e acompanhamento. Um componente `LibraryScreen` concentra busca, filtros e cards; `App` mantém o carregamento e cadastro existentes. Ícones SVG reutilizam o componente `Icon`.

**Tech Stack:** React 19, Vitest, Testing Library, ASP.NET Core, Entity Framework Core, xUnit e CSS responsivo.

**Spec:** `docs/superpowers/specs/2026-11-06-restaurar-biblioteca-design.md`

## Global Constraints

- Usar somente dados de `data/gamelife.db`.
- Não criar jogos, horas, conquistas, anos ou status fictícios.
- Dados ausentes aparecem como “Não informado”.
- Nenhum banco, backup ou dado pessoal entra no Git.
- Preservar cadastro, coleções e demais abas.

## Review Focus

- Jogo sem acompanhamento: card deve existir com métricas “Não informado”.
- Mesmo título em plataformas diferentes: cada posse mantém seu acompanhamento correto.
- Busca com acentos e diferenças de caixa: deve localizar o título esperado.
- Filtros combinados sem resultado: deve mostrar estado vazio claro.
- Tela estreita: grade deve reduzir colunas sem cortar cards ou controles.

---

### Task 1: Enriquecer a biblioteca com acompanhamento real

**Files:**
- Modify: `src/GameLife.Api/Controllers/BibliotecaController.cs`
- Test: `tests/GameLife.Api.Tests/Api/BibliotecaEndpointsTests.cs`

**Interfaces:**
- Produces: `ItemBibliotecaResponse` com `AnoLancamento`, `Status`, `TempoEmHoras`, `ConquistasPossiveis` e `ConquistasObtidas`

- [ ] Escrever testes falhos para item com acompanhamento e item sem acompanhamento.
- [ ] Executar os testes focados e confirmar as falhas esperadas.
- [ ] Implementar a projeção pelo par jogo/plataforma sem duplicação.
- [ ] Executar novamente os testes focados e confirmar aprovação.

### Task 2: Criar cards, busca e filtros

**Files:**
- Create: `src/GameLife.Web/src/screens/LibraryScreen.jsx`
- Create: `src/GameLife.Web/src/screens/LibraryScreen.test.jsx`
- Modify: `src/GameLife.Web/src/App.jsx`
- Modify: `src/GameLife.Web/src/App.css`

**Interfaces:**
- Consumes: itens enriquecidos de `/biblioteca`
- Produces: `LibraryScreen({ items, loading, message, formProps })`

- [ ] Escrever testes falhos para renderização completa, busca, plataforma, mídia física, filtros combinados e dados ausentes.
- [ ] Executar Vitest focado e confirmar as falhas esperadas.
- [ ] Implementar o componente e integrar o formulário existente.
- [ ] Adicionar estilos responsivos de cards conforme a referência.
- [ ] Executar novamente os testes focados e confirmar aprovação.

### Task 3: Padronizar ícones do menu

**Files:**
- Modify: `src/GameLife.Web/src/App.jsx`
- Test: `src/GameLife.Web/src/App.test.jsx`

**Interfaces:**
- Consumes: componente `Icon`
- Produces: navegação sem caracteres Unicode usados como ícones

- [ ] Escrever teste falho que exige ícone SVG em cada botão do menu e rejeita os símbolos atuais.
- [ ] Executar o teste e confirmar a falha esperada.
- [ ] Substituir símbolos por caminhos SVG acessíveis e decorativos.
- [ ] Executar novamente o teste e confirmar aprovação.

### Task 4: Verificação e publicação protegida

**Files:**
- Verify only

**Interfaces:**
- Consumes: frontend, API, proteção do SQLite e publicação oficial
- Produces: executável validado com banco preservado

- [ ] Executar testes frontend e build.
- [ ] Executar testes .NET completos e testes PowerShell relevantes.
- [ ] Executar `scripts/publish-local.ps1` e confirmar comparação de contagens.
- [ ] Abrir o executável, conferir cards/filtros e fechar normalmente.
- [ ] Confirmar atalho, integridade do SQLite e contagens finais.
