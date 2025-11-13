using CalculadoraEVTC.Models;
using Microsoft.EntityFrameworkCore;

namespace CalculadoraEVTC.Data
{
    public class CotacaoRepository : ICotacaoRepository
    {
        private readonly CalculadoraContext _context;
        private readonly ILogger<CotacaoRepository> _logger;

        public CotacaoRepository(CalculadoraContext context, ILogger<CotacaoRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Cotacao>> ObterCotacoesPorPeriodoAsync(DateTime dataInicio, DateTime dataFim, string indexador)
        {
            _logger.LogInformation("Buscando cotações do indexador {Indexador} entre {DataInicio:yyyy-MM-dd} e {DataFim:yyyy-MM-dd}",
                indexador, dataInicio, dataFim);

            // Normalizar parâmetros para evitar problemas com time-component e caixa/espaços no indexador
            var dataInicioSemHora = dataInicio.Date;
            var dataFimInclusivo = dataFim.Date.AddDays(1).AddTicks(-1); // inclui todo o dia final
            var indexadorNormalized = (indexador ?? string.Empty).Trim().ToLowerInvariant();

            var cotacoes = await _context.Cotacoes
                .AsNoTracking()
                .Where(c => c.Indexador.ToLower() == indexadorNormalized && c.Data >= dataInicioSemHora && c.Data <= dataFimInclusivo)
                .OrderBy(c => c.Data)
                .ToListAsync();

            _logger.LogInformation("Encontradas {Quantidade} cotações", cotacoes.Count);
            return cotacoes;
        }

        public async Task<List<Cotacao>> ObterTodasCotacoesAsync()
        {
            _logger.LogInformation("Buscando todas as cotações");
            return await _context.Cotacoes.OrderBy(c => c.Data).ToListAsync();
        }
    }
}
