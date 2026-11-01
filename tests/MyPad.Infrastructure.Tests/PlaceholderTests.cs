[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace MyPad.Infrastructure.Tests;

[TestClass]
public class InfrastructureSanityTests
{
    [TestMethod]
    public void InfrastructureAssembly_ShouldLoadSuccessfully()
    {
        var assembly = typeof(InfrastructureSanityTests).Assembly;
        Assert.IsNotNull(assembly);
    }
}
