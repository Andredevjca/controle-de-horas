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
        documento.Info.Title = "Controle de horas";
        documento.Info.Author = nomeUsuario ?? "";
        var normal = new XFont("Relatorio", 10, XFontStyleEx.Regular);
        var negrito = new XFont("Relatorio", 10, XFontStyleEx.Bold);
        var titulo = new XFont("Relatorio", 20, XFontStyleEx.Bold);
        var azul = new XSolidBrush(XColor.FromArgb(21, 32, 51));
        XGraphics? g = null;
        double y = 0;
        const double margem = 40, largura = 515, limite = 785;
        void Pagina()
        {
            g?.Dispose();
            var pagina = documento.AddPage(); pagina.Size = PdfSharp.PageSize.A4;
            g = XGraphics.FromPdfPage(pagina);
            g.DrawString("CONTROLE DE HORAS", titulo, azul, margem, 60);
            g.DrawString($"{painel.Filtro.Inicio:dd/MM/yyyy} a {painel.Filtro.Fim:dd/MM/yyyy}", normal, XBrushes.DimGray, margem, 80);
            g.DrawLine(XPens.LightGray, margem, 90, margem + largura, 90);
            g.DrawString($"Emitido em {Horario.AgoraLocal:dd/MM/yyyy HH:mm} (Brasília) | Página {documento.PageCount}", new XFont("Relatorio", 8), XBrushes.DimGray, margem, 817);
            y = 110;
        }
        void Linha(string texto, bool destaque = false)
        {
            if (y > limite) Pagina();
            g!.DrawString(texto, destaque ? negrito : normal, azul, margem, y);
            y += 15;
        }
        void Texto(string? texto, bool destaque = false)
        {
            var fonte = destaque ? negrito : normal;
            foreach (var paragrafo in (texto ?? "").Replace("\r", "").Split('\n'))
            {
                var linha = "";
                foreach (var palavra in paragrafo.Replace('\t', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var limpa = new string(palavra.Where(c => !char.IsControl(c)).ToArray());
                    var candidata = linha.Length == 0 ? limpa : linha + " " + limpa;
                    if (g!.MeasureString(candidata, fonte).Width <= largura) { linha = candidata; continue; }
                    if (linha.Length > 0) { Linha(linha, destaque); linha = ""; }
                    // Break only unbroken identifiers wider than a full line.
                    foreach (var c in limpa)
                    {
                        if (g.MeasureString(linha + c, fonte).Width > largura && linha.Length > 0) { Linha(linha, destaque); linha = ""; }
                        linha += c;
                    }
                }
                Linha(linha, destaque);
            }
        }
        void Secao(string nome) { if (y + 65 > limite) Pagina(); y += 12; Texto(nome, true); y += 5; }
        try
        {
            Pagina();
            Texto(nomeUsuario, true);
            if (painel.Filtro.DemandaId.HasValue) Texto($"Filtro: DEV-{painel.Filtro.DemandaId:000}");
            if (!string.IsNullOrEmpty(painel.Filtro.Status)) Texto($"Situação: {painel.Filtro.Status}");
            Texto($"Horas trabalhadas: {Formato.Horas(painel.Segundos)} | Valor/hora: {Formato.Dinheiro(painel.ValorHora)}");
            Texto($"Total estimado: {Formato.Dinheiro(painel.Estimado)} | Pago: {Formato.Dinheiro(painel.Pago)}");
            Texto($"Saldo a receber: {Formato.Dinheiro(painel.Saldo)}", true);
            Secao("HORAS DO PERÍODO");
            if (painel.Linhas.Count == 0) Texto("Nenhuma hora registrada para os filtros selecionados.");
            foreach (var linha in painel.Linhas)
            {
                if (y + 60 > limite) Pagina();
                Texto($"{linha.Data:dd/MM/yyyy} | DEV-{linha.DemandaId:000} | {Formato.Horas(linha.Segundos)} | {Formato.Dinheiro(linha.Valor(painel.ValorHora))}", true);
                Texto(linha.Titulo);
                if (!string.IsNullOrWhiteSpace(linha.Descricao)) Texto(linha.Descricao);
                y += 8;
            }
            Secao("PAGAMENTOS DA COMPETÊNCIA");
            if (painel.Pagamentos.Count == 0) Texto("Nenhum pagamento para a competência selecionada.");
            foreach (var pagamento in painel.Pagamentos)
            {
                Texto($"{pagamento.DataPagamento:dd/MM/yyyy} | {pagamento.PeriodoInicio:dd/MM/yyyy} a {pagamento.PeriodoFim:dd/MM/yyyy} | {Formato.Dinheiro(pagamento.ValorPago)}", true);
                if (!string.IsNullOrWhiteSpace(pagamento.Observacao)) Texto(pagamento.Observacao);
                y += 8;
            }
            Secao("RESUMO");
            Texto($"Total: {Formato.Dinheiro(painel.Estimado)} | Pagamentos: {Formato.Dinheiro(painel.Pago)} | Saldo: {Formato.Dinheiro(painel.Saldo)}", true);
            Texto("Pagamentos incluídos quando toda a competência está contida no período. Cronômetros em curso são contabilizados até a emissão.");
        }
        finally { g?.Dispose(); }
        using var fluxo = new MemoryStream(); documento.Save(fluxo, false); return fluxo.ToArray();
    }

    private sealed class FontesRelatorio : IFontResolver
    {
        public string DefaultFontName => "Relatorio";
        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) => new(isBold ? "relatorio-bold" : "relatorio-regular");
        public byte[] GetFont(string faceName)
        {
            var bold = faceName == "relatorio-bold";
            var caminhos = new[] {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), bold ? "arialbd.ttf" : "arial.ttf"),
                $"/usr/share/fonts/truetype/dejavu/DejaVuSans{(bold ? "-Bold" : "")}.ttf",
                $"/usr/share/fonts/truetype/liberation2/LiberationSans-{(bold ? "Bold" : "Regular")}.ttf"
            };
            var caminho = caminhos.FirstOrDefault(File.Exists) ?? throw new InvalidOperationException("Instale as fontes Arial ou DejaVu Sans no servidor para gerar o PDF.");
            return File.ReadAllBytes(caminho);
        }
    }
}
