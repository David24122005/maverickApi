namespace maverickApi.Dtos
{
    public class EditarUsuarioDto
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Apellidos { get; set; }
        public string? Email { get; set; }
        public bool Admin { get; set; }
    }
}