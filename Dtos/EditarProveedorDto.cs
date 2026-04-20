namespace maverickApi.Dtos
{
    public class EditarProveedorDto
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public string? Rfc { get; set; }
        public bool Activo { get; set; }
    }
}