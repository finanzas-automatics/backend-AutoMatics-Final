using System.Collections.Generic;

namespace AutoMatics.Domain.Clientes.Model.Aggregates
{
    public class Cliente
    {
        public int Id { get; private set; }

        public int UsuarioId { get; private set; }

        public string TipoDocumento { get; private set; } = string.Empty;
        public string NumeroDocumento { get; private set; } = string.Empty;
        public string Nombres { get; private set; } = string.Empty;
        public string Apellidos { get; private set; } = string.Empty;
        public string? Correo { get; private set; }
        public string? Telefono { get; private set; }
        public string? Direccion { get; private set; }
        public decimal IngresosNetosMensuales { get; private set; }
        public string EstadoCrediticio { get; private set; } = "En Evaluación";

        public Vehiculo? VehiculoObjetivo { get; private set; }
        public ICollection<SustentoCliente> Sustentos { get; private set; } = new List<SustentoCliente>();

        protected Cliente() { }

        public Cliente(int usuarioId, string tipoDocumento, string numeroDocumento, string nombres, string apellidos, string? correo, string? telefono, string? direccion, decimal ingresosNetosMensuales)
        {
            UsuarioId = usuarioId;
            TipoDocumento = tipoDocumento;
            NumeroDocumento = numeroDocumento;
            Nombres = nombres;
            Apellidos = apellidos;
            Correo = correo;
            Telefono = telefono;
            Direccion = direccion;
            IngresosNetosMensuales = ingresosNetosMensuales;
        }

        public void UpdateData(string nombres, string apellidos, string? correo, string? telefono, string? direccion, decimal ingresosNetosMensuales, string estadoCrediticio)
        {
            Nombres = nombres;
            Apellidos = apellidos;
            Correo = correo;
            Telefono = telefono;
            Direccion = direccion;
            IngresosNetosMensuales = ingresosNetosMensuales;
            EstadoCrediticio = estadoCrediticio;
        }

        public void AgregarSustento(SustentoCliente sustento)
        {
            Sustentos.Add(sustento);
        }

        public void AsignarVehiculo(string marca, string modelo, int? año, decimal precio, string moneda, string estado, string? tipoCombustible, string? transmision, string? motor)
        {
            VehiculoObjetivo = new Vehiculo(marca, modelo, año, precio, moneda, estado, tipoCombustible, transmision, motor);
        }
    }

    public class Vehiculo
    {
        public int Id { get; private set; }
        public int ClienteId { get; private set; }
        public string Marca { get; private set; } = string.Empty;
        public string Modelo { get; private set; } = string.Empty;
        public int? Año { get; private set; }
        public decimal Precio { get; private set; }
        public string Moneda { get; private set; } = string.Empty;
        public string Estado { get; private set; } = string.Empty;
        public string? TipoCombustible { get; private set; }
        public string? Transmision { get; private set; }
        public string? Motor { get; private set; }

        protected Vehiculo() { }

        public Vehiculo(string marca, string modelo, int? año, decimal precio, string moneda, string estado, string? tipoCombustible, string? transmision, string? motor)
        {
            Marca = marca;
            Modelo = modelo;
            Año = año;
            Precio = precio;
            Moneda = moneda;
            Estado = estado;
            TipoCombustible = tipoCombustible;
            Transmision = transmision;
            Motor = motor;
        }
    }
}