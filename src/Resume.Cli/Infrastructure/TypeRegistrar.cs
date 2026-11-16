using Microsoft.Extensions.DependencyInjection;

using Spectre.Console.Cli;

namespace Resume.Cli.Infrastructure;

internal sealed class TypeRegistrar(IServiceCollection services) : ITypeRegistrar
{
  public ITypeResolver Build() => new TypeResolver(services.BuildServiceProvider());

  public void Register(Type service, Type implementation)
  {
    ArgumentNullException.ThrowIfNull(service);
    ArgumentNullException.ThrowIfNull(implementation);
    services.AddSingleton(service, implementation);
  }

  public void RegisterInstance(Type service, object implementation)
  {
    ArgumentNullException.ThrowIfNull(service);
    ArgumentNullException.ThrowIfNull(implementation);
    services.AddSingleton(service, implementation);
  }

  public void RegisterLazy(Type service, Func<object> factory)
  {
    ArgumentNullException.ThrowIfNull(service);
    ArgumentNullException.ThrowIfNull(factory);
    services.AddSingleton(service, _ => factory());
  }

  private sealed class TypeResolver(ServiceProvider provider) : ITypeResolver, IDisposable
  {
    public object? Resolve(Type? type) => type is null ? null : provider.GetService(type);

    public void Dispose() => provider.Dispose();
  }
}
