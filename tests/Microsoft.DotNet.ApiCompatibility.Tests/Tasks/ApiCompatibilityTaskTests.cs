using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace Microsoft.DotNet.ApiCompatibility.Tests
{
    internal static class FileHelpers
    {
        internal static string GetTempDirectory([CallerMemberName] string memberName = null)
        {
            string directory = Path.Combine(Path.GetTempPath(), $"{memberName}_{Guid.NewGuid().ToString("N").Substring(0, 4)}");

            Directory.CreateDirectory(directory);
            return directory;
        }
    }
    public class ApiCompatibilityTaskTests
    {
        private const string _taskAssemblyName = "Microsoft.DotNet.ApiCompatibility.Tasks";
        private readonly string _taskAssembly = Path.Combine(AppContext.BaseDirectory, _taskAssemblyName + ".dll");
        private readonly string _propsPath = Path.Combine(AppContext.BaseDirectory, _taskAssemblyName + ".props");
        private readonly string _targetsPath = Path.Combine(AppContext.BaseDirectory, _taskAssemblyName + ".targets");

        [Fact]
        public void ProjectsWithSameTypesNoErrors()
        {

        }
    }
}
