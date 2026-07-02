namespace AutoMatics.Domain.Clientes.Model.Commands
{
    public record ActualizarClienteCommand(
        int Id, string Nombres, string Apellidos, string Correo, string Telefono, 
        string Direccion, decimal IngresosNetosMensuales, string EstadoCrediticio
    );
}