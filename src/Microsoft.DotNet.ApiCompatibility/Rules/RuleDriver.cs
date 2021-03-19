using Microsoft.CodeAnalysis;
using Microsoft.DotNet.ApiCompatibility.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.ApiCompatibility.Rules
{
    public interface IRuleDriverFactory
    {
        IRuleDriver GetRuleDriver();
    }

    public class RuleDriverFactory : IRuleDriverFactory
    {
        private RuleDriver _driver;
        public IRuleDriver GetRuleDriver()
        {
            if (_driver == null)
                _driver = new RuleDriver();

            return _driver;
        }
    }

    public interface IRuleDriver
    {
        IEnumerable<CompatDifference> Run<T>(ElementMapper<T> mapper);
    }

    internal class RuleDriver : IRuleDriver
    {
        private readonly IEnumerable<Rule> _rules;

        internal RuleDriver()
        {
            _rules = GetRules();
        }

        public IEnumerable<CompatDifference> Run<T>(ElementMapper<T> mapper)
        {
            List<CompatDifference> differences = new();
            if (mapper is AssemblyMapper am)
            {
                Run(am.Left, am.Right, differences);
            }
            if (mapper is TypeMapper tm)
            {
                Run(tm.Left, tm.Right, differences);
            }

            return differences;
        }

        private void Run(IAssemblySymbol left, IAssemblySymbol right, List<CompatDifference> difference)
        {
            foreach (Rule rule in _rules)
            {
                rule.Run(left, right, difference);
            }
        }

        private void Run(ITypeSymbol left, ITypeSymbol right, List<CompatDifference> difference)
        {
            foreach (Rule rule in _rules)
            {
                rule.Run(left, right, difference);
            }
        }

        private IEnumerable<Rule> GetRules()
        {
            return this.GetType().Assembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(Rule).IsAssignableFrom(t))
                .Select(t => (Rule)Activator.CreateInstance(t));
        }
    }
}
