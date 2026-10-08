using ClosedXML.Excel;
using ControleHoras.Models;
using ControleHoras.Interfaces.Services;

namespace ControleHoras.Services;

public class RelatoriosServico(IPainelServico painel) : IRelatoriosServico
{
    public Task<Painel> PainelAsync(int usuarioId, FiltroPeriodo? filtro = null) => painel.PainelAsync(usuarioId, filtro);

    public async Task<byte[]> ExportarExcelAsync(int usuarioId, FiltroPeriodo filtro, string? nomeUsuario)
    {
        var painel = await PainelAsync(usuarioId, filtro);
        return GerarExcel(painel, nomeUsuario);
    }

    public byte[] GerarPdf(Painel painel, string? nomeUsuario) => RelatorioPdf.Gerar(painel, nomeUsuario);

    public byte[] GerarExcel(Painel painel, string? nomeUsuario)
    {
        var filtro = painel.Filtro;
        using var arquivo = new XLWorkbook();
        var planilha = arquivo.Worksheets.Add("Controle de horas");
        planilha.Cell(1, 1).Value = "CONTROLE DE HORAS";
        planilha.Range(1, 1, 1, 5).Merge().Style.Font.SetBold().Font.SetFontSize(18);
        planilha.Cell(2, 1).Value = nomeUsuario;
        planilha.Cell(3, 1).Value = $"{filtro.Inicio:dd/MM/yyyy} a {filtro.Fim:dd/MM/yyyy}";
        planilha.Cell(4, 1).Value = "Valor/hora";
        if (painel.ValorHora.HasValue) { planilha.Cell(4, 2).Value = painel.ValorHora.Value; planilha.Cell(4, 2).Style.NumberFormat.Format = "R$ #,##0.00"; }
        else planilha.Cell(4, 2).Value = "Não definido";
        planilha.Cell(5, 1).Value = "Pagamento: " + (filtro.Pagamento ?? "Todos") + ". Valores pagos distribuídos nas horas da competência; recibos integrais na aba Pagamentos.";
        var titulos = new[] { "Data", "Demanda", "Descrição", "Horas", "Valor estimado", "Pago nas horas", "A receber", "Pagamento" };
        for (var coluna = 0; coluna < titulos.Length; coluna++) planilha.Cell(6, coluna + 1).Value = titulos[coluna];
        var linha = 7;
        foreach (var registro in painel.Linhas)
        {
            planilha.Cell(linha, 1).Value = registro.Data; planilha.Cell(linha, 1).Style.DateFormat.Format = "dd/mm/yyyy";
            planilha.Cell(linha, 2).Value = $"DEV-{registro.DemandaId:000} · {registro.Titulo}";
            planilha.Cell(linha, 3).Value = registro.Descricao ?? "";
            planilha.Cell(linha, 4).Value = registro.Segundos / 86400d; planilha.Cell(linha, 4).Style.NumberFormat.Format = "[h]:mm:ss";
            if (painel.ValorHora.HasValue) { planilha.Cell(linha, 5).Value = (registro.Estimado ?? registro.Valor(painel.ValorHora))!.Value; planilha.Cell(linha, 5).Style.NumberFormat.Format = "R$ #,##0.00"; }
            planilha.Cell(linha, 6).Value = registro.Pago;
            if (registro.Saldo.HasValue) planilha.Cell(linha, 7).Value = registro.Saldo.Value;
            planilha.Range(linha, 6, linha, 7).Style.NumberFormat.Format = "R$ #,##0.00";
            planilha.Cell(linha, 8).Value = registro.SituacaoPagamento;
            linha++;
        }
        planilha.Cell(linha + 1, 1).Value = "Total de horas"; planilha.Cell(linha + 1, 2).Value = Formato.Horas(painel.Segundos);
        planilha.Cell(linha + 2, 1).Value = "Total estimado"; planilha.Cell(linha + 2, 2).Value = Formato.Dinheiro(painel.Estimado);
        planilha.Cell(linha + 3, 1).Value = "Pagamentos"; planilha.Cell(linha + 3, 2).Value = painel.Pago;
        planilha.Cell(linha + 4, 1).Value = "Saldo a receber"; planilha.Cell(linha + 4, 2).Value = Formato.Dinheiro(painel.Saldo);
        planilha.Range(6, 1, Math.Max(6, linha - 1), 8).Style.Border.BottomBorder = XLBorderStyleValues.Hair;
        planilha.Range(6, 1, 6, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#152033");
        planilha.Range(6, 1, 6, 8).Style.Font.FontColor = XLColor.White;
        planilha.Columns().AdjustToContents(); planilha.Column(2).Width = 48; planilha.Column(3).Width = 40;
        var recebimentos = arquivo.Worksheets.Add("Pagamentos");
        recebimentos.Cell(1, 1).Value = "Data"; recebimentos.Cell(1, 2).Value = "Competência"; recebimentos.Cell(1, 3).Value = "Valor pago"; recebimentos.Cell(1, 4).Value = "Observação"; recebimentos.Cell(1, 5).Value = "Demanda"; recebimentos.Cell(1, 6).Value = "Status do registro";
        var indice = 2;
        foreach (var pagamento in painel.Pagamentos) { recebimentos.Cell(indice, 1).Value = pagamento.DataPagamento.ToString("dd/MM/yyyy"); recebimentos.Cell(indice, 2).Value = $"{pagamento.PeriodoInicio:dd/MM/yyyy} a {pagamento.PeriodoFim:dd/MM/yyyy}"; recebimentos.Cell(indice, 3).Value = pagamento.ValorPago; recebimentos.Cell(indice, 4).Value = pagamento.Observacao ?? ""; recebimentos.Cell(indice, 5).Value = pagamento.DemandaNome ?? "Sem demanda"; recebimentos.Cell(indice++, 6).Value = "Recebido"; }
        recebimentos.Columns().AdjustToContents();
        using var fluxo = new MemoryStream(); arquivo.SaveAs(fluxo);
        return fluxo.ToArray();
    }
}
