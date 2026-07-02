using Microsoft.Extensions.DependencyInjection;
using AutoMatics.Application.IAM.Interfaces;
using AutoMatics.Application.IAM.Internal;
using AutoMatics.Application.Clientes.Interfaces;
using AutoMatics.Application.Clientes.Internal;
using AutoMatics.Application.Creditos.Interfaces;
using AutoMatics.Application.Creditos.Internal.CommandServices;
using AutoMatics.Application.Creditos.Internal.QueryServices;
using AutoMatics.Domain.Creditos.Services;


namespace AutoMatics.Application.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<MotorFinancieroDomainService>();
            services.AddScoped<IAuthCommandService, AuthCommandService>();
            services.AddScoped<IClienteCommandService, ClienteCommandService>();
            services.AddScoped<ISimulacionCommandService, SimulacionCommandService>();
            services.AddScoped<ISimulacionQueryService, SimulacionQueryService>();
            return services;
        }
    }
}