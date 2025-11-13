using CalculadoraEVTC.Data;
using CalculadoraEVTC.Models;
using CalculadoraEVTC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CalculadoraEVTC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CalculadoraController : ControllerBase
    {
        private readonly ICalculadoraService _calculadoraService;
        private readonly ICotacaoRepository _cotacaoRepository;
        private readonly CalculadoraContext _context;
        private readonly ILogger<CalculadoraController> _logger;

        public CalculadoraController(
            ICalculadoraService calculadoraService,
            ICotacaoRepository cotacaoRepository,
            CalculadoraContext context,
            ILogger<CalculadoraController> logger)
        {
            _calculadoraService = calculadoraService;
            _cotacaoRepository = cotacaoRepository;
            _context = context;
            _logger = logger;
        }

        [HttpPost("calcular")]
        public async Task<ActionResult<CalculoResponse>> Calcular([FromBody] CalculoRequest request)
        {
            try
            {
                _logger.LogInformation("Recebida requisição de cálculo");
                var resultado = await _calculadoraService.CalcularInvestimentoAsync(request);
                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Erro de validação: {Mensagem}", ex.Message);
                return BadRequest(new { erro = ex.Message });
            }
            catch (InvalidOperationException ex) // trata caso de dados ausentes
            {
                _logger.LogWarning(ex, "InvalidOperation no cálculo");
                return BadRequest(new { erro = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar cálculo");
                return StatusCode(500, new { erro = "Erro interno ao processar o cálculo" });
            }
        }

        [HttpGet("cotacoes")]
        public async Task<ActionResult<List<Cotacao>>> ListarCotacoes()
        {
            try
            {
                _logger.LogInformation("Recebida requisição para listar cotações");
                var cotacoes = await _cotacaoRepository.ObterTodasCotacoesAsync();
                return Ok(cotacoes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar cotações");
                return StatusCode(500, new { erro = "Erro ao listar cotações" });
            }
        }

        // Endpoint de diagnóstico: mostra connection string, total de linhas e 5 primeiros registros
        [HttpGet("debug/dbinfo")]
        public async Task<ActionResult> DebugDbInfo()
        {
            try
            {
                var connString = _context.Database.GetDbConnection().ConnectionString;
                var count = await _context.Cotacoes.CountAsync();
                var sample = await _context.Cotacoes.OrderBy(c => c.Id).Take(5).ToListAsync();

                return Ok(new
                {
                    connectionString = connString,
                    totalRows = count,
                    sample
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter debug dbinfo");
                return StatusCode(500, new { erro = ex.Message });
            }
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }
    }
}