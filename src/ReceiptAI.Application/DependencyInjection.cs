using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ReceiptAI.Application.Common.Behaviors;

namespace ReceiptAI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Registers every IValidator<T> in this assembly without needing FluentValidation.DependencyInjectionExtensions.
        var validators = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsGenericTypeDefinition: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))
                .Select(i => (Service: i, Implementation: t)));

        foreach (var (service, implementation) in validators)
            services.AddScoped(service, implementation);

        return services;
    }
}
