# Consolidação do GameLife em monorepositório

## Intenção

Consolidar a API .NET, o frontend React e o aplicativo desktop WPF/WebView2 em um único repositório público chamado `Gamelife`. A mudança deve reduzir o custo de manutenção, gerar um executável local por um único fluxo de publicação e preservar ao máximo o código, os testes, as migrations, os dados e o histórico Git existentes.

## Objetivos

- Manter backend, frontend e desktop em um único repositório e em uma estrutura previsível.
- Preservar o histórico Git dos repositórios existentes, inclusive os commits da branch do desktop.
- Preservar o banco MySQL atual e suas migrations.
- Organizar o React por telas, layouts, componentes, serviços e estilos para permitir manutenção localizada.
- Gerar e testar o executável Windows com um único script.
- Permitir importação em massa da biblioteca e da lista de desejos a partir de Excel ou CSV.
- Preparar a arquitetura para futuras integrações com provedores de bibliotecas de jogos.
- Tornar o repositório seguro para publicação, sem segredos ou dados pessoais no estado atual ou no histórico preservado.

## Fora do escopo inicial

- Reescrever a API, o React ou o desktop do zero.
- Trocar o MySQL por outro banco durante a consolidação.
- Alterar o visual ou o comportamento das telas enquanto elas são separadas em componentes.
- Implementar imediatamente integrações com GOG, Steam ou outros provedores sem antes validar meios oficiais, autenticação, limites e formatos disponíveis.
- Excluir os repositórios antigos antes da validação completa do novo repositório.

## Antiobjetivos

- Criar novos repositórios ou submódulos para cada camada.
- Editar diretamente os arquivos compilados em `dist`, `build`, `wwwroot` ou `artifacts`.
- Versionar banco, backups, planilhas pessoais, fotos, configurações locais, credenciais ou caminhos específicos do computador do usuário.
- Remover funcionalidades apenas para simplificar a migração.

## Estado atual relevante

- A API e as migrations estão no repositório do backend.
- O aplicativo desktop está em uma branch/worktree própria do backend e possui commits posteriores à branch de origem.
- O frontend React está em um repositório separado.
- O script atual compila o frontend externo, copia o resultado e publica o desktop.
- O desktop usa WPF com WebView2 para exibir o React e inicia a API local.
- O MySQL armazena os dados fora do Git e deve continuar sendo a fonte persistente.

## Abordagens consideradas

### 1. Monorepositório com os históricos preservados, escolhida

Partir da linha de desenvolvimento que contém API e desktop, incorporar o histórico do frontend sem squash e reorganizar os arquivos gradualmente. Essa opção mantém rastreabilidade e entrega uma única unidade de desenvolvimento, teste e publicação.

### 2. Novo repositório somente com o estado atual

Copiar apenas os arquivos mais recentes. É operacionalmente simples, mas perde o histórico que ajuda a entender e reverter decisões anteriores.

### 3. Frontend como submódulo

Manter os históricos separados e referenciar o frontend no repositório principal. Foi descartada porque conserva a dependência de múltiplos repositórios e torna clonagem, publicação e manutenção mais frágeis.

## Estrutura proposta

```text
Gamelife/
├── .github/
│   └── workflows/
├── docs/
├── scripts/
│   ├── development.ps1
│   ├── publish.ps1
│   ├── backup-mysql.ps1
│   └── create-shortcut.ps1
├── src/
│   ├── GameLife.Api/
│   ├── GameLife.Desktop/
│   └── GameLife.Web/
│       ├── public/
│       └── src/
│           ├── api/
│           ├── components/
│           ├── layouts/
│           ├── screens/
│           └── styles/
├── tests/
│   ├── GameLife.Api.Tests/
│   └── GameLife.Web.Tests/
├── .gitignore
├── README.md
└── GameLife.slnx
```

Cada tela React terá sua própria pasta, componente, teste e folha de estilos. O layout, menu lateral, ícones e componentes reutilizáveis ficarão fora das telas. Cores, fontes e espaçamentos compartilhados ficarão em tokens globais. A separação inicial deve manter a aparência e o comportamento atuais.

## Consolidação do Git

