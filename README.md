# Controle de horas

Aplicação ASP.NET Core MVC (.NET 10), Dapper e MySQL. Interface em português, com Bootstrap 5, Font Awesome e Nunito, inspirada no projeto `C:\jca-inventario`: sidebar azul-escuro, cabeçalho compacto, fundo claro e tabelas discretas. Sem Entity Framework, jQuery ou frameworks JavaScript.

## Executar

Requer .NET 10 SDK e MySQL 8.0 ou superior. Configure a conexao real em `appsettings.Local.json`, chave `ConnectionStrings:BancoDados`, ou na variavel `ConnectionStrings__BancoDados`.

Execute `powershell -File .\iniciar-local.ps1` em `C:\controle-de-horas` e abra http://localhost:5197.

O login consulta a tabela `usuarios` e valida a senha com hash. Nao existe usuario ou senha padrao. A inicializacao cria apenas a estrutura do banco. Para um banco vazio, o primeiro administrador pode ser cadastrado informando explicitamente `Administrador:Nome`, `Administrador:Login` e `Administrador:SenhaInicial` (minimo de 12 caracteres) na configuracao local ou em variaveis de ambiente com `__` no lugar de `:`. A senha e gravada como hash; remova a configuracao inicial depois do cadastro. Usuarios existentes nunca sao sobrescritos.

O script nao inicia nem configura bancos demonstrativos. A pasta legada `.dados-teste/mysql` foi excluida do versionamento, mas pode conter registros reais e nao deve ser apagada sem identificar seu conteudo.

## Organiza??o do c?digo

Cada ?rea possui seu controller, servi?o e interface de servi?o: **Autentica??o, Dashboard, Demandas, Apontamentos, Pagamentos, Relat?rios, Configura??es e Usu?rios**.

- `Controllers/<?rea>`: recebe a requisi??o, trata o resultado HTTP e chama apenas o servi?o da ?rea.
- `Services/<?rea>` e `Interfaces/Services`: regras e contratos espec?ficos de cada funcionalidade. A exporta??o Excel fica em `RelatoriosServico`; autentica??o e altera??o de senha tamb?m s?o tratadas nos servi?os.
- `Repositories/<?rea>` e `Interfaces/Repositories`: consultas Dapper separadas em Demandas (incluindo projetos), Apontamentos, Pagamentos, Configura??es, Usu?rios e Hist?rico.
- `Services/Painel`: composi??o das consultas utilizadas pelas telas. Dashboard e Relat?rios reutilizam os reposit?rios das entidades, sem duplicar SQL ou criar reposit?rios vazios por tela. Autentica??o reutiliza o reposit?rio de Usu?rios.
- `Services/Compartilhado`: hor?rio de Bras?lia, valida??es e recorte dos intervalos por dia.
- `Data/SessaoBanco.cs` e `IUnidadeTrabalho`: conex?o e transa??o compartilhadas entre os reposit?rios na mesma requisi??o. Altera??es de demanda, intervalos e auditoria continuam at?micas, com bloqueio por usu?rio.
- `Dependecias/InjecaoDependencias.cs`: registro de servi?os, reposit?rios e sess?o com escopo por requisi??o.
- `Models/<?rea>`: uma classe por arquivo, organizada em Usu?rios, Demandas, Projetos, Apontamentos, Pagamentos, Hist?rico, Relat?rios e Painel. `Models/Compartilhado` re?ne o filtro de per?odo e a formata??o. O namespace `ControleHoras.Models` ? mantido para uso comum nas telas e servi?os.
- `Views` e `wwwroot`: telas Razor e arquivos est?ticos.
- `wwwroot/js/navigation.js`: navega??o por fetch; `horas.js`: inicializa??o dos cron?metros ap?s cada navega??o.

Para manter uma funcionalidade, comece pelo controller da ?rea e siga sua interface de servi?o at? o reposit?rio respons?vel pelos dados.

## Funcionalidades

- Login com cookies nativos, hash de senha, bloqueio de usuários inativos, invalidação de sessões após alteração de usuário/senha e limite de tentativas de login.
- Dashboard, demandas com projeto e prioridade, edição, detalhes, prazos, conclusão, cancelamento e reabertura.
- Iniciar, pausar, continuar e finalizar. Um intervalo de trabalho por retomada. O servidor/banco é a fonte oficial; fechar o navegador não para o relógio.
- Apenas um cronômetro por usuário, com transação, bloqueio por usuário e índice único no banco.
- Apontamento manual, inclusive entre dias; rejeição de sobreposição e horas futuras.
- Histórico filtrável por período, demanda e status; auditoria de alterações importantes.
- Valor/hora opcional e retroativo. Sem tarifa, estimativas e saldo ficam “—”; os pagamentos e as horas permanecem registrados.
- Pagamentos parciais por competência, com data, valor e observação.
- Relatório por período, Excel `.xlsx` com planilha de pagamentos e versão de impressão. Para PDF, clique em **PDF / Imprimir**, depois **Salvar em PDF / Imprimir** e escolha **Salvar como PDF** no navegador. A geração PDF utiliza a impressão do navegador, sem serviço externo.
- Gestão de usuários exclusiva do administrador inicial. Cada usuário acessa somente suas próprias demandas, horas, projetos, tarifa e pagamentos. Não há exclusão física de usuários.

## Regras de cálculo

Os instantes de início/fim e a auditoria usam UTC no banco. A interface e o recorte diário usam America/Sao_Paulo. Pausar fecha o intervalo, e continuar abre outro. As pausas ficam registradas separadamente e não entram na soma. Intervalos que cruzam a meia-noite são divididos por dia nos relatórios. Horas exibidas usam horas e minutos; os valores usam a duração exata em segundos dividida por 3600, com arredondamento somente na apresentação.

A tarifa atual vale para todas as horas históricas. Pagamentos são lançados em reais e não mudam ao alterar a tarifa. O dashboard mostra o saldo global. Relatórios e pagamentos somam recebimentos cuja competência inteira está dentro do período filtrado, sem rateio implícito. Saldo negativo significa crédito recebido. Cronômetros em andamento são incluídos até o instante da consulta. O total financeiro pode diferir em centavos da soma de linhas arredondadas individualmente, pois é calculado sobre o tempo total exato.

## Organização

`Controllers/` contém os controladores por área; `Models/` reúne as entidades e os modelos de tela; `Services/` concentra regras e cálculos; `Repositories/` contém o contrato e as consultas Dapper parametrizadas; `Data/` contém a inicialização e o SQL; `Views/` contém Razor; `wwwroot/` contém Bootstrap local, estilos e JavaScript puro. O repositório é compartilhado entre as áreas para manter uma única transação nas operações que alteram demanda, intervalo e auditoria.

Os estilos de referência foram copiados para `wwwroot/css/referencia.css` e adaptados em `horas.css`. O projeto `jca-inventario` não foi alterado. Nunito e Font Awesome são carregados por CDN; o restante da interface e o Bootstrap são locais.

## Compilar e abrir no Visual Studio

Abra o arquivo ControleHoras.slnx no Visual Studio. A solucao contem apenas o projeto da aplicacao.

Para compilar pelo terminal: dotnet build ControleHoras.slnx
