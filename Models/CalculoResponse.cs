namespace CalculadoraEVTC.Models
{
    public class CalculoResponse
    {
        public decimal ValorInvestido { get; set; }
        public DateTime DataAplicacao { get; set; }
        public DateTime DataFinal { get; set; }
        public decimal FatorAcumulado { get; set; }
        public decimal ValorAtualizado { get; set; }
        public int DiasUteis { get; set; }
    }
}
