using AutoMatics.Controllers.Resources;

namespace AutoMatics.Domain.Clientes.Model.Commands
{
    // Usamos el Resource del vehículo directamente aquí para no crear 20 variables
    public record CrearClienteCommand(
        string TipoDocumento, string NumeroDocumento, string Nombres, string Apellidos, 
        string? Correo, string? Telefono, string? Direccion, decimal IngresosNetosMensuales,
        VehicleCreateResource? Vehiculo
    );
}