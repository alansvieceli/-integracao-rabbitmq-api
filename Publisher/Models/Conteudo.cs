using System.ComponentModel.DataAnnotations;

namespace Publisher.Models
{
    public class Conteudo
    {
        [Required]
        public string Mensagem { get; set; }
    }
}