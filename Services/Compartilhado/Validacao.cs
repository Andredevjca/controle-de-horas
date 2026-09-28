namespace ControleHoras.Services;

public static class Validacao
{
    public static void Exigir(bool condicao, string mensagem)
    {
        if (!condicao) throw new InvalidOperationException(mensagem);
    }
}
