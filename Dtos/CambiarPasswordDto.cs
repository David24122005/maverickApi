namespace maverickApi.Dtos
{
    public class CambiarPasswordDto
    {
        public int UsuarioId { get; set; }
        public string? PasswordActual { get; set; }
        public string? NuevaPassword { get; set; }
    }
}