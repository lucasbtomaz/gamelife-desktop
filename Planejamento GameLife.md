# Planejamento do GameLife

## 1. Levantamento de requisitos

O levantamento de requisitos começa pela definição do problema, do público-alvo e do escopo do produto.

### Requisitos funcionais

Os requisitos funcionais descrevem o que o sistema fará.

- Cadastro e autenticação de usuário.
- Criação e edição de perfil.
- Registro de atividades.
- Concessão de XP.
- Evolução de nível.
- Desbloqueio de conquistas.
- Criação e acompanhamento de missões.
- Consulta de histórico e ranking.

Exemplo:

```markdown
RF-001 — Registrar atividade

Como jogador,
quero registrar uma atividade concluída,
para receber XP e acompanhar meu progresso.

Critérios de aceitação:
- O jogador deve estar autenticado.
- A atividade deve possuir título e categoria.
- A duração deve ser maior que zero.
- O sistema deve calcular o XP.
- A atividade deve aparecer no histórico.
```

### Requisitos não funcionais

- Senhas armazenadas com hash seguro.
- Autenticação por token.
- Validação de todas as entradas.
- Logs sem informações sensíveis.
- Definição do tempo de resposta esperado.
- Backup do banco de dados.
- Controle de acesso por usuário.
- Paginação de históricos e rankings.

Também deve ser documentado o que ficará fora da primeira versão.

## 2. Modelagem do banco de dados

O banco deve ser planejado em três etapas: modelo conceitual, modelo lógico e implementação por migrations.

### Estrutura inicial

```text
User
- Id
- Name
- Email
- PasswordHash
- CreatedAt
- UpdatedAt

Activity
- Id
- UserId
- CategoryId
- Title
- Description
- Duration
- EarnedXp
- CompletedAt
- CreatedAt

Category
- Id
- Name
- XpMultiplier

Achievement
- Id
- Name
- Description
- RequiredValue
- RewardXp

UserAchievement
- UserId
- AchievementId
- UnlockedAt

Mission
- Id
- Title
- Description
- Goal
- RewardXp
- StartsAt
- EndsAt

UserMission
- UserId
- MissionId
- Progress
- Status

XpTransaction
- Id
- UserId
- SourceType
- SourceId
- Amount
- CreatedAt
```

### Relacionamentos principais

```text
User 1:N Activity
Category 1:N Activity
User N:N Achievement
User N:N Mission
User 1:N XpTransaction
```

### Cuidados de qualidade e segurança

- Utilizar chaves estrangeiras.
- Criar índices para e-mail, usuário, datas e campos pesquisados.
- Definir restrição `UNIQUE` no e-mail.
- Escolher tipos apropriados e limites de tamanho.
- Nunca guardar senhas, tokens ou segredos em texto puro.
- Registrar o histórico de XP, em vez de alterar somente um total.
- Definir regras de exclusão e atualização.
- Utilizar transações ao registrar atividade, XP e conquista juntos.

## 3. Design e wireframes

Antes da definição de cores e identidade visual, devem ser desenhados os fluxos e as telas essenciais.

1. Login e cadastro.
2. Página inicial.
3. Registro de atividade.
4. Histórico.
5. Perfil e evolução.
6. Missões.
7. Conquistas.
8. Ranking.

Para cada tela, documentar:

- objetivo;
- ação principal;
- informações exibidas;
- estados vazio, carregando e erro;
- validações;
- comportamento em celular e desktop;
- destino após cada ação.

Os primeiros wireframes podem ser feitos em escala de cinza. O fluxo deve ser validado antes do design visual.

## 4. Planejamento de rotas e schemas

### Rotas iniciais

```http
POST   /api/v1/auth/register
POST   /api/v1/auth/login
POST   /api/v1/auth/refresh

GET    /api/v1/users/me
PATCH  /api/v1/users/me

POST   /api/v1/activities
GET    /api/v1/activities
GET    /api/v1/activities/{id}
PATCH  /api/v1/activities/{id}
DELETE /api/v1/activities/{id}

GET    /api/v1/missions
GET    /api/v1/achievements
GET    /api/v1/rankings
```

Para cada rota, registrar:

- método e URL;
- necessidade de autenticação;
- parâmetros;
- schema de entrada;
- schema de saída;
- validações;
- códigos HTTP;
- erros esperados;
- regras de autorização.

Exemplo de schema de entrada:

```json
{
  "title": "Corrida matinal",
  "categoryId": "uuid",
  "durationMinutes": 30,
  "completedAt": "2026-08-25T10:00:00Z"
}
```

As entidades do banco não devem ser expostas diretamente como contratos da API. Devem ser criados DTOs específicos para entrada e saída.

## 5. Configuração de ambiente

### Arquivos sugeridos

```text
.env.example
.env.development
.env.test
```

### Variáveis iniciais

```env
ASPNETCORE_ENVIRONMENT=Development
DATABASE_CONNECTION_STRING=
JWT_SECRET=
JWT_ISSUER=
JWT_AUDIENCE=
JWT_EXPIRATION_MINUTES=
CORS_ALLOWED_ORIGINS=
LOG_LEVEL=
```

O arquivo `.env.example` pode ser versionado sem valores reais. Arquivos com segredos devem permanecer no `.gitignore`. Em produção, deve ser utilizado o gerenciador de segredos da plataforma.

## 6. Planejamento de entregas

### Entrega 1: Fundação

- Estrutura da solução.
- Banco de dados e migrations.
- Tratamento global de erros.
- Logs e configurações.
- Testes básicos.

### Entrega 2: Usuário

- Cadastro.
- Login.
- Autorização.
- Perfil.

### Entrega 3: Atividades

- Categorias.
- Registro de atividade.
- Histórico.
- Cálculo e histórico de XP.

### Entrega 4: Gamificação

- Níveis.
- Missões.
- Conquistas.
- Ranking.

### Entrega 5: Qualidade

- Testes de integração.
- Segurança.
- Documentação OpenAPI.
- Observabilidade.
- Deploy e backup.

Cada entrega deve possuir escopo, critérios de aceite, dependências e definição de concluído.

## 7. Artefatos esperados

Ao final do planejamento, o projeto deve possuir:

- visão e escopo do produto;
- requisitos funcionais e não funcionais;
- regras de negócio;
- histórias com critérios de aceitação;
- diagrama de contexto;
- fluxos de usuário;
- wireframes;
- modelo entidade-relacionamento;
- catálogo de rotas e schemas;
- plano de entregas;
- configuração dos ambientes;
- decisões arquiteturais registradas;
- estratégia de testes e segurança.

## 8. Próximos passos

1. Escrever a visão do GameLife.
2. Identificar os perfis de usuário.
3. Selecionar de três a cinco funcionalidades para o MVP.
4. Escrever os requisitos e critérios de aceitação dessas funcionalidades.
5. Desenhar os principais fluxos de usuário.
6. Criar os wireframes iniciais.
7. Modelar as entidades e seus relacionamentos.
8. Definir contratos, rotas e schemas da API.
9. Dividir a implementação em entregas pequenas.
10. Configurar os ambientes de desenvolvimento, teste e produção.
