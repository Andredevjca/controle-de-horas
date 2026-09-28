namespace ControleHoras.Models;

public static class Formato
{
    public static string Horas(double segundos) => $"{(int)(segundos / 3600):00}h {(int)(segundos % 3600 / 60):00}min";
    public static string Dinheiro(decimal? valor) => valor?.ToString("C2") ?? "—";
}
