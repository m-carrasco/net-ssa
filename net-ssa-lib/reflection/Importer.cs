using Mono.Cecil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;

namespace NetSsa.Reflection
{
    public class Importer{

        public static TypeReference Import(Type type, ISet<AssemblyDefinition> loadedAssemblies) {
            foreach (Mono.Cecil.AssemblyDefinition assembly in loadedAssemblies){
                if (type.Assembly.GetName().Name.Equals(assembly.Name.Name)){
                    return Import(type, assembly);
                }
            }
            throw new NotSupportedException("No cecil loaded assembly found for the System.Type: " + type);
        }

        public static TypeReference Import(Type type, AssemblyDefinition assembly){
            if (type.ContainsGenericParameters){
                Type[] reflectionGA = type.GetGenericArguments();
                TypeReference[] cecilGA = new TypeReference[reflectionGA.Length];
                for (int i =0; i < reflectionGA.Length; i++){
                    Type genericArgument = reflectionGA[i];
                    if (genericArgument.IsGenericParameter){
                        IGenericParameterProvider context = null;
                        if (genericArgument.IsGenericTypeParameter){
                            context = assembly.MainModule.ImportReference(genericArgument.DeclaringType);
                        } else if (genericArgument.IsGenericMethodParameter) {
                            context = assembly.MainModule.ImportReference(genericArgument.DeclaringMethod);
                        }
                        cecilGA[i] = assembly.MainModule.ImportReference(genericArgument, context);
                    } else{
                        cecilGA[i] = assembly.MainModule.ImportReference(genericArgument);
                    }
                }
                TypeReference genericDefinition = assembly.MainModule.ImportReference(type.GetGenericTypeDefinition());
                return genericDefinition.MakeGenericInstanceType(cecilGA);
            }

            return assembly.MainModule.ImportReference(type);
        }
    }
}