using System.Collections.Concurrent;
using System.Diagnostics;
using NUnit.Framework.Internal;


namespace NoeticTools.Git2SemVer.Testing.Core;

/// <summary>
///     Manage the creation and release of directories used by concurrent test cases.
/// </summary>
public static class TestDirectoryResource
{
    private static readonly ConcurrentDictionary<TestExecutionContext, DirectoryInfo> ResourceByTestContext = [];
    private static int _nextContextId;

    public static DirectoryInfo Get()
    {
        var currentContext = TestExecutionContext.CurrentContext;
        if (ResourceByTestContext.TryGetValue(currentContext, out var directory))
        {
            return directory;
        }

        var contextId = _nextContextId++;
        var testFolderName = $"{currentContext.CurrentTest.MethodName}.TestFolder{contextId}";
        var testFolderPath = Path.Combine(TestContext.CurrentContext.TestDirectory, testFolderName);
        Assert.That(testFolderPath, Does.Not.Exist, "The test directory '{0}' already exists.", testFolderPath);
        directory = Directory.CreateDirectory(testFolderPath);

        if (!ResourceByTestContext.TryAdd(currentContext, directory))
        {
            Assert.Fail("The context already has an assigned test directory.");
        }

        return directory;
    }

    public static void Release()
    {
        var currentContext = TestExecutionContext.CurrentContext;
        if (!ResourceByTestContext.Remove(currentContext, out var directory))
        {
            Assert.Fail("Context does not have a test directory to release.");
        }

        directory!.Delete(true);
        if (!TestHelper.WaitUntil(() => !directory.Exists))
        {
            Assert.Fail($"Unable to release a {directory.FullName}.");
        }
    }
}