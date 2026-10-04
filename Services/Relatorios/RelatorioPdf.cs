using System.Globalization;
using ControleHoras.Models;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace ControleHoras.Services;

public static class RelatorioPdf
{
    static RelatorioPdf() { GlobalFontSettings.FontResolver = new FontesRelatorio(); }

    public static byte[] Gerar(Painel painel, string? nomeUsuario)
    {
        using var documento = new PdfDocument();
        documento.Info.Title = "Relatório de horas";
        documento.Info.Author = nomeUsuario ?? "";
        var cultura = CultureInfo.GetCultureInfo("pt-BR");
        var emissao = Horario.AgoraLocal;
        string Dinheiro(decimal? valor) => valor?.ToString("C2", cultura) ?? "Não definido";

        var corpo = new XFont("Relatorio", 8, XFontStyleEx.Regular);
        var pequeno = new XFont("Relatorio", 7, XFontStyleEx.Regular);
        var rotulo = new XFont("Relatorio", 7, XFontStyleEx.Bold);
        var forte = new XFont("Relatorio", 8, XFontStyleEx.Bold);
        var titulo = new XFont("Relatorio", 26, XFontStyleEx.Bold);
        var subtitulo = new XFont("Relatorio", 12, XFontStyleEx.Bold);
        var numero = new XFont("Relatorio", 19, XFontStyleEx.Bold);
        var tinta = new XSolidBrush(XColor.FromArgb(23, 42, 62));
        var suave = new XSolidBrush(XColor.FromArgb(104, 119, 134));
        var azul = new XSolidBrush(XColor.FromArgb(19, 43, 65));
        var verde = new XSolidBrush(XColor.FromArgb(20, 116, 105));
        var gelo = new XSolidBrush(XColor.FromArgb(243, 247, 249));
        var verdeClaro = new XSolidBrush(XColor.FromArgb(232, 245, 241));
        var borda = new XPen(XColor.FromArgb(220, 228, 234), .6);
        const double margem = 38, limite = 777;
        double largura = 0, y = 0;
        XGraphics? g = null;

        // Every text box has a real content width, including right-aligned values.
        void Texto(string texto, XFont fonte, XBrush cor, double x, double top,
            double w, double h, bool direita = false)
        {
            g!.DrawString(texto, fonte, cor, new XRect(x, top, w, h),
                new XStringFormat { Alignment = direita ? XStringAlignment.Far : XStringAlignment.Near,
                    LineAlignment = XLineAlignment.Center });
        }

        XFont Ajustar(string texto, XFont fonte, double w)
        {
            var tamanho = fonte.Size;
            while (tamanho > 6 && g!.MeasureString(texto, fonte).Width > w)
                fonte = new XFont("Relatorio", tamanho -= .5, fonte.Style);
            return fonte;
        }

        List<string> Quebrar(string? texto, XFont fonte, double w)
        {
            var linhas = new List<string>();
            foreach (var paragrafo in (texto ?? "").Replace("\r", "").Split('\n'))
            {
                var atual = "";
                foreach (var palavra in paragrafo.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var candidato = atual.Length == 0 ? palavra : atual + " " + palavra;
                    if (g!.MeasureString(candidato, fonte).Width <= w) { atual = candidato; continue; }
                    if (atual.Length > 0) linhas.Add(atual);
                    atual = "";
                    foreach (var caractere in palavra)
                    {
                        if (g.MeasureString(atual + caractere, fonte).Width > w && atual.Length > 0)
                        { linhas.Add(atual); atual = ""; }
                        atual += caractere;
                    }
                }
                linhas.Add(atual);
            }
            return linhas;
        }

        void Pagina()
        {
            g?.Dispose();
            var pagina = documento.AddPage();
            pagina.Size = PdfSharp.PageSize.A4;
            largura = pagina.Width.Point - margem * 2;
            g = XGraphics.FromPdfPage(pagina);
            g.DrawRectangle(verde, margem, 30, 22, 3);
            Texto("CONTROLE DE HORAS", rotulo, tinta, margem + 30, 24, 200, 16);
            Texto("RELATÓRIO PROFISSIONAL", pequeno, suave, margem + largura - 180, 24, 180, 16, true);
            g.DrawLine(borda, margem, 50, margem + largura, 50);
            y = 68;
            if (documento.PageCount > 1)
            {
                Texto("Detalhamento do período", subtitulo, tinta, margem, y, 260, 20);
                Texto($"{painel.Filtro.Inicio:dd/MM/yyyy} a {painel.Filtro.Fim:dd/MM/yyyy}",
                    pequeno, suave, margem + 280, y, largura - 280, 20, true);
                y += 34;
            }
        }

        void Garantir(double altura)
        {
            if (y + altura > limite) Pagina();
        }

        void Secao(string indice, string nome, string detalhe, double reserva)
        {
            Garantir(46 + reserva);
            y += 10;
            Texto(indice, rotulo, verde, margem, y, 24, 18);
            Texto(nome, subtitulo, tinta, margem + 27, y, largura - 27, 18);
            y += 21;
            Texto(detalhe, pequeno, suave, margem + 27, y, largura - 27, 13);
            y += 20;
        }

        void Indicador(double x, double w, string label, string valor, string detalhe, bool destaque)
        {
            var fundo = destaque ? azul : gelo;
            var cor = destaque ? XBrushes.White : tinta;
            g!.DrawRectangle(fundo, x, y, w, 90);
            Texto(label, rotulo, destaque ? XBrushes.White : suave, x + 14, y + 12, w - 28, 13);
            Texto(valor, Ajustar(valor, numero, w - 28), cor, x + 14, y + 33, w - 28, 27);
            Texto(detalhe, pequeno, destaque ? XBrushes.White : suave, x + 14, y + 65, w - 28, 13);
        }

        var horasColunas = new double[] { 65, 0, 48, 48, 68, 91 };
        var pagamentoColunas = new double[] { 68, 145, 0, 96 };

        void Cabecalho(double[] colunas, string[] nomes)
        {
            g!.DrawRectangle(azul, margem, y, largura, 26);
            var x = margem;
            for (var i = 0; i < colunas.Length; i++)
            {
                Texto(nomes[i], rotulo, XBrushes.White, x + 8, y, colunas[i] - 16, 26,
                    i == colunas.Length - 1);
                x += colunas[i];
            }
            y += 26;
        }

        void PrepararLinha(double altura, double[] colunas, string[] nomes)
        {
            if (y + altura > limite)
            {
                Pagina();
                Cabecalho(colunas, nomes);
            }
        }

        void Celula(string texto, int coluna, double[] colunas, double altura, XFont fonte,
            XBrush? cor = null, bool direita = false)
        {
            var x = margem + colunas.Take(coluna).Sum() + 8;
            var w = colunas[coluna] - 16;
            Texto(texto, Ajustar(texto, fonte, w), cor ?? tinta, x, y, w, altura, direita);
        }

        void Vazio(string mensagem)
        {
            g!.DrawRectangle(gelo, margem, y, largura, 45);
            Texto(mensagem, corpo, suave, margem + 14, y, largura - 28, 45);
            y += 45;
        }

        try
        {
            Pagina();
            Texto("Relatório de horas", titulo, tinta, margem, y, largura, 38);
            y += 42;
            Texto($"{painel.Filtro.Inicio:dd/MM/yyyy} a {painel.Filtro.Fim:dd/MM/yyyy}",
                corpo, suave, margem, y, largura, 17);
            y += 25;
            Texto("PROFISSIONAL", rotulo, verde, margem, y, largura, 13);
            y += 17;
            foreach (var linha in Quebrar(string.IsNullOrWhiteSpace(nomeUsuario) ? "Não informado" : nomeUsuario,
                subtitulo, largura))
            {
                Garantir(19);
                Texto(linha, subtitulo, tinta, margem, y, largura, 19);
                y += 19;
            }
            var filtros = new List<string>();
            if (painel.Filtro.DemandaId.HasValue) filtros.Add($"Demanda DEV-{painel.Filtro.DemandaId:000}");
            if (!string.IsNullOrWhiteSpace(painel.Filtro.Status)) filtros.Add($"Situação: {painel.Filtro.Status}");
            foreach (var linha in filtros.Count == 0 ? new List<string>() : Quebrar(string.Join("  |  ", filtros), pequeno, largura))
            {
                Garantir(15);
                Texto(linha, pequeno, suave, margem, y, largura, 15);
                y += 15;
            }
            y += 12;
            Garantir(90);
            var card = (largura - 20) / 3;
            Indicador(margem, card, "HORAS TRABALHADAS", Formato.Horas(painel.Segundos),
                $"{painel.Linhas.Count} registro(s) no período", false);
            Indicador(margem + card + 10, card, "VALOR POR HORA", Dinheiro(painel.ValorHora),
                "Tarifa aplicada ao relatório", false);
            Indicador(margem + (card + 10) * 2, card, "TOTAL ESTIMADO", Dinheiro(painel.Estimado),
                "Referente às horas registradas", true);
            y += 98;

            horasColunas[1] = largura - horasColunas.Sum();
            var nomesHoras = new[] { "DATA", "DEMANDA / ATIVIDADE", "INÍCIO", "FIM", "TEMPO", "VALOR" };
            Secao("01", "Atividades do período", "Registros de trabalho e valores correspondentes.", 71);
            Cabecalho(horasColunas, nomesHoras);
            if (painel.Linhas.Count == 0) Vazio("Nenhuma hora registrada para os filtros selecionados.");
            for (var i = 0; i < painel.Linhas.Count; i++)
            {
                var linha = painel.Linhas[i];
                var textos = Quebrar(linha.Titulo, forte, horasColunas[1] - 16)
                    .Select(t => (Texto: t, Fonte: forte, Cor: (XBrush)tinta)).ToList();
                textos.Add(($"DEV-{linha.DemandaId:000}", pequeno, suave));
                if (!string.IsNullOrWhiteSpace(linha.Descricao))
                    textos.AddRange(Quebrar(linha.Descricao, pequeno, horasColunas[1] - 16)
                        .Select(t => (t, pequeno, (XBrush)suave)));
                // Split exceptionally long descriptions into page-sized pieces without dropping text.
                var posicao = 0;
                while (posicao < textos.Count)
                {
                    PrepararLinha(44, horasColunas, nomesHoras);
                    var quantidade = Math.Min(textos.Count - posicao, (int)((limite - y - 16) / 11));
                    var altura = Math.Max(44, quantidade * 11 + 16);
                    if (i % 2 == 0) g!.DrawRectangle(gelo, margem, y, largura, altura);
                    if (posicao == 0)
                    {
                        Celula(linha.Data.ToString("dd/MM/yyyy"), 0, horasColunas, altura, pequeno);
                        Celula(linha.Inicio.ToString("HH:mm:ss"), 2, horasColunas, altura, pequeno);
                        Celula(linha.EmAndamento ? "Em curso" : linha.Fim.ToString("HH:mm:ss"), 3,
                            horasColunas, altura, pequeno, linha.EmAndamento ? verde : tinta);
                        Celula(Formato.Horas(linha.Segundos), 4, horasColunas, altura, forte);
                        Celula(Dinheiro(linha.Valor(painel.ValorHora)), 5, horasColunas, altura, forte, direita: true);
                    }
                    for (var j = 0; j < quantidade; j++)
                    {
                        var texto = textos[posicao + j];
                        Texto(texto.Texto, texto.Fonte, texto.Cor, margem + horasColunas[0] + 8,
                            y + 8 + j * 11, horasColunas[1] - 16, 11);
                    }
                    y += altura;
                    g!.DrawLine(borda, margem, y, margem + largura, y);
                    posicao += quantidade;
                }
            }
            PrepararLinha(32, horasColunas, nomesHoras);
            g!.DrawRectangle(verdeClaro, margem, y, largura, 32);
            Texto("TOTAL DO PERÍODO", rotulo, verde, margem + 8, y, 250, 32);
            Celula(Formato.Horas(painel.Segundos), 4, horasColunas, 32, forte, verde);
            Celula(Dinheiro(painel.Estimado), 5, horasColunas, 32, forte, verde, true);
            y += 32;

            pagamentoColunas[2] = largura - pagamentoColunas.Sum();
            var nomesPagamentos = new[] { "DATA", "COMPETÊNCIA", "OBSERVAÇÃO", "VALOR PAGO" };
            Secao("02", "Pagamentos", "Pagamentos da competência incluídos no período selecionado.", 71);
            Cabecalho(pagamentoColunas, nomesPagamentos);
            if (painel.Pagamentos.Count == 0) Vazio("Nenhum pagamento para a competência selecionada.");
            for (var i = 0; i < painel.Pagamentos.Count; i++)
            {
                var pagamento = painel.Pagamentos[i];
                var linhas = Quebrar(string.IsNullOrWhiteSpace(pagamento.Observacao) ? "-" : pagamento.Observacao,
                    pequeno, pagamentoColunas[2] - 16);
                var posicao = 0;
                while (posicao < linhas.Count)
                {
                    PrepararLinha(34, pagamentoColunas, nomesPagamentos);
                    var quantidade = Math.Min(linhas.Count - posicao, (int)((limite - y - 16) / 11));
                    var altura = Math.Max(34, quantidade * 11 + 16);
                    if (i % 2 == 0) g.DrawRectangle(gelo, margem, y, largura, altura);
                    if (posicao == 0)
                    {
                        Celula($"{pagamento.DataPagamento:dd/MM/yyyy}", 0, pagamentoColunas, altura, pequeno);
                        Celula($"{pagamento.PeriodoInicio:dd/MM/yyyy} a {pagamento.PeriodoFim:dd/MM/yyyy}",
                            1, pagamentoColunas, altura, pequeno);
                        Celula(Dinheiro(pagamento.ValorPago), 3, pagamentoColunas, altura, forte, direita: true);
                    }
                    for (var j = 0; j < quantidade; j++)
                        Texto(linhas[posicao + j], pequeno, suave, margem + pagamentoColunas[0] + pagamentoColunas[1] + 8,
                            y + 8 + j * 11, pagamentoColunas[2] - 16, 11);
                    y += altura;
                    g.DrawLine(borda, margem, y, margem + largura, y);
                    posicao += quantidade;
                }
            }

            // Keep the financial summary and its explanatory notes together.
            Secao("03", "Fechamento financeiro", "Consolidação dos valores deste relatório.", 153);
            g.DrawRectangle(gelo, margem, y, largura, 49);
            Texto("TOTAL APURADO", rotulo, suave, margem + 14, y + 7, 220, 13);
            Texto(Dinheiro(painel.Estimado), subtitulo, tinta, margem + 14, y + 22, 220, 20);
            Texto("TOTAL PAGO", rotulo, suave, margem + largura / 2 + 14, y + 7, 220, 13);
            Texto(Dinheiro(painel.Pago), subtitulo, tinta, margem + largura / 2 + 14, y + 22, 220, 20);
            y += 55;
            g.DrawRectangle(azul, margem, y, largura, 55);
            var saldoLabel = painel.Saldo < 0 ? "CRÉDITO / VALOR PAGO A MAIOR" : "SALDO A RECEBER";
            Texto(saldoLabel, rotulo, XBrushes.White, margem + 14, y, largura / 2, 55);
            var saldo = Dinheiro(painel.Saldo);
            Texto(saldo, Ajustar(saldo, numero, largura / 2 - 28), XBrushes.White,
                margem + largura / 2, y, largura / 2 - 14, 55, true);
            y += 65;
            Texto("Pagamentos incluídos quando toda a competência está contida no período.", pequeno, suave,
                margem, y, largura, 13);
            Texto("Cronômetros em curso são contabilizados até o momento da emissão.", pequeno, suave,
                margem, y + 14, largura, 13);

            g.Dispose();
            g = null;
            for (var i = 0; i < documento.PageCount; i++)
            {
                using var rodape = XGraphics.FromPdfPage(documento.Pages[i], XGraphicsPdfPageOptions.Append);
                rodape.DrawLine(borda, margem, 795, margem + largura, 795);
                rodape.DrawString($"Emitido em {emissao:dd/MM/yyyy HH:mm} (Horário de Brasília)", pequeno, suave,
                    new XRect(margem, 803, largura - 90, 14), XStringFormats.TopLeft);
                rodape.DrawString($"{i + 1:00} / {documento.PageCount:00}", rotulo, tinta,
                    new XRect(margem + largura - 80, 803, 80, 14), XStringFormats.TopRight);
            }
        }
        finally { g?.Dispose(); }
        using var fluxo = new MemoryStream();
        documento.Save(fluxo, false);
        return fluxo.ToArray();
    }
    private sealed class FontesRelatorio : IFontResolver
    {
        public string DefaultFontName => "Relatorio";
        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) => new(isBold ? "relatorio-bold" : "relatorio-regular");
        public byte[] GetFont(string faceName)
        {
            var bold = faceName == "relatorio-bold";
            var caminhos = new[] {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), bold ? "Nunito-Bold.ttf" : "Nunito-Regular.ttf"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), bold ? "arialbd.ttf" : "arial.ttf"),
                $"/usr/share/fonts/truetype/dejavu/DejaVuSans{(bold ? "-Bold" : "")}.ttf",
                $"/usr/share/fonts/truetype/liberation2/LiberationSans-{(bold ? "Bold" : "Regular")}.ttf"
            };
            var caminho = caminhos.FirstOrDefault(File.Exists) ?? throw new InvalidOperationException("Instale as fontes Nunito, Arial ou DejaVu Sans no servidor para gerar o PDF.");
            return File.ReadAllBytes(caminho);
        }
    }
}
