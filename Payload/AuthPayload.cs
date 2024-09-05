namespace SistemaDoacaoSangue.Payloads
{
    public class AuthPayload
    {
        public string ?Token { get; set; }
        public string ?Message { get; set; }
        public string ?Id { get; set; }
        public string ?Tipo_usuario { get; set; }
    }
}
