using Microsoft.CodeAnalysis;
using Microsoft.DotNet.ApiCompatibility.Abstractions;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.DotNet.ApiCompatibility.Tests.Rules
{
    public class MembersMustExistTests
    {
        [Fact]
        public static void MissingMembersAreReported()
        {
            string leftSyntax = @"
namespace CompatTests
{
  public class First
  {
    public string Parameterless() { }
    public void ShouldReportMethod(string a, string b) { }
    public string ShouldReportMissingProperty { get; }
    public string this[int index] { get; }
    public event EventHandler ShouldReportMissingEvent;
    public int ReportMissingField = 0;
  }

  public delegate void EventHandler(object sender EventArgs e);
}
";

            string rightSyntax = @"
namespace CompatTests
{
  public class First
  {
    public string Parameterless() { }
  }
  public delegate void EventHandler(object sender EventArgs e);
}
";

            IAssemblySymbol left = SymbolFactory.GetAssemblyFromSyntax(leftSyntax);
            IAssemblySymbol right = SymbolFactory.GetAssemblyFromSyntax(rightSyntax);
            ApiDiffer differ = new();
            IEnumerable<CompatDifference> differences = differ.GetDifferences(new[] { left }, new[] { right });

            CompatDifference[] expected = new[]
            {
                new CompatDifference(DiagnosticIds.MemberMustExist, "Member 'CompatTests.First.ShouldReportMethod(string, string)' exists on the contract but not on the implementation", DifferenceType.Removed, "M:CompatTests.First.ShouldReportMethod(System.String,System.String)"),
                new CompatDifference(DiagnosticIds.MemberMustExist, "Member 'CompatTests.First.ShouldReportMissingProperty.get' exists on the contract but not on the implementation", DifferenceType.Removed, "M:CompatTests.First.get_ShouldReportMissingProperty"),
                new CompatDifference(DiagnosticIds.MemberMustExist, "Member 'CompatTests.First.this[int].get' exists on the contract but not on the implementation", DifferenceType.Removed, "M:CompatTests.First.get_Item(System.Int32)"),
                new CompatDifference(DiagnosticIds.MemberMustExist, "Member 'CompatTests.First.ShouldReportMissingEvent.add' exists on the contract but not on the implementation", DifferenceType.Removed, "M:CompatTests.First.add_ShouldReportMissingEvent(CompatTests.EventHandler)"),
                new CompatDifference(DiagnosticIds.MemberMustExist, "Member 'CompatTests.First.ShouldReportMissingEvent.remove' exists on the contract but not on the implementation", DifferenceType.Removed, "M:CompatTests.First.remove_ShouldReportMissingEvent(CompatTests.EventHandler)"),
                new CompatDifference(DiagnosticIds.MemberMustExist, "Member 'CompatTests.First.ReportMissingField' exists on the contract but not on the implementation", DifferenceType.Removed, "F:CompatTests.First.ReportMissingField"),
            };

            Assert.Equal(expected, differences);
        }
    }
}