1. Criar uma cópia de trabalho de migração sem alterar as pastas atuais.
2. Usar como base a branch que contém API e desktop.
3. Incorporar a branch atual do backend caso ela possua mudanças ainda não presentes na branch desktop.
4. Importar o histórico do frontend para `src/GameLife.Web` sem squash e aceitar a união de históricos não relacionados.
5. Mover API, desktop e testes para a estrutura nova em commits pequenos e rastreáveis.
6. Fazer a varredura completa do histórico consolidado antes de qualquer envio ao GitHub.
7. Criar o repositório público `Gamelife` e enviar apenas o histórico aprovado.
8. Manter os repositórios antigos disponíveis até a validação final e a confirmação do usuário.

Se a varredura encontrar segredo ou dado pessoal em qualquer commit, a segurança terá prioridade sobre a preservação literal do histórico. O conteúdo deverá ser removido e o histórico afetado, reescrito antes da publicação.

## Configuração e proteção de dados

- O repositório conterá somente exemplos sem valores reais, como `appsettings.Example.json` ou documentação de User Secrets.
- Credenciais MySQL continuarão em User Secrets ou em arquivo local dentro de `%LOCALAPPDATA%`, nunca em arquivos versionados.
- Arquivos `.env`, configurações locais, planilhas importadas, bancos, dumps, backups, logs, uploads, fotos de perfil, builds e artefatos serão ignorados.
- Caminhos absolutos do computador não serão usados no código, scripts ou documentação versionada.
- A configuração do frontend será relativa à raiz do repositório.
- O histórico completo será examinado por scanner de segredos antes do primeiro push público.
- A integração contínua executará testes, build e varredura de segredos em novos commits.
- Os dados atuais do MySQL não serão copiados nem publicados. A migração reorganiza código, não o conteúdo do banco.

## Execução e publicação

Durante o desenvolvimento, um script na raiz iniciará o frontend em modo Vite e a API local com as configurações de desenvolvimento. O desktop poderá usar o endereço de desenvolvimento ou os recursos compilados conforme o perfil escolhido.

Na publicação, um único script deverá:

1. restaurar dependências;
2. executar testes do frontend e backend;
3. compilar o React;
4. copiar a saída compilada para a área de publicação do desktop;
5. publicar o WPF, a API e os recursos web em uma pasta autocontida;
6. validar a presença dos arquivos obrigatórios;
7. produzir o executável e, opcionalmente, atualizar o atalho local.

O usuário editará o código em `src/GameLife.Web`, mas usará normalmente apenas o executável publicado. Arquivos compilados permanecerão descartáveis e não serão fonte de manutenção.

### Experiência de uso no Windows

O resultado seguirá o mesmo modelo de uso local do FinBot:

- o aplicativo será publicado em uma pasta técnica local;
- um script de instalação local criará o atalho `GameLife` na Área de Trabalho;
- dois cliques no atalho abrirão diretamente a janela do aplicativo, sem terminal e sem navegador externo;
- fechar a janela encerrará a API embutida e o WebView2 de forma controlada;
- abrir novamente reutilizará a mesma configuração e o mesmo banco MySQL;
- atualizar o executável não apagará nem recriará o banco;
- erros de inicialização serão registrados em `%LOCALAPPDATA%\GameLife` e apresentados em mensagem simples.

Não será exigido um instalador único na primeira entrega. A pasta autocontida e o atalho serão detalhes técnicos ocultos do uso diário. Um instalador poderá ser acrescentado depois sem alterar a arquitetura do aplicativo.

## Banco e preservação dos dados

- O MySQL atual será mantido.
- As migrations existentes serão preservadas e executadas pela nova estrutura sem recriar o banco.
- Antes de validações que possam alterar esquema, será gerado um backup recuperável.
- A configuração local existente poderá ser migrada ou reconhecida pelo novo executável.
- A aceitação exige abrir o novo executável e visualizar os registros atuais sem recadastro.

## Importação de Excel e CSV

A importação será implementada depois da consolidação estrutural, para não misturar mudança de arquitetura com funcionalidade nova.

O fluxo terá:

1. seleção de arquivo `.xlsx` ou `.csv` pelo usuário;
2. leitura local sem envio a serviços externos;
3. pré-visualização das colunas e mapeamento para título, plataforma, mídia, status, preço, observação e demais campos aplicáveis;
4. normalização de textos, plataformas e valores;
5. identificação de duplicidades contra a planilha e o banco;
6. resumo do que será criado, atualizado, ignorado ou rejeitado;
7. confirmação explícita;
8. gravação transacional;
9. relatório de erros por linha.

