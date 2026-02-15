using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Architecture
{
    /// <summary>
    /// Verifies that the Project Phoenix assembly dependency rules are enforced.
    /// Uses reflection to inspect referenced assemblies at runtime and validate
    /// the layered architecture: Core -> Data -> Serialization -> Models -> Infrastructure -> Common.
    /// </summary>
    public class AssemblyBoundaryTests
    {
        // Assembly name constants matching the .csproj AssemblyName values
        private const string CoreAssembly = "EVEMon.Core";
        private const string DataAssembly = "EVEMon.Data";
        private const string SerializationAssembly = "EVEMon.Serialization";
        private const string ModelsAssembly = "EVEMon.Models";
        private const string InfrastructureAssembly = "EVEMon.Infrastructure";
        private const string CommonAssembly = "EVEMon.Common";

        /// <summary>
        /// Loads an assembly by name from the current AppDomain.
        /// Falls back to loading via Assembly.Load for assemblies not yet in memory.
        /// </summary>
        private static Assembly LoadAssembly(string assemblyName)
        {
            // Try to find it already loaded
            var loaded = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == assemblyName);

            if (loaded != null)
                return loaded;

            // Force load it (it's a dependency of our test project)
            return Assembly.Load(assemblyName);
        }

        /// <summary>
        /// Gets the names of all EVEMon project assemblies referenced by the given assembly.
        /// Filters out framework/NuGet dependencies to focus on project-to-project refs.
        /// </summary>
        private static HashSet<string> GetEveMonReferences(Assembly assembly)
        {
            return assembly.GetReferencedAssemblies()
                .Where(a => a.Name != null && a.Name.StartsWith("EVEMon", StringComparison.OrdinalIgnoreCase))
                .Select(a => a.Name!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        #region Core Has Zero EVEMon Dependencies

        [Fact]
        public void Core_HasZeroEveMonDependencies()
        {
            // Arrange
            var coreAssembly = LoadAssembly(CoreAssembly);

            // Act
            var evemonRefs = GetEveMonReferences(coreAssembly);

            // Assert
            evemonRefs.Should().BeEmpty(
                "EVEMon.Core is the leaf assembly and must not reference any other EVEMon assembly. " +
                $"Found references: [{string.Join(", ", evemonRefs)}]");
        }

        #endregion

        #region Data Only Depends On Core

        [Fact]
        public void Data_OnlyDependsOnCore()
        {
            // Arrange
            var dataAssembly = LoadAssembly(DataAssembly);
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { CoreAssembly };

            // Act
            var evemonRefs = GetEveMonReferences(dataAssembly);

            // Assert
            evemonRefs.Should().BeSubsetOf(allowed,
                $"EVEMon.Data should only reference EVEMon.Core. " +
                $"Found references: [{string.Join(", ", evemonRefs)}]");
        }

        #endregion

        #region Models Does Not Reference Common

        [Fact]
        public void Models_DoesNotReferenceCommon()
        {
            // Arrange
            var modelsAssembly = LoadAssembly(ModelsAssembly);

            // Act
            var evemonRefs = GetEveMonReferences(modelsAssembly);

            // Assert
            evemonRefs.Should().NotContain(CommonAssembly,
                "EVEMon.Models must not reference EVEMon.Common to avoid circular dependencies. " +
                "Models sits below Common in the dependency graph.");
        }

        [Fact]
        public void Models_DoesNotReferenceInfrastructure()
        {
            // Arrange
            var modelsAssembly = LoadAssembly(ModelsAssembly);

            // Act
            var evemonRefs = GetEveMonReferences(modelsAssembly);

            // Assert
            evemonRefs.Should().NotContain(InfrastructureAssembly,
                "EVEMon.Models must not reference EVEMon.Infrastructure.");
        }

        #endregion

        #region No Circular Project References

        [Fact]
        public void NoCircularProjectReferences()
        {
            // Arrange - load all EVEMon assemblies
            var assemblyNames = new[]
            {
                CoreAssembly, DataAssembly, SerializationAssembly,
                ModelsAssembly, InfrastructureAssembly, CommonAssembly
            };

            // Build adjacency list: assembly -> set of EVEMon assemblies it references
            var graph = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var name in assemblyNames)
            {
                try
                {
                    var asm = LoadAssembly(name);
                    graph[name] = GetEveMonReferences(asm);
                }
                catch
                {
                    // Assembly might not be available; skip it
                    graph[name] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }
            }

            // Act - detect cycles using DFS
            var cycles = FindCycles(graph);

            // Assert
            cycles.Should().BeEmpty(
                "no circular references should exist between EVEMon assemblies. " +
                $"Found cycles: [{string.Join("; ", cycles)}]");
        }

        /// <summary>
        /// Finds all cycles in a directed graph using DFS.
        /// Returns human-readable cycle descriptions.
        /// </summary>
        private static List<string> FindCycles(Dictionary<string, HashSet<string>> graph)
        {
            var cycles = new List<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var inStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var path = new List<string>();

            foreach (var node in graph.Keys)
            {
                if (!visited.Contains(node))
                {
                    DFS(node, graph, visited, inStack, path, cycles);
                }
            }

            return cycles;
        }

        private static void DFS(
            string node,
            Dictionary<string, HashSet<string>> graph,
            HashSet<string> visited,
            HashSet<string> inStack,
            List<string> path,
            List<string> cycles)
        {
            visited.Add(node);
            inStack.Add(node);
            path.Add(node);

            if (graph.TryGetValue(node, out var neighbors))
            {
                foreach (var neighbor in neighbors)
                {
                    if (inStack.Contains(neighbor))
                    {
                        // Found a cycle
                        int cycleStart = path.IndexOf(neighbor);
                        var cyclePath = path.Skip(cycleStart).ToList();
                        cyclePath.Add(neighbor); // complete the cycle
                        cycles.Add(string.Join(" -> ", cyclePath));
                    }
                    else if (!visited.Contains(neighbor) && graph.ContainsKey(neighbor))
                    {
                        DFS(neighbor, graph, visited, inStack, path, cycles);
                    }
                }
            }

            path.RemoveAt(path.Count - 1);
            inStack.Remove(node);
        }

        #endregion

        #region Infrastructure Layer Boundary

        [Fact]
        public void Infrastructure_DoesNotReferenceCommon()
        {
            // Arrange
            var infraAssembly = LoadAssembly(InfrastructureAssembly);

            // Act
            var evemonRefs = GetEveMonReferences(infraAssembly);

            // Assert
            evemonRefs.Should().NotContain(CommonAssembly,
                "EVEMon.Infrastructure must not reference EVEMon.Common. " +
                "Infrastructure sits below Common in the dependency graph.");
        }

        #endregion

        #region Serialization Layer Boundary

        [Fact]
        public void Serialization_DoesNotReferenceCommon()
        {
            // Arrange
            var serializationAssembly = LoadAssembly(SerializationAssembly);

            // Act
            var evemonRefs = GetEveMonReferences(serializationAssembly);

            // Assert
            evemonRefs.Should().NotContain(CommonAssembly,
                "EVEMon.Serialization must not reference EVEMon.Common.");
        }

        [Fact]
        public void Serialization_DoesNotReferenceInfrastructure()
        {
            // Arrange
            var serializationAssembly = LoadAssembly(SerializationAssembly);

            // Act
            var evemonRefs = GetEveMonReferences(serializationAssembly);

            // Assert
            evemonRefs.Should().NotContain(InfrastructureAssembly,
                "EVEMon.Serialization must not reference EVEMon.Infrastructure.");
        }

        [Fact]
        public void Serialization_DoesNotReferenceModels()
        {
            // Arrange
            var serializationAssembly = LoadAssembly(SerializationAssembly);

            // Act
            var evemonRefs = GetEveMonReferences(serializationAssembly);

            // Assert
            evemonRefs.Should().NotContain(ModelsAssembly,
                "EVEMon.Serialization must not reference EVEMon.Models.");
        }

        #endregion

        #region Common Is Top-Level Consumer

        [Fact]
        public void Common_ReferencesCore()
        {
            // Arrange
            var commonAssembly = LoadAssembly(CommonAssembly);

            // Act
            var evemonRefs = GetEveMonReferences(commonAssembly);

            // Assert - Common should depend on all lower layers
            evemonRefs.Should().Contain(CoreAssembly,
                "EVEMon.Common should reference EVEMon.Core as the base interface layer.");
        }

        [Fact]
        public void Common_ReferencesInfrastructure()
        {
            // Arrange
            var commonAssembly = LoadAssembly(CommonAssembly);

            // Act
            var evemonRefs = GetEveMonReferences(commonAssembly);

            // Assert
            evemonRefs.Should().Contain(InfrastructureAssembly,
                "EVEMon.Common should reference EVEMon.Infrastructure for service implementations.");
        }

        #endregion
    }
}
