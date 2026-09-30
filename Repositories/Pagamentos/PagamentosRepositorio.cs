using ControleHoras.Data;
using ControleHoras.Models;
using ControleHoras.Interfaces.Repositories;
using Dapper;

namespace ControleHoras.Repositories;

public class PagamentosRepositorio(SessaoBanco sessao) : IPagamentosRepositorio
{
    public Task<List<Pagamento>> PagamentosAsync(int usuarioId) => sessao.ListarAsync<Pagamento>("SELECT p.*, d.titulo AS demanda_nome FROM pagamentos p LEFT JOIN demandas d ON d.id=p.demanda_id AND d.usuario_id=p.usuario_id WHERE p.usuario_id=@usuarioId ORDER BY p.data_pagamento DESC,p.id DESC", new { usuarioId });

    public Task<int> InserirPagamentoAsync(int usuarioId, Pagamento pagamento) => sessao.ExecutarAsync("INSERT INTO pagamentos(usuario_id,demanda_id,data_pagamento,periodo_inicio,periodo_fim,valor_pago,observacao) VALUES(@usuarioId,@DemandaId,@DataPagamento,@PeriodoInicio,@PeriodoFim,@ValorPago,@Observacao)", new { usuarioId, pagamento.DemandaId, pagamento.DataPagamento, pagamento.PeriodoInicio, pagamento.PeriodoFim, pagamento.ValorPago, pagamento.Observacao });
}
