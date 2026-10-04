# Controle de horas

Aplicação ASP.NET Core MVC (.NET 10), Dapper e MySQL. Interface em português, com Bootstrap 5, Font Awesome e Nunito, inspirada no projeto `C:\jca-inventario`: sidebar azul-escuro, cabeçalho compacto, fundo claro e tabelas discretas. Sem Entity Framework, jQuery ou frameworks JavaScript.

## Executar

Requer .NET 10 SDK e MySQL 8.0 ou superior. Configure a conexao real em `appsettings.Local.json`, chave `ConnectionStrings:BancoDados`, ou na variavel `ConnectionStrings__BancoDados`.

Execute `powershell -File .\iniciar-local.ps1` em `C:\controle-de-horas` e abra http://localhost:5197.

Antes de atender requisicoes, a inicializacao cria o banco caso ele nao exista e garante a estrutura das tabelas. Se a tabela `usuarios` estiver vazia, cadastra automaticamente o administrador ativo `andre`, com senha inicial `12345`. A senha e gravada como hash e validada pelo login. Usuarios e senhas existentes nunca sao sobrescritos nas proximas inicializacoes. O servidor MySQL deve estar disponivel e a conexao configurada deve ter permissao para criar o banco.

Para personalizar o primeiro administrador, informe `Administrador:Nome`, `Administrador:Login` e `Administrador:SenhaInicial` (minimo de 12 caracteres) na configuracao local ou em variaveis de ambiente com `__` no lugar de `:`. Quando informadas, essas credenciais substituem o acesso padrao; remova a configuracao inicial depois do cadastro.

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

## Envio de relatórios pelo WhatsApp

Em **Relatórios / Valores → Enviar pelo WhatsApp**:

1. Conecte seu WhatsApp pelo QR Code (Aparelhos conectados no celular).
2. Cadastre o nome do cliente, telefone com DDD e template da mensagem. Cada usuário possui seus próprios contatos e sua própria instância.
3. Selecione um cliente salvo e o formato **PDF** ou **Excel**. Confira a mensagem e use **Conferir documento** para baixar uma prévia com os filtros atuais.
4. Clique em **Enviar pelo WhatsApp**. O histórico mantém destinatário, template original, mensagem preenchida, filtros, formato, arquivo integral, datas, resultado e protocolo Evolution.

Variáveis do template: `{cliente}`, `{inicio}`, `{fim}`, `{horas}`, `{valor}` e `{saldo}`. O template aceita até 1.000 caracteres; a mensagem preenchida aceita até 1.024. O telefone segue a normalização brasileira do disparo-whatsapp (DDI 55). O anexo tem limite de 10 MB; configure `max_allowed_packet` do MySQL para pelo menos 16 MB.

O PDF agora é gerado no servidor, inclusive pelo botão PDF da tela de valores. O Excel continua usando ClosedXML. Ambos usam o mesmo cálculo e filtros da tela; no envio, a mensagem e o documento compartilham a mesma leitura das horas. Cronômetros ativos podem mudar entre a prévia e o envio. A antiga página `/Relatorios/Imprimir` continua disponível.

### Configuração Evolution

Defina em `appsettings.Local.json` (ignorado pelo Git) ou por variáveis de ambiente:

```json
{
  "Evolution": {
    "Url": "https://sua-evolution.example",
    "ApiKey": "CHAVE_DA_EVOLUTION",
    "InstancePrefix": "controle-horas"
  }
}
```

Variáveis equivalentes: `Evolution__Url`, `Evolution__ApiKey`, `Evolution__InstancePrefix`. A chave nunca é enviada ao navegador. Nesta instalação local, URL e chave foram reaproveitadas da configuração do projeto `C:\disparo-whatsapp`, sem modificar esse projeto. Em outro servidor, configure-as novamente: `appsettings.Local.json` não é publicado.

O fluxo usa os mesmos endpoints de conexão do projeto de referência: `instance/fetchInstances`, `instance/create` com `WHATSAPP-BAILEYS`, `instance/connect/{instance}` e `instance/logout/{instance}`. O documento é enviado por `message/sendMedia/{instance}` com `mediatype=document`, MIME, nome de arquivo, conteúdo base64 e legenda. Não cria nem desconecta contas durante testes. A instância `{InstancePrefix}-{usuarioId}` é persistida e só pode ser operada pelo seu proprietário. Use prefixos distintos em instalações que compartilhem a mesma Evolution.

As tabelas aditivas `whatsapp_contatos`, `whatsapp_conexoes` e `whatsapp_envios` são criadas pela inicialização existente (`CREATE TABLE IF NOT EXISTS`). As três tabelas já foram aplicadas ao banco local configurado durante esta implementação. Não houve alteração de registros existentes.

**Status:** `Aceito` confirma apenas a aceitação pela Evolution; não confirma entrega/leitura. `Falhou` registra rejeição ou conexão ausente. `Incerto` registra falha de transporte/resposta ambígua. Um processo encerrado durante o envio pode deixar `Enviando`; consulte a conversa antes de reenviar. Não há repetição automática nem retomada de envios. A reserva no banco precede qualquer chamada de envio, e a chave única por solicitação evita duplicidade em POSTs repetidos. O arquivo baixado no histórico é a cópia persistida, não uma regeneração do relatório.

Para geração PDF no Windows, as fontes Arial precisam estar instaladas. No Linux, instale DejaVu Sans ou Liberation Sans. Implementação baseada em [PDFsharp](https://docs.pdfsharp.net/).

### Verificações

```powershell
# Testes com gateway/repositório simulados, sem envio real.
dotnet run --project tests/WhatsApp.Checks

# Validação MySQL com usuário desativado e registros temporários removidos ao final.
# Não usa a Evolution e não altera contas existentes.
dotnet run --project tests/WhatsApp.Checks -- --database-checks

# Ambiente de testes de interface, isolado do banco e da Evolution, na porta 5199.
dotnet run --project tests/WhatsApp.Checks -- --preview
# Em outro terminal, enquanto a prévia estiver aberta:
dotnet run --project tests/WhatsApp.Checks --no-build -- --http-checks
```

Com a aplicação principal aberta e os binários bloqueados, acrescente `-p:BaseOutputPath=C:/controle-de-horas/obj/check-bin/` antes de `--` em cada comando de teste. Use o mesmo caminho na compilação e execução.

Os testes cobrem PDF de uma e várias páginas, Excel, variáveis, normalização, isolamento por usuário, registro prévio ao envio, idempotência, indisponibilidade, resposta ambígua, contrato Evolution, antiforgery, formulários e download dos anexos. PDFs de teste ficam em `obj/whatsapp-checks` e foram renderizados para inspeção visual. Nenhuma mensagem real foi enviada na implementação.
