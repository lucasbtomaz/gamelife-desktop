# Estabilização da API GameLife para uso local

Data: 12 de outubro de 2026

## Objetivo

Deixar a API pronta para uso no computador pessoal, com persistência segura no MySQL, sem expor credenciais no GitHub e com uma base simples para evoluir conforme os estudos.

## Ordem de execução

1. Corrigir o build.
2. Corrigir o modelo e as migrations.
3. Adicionar DTOs e validações.
4. Criar testes automatizados e integração contínua.
5. Configurar a proteção da branch `main`.

## Arquitetura mantida

- Manter uma única Web API ASP.NET Core com EF Core e MySQL.
- Manter controllers convencionais e rotas REST simples.
- Usar entidades do EF Core somente na persistência; requisições e respostas usarão DTOs.
- Não adicionar autenticação, serviços externos ou camadas extras nesta etapa.

## Correção do build e da configuração

- Remover o fallback da aplicação para banco em memória.
- Exigir a connection string `GameLife` ao iniciar a API e apresentar um erro claro se ela estiver ausente.
- Manter a senha somente em User Secrets no desenvolvimento local.
- Manter `appsettings.json` sem usuário, senha ou outros dados sigilosos.
- Não registrar a connection string nos logs.

## Modelo de dados

### Lista de desejos e preços

- Um jogo poderá possuir no máximo um `ItemListaDesejos`.
- Cada item poderá possuir vários `RegistroPreco`, um para cada loja, plataforma e data consultada.
- A navegação correta será `ItemListaDesejos.RegistrosPrecos` do tipo `ICollection<RegistroPreco>`.
- O índice único em `ItemListaDesejos.JogoId` impedirá itens duplicados sem limitar o histórico de preços.

### Biblioteca

- Um jogo poderá existir em vários canais ao mesmo tempo.
- A entidade `PosseJogo` receberá o campo `Formato`, com os valores iniciais `Digital`, `MidiaFisica` e `Assinatura`.
- A unicidade será definida pela combinação `JogoId + CanalId + Formato`.
- Assim, o mesmo jogo poderá existir na Steam, Epic, Xbox e PlayStation, ou até no mesmo canal em formatos diferentes.

### Migration corretiva

- Corrigir o relacionamento indevido entre `RegistroJogo` e `ItemListaDesejos`.
- Adicionar o campo `Formato` em `PosseJogo`.
- Adicionar os índices únicos aprovados.
- Gerar uma nova migration corretiva, preservando o histórico já versionado.

## DTOs e validações

- Criar DTOs de entrada e saída para os endpoints existentes.
- Não aceitar IDs, propriedades de navegação ou campos controlados pelo servidor em DTOs de criação.
- Aplicar limites de tamanho e obrigatoriedade nos textos.
- Validar valores monetários não negativos.
- Validar que conquistas obtidas não ultrapassem conquistas possíveis.
- Retornar erros no padrão `ProblemDetails`, com códigos HTTP adequados, incluindo `400` para entrada inválida, `404` para recurso ausente e `409` para duplicidade.

## Resiliência e segurança local

- Configurar CORS por lista de origens permitidas, lida da configuração, para atender apenas ao frontend local conhecido.
- Adicionar tratamento centralizado de exceções sem retornar detalhes internos ao cliente.
- Aceitar `CancellationToken` nas operações assíncronas dos controllers.
- Manter a API destinada ao uso local enquanto não houver autenticação. Ela não deverá ser publicada na internet nesta etapa.

## Testes

- Criar um projeto xUnit separado.
- Usar EF Core InMemory somente no projeto de testes, nunca como fallback da aplicação.
- Criar testes das validações e das regras de unicidade relevantes.
- Criar testes de integração com `WebApplicationFactory` para os endpoints.
- Cobrir um fluxo de ponta a ponta: criar jogo e canal, adicionar à lista de desejos, registrar preços e registrar posses em canais ou formatos diferentes.
- Executar `restore`, `build` e `test` na integração contínua.

## Colaboração no GitHub

- Adicionar `CONTRIBUTING.md` com o fluxo de fork, branch, testes e pull request.
- Adicionar modelo de pull request e `CODEOWNERS`.
- Criar um workflow de CI para validar build e testes.
- Proteger a `main` com pull request obrigatório, verificações da CI, resolução de conversas e bloqueio de force push e exclusão.
- Exigir uma aprovação do responsável pelo código para contribuições externas. Alterações do próprio responsável dependerão de outro revisor ou de uma exceção administrativa consciente.

## Fora do escopo

- Autenticação e autorização.
- Publicação da API na internet.
- Integração automática com lojas ou plataformas.
- Alterações no frontend.
- Importação automática da planilha.

## Critérios de conclusão

- A solução compila sem erros.
- O modelo do EF Core e as migrations estão sincronizados.
- A migration é aplicável ao MySQL local usando uma credencial fora do repositório.
- Os endpoints persistem e recuperam os dados necessários para lista de desejos e biblioteca.
- Os testes passam localmente e na CI.
- A varredura do repositório não encontra credenciais reais.
- A `main` recebe alterações por pull request conforme as regras configuradas.
