# Controle de horas

Aplicação ASP.NET Core MVC (.NET 10), Dapper e MySQL. Interface em português, com Bootstrap 5, Font Awesome e Nunito, inspirada no projeto `C:\jca-inventario`: sidebar azul-escuro, cabeçalho compacto, fundo claro e tabelas discretas. Sem Entity Framework, jQuery ou frameworks JavaScript.

## Executar

Requer .NET 10 SDK e MySQL 8.0 ou superior. Configure a conexao real em `appsettings.Local.json`, chave `ConnectionStrings:BancoDados`, ou na variavel `ConnectionStrings__BancoDados`.

Execute `powershell -File .\iniciar-local.ps1` em `C:\controle-de-horas` e abra http://localhost:5197.

O login consulta a tabela `usuarios` e valida a senha com hash. Nao existe usuario ou senha padrao. A inicializacao cria apenas a estrutura do banco. Para um banco vazio, o primeiro administrador pode ser cadastrado informando explicitamente `Administrador:Nome`, `Administrador:Login` e `Administrador:SenhaInicial` (minimo de 12 caracteres) na configuracao local ou em variaveis de ambiente com `__` no lugar de `:`. A senha e gravada como hash; remova a configuracao inicial depois do cadastro. Usuarios existentes nunca sao sobrescritos.

O script nao inicia nem configura bancos demonstrativos. A pasta legada `.dados-teste/mysql` foi excluida do versionamento, mas pode conter registros reais e nao deve ser apagada sem identificar seu conteudo.

## Organizacao

- `Interfaces/Repositories` e `Interfaces/Services`: contratos usados pelos controladores.
- `Dependecias/InjecaoDependencias.cs`: registros das dependencias da aplicacao.
- `Repositories`: consultas Dapper; `Services`: regras de negocio.
- `wwwroot/js/navigation.js`: navegacao por fetch, seguindo o padrao de jca-inventario. Mantem menu e cabecalho, atualiza conteudo, titulo, item ativo e historico. Formularios mantem antiforgery e confirmacoes; POST nao e repetido automaticamente em falhas. Login, logout, download e impressao usam o fluxo proprio.
- `wwwroot/js/horas.js`: inicializacao dos cronometros apos cada navegacao, descartando intervalos anteriores.

Layout, estilos e telas preservados. O projeto de referencia nao foi alterado.

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

## Validação

```powershell
dotnet build
dotnet run --project Tests/ControleHoras.Testes.csproj
```

Os testes de integração usam MySQL real em `127.0.0.1:3307` e banco `controle_horas_testes`. Para outra conexão, defina `CONEXAO_TESTES`; o nome do banco obrigatoriamente deve terminar em `_testes`. Cada execução cria um usuário isolado e preserva os dados para inspeção. Incluem 20 verificações de hash, inicializacao sem usuario padrao, meia-noite, tarifa opcional e retroativa, pagamento parcial, isolamento, sobreposição, concorrência, persistência do cronômetro, pausa/retomada, finalização, inativação e auditoria.


O fluxo HTTP pode ser repetido em uma segunda instância, apontada para o banco de testes:

```powershell
$env:ConnectionStrings__BancoDados='Server=127.0.0.1;Port=3307;Database=controle_horas_testes;User=root;Password=;'
dotnet run --no-build --urls http://localhost:5198
# Em outro terminal:
powershell -NoProfile -ExecutionPolicy Bypass -File Tests/fluxo-http.ps1 -UsuarioAdministrador <login-do-banco-de-testes> -SenhaAdministrador <senha>
```

A validação HTTP cobre as telas, cadastro e login de usuário, demanda, cronômetro, lançamento manual, tarifa brasileira, pagamento, total/saldo, XLSX, impressão/PDF, acesso administrativo e CSRF. O navegador automatizado não estava disponível nesta sessão; a interface foi verificada por compilação Razor e respostas HTTP, sem captura visual.
