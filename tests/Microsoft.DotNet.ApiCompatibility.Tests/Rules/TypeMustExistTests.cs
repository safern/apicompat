using Microsoft.CodeAnalysis;
using Microsoft.DotNet.ApiCompatibility.Abstractions;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.DotNet.ApiCompatibility.Tests
{
    public class TypeMustExistTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("CP002")]
        public void MissingPublicTypeInRightIsReported(string noWarn)
        {
            string leftSyntax = @"

namespace CompatTests
{
  public class First { }
  public class Second { }
}
";

            string rightSyntax = @"
namespace CompatTests
{
  public class First { }
}
";

            ApiDiffer differ = new();
            differ.NoWarn = noWarn;
            bool enableNullable = false;
            IAssemblySymbol left = SymbolFactory.GetAssemblyFromSyntax(leftSyntax, enableNullable);
            IAssemblySymbol right = SymbolFactory.GetAssemblyFromSyntax(rightSyntax, enableNullable);
            IEnumerable<CompatDifference> differences = differ.GetDifferences(new[] { left }, new[] { right });

            CompatDifference[] expected = new []
            {
                new CompatDifference(DiagnosticIds.TypeMustExist, $"Type 'CompatTests.Second' exists on the contract but not on the implementation", DifferenceType.Removed, "T:CompatTests.Second")
            };

            Assert.Equal(expected, differences);
        }

        [Fact]
        public void MissingTypeFromTypeForwardIsReported()
        {
            string forwardedTypeSyntax = @"
namespace CompatTests
{
  public class ForwardedTestType { }
}
";
            string leftSyntax = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(CompatTests.ForwardedTestType))]
namespace CompatTests
{
  public class First { }
}
";

            string rightSyntax = @"
namespace CompatTests
{
  public class First { }
}
";
            IAssemblySymbol left = SymbolFactory.GetAssemblyFromSyntaxWithReferences(leftSyntax, new[] { forwardedTypeSyntax });
            IAssemblySymbol right = SymbolFactory.GetAssemblyFromSyntax(rightSyntax);
            ApiDiffer differ = new();
            IEnumerable<CompatDifference> differences = differ.GetDifferences(new[] { left }, new []{ right });

            CompatDifference[] expected = new []
            {
                new CompatDifference(DiagnosticIds.TypeMustExist, $"Type 'CompatTests.ForwardedTestType' exists on the contract but not on the implementation", DifferenceType.Removed, "T:CompatTests.ForwardedTestType")
            };

            Assert.Equal(expected, differences);
        }

        [Fact]
        public void TypeForwardExistsOnBoth()
        {
            string forwardedTypeSyntax = @"
namespace CompatTests
{
  public class ForwardedTestType { }
}
";
            string syntax = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(CompatTests.ForwardedTestType))]
namespace CompatTests
{
  public class First { }
}
";
            IEnumerable<string> references = new[] { forwardedTypeSyntax };
            IAssemblySymbol left = SymbolFactory.GetAssemblyFromSyntaxWithReferences(syntax, references);
            IAssemblySymbol right = SymbolFactory.GetAssemblyFromSyntaxWithReferences(syntax, references);
            ApiDiffer differ = new();
            Assert.Empty(differ.GetDifferences(new[] { left }, new[] { right }));
        }

        [Fact]
        public void NoDifferencesReportedWithNoWarn()
        {
            string leftSyntax = @"

namespace CompatTests
{
  public class First { }
  public class Second { }
}
";

            string rightSyntax = @"
namespace CompatTests
{
  public class First { }
}
";

            ApiDiffer differ = new();
            differ.NoWarn = DiagnosticIds.TypeMustExist;
            IAssemblySymbol left = SymbolFactory.GetAssemblyFromSyntax(leftSyntax);
            IAssemblySymbol right = SymbolFactory.GetAssemblyFromSyntax(rightSyntax);
            Assert.Empty(differ.GetDifferences(new[] { left }, new[] { right }));
        }

        [Fact]
        public void DifferenceIsIgnoredForMember()
        {
            string leftSyntax = @"

namespace CompatTests
{
  public class First { }
  public class Second { }
  public class Third { }
  public class Fourth { }
}
";

            string rightSyntax = @"
namespace CompatTests
{
  public class First { }
}
";

            (string, string)[] ignoredDifferences = new[]
            {
                (DiagnosticIds.TypeMustExist, "T:CompatTests.Second"),
            };

            ApiDiffer differ = new();
            differ.IgnoredDifferences = ignoredDifferences;

            IAssemblySymbol left = SymbolFactory.GetAssemblyFromSyntax(leftSyntax);
            IAssemblySymbol right = SymbolFactory.GetAssemblyFromSyntax(rightSyntax);
            IEnumerable<CompatDifference> differences = differ.GetDifferences(new[] { left }, new[] { right });

            CompatDifference[] expected = new[]
            {
                new CompatDifference(DiagnosticIds.TypeMustExist, $"Type 'CompatTests.Third' exists on the contract but not on the implementation", DifferenceType.Removed, "T:CompatTests.Third"),
                new CompatDifference(DiagnosticIds.TypeMustExist, $"Type 'CompatTests.Fourth' exists on the contract but not on the implementation", DifferenceType.Removed, "T:CompatTests.Fourth")
            };

            Assert.Equal(expected, differences);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void InternalTypesAreIgnoredWhenSpecified(bool includeInternalSymbols)
        {
            string leftSyntax = @"

namespace CompatTests
{
  public class First { }
  internal class InternalType { }
}
";

            string rightSyntax = @"
namespace CompatTests
{
  public class First { }
}
";

            ApiDiffer differ = new(includeInternalSymbols);
            IAssemblySymbol left = SymbolFactory.GetAssemblyFromSyntax(leftSyntax);
            IAssemblySymbol right = SymbolFactory.GetAssemblyFromSyntax(rightSyntax);
            IEnumerable<CompatDifference> differences = differ.GetDifferences(new[] { left }, new[] { right });
            
            if (!includeInternalSymbols)
            {
                Assert.Empty(differences);
            }
            else
            {
                CompatDifference[] expected = new []
                {
                    new CompatDifference(DiagnosticIds.TypeMustExist, $"Type 'CompatTests.InternalType' exists on the contract but not on the implementation", DifferenceType.Removed, "T:CompatTests.InternalType")
                };

                Assert.Equal(expected, differences);
            }
        }
    }

}
