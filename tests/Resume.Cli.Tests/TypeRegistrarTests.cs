using Microsoft.Extensions.DependencyInjection;

using Resume.Cli.Infrastructure;

namespace Resume.Cli.Tests;

public sealed class TypeRegistrarTests
{
  [Fact]
  public void Register_RegisterInstance_And_RegisterLazy_ResolveExpectedInstances()
  {
    var services = new ServiceCollection();
    var registrar = new TypeRegistrar(services);

    registrar.Register(typeof(IDummyService), typeof(DummyService));
    registrar.RegisterInstance(typeof(string), "hello");
    registrar.RegisterLazy(typeof(Uri), static () => new Uri("https://example.com"));

    var resolver = registrar.Build();

    try
    {
      resolver.Resolve(null).Should().BeNull();
      resolver.Resolve(typeof(IDummyService)).Should().BeOfType<DummyService>().Which.Value.Should().Be("default");
      resolver.Resolve(typeof(string)).Should().Be("hello");
      resolver.Resolve(typeof(Uri)).Should().Be(new Uri("https://example.com"));
    }
    finally
    {
      (resolver as IDisposable)?.Dispose();
    }
  }

  [Fact]
  public void GuardClauses_ThrowOnNullArguments()
  {
    var registrar = new TypeRegistrar(new ServiceCollection());

    var act1 = () => registrar.Register(null!, typeof(DummyService));
    var act2 = () => registrar.Register(typeof(IDummyService), null!);
    var act3 = () => registrar.RegisterInstance(null!, "x");
    var act4 = () => registrar.RegisterInstance(typeof(string), null!);
    var act5 = () => registrar.RegisterLazy(null!, static () => "x");
    var act6 = () => registrar.RegisterLazy(typeof(string), null!);

    act1.Should().Throw<ArgumentNullException>();
    act2.Should().Throw<ArgumentNullException>();
    act3.Should().Throw<ArgumentNullException>();
    act4.Should().Throw<ArgumentNullException>();
    act5.Should().Throw<ArgumentNullException>();
    act6.Should().Throw<ArgumentNullException>();
  }

  private interface IDummyService
  {
    string Value { get; }
  }

  private sealed class DummyService : IDummyService
  {
    public string Value => "default";
  }
}
