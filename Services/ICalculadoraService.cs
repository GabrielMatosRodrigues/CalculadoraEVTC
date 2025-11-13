using CalculadoraEVTC.Models;

namespace CalculadoraEVTC.Services
{
    public interface ICalculadoraService
    {
        Task<CalculoResponse> CalcularInvestimentoAsync(CalculoRequest request);
    }
}
