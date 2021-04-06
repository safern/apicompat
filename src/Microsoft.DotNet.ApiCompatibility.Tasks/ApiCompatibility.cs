using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.ApiCompatibility.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Microsoft.DotNet.ApiCompatibility.Tasks
{
    public class ApiCompatibility : BuildTask
    {
        public ApiCompatibility()
        {
            AssemblyLoadContext.Default.Resolving += ResolveRoslynDependencies;
        }

        private Assembly ResolveRoslynDependencies(AssemblyLoadContext context, AssemblyName name)
        {
            if (string.IsNullOrEmpty(RoslynPath))
            {
                throw new ArgumentNullException(nameof(RoslynPath), $"Please provide a valid roslyn path");
            }

            if (!Directory.Exists(RoslynPath))
            {
                throw new ArgumentException($"Provided '{nameof(RoslynPath)}': {RoslynPath} doesn't exist.");
            }

            if (name.Name.StartsWith("Microsoft.CodeAnalysis", StringComparison.OrdinalIgnoreCase))
            {
                string filePath = Path.Combine(RoslynPath, name.Name + ".dll");
                if (File.Exists(filePath))
                    return Assembly.LoadFrom(filePath);
            }

            return null;
        }

        [Required]
        public string RoslynPath { get; set; }
        [Required]
        public string[] RightDirectories { get; set; }
        // Right dependencies should be a super set of left's
        public string[] RightDependsOn { get; set; }
        public string[] LeftPaths { get; set; }
        public string[] LeftSourcesPath { get; set; }
        public string AssemblyName { get; set; }
        public string NoWarn { get; set; }
        public bool IncludeInternals { get; set; }
        public string LeftName { get; set; }
        public string RightName { get; set; }
        public bool ValidateMatchingAssemblyIdentities { get; set; }
        public bool ShouldResolveAssemblyReferences { get; set; }

        public override bool Execute()
        {
            if ((LeftPaths == null || LeftPaths.Length == 0) && (LeftSourcesPath == null || LeftSourcesPath.Length == 0))
            {
                Log.LogError($"'{nameof(LeftPaths)}' or '{nameof(LeftSourcesPath)}' must contain at least one element to run ApiCompatibility.");
                return false;
            }

            if (RightDirectories == null || RightDirectories.Length == 0)
            {
                Log.LogError($"'{nameof(RightDirectories)}' must contain at least one directory to search for the right binaries.");
                return false;
            }

            if (string.IsNullOrEmpty(LeftName))
            {
                LeftName = "contract";
            }

            if (string.IsNullOrEmpty(RightName))
            {
                RightName = "implementation";
            }

            IEnumerable<IAssemblySymbol> leftSymbols;
            HashSet<string> rightDependsOnDirs = new HashSet<string>();

            if (ShouldResolveAssemblyReferences && RightDependsOn != null)
            {
                foreach (string dependency in RightDependsOn)
                {
                    if (Directory.Exists(dependency))
                    {
                        rightDependsOnDirs.Add(dependency);
                    }
                    else if (File.Exists(dependency))
                    {
                        rightDependsOnDirs.Add(Path.GetDirectoryName(dependency));
                    }
                }
            }

            AssemblySymbolLoader leftLoader;
            if (LeftPaths != null)
            {
                leftLoader = new AssemblySymbolLoader(resolveAssemblyReferences: ShouldResolveAssemblyReferences);
                leftLoader.AddReferenceSearchDirectories(rightDependsOnDirs);
                leftSymbols = leftLoader.LoadAssemblies(LeftPaths);
            }
            else
            {
                leftLoader = new AssemblySymbolLoader(AssemblyName);
                leftSymbols = new[]
                {
                    leftLoader.LoadAssemblyFromSourceFiles(LeftSourcesPath, RightDependsOn),
                };
                ValidateMatchingAssemblyIdentities = false; // in memory assembly might not match identities
            }

            AssemblySymbolLoader rightLoader = new(resolveAssemblyReferences: ShouldResolveAssemblyReferences);
            rightLoader.AddReferenceSearchDirectories(RightDependsOn ?? Array.Empty<string>());
            IEnumerable<IAssemblySymbol> rightSymbols = rightLoader.LoadMatchingAssemblies(leftSymbols, RightDirectories, validateMatchingIdentity: ValidateMatchingAssemblyIdentities);

            ApiDiffer differ = new(includeInternalSymbols: IncludeInternals);
            differ.NoWarn = NoWarn;

            IEnumerable<CompatDifference> differences = differ.GetDifferences(leftSymbols, rightSymbols);

            foreach (CompatDifference difference in differences)
            {
                Log.LogError(difference.ToString());
            }

            return !Log.HasLoggedErrors;
        }
    }

    public abstract partial class BuildTask : ITask
    {
        private Log _log = null;

        internal Log Log
        {
            get { return _log ??= new Log(new TaskLoggingHelper(this)); }
        }

        public BuildTask()
        {
        }

        public IBuildEngine BuildEngine
        {
            get;
            set;
        }

        public ITaskHost HostObject
        {
            get;
            set;
        }

        public abstract bool Execute();
    }

    internal class Log : ILog
    {
        private readonly TaskLoggingHelper _logger;
        public Log(TaskLoggingHelper logger)
        {
            _logger = logger;
        }

        public void LogError(string message, params object[] messageArgs)
        {
            _logger.LogError(message, messageArgs);
        }

        public void LogErrorFromException(Exception exception, bool showStackTrace)
        {
            _logger.LogErrorFromException(exception, showStackTrace);
        }

        public void LogMessage(string message, params object[] messageArgs)
        {
            _logger.LogMessage(message, messageArgs);
        }

        public void LogMessage(LogImportance importance, string message, params object[] messageArgs)
        {
            _logger.LogMessage((MessageImportance)importance, message, messageArgs);
        }

        public void LogWarning(string message, params object[] messageArgs)
        {
            _logger.LogWarning(message, messageArgs);
        }

        public bool HasLoggedErrors { get { return _logger.HasLoggedErrors; } }
    }

    public enum LogImportance
    {
        Low = MessageImportance.Low,
        Normal = MessageImportance.Normal,
        High = MessageImportance.High
    }


    public interface ILog
    {
        //
        // Summary:
        //     Logs an error with the specified message.
        //
        // Parameters:
        //   message:
        //     The message.
        //
        //   messageArgs:
        //     Optional arguments for formatting the message string.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     message is null.
        void LogError(string message, params object[] messageArgs);

        //
        // Summary:
        //     Logs a message with the specified string.
        //
        // Parameters:
        //   message:
        //     The message.
        //
        //   messageArgs:
        //     The arguments for formatting the message.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     message is null.
        void LogMessage(string message, params object[] messageArgs);

        //
        // Summary:
        //     Logs a message with the specified string and importance.
        //
        // Parameters:
        //   importance:
        //     One of the enumeration values that specifies the importance of the message.
        //
        //   message:
        //     The message.
        //
        //   messageArgs:
        //     The arguments for formatting the message.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     message is null.
        void LogMessage(LogImportance importance, string message, params object[] messageArgs);

        //
        // Summary:
        //     Logs a warning with the specified message.
        //
        // Parameters:
        //   message:
        //     The message.
        //
        //   messageArgs:
        //     Optional arguments for formatting the message string.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     message is null.
        void LogWarning(string message, params object[] messageArgs);
    }
}