A primeira versão terá um modelo de planilha documentado, mas o mapeamento de colunas permitirá aproveitar a planilha que o usuário já possui. Arquivos reais usados na importação não serão versionados.

### Estrutura da planilha existente

A planilha fornecida possui oito abas: `INICIO`, `BIBLIOTECA`, `BACKLOG`, `BACK`, `LISTA DE DESEJOS`, `ABANDONADO`, `ZERADOS` e `ESTANTE DE LIVROS`.

- `BIBLIOTECA` possui cerca de mil linhas, com título e colunas de posse por plataforma ou serviço.
- `BACKLOG` e `BACK` possuem mais de mil linhas e sobreposição de jogos, sugestões, estimativas, posse e preços por loja.
- `LISTA DE DESEJOS` possui centenas de linhas e preços distribuídos por loja, mídia física e justificativa.
- `ZERADOS` contém plataforma, data de conclusão, tempo, nota, dificuldade e conquistas.
- `ABANDONADO` contém jogos interrompidos e dados de conquistas.
- `ESTANTE DE LIVROS` é um domínio separado e não será importada na primeira entrega do GameLife.
- Há fórmulas cruzadas, células com marcadores textuais como indisponibilidade, linhas incompletas, conteúdo possivelmente duplicado e fórmulas antigas com referências inválidas.

A importação inicial usará os valores relevantes das abas `BIBLIOTECA`, `LISTA DE DESEJOS`, `ZERADOS` e `ABANDONADO`. `BACKLOG` e `BACK` serão apresentados como fontes opcionais na pré-visualização, pois podem repetir registros da lista de desejos. Fórmulas e células de resumo não serão importadas como jogos.

O importador identificará as colunas pelo cabeçalho, não por posição fixa. Nomes de plataformas e lojas serão normalizados para os cadastros internos. Um mesmo título poderá gerar múltiplas relações de posse ou preço, sem criar cópias desnecessárias do jogo. Registros duvidosos ficarão pendentes para decisão do usuário na pré-visualização.

## Integrações externas

As integrações com GOG, Steam e outros serviços formarão uma etapa independente. Antes da implementação serão verificados meios oficiais, exportações disponíveis, autenticação, termos de uso, limites e os dados realmente retornados. Não serão armazenadas senhas dos serviços nem adotada automação por captura de tela como base principal.

A arquitetura de importação terá uma interface comum para fontes de dados. Excel/CSV será a primeira fonte; conectores externos poderão produzir o mesmo formato interno de pré-visualização, deduplicação e confirmação.

## Sequência de entrega

1. Criar e sanear o monorepositório local preservando históricos.
2. Reorganizar caminhos, solução, scripts e testes sem alterar comportamento.
3. Separar o frontend por telas e componentes, mantendo o visual atual.
4. Validar API, banco existente, desktop e fluxo de publicação.
5. Criar o repositório público e publicar o histórico saneado.
6. Implementar a importação Excel/CSV com pré-visualização e deduplicação.
7. Avaliar e priorizar conectores externos.
8. Somente após aceite, arquivar ou excluir os repositórios anteriores.

## Critérios de aceite

- Um clone limpo do repositório contém todas as camadas necessárias.
- Nenhum segredo, dado pessoal, arquivo de banco, planilha ou caminho pessoal aparece no conteúdo ou histórico publicado.
- Testes existentes continuam passando depois da mudança de estrutura.
- O frontend compila e cada tela pode ser mantida em arquivos próprios.
- O executável abre sem navegador externo e acessa a API local.
- O executável reconhece o banco MySQL atual e exibe os dados existentes.
- Um atalho na Área de Trabalho abre a janela diretamente, e fechar e reabrir preserva todos os dados.
- Um único comando gera a publicação local.
- A importação aceita Excel/CSV, mostra uma prévia, trata duplicidades e não realiza gravação parcial em caso de erro.
- Os repositórios antigos permanecem intactos até a confirmação final.

## Decisões deixadas para implementação

- Biblioteca .NET usada para ler `.xlsx`, escolhida entre opções mantidas e compatíveis com a licença do projeto.
- Scanner de segredos local e da integração contínua.
- Detalhes internos do serviço de importação e dos DTOs, desde que mantenham prévia, validação, deduplicação e transação.
- Estratégia técnica de desenvolvimento do WebView2 contra Vite, desde que publicação e uso final permaneçam simples.

