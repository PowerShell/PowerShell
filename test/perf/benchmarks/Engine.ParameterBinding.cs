// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Engine
{
    [MemoryDiagnoser]
    [BenchmarkCategory(Categories.Engine, Categories.Internal)]
    public class ParameterLookup
    {
        private MergedCommandParameterMetadata metadata;

        [Params("Parameter00", "Parameter31", "parameter31", "Alias31", "Last", "Missing")]
        public string ParameterName { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            metadata = new MergedCommandParameterMetadata();
            for (int i = 0; i < 32; i++)
            {
                string name = $"Parameter{i:D2}";
                var parameter = new MergedCompiledCommandParameter(
                    new CompiledCommandParameter(
                        new RuntimeDefinedParameter(name, typeof(int), new Collection<Attribute>()),
                        processingDynamicParameters: false),
                    ParameterBinderAssociation.DeclaredFormalParameters);

                metadata.BindableParameters.Add(name, parameter);
                metadata.AliasedParameters.Add($"Alias{i:D2}", parameter);
            }

            metadata.AliasedParameters.Add("LastParameter", metadata.BindableParameters["Parameter31"]);
            metadata.MakeReadOnly();
        }

        [Benchmark]
        public object ResolveParameter()
        {
            return metadata.GetMatchingParameter(ParameterName, false, true, null);
        }
    }
}
