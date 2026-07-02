namespace AutoMatics.Domain.IAM.Model.Aggregates
{
    public class Usuario
    {
        public int Id { get; private set; }
        public string Nombres { get; private set; } = string.Empty;
        public string Apellidos { get; private set; } = string.Empty;
        public string Correo { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public string Dni { get; private set; } = string.Empty; 
        protected Usuario() { }

        public Usuario(string nombres, string apellidos, string correo, string passwordHash, string dni)
        {
            Nombres = nombres;
            Apellidos = apellidos;
            Correo = correo;
            PasswordHash = passwordHash;
            Dni = dni;
        }
    }
}