namespace SistemaDoacaoSangue.DTO
{
    public class AgendamentoDTO
    {
        public DateTime Data { get; set; }         // Data do agendamento
        public TimeSpan Hora { get; set; }         // Hora do agendamento
        public string? Status { get; set; }
        public string? Obs { get; set; }

    }
}
