using CalculadoraEVTC.Data;
using CalculadoraEVTC.Models;

namespace CalculadoraEVTC.Services
{
    public class CalculadoraService : ICalculadoraService
    {
        private readonly ICotacaoRepository _cotacaoRepository;
        private readonly ILogger<CalculadoraService> _logger;
        private const int DiasUteisAno = 252;

        public CalculadoraService(ICotacaoRepository cotacaoRepository, ILogger<CalculadoraService> logger)
        {
            _cotacaoRepository = cotacaoRepository;
            _logger = logger;
        }

        public async Task<CalculoResponse> CalcularInvestimentoAsync(CalculoRequest request)
        {
            _logger.LogInformation("Iniciando cálculo para valor {Valor}, data aplicação {DataAplicacao:yyyy-MM-dd}, data final {DataFinal:yyyy-MM-dd}",
                request.ValorInvestido, request.DataAplicacao, request.DataFinal);

            ValidarRequest(request);

            // Obter cotações do período (incluindo um dia antes para pegar a taxa do primeiro dia útil)
            var dataInicioBusca = request.DataAplicacao.AddDays(-5); // Garantir que pegamos o dia anterior
            var cotacoes = await _cotacaoRepository.ObterCotacoesPorPeriodoAsync(
                dataInicioBusca, request.DataFinal, "SQI");

            if (cotacoes.Count == 0)
            {
                throw new InvalidOperationException("Não foram encontradas cotações para o período especificado");
            }

            // Calcular fator acumulado
            var fatorAcumulado = CalcularFatorAcumulado(cotacoes, request.DataAplicacao, request.DataFinal);

            // Calcular valor atualizado
            var valorAtualizado = Truncar(request.ValorInvestido * fatorAcumulado, 8);

            var diasUteis = ContarDiasUteis(cotacoes, request.DataAplicacao, request.DataFinal);

            _logger.LogInformation("Cálculo concluído: Fator={Fator}, ValorAtualizado={ValorAtualizado}, DiasUteis={DiasUteis}",
                fatorAcumulado, valorAtualizado, diasUteis);

            return new CalculoResponse
            {
                ValorInvestido = request.ValorInvestido,
                DataAplicacao = request.DataAplicacao,
                DataFinal = request.DataFinal,
                FatorAcumulado = fatorAcumulado,
                ValorAtualizado = valorAtualizado,
                DiasUteis = diasUteis
            };
        }

        private void ValidarRequest(CalculoRequest request)
        {
            if (request.ValorInvestido <= 0)
                throw new ArgumentException("O valor investido deve ser maior que zero");

            if (request.DataFinal <= request.DataAplicacao)
                throw new ArgumentException("A data final deve ser posterior à data de aplicação");
        }

        private decimal CalcularFatorAcumulado(List<Cotacao> cotacoes, DateTime dataAplicacao, DateTime dataFinal)
        {
            decimal fatorAcumulado = 1.0m;

            // Agrupar por data (apenas a parte Date) e escolher um valor por dia para evitar exceção de chave duplicada
            var cotacoesDict = cotacoes
                .GroupBy(c => c.Data.Date)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(c => c.Data).First().Valor // se houver vários no mesmo dia, pega o mais recente
                );

            // Obter lista de datas úteis do período (comparando apenas as partes Date) e remover duplicatas
            var datasUteis = cotacoes
                .Where(c => c.Data.Date >= dataAplicacao.Date && c.Data.Date < dataFinal.Date)
                .Select(c => c.Data.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            _logger.LogInformation("Processando {Quantidade} dias úteis", datasUteis.Count);

            foreach (var dataAtual in datasUteis)
            {
                // Para cada dia útil, buscar a taxa do dia útil anterior
                var dataAnterior = ObterDiaUtilAnterior(dataAtual, cotacoesDict);

                if (dataAnterior.HasValue && cotacoesDict.ContainsKey(dataAnterior.Value))
                {
                    var taxaAnual = cotacoesDict[dataAnterior.Value];
                    var fatorDiario = CalcularFatorDiario(taxaAnual);

                    fatorAcumulado *= fatorDiario;

                    _logger.LogDebug("Data {Data:yyyy-MM-dd}: Taxa={Taxa}%, FatorDiario={FatorDiario}, FatorAcum={FatorAcum}",
                        dataAtual, taxaAnual, fatorDiario, fatorAcumulado);
                }
                else
                {
                    _logger.LogWarning("Não foi encontrada taxa do dia anterior para {Data}", dataAtual);
                }
            }

            return Truncar(fatorAcumulado, 16);
        }

        private DateTime? ObterDiaUtilAnterior(DateTime data, Dictionary<DateTime, decimal> cotacoesDict)
        {
            var dataAnterior = data.AddDays(-1);

            // Procurar até 7 dias atrás por um dia útil
            for (int i = 0; i < 7; i++)
            {
                var chave = dataAnterior.Date;
                if (cotacoesDict.ContainsKey(chave))
                    return chave;

                dataAnterior = dataAnterior.AddDays(-1);
            }

            return null;
        }

        private int ContarDiasUteis(List<Cotacao> cotacoes, DateTime dataAplicacao, DateTime dataFinal)
        {
            return cotacoes.Count(c => c.Data.Date >= dataAplicacao.Date && c.Data.Date < dataFinal.Date);
        }

        public static decimal CalcularFatorDiario(decimal taxaAnual)
        {
            // fator_diario = (1 + taxa_anual/100)^(1/252)
            double taxa = (double)(taxaAnual / 100m);
            double base_calculo = 1.0 + taxa;
            double expoente = 1.0 / DiasUteisAno;
            double resultado = Math.Pow(base_calculo, expoente);

            // Arredondar na 8ª casa decimal
            return Arredondar((decimal)resultado, 8);
        }

        private static decimal Arredondar(decimal valor, int casasDecimais)
        {
            return Math.Round(valor, casasDecimais, MidpointRounding.AwayFromZero);
        }

        private static decimal Truncar(decimal valor, int casasDecimais)
        {
            decimal multiplicador = (decimal)Math.Pow(10, casasDecimais);
            return Math.Truncate(valor * multiplicador) / multiplicador;
        }
    }
}