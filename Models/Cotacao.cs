namespace CalculadoraEVTC.Models
{
    public class Cotacao
    {
        public int Id { get; set; }
        public DateTime Data { get; set; }
        public string Indexador { get; set; } = string.Empty;
        public decimal Valor { get; set; }
    }
}
