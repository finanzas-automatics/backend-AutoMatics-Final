using AutoMatics.Controllers.Resources;

namespace AutoMatics.Domain.Clientes.Model.Commands
{
    // ✨ AHORA EXIGIMOS EL ID DEL USUARIO COMO PRIMER PARÁMETRO
    public record CrearClienteCommand(
        int UsuarioId, 
        string TipoDocumento, string NumeroDocumento, string Nombres, string Apellidos, 
        string? Correo, string? Telefono, string? Direccion, decimal IngresosNetosMensuales,
        VehicleCreateResource? Vehiculo
    );
}