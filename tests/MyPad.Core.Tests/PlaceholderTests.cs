[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace MyPad.Core.Tests;

[TestClass]
public class CoreSanityTests
{
    [TestMethod]
    public void CoreAssembly_ShouldLoadSuccessfully()
    {
        var assembly = typeof(CoreSanityTests).Assembly;
        Assert.IsNotNull(assembly);
    }
}
