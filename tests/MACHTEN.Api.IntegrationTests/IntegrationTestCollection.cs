namespace MACHTEN.Api.IntegrationTests;

/// <summary>
/// One container set for the whole assembly.
///
/// With IClassFixture, xUnit builds a separate factory per test class and runs
/// those classes in parallel — three SQL Server and three Redis containers at
/// once. That is survivable on a developer machine and not on a 7 GB CI runner,
/// where SQL Server alone wants ~2 GB apiece.
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<MachtenApiFactory>
{
    public const string Name = "integration";
}
