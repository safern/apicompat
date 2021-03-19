using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Microsoft.DotNet.ApiCompatibility
{
    public class AssemblyLoader
    {
        private readonly HashSet<string> _dependencyDirectories = new();
        private readonly Dictionary<string, MetadataReference> _loadedAssemblies;
        private CSharpCompilation _cSharpCompilation;

        public AssemblyLoader()
        {
            _loadedAssemblies = new Dictionary<string, MetadataReference>();
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable);
            _cSharpCompilation = CSharpCompilation.Create($"AssemblyLoader_{DateTime.Now:MM_dd_yy_HH_mm_ss_FFF}", options: compilationOptions);
        }

        public void AddDependencyPath(string paths)
        {
            foreach (string path in SplitPaths(paths))
                _dependencyDirectories.Add(path);
        }

        public bool HasDiagnostics(out IEnumerable<Diagnostic> diagnostics)
        {
            diagnostics = _cSharpCompilation.GetDiagnostics();
            return diagnostics.Any();
        }

        private static string[] SplitPaths(string paths) =>
            paths == null ? Array.Empty<string>() : paths.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        public IEnumerable<IAssemblySymbol> LoadAssemblies(string paths)
        {
            string[] assemblyPaths = SplitPaths(paths);

            if (assemblyPaths.Length == 0)
            {
                return Array.Empty<IAssemblySymbol>();
            }

            IEnumerable<MetadataReference> assembliesToReturn = LoadFromPaths(assemblyPaths);

            List<IAssemblySymbol> result = new List<IAssemblySymbol>();
            foreach (MetadataReference assembly in assembliesToReturn)
            {
                ISymbol symbol = _cSharpCompilation.GetAssemblyOrModuleSymbol(assembly);
                if (symbol is IAssemblySymbol assemblySymbol)
                {
                    result.Add(assemblySymbol);
                }
            }

            return result;
        }

        public IEnumerable<IAssemblySymbol> LoadMatchingAssemblies(IEnumerable<IAssemblySymbol> fromAssemblies, IEnumerable<string> searchPaths)
        {
            List<IAssemblySymbol> matchingAssemblies = new List<IAssemblySymbol>();
            foreach (IAssemblySymbol assembly in fromAssemblies)
            {
                bool found = false;
                string name = $"{assembly.Name}.dll";
                foreach (string directory in searchPaths)
                {
                    if (!Directory.Exists(directory))
                    {
                        throw new ArgumentException($"Directory '{directory}' does not exist", nameof(searchPaths));
                    }

                    string possiblePath = Path.Combine(directory, name);
                    if (File.Exists(possiblePath))
                    {
                        MetadataReference reference = CreateMetadataReferenceIfNeeded(possiblePath);
                        ISymbol symbol = _cSharpCompilation.GetAssemblyOrModuleSymbol(reference);
                        if (symbol is IAssemblySymbol matchingAssembly)
                        {
                            if (!matchingAssembly.Identity.Equals(assembly.Identity))
                            {
                                _cSharpCompilation = _cSharpCompilation.RemoveReferences(new[] { reference });
                                _loadedAssemblies.Remove(name);
                                continue;
                            }

                            // TODO: version and pkt check
                            matchingAssemblies.Add(matchingAssembly);
                            found = true;
                            break;
                        }
                    }
                }

                // TODO: check if found and log error
            }

            return matchingAssemblies;
        }

        private IEnumerable<MetadataReference> LoadFromPaths(IEnumerable<string> paths)
        {
            List<MetadataReference> result = new List<MetadataReference>();
            foreach (string path in paths)
            {
                string resolvedPath = Environment.ExpandEnvironmentVariables(path);
                if (Directory.Exists(resolvedPath))
                {
                    _dependencyDirectories.Add(resolvedPath);
                    result.AddRange(LoadAssembliesFromDirectory(resolvedPath));
                }
                else if (File.Exists(resolvedPath))
                {
                    _dependencyDirectories.Add(Path.GetDirectoryName(resolvedPath));
                    result.Add(CreateMetadataReferenceIfNeeded(resolvedPath));
                }
            }

            return result;
        }

        private IEnumerable<MetadataReference> LoadAssembliesFromDirectory(string directory)
        {
            foreach (string assembly in Directory.EnumerateFiles(directory, "*.dll"))
            {
                yield return CreateMetadataReferenceIfNeeded(assembly);
            }
        }

        private MetadataReference CreateMetadataReferenceIfNeeded(string assembly)
        {
            // Roslyn doesn't support having two assemblies as references with the same identity and then getting the symbol for it.
            string name = Path.GetFileName(assembly);
            if (!_loadedAssemblies.TryGetValue(name, out MetadataReference metadataReference))
            {
                metadataReference = CreateAndAddReferenceToCompilation(name, assembly);
            }

            return metadataReference;
        }

        private MetadataReference CreateAndAddReferenceToCompilation(string name, string assemblyPath)
        {
            MetadataReference metadataReference = MetadataReference.CreateFromFile(assemblyPath);
            _loadedAssemblies.Add(name, metadataReference);
            _cSharpCompilation = _cSharpCompilation.AddReferences(new MetadataReference[] { metadataReference });
            ResolveReferences(assemblyPath);
            return metadataReference;
        }

        private void ResolveReferences(string assemblyPath)
        {
            using Stream fileStream = File.OpenRead(assemblyPath);
            using PEReader peReader = new(fileStream);
            MetadataReader reader = peReader.GetMetadataReader();
            foreach (AssemblyReferenceHandle handle in reader.AssemblyReferences)
            {
                AssemblyReference reference = reader.GetAssemblyReference(handle);
                string name = $"{reader.GetString(reference.Name)}.dll";
                if (!_loadedAssemblies.TryGetValue(name, out MetadataReference _))
                {
                    foreach (string directory in _dependencyDirectories)
                    {
                        string potentialPath = Path.Combine(directory, name);
                        if (File.Exists(potentialPath))
                        {
                            // TODO: add version check?
                            CreateAndAddReferenceToCompilation(name, potentialPath);
                            break;
                        }
                    }
                }
            }

            // TODO: log error, couldn't resolve reference
        }
    }
}
