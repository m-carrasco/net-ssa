using Mono.Cecil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;

namespace NetSsa.Reflection
{
    public class Importer{

        /*
            The DefaultReflectionImporter may generate poor TypeReferences for
            the built-in types if they come from a different library than the one of the runtime.
            However, net-ssa algorithms consider two types to be equal if they share the same
            namespace and name.
        */
        public static TypeReference Import(Type type, IReflectionImporter reflectionImporter) {
            if (type.ContainsGenericParameters){
                Type[] reflectionGA = type.GetGenericArguments();
                TypeReference[] cecilGA = new TypeReference[reflectionGA.Length];
                for (int i =0; i < reflectionGA.Length; i++){
                    Type genericArgument = reflectionGA[i];
                    if (genericArgument.IsGenericParameter){
                        IGenericParameterProvider context = null;
                        if (genericArgument.IsGenericTypeParameter){
                            context = reflectionImporter.ImportReference(genericArgument.DeclaringType, null);
                        } else if (genericArgument.IsGenericMethodParameter) {
                            context = reflectionImporter.ImportReference(genericArgument.DeclaringMethod, null);
                        }
                        cecilGA[i] = reflectionImporter.ImportReference(genericArgument, context);
                    } else{
                        cecilGA[i] = reflectionImporter.ImportReference(genericArgument, null);
                    }
                }

                TypeReference genericDefinition = reflectionImporter.ImportReference(type.GetGenericTypeDefinition(), null);
                return genericDefinition.MakeGenericInstanceType(cecilGA);
            }

            return reflectionImporter.ImportReference(type, null);

        }
    }
}