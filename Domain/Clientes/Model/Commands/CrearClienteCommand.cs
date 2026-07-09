using AutoMatics.Controllers.Resources;

namespace AutoMatics.Domain.Clientes.Model.Commands
{

    public record CrearClienteCommand(
        int UsuarioId,
        string TipoDocumento, string NumeroDocumento, string Nombres, string Apellidos,
        string? Correo, string? Telefono, string? Direccion, decimal IngresosNetosMensuales,
        VehicleCreateResource? Vehiculo
    );
}