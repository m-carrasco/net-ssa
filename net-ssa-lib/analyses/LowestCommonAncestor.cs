using Mono.Cecil;
using System;
using System.Collections.Generic;
using NetSsa.Reflection;
using System.Linq;
using Mono.Cecil.Rocks;

namespace NetSsa.Analyses
{
    public class LowestCommonAncestor{
        private ISet<AssemblyDefinition> _loadedAssemblies;
        private TypeAdapter _typeAdapter;

        public LowestCommonAncestor(ISet<AssemblyDefinition> loadedAssemblies, TypeAdapter typeAdapter){
            _loadedAssemblies = loadedAssemblies;
            _typeAdapter = typeAdapter;
        }

        // The main logic of this procedure is on ClassHierarchy.GetLowestCommonAncestor
        // Please read its documentation.
        public TypeReference GetLowestCommonAncestor(TypeReference a, TypeReference b){
            return GetLowestCommonAncestor(new TypeReference[] {a , b});
        }

        public TypeReference GetLowestCommonAncestor(TypeReference[] typeReferences){
            Type systemReflectionLca = ClassHierarchy.GetLowestCommonAncestor(typeReferences.Select(tr => _typeAdapter.ToSystemReflectionType(tr)).ToArray());
            return Importer.Import(systemReflectionLca, _loadedAssemblies);
        }
    }
}