using System;

namespace AutoMatics.Controllers.Resources
{
    public record ClienteCreateResource(
        string DocumentType,
        string DocumentNumber,
        string FirstName,
        string LastName,
        string? Email,
        string? Phone,
        string? Address,
        decimal MonthlyIncome,
        VehicleCreateResource? Vehicle
    );

    public record ClienteUpdateResource(
        string DocumentType,
        string DocumentNumber,
        string FirstName,
        string LastName,
        string? Email,
        string? Phone,
        string? Address,
        decimal MonthlyIncome,
        string Status,
        VehicleCreateResource? Vehicle
    );
    public record VehicleCreateResource(
        string Brand,
        string Model,
        int? Year,
        decimal Price,
        string Currency,
        string Status,
        string? FuelType,
        string? Transmission,
        string? Engine
    );

    public record ClienteListResponse(
        int Id,
        string FullName,
        string DocumentNumber,
        string? Email,
        string Status,
        string? VehicleName,
        decimal? VehiclePrice,
        string? VehicleCurrency,
        int? VehicleId
    );

    public record ClienteDetalleResource(
        int Id,
        string DocumentType,
        string DocumentNumber,
        string FirstName,
        string LastName,
        string FullName,
        string? Email,
        string? Phone,
        string? Address,
        decimal MonthlyIncome,
        string Status,
        VehicleDetalleResource? Vehicle
    );

    public record VehicleDetalleResource(
        int Id,
        string Brand,
        string Model,
        int? Year,
        decimal Price,
        string Currency,
        string Status,
        string? FuelType,
        string? Transmission,
        string? Engine
    );
}