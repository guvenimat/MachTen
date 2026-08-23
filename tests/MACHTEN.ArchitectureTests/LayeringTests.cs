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

    [Test]
    public async Task Domain_DoesNotDependOnAnyOtherLayer()
    {
        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        await AssertSuccess(result, "Domain must not reference Application, Infrastructure or Api.");
    }

    [Test]
    public async Task Application_DoesNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        await AssertSuccess(result, "Application must talk to Infrastructure through its own Contracts, never directly.");
    }

    [Test]
    public async Task Infrastructure_DoesNotDependOnApi()
    {
        var result = Types.InAssembly(Infrastructure)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        await AssertSuccess(result, "Infrastructure must not reach back into the web layer.");
    }

    [Test]
    public async Task Domain_DoesNotDependOnEntityFrameworkOrAspNetCore()
    {
        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        await AssertSuccess(result, "Domain must stay free of persistence and hosting concerns.");
    }

    [Test]
    public async Task Application_DoesNotDependOnAspNetCore()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        await AssertSuccess(result, "Use cases must be callable without a web host — that is what keeps them testable.");
    }

    [Test]
    public async Task Application_DoesNotDependOnEntityFrameworkCore()
    {
        // Orders and auth read through Dapper.AOT now, so the use cases have no
        // reason to see EF. EF survives only in Infrastructure, where it owns
        // the schema and generates migrations — and that is the last thing
        // standing between this template and Native AOT.
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        await AssertSuccess(result, "Application must stay free of EF Core.");
    }

    private static async Task AssertSuccess(NetArchTest.Rules.TestResult result, string because)
    {
        await Assert.That(result.IsSuccessful)
            .IsTrue()
            .Because($"{because}{Environment.NewLine}Offending types:{Environment.NewLine}" +
                     string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }
}
