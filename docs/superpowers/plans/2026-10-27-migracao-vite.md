# Migração do GameLife para Vite Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migrar o frontend React do Create React App para Vite mantendo comportamento e publicação local.

**Architecture:** Vite compila o React em `dist`; Vitest executa os testes no jsdom; a API .NET incorpora `dist` no executável autocontido.

**Tech Stack:** React 19, Vite, Vitest, Testing Library, .NET 10.

**Spec:** `docs/superpowers/specs/2026-10-27-migracao-vite-design.md`

## Global Constraints

- Não alterar a interface nem os contratos HTTP.
- Não alterar dados ou schema MySQL.
- Manter o atalho e a publicação Windows funcionando.

## Review Focus

- Os três testes React devem preservar o comportamento atual.
- O proxy deve encaminhar todas as rotas usadas no desenvolvimento.
- O build deve produzir `dist/index.html` e ativos.
- O executável publicado deve conter `wwwroot/index.html` vindo de `dist`.
- Nenhuma dependência do Create React App deve permanecer.

### Task 1: Migrar configuração e testes

- [ ] Substituir scripts e dependências por Vite/Vitest.
- [ ] Criar `vite.config.js` e `index.html`.
- [ ] Renomear arquivos JSX e adaptar globals de teste.
- [ ] Instalar dependências e executar os três testes.

### Task 2: Atualizar publicação local

- [ ] Trocar a origem estática da API de `build` para `dist`.
- [ ] Atualizar documentação e script de publicação.
- [ ] Executar build React, testes .NET e publicação win-x64.
- [ ] Validar `/health`, `/` e `/biblioteca` pelo inicializador real.
