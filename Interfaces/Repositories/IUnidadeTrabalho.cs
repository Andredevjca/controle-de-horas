namespace ControleHoras.Interfaces.Repositories;

public interface IUnidadeTrabalho
{
    Task TransacionarAsync(int usuarioId, Func<Task> operacao);
}
