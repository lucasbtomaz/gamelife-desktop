# Dashboard, Perfil e Coleções Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Criar navegação e persistência completas para Início, Biblioteca, Coleções e Configurações.

**Architecture:** A API adiciona perfil único, conquistas e coleções com EF Core/MySQL. O React consome endpoints REST e mantém navegação interna por estado, sem dependência de roteador externo.

**Tech Stack:** .NET 10, EF Core/MySQL, xUnit, React 19, Vite, Vitest.

**Spec:** `docs/superpowers/specs/2026-10-27-dashboard-perfil-colecoes-design.md`

## Global Constraints

- Preservar dados e endpoints existentes.
- Não incluir autenticação ou capturas.
- Manter publicação local e atalho funcionando.
- Usar testes antes de cada mudança de comportamento.

## Review Focus

- Perfil deve existir em banco vazio e aceitar atualização segura.
- Foto acima de 2 MB deve ser rejeitada.
- Coleção não pode receber o mesmo jogo duas vezes.
- Exclusão de coleção não pode excluir jogos.
- Dashboard deve funcionar com banco vazio.

### Task 1: Perfil, conquistas e dashboard

- [ ] Criar testes HTTP para perfil inicial, atualização e dashboard vazio.
- [ ] Criar entidades, configuração EF e endpoints.
- [ ] Validar foto e contratos.
- [ ] Confirmar testes focados.

### Task 2: Coleções personalizadas

- [ ] Criar testes HTTP de criação, associação, duplicidade e exclusão.
- [ ] Criar entidades de coleção e relacionamento.
- [ ] Implementar endpoints REST.
- [ ] Confirmar testes focados.

### Task 3: Migration MySQL

- [ ] Gerar migration somente aditiva.
- [ ] Revisar SQL e aplicar ao banco local.
- [ ] Confirmar consulta real dos novos endpoints.

### Task 4: Navegação e telas React

- [ ] Escrever testes para menus e troca de telas.
- [ ] Implementar sidebar e telas Início, Biblioteca, Coleções e Configurações.
- [ ] Implementar upload de foto e ações de coleções.
- [ ] Confirmar Vitest e build Vite.

### Task 5: Publicação e verificação

- [ ] Executar publicação completa.
- [ ] Validar endpoints e telas pelo executável.
- [ ] Executar auditorias e revisão independente.
