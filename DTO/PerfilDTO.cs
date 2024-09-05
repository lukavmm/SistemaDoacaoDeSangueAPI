namespace SistemaDoacaoSangue.DTO
{
    public class PerfilDTO
    {
        //TABELA DOADORES
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Tipo_usuario { get; set; }
        public string? NomeCompleto { get; set; }
        public DateTime? DataNascimento { get; set; }
        public string? TipoSanguineo { get; set; }
        public string? Sexo { get; set; }
        public string? Endereco { get; set; }
        public string? Telefone { get; set; }
        public DateTime? DataUltimaDoacao { get; set; }
        public string? Cpf { get; set; }
        public DateTime? DataAtualizacao { get; set; }
        public decimal? Peso { get; set; }


        //TABELA HEMOCENTROS

        public string? NomeHemocentro { get; set; }
        public string? Cpnj { get; set; }
    }
}
