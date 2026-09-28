using ControleHoras.Models;
using static ControleHoras.Services.Horario;

namespace ControleHoras.Services;

public static class CalculoHoras
{
    public static List<LinhaRelatorio> Recortar(IEnumerable<Apontamento> apontamentos, DateTime inicio, DateTime fim)
    {
        var linhas = new List<LinhaRelatorio>();
        var agora = DateTime.UtcNow;
        foreach (var apontamento in apontamentos)
        {
            var primeiro = Local(apontamento.Inicio);
            var ultimo = Local(apontamento.Fim ?? agora);
            var cursor = primeiro > inicio.Date ? primeiro : inicio.Date;
            var limite = ultimo < fim.Date.AddDays(1) ? ultimo : fim.Date.AddDays(1);
            while (cursor < limite)
            {
                var ate = cursor.Date.AddDays(1) < limite ? cursor.Date.AddDays(1) : limite;
                linhas.Add(new() { Data = cursor.Date, DemandaId = apontamento.DemandaId, Titulo = apontamento.Titulo, Inicio = cursor, Fim = ate, EmAndamento = apontamento.Fim == null, Descricao = apontamento.Descricao });
                cursor = ate;
            }
        }
        return linhas.OrderByDescending(l => l.Inicio).ToList();
    }
}
