using ControleHoras.Data;
using ControleHoras.Models;
using ControleHoras.Interfaces.Repositories;
using Dapper;

namespace ControleHoras.Repositories;

public class PagamentosRepositorio(SessaoBanco sessao) : IPagamentosRepositorio
{
    public Task<List<Pagamento>> PagamentosAsync(int usuarioId) => sessao.ListarAsync<Pagamento>("SELECT * FROM pagamentos WHERE usuario_id=@usuarioId ORDER BY data_pagamento DESC,id DESC", new { usuarioId });

    public Task<int> InserirPagamentoAsync(int usuarioId, Pagamento pagamento) => sessao.ExecutarAsync("INSERT INTO pagamentos(usuario_id,data_pagamento,periodo_inicio,periodo_fim,valor_pago,observacao) VALUES(@usuarioId,@DataPagamento,@PeriodoInicio,@PeriodoFim,@ValorPago,@Observacao)", new { usuarioId, pagamento.DataPagamento, pagamento.PeriodoInicio, pagamento.PeriodoFim, pagamento.ValorPago, pagamento.Observacao });
}
