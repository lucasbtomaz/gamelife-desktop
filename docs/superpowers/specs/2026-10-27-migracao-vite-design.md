# Migração do frontend GameLife para Vite

## Objetivo

Substituir o Create React App por Vite sem alterar interface, contratos HTTP, banco MySQL ou experiência do atalho local.

## Decisão

- Usar Vite com `@vitejs/plugin-react`.
- Usar Vitest com jsdom e Testing Library.
- Manter os três testes de comportamento existentes.
- Gerar o frontend em `dist` e atualizar a publicação .NET para incorporar essa pasta.
- Configurar proxy de desenvolvimento para `/biblioteca`, `/api` e `/health` em `http://127.0.0.1:5044`.

## Fora do escopo

- Mudanças visuais ou funcionais.
- Mudanças no MySQL ou nos endpoints.
- Publicação na internet.
