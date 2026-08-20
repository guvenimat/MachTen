using System.Reflection;
using NetArchTest.Rules;

namespace MACHTEN.ArchitectureTests;

/// <summary>
/// Turns the Clean Architecture dependency rule into something the build
/// enforces. Without these, "Domain depends on nothing" is just a claim in the
/// README that decays the first time someone adds a convenient using.
/// </summary>
public class LayeringTests
{
    private static readonly Assembly Domain = typeof(Domain.ValueObjects.Money).Assembly;
    private static readonly Assembly Application = typeof(Application.Contracts.ICacheStore).Assembly;
    private static readonly Assembly Infrastructure = typeof(Infrastructure.Persistence.MachtenDbContext).Assembly;

    private const string ApplicationNamespace = "MACHTEN.Application";
    private const string InfrastructureNamespace = "MACHTEN.Infrastructure";
    private const string ApiNamespace = "MACHTEN.Api";

    [Fact]
    public void Domain_DoesNotDependOnAnyOtherLayer()
    {
        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        AssertSuccess(result, "Domain must not reference Application, Infrastructure or Api.");
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        AssertSuccess(result, "Application must talk to Infrastructure through its own Contracts, never directly.");
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnApi()
    {
        var result = Types.InAssembly(Infrastructure)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        AssertSuccess(result, "Infrastructure must not reach back into the web layer.");
    }

    [Fact]
    public void Domain_DoesNotDependOnEntityFrameworkOrAspNetCore()
    {
        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        AssertSuccess(result, "Domain must stay free of persistence and hosting concerns.");
    }

    [Fact]
    public void Application_DoesNotDependOnAspNetCore()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        AssertSuccess(result, "Use cases must be callable without a web host — that is what keeps them testable.");
    }

    private static void AssertSuccess(TestResult result, string because)
    {
        Assert.True(
            result.IsSuccessful,
            $"{because}{Environment.NewLine}Offending types:{Environment.NewLine}" +
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }
}
