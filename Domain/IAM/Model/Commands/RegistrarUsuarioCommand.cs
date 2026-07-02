namespace AutoMatics.Domain.IAM.Model.Commands
{
    public record RegistrarUsuarioCommand(string Nombres, string Apellidos, string Correo, string Password, string Dni);
}