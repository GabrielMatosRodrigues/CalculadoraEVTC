using CalculadoraEVTC.Models;

namespace CalculadoraEVTC.Data
{
    public interface ICotacaoRepository
    {
        Task<List<Cotacao>> ObterCotacoesPorPeriodoAsync(DateTime dataInicio, DateTime dataFim, string indexador);
        Task<List<Cotacao>> ObterTodasCotacoesAsync();
    }
}
