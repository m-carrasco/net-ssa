using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Mono.Cecil;

namespace NetSsa.Reflection
{
    public class TypeAdapter {
        public readonly MetadataLoadContext MetadataLoadContext;
        private readonly IDictionary<String, System.Reflection.Assembly> LoadedAssemblies = new Dictionary<String, System.Reflection.Assembly>();
        private readonly IDictionary<Mono.Cecil.TypeReference, System.Type> TypeMapping = new Dictionary<Mono.Cecil.TypeReference, System.Type>();
        
        public TypeAdapter(MetadataLoadContext m){
            MetadataLoadContext = m;
        }

        // TODO: Document IModifierType limitation.
        //       Method.MetadataToken limitation.
        //       FunctionPointerType limitation.
        // Document that we could use the name of the type but there is a non-trivial mismatch between cecil and System.Reflection 
        public System.Type ToSystemReflectionType(Mono.Cecil.TypeReference typeReference) {

            System.Type result = null;
            if (TypeMapping.TryGetValue(typeReference, out result)){
                return result;
            }
            
            // Do not use typeReference.Module
            // that is the module where the TypeReference is placed.
            // We want to know the assembly of the referenced type. 
            String assemblyName = typeReference.Scope.Name;
            assemblyName = assemblyName.Replace(".dll", "");

            System.Reflection.Assembly assembly = null;
            if (!LoadedAssemblies.TryGetValue(assemblyName, out assembly)){
                // At the time of writing, MetadataLoadContext is not caching this result.
                assembly = MetadataLoadContext.LoadFromAssemblyName(assemblyName);
                LoadedAssemblies[assemblyName] = assembly;
            } 

            if (typeReference is Mono.Cecil.PointerType){
                result = ToSystemReflectionType(typeReference.GetElementType()).MakePointerType();
            } else if (typeReference is Mono.Cecil.ByReferenceType){
                result = ToSystemReflectionType(typeReference.GetElementType()).MakeByRefType();
            } else if (typeReference is Mono.Cecil.ArrayType arrayType){
                result = ToSystemReflectionType(typeReference.GetElementType()).MakeArrayType(arrayType.Rank);
            } else if (typeReference is Mono.Cecil.IModifierType modifierType){
                // TODO: Find the correct way to create a Reflection's Type which keeps this information.
                // Currently, I don't think there is an easy way if any.
                // Perhaps, using a TypeBuilder and constructing a Field of this Type?
                result = ToSystemReflectionType(modifierType.ElementType);
            } else if (typeReference is GenericParameter gp){

                if (gp.Type.Equals(GenericParameterType.Method)){
                    MethodDefinition methodDef = (MethodDefinition)gp.Owner;
                    TypeReference declaringType = methodDef.DeclaringType;
                    System.Type srDeclaringType = ToSystemReflectionType(declaringType);

                    BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Public | (methodDef.IsStatic ? BindingFlags.Static : BindingFlags.Instance);
                    
                    // Using MetadataToken implies that the dll inspected using Reflection
                    // must be the same one opened using Cecil!
                    MethodInfo mi = srDeclaringType.GetMethods(bindingFlags).Where(mi => mi.MetadataToken == methodDef.MetadataToken.ToInt32()).Single();
                    
                    result = mi.GetGenericArguments()[gp.Position];
                } else if (gp.Type.Equals(GenericParameterType.Type)){
                    TypeDefinition typeDef = (TypeDefinition)gp.Owner;
                    System.Type srDeclaringType = ToSystemReflectionType(typeDef);
                    result = srDeclaringType.GetGenericArguments()[gp.Position];
                } else{
                    throw new NotSupportedException("Unexpected case");
                }

            } else if (typeReference is Mono.Cecil.GenericInstanceType gi) {
                System.Type genericDef = ToSystemReflectionType(gi.ElementType);
                Type[] genericArguments = gi.GenericArguments.Select(arg => ToSystemReflectionType(arg)).ToArray();
                result = genericDef.MakeGenericType(genericArguments);
            } else if (typeReference is FunctionPointerType fpt){
                // To the best of my knowledge, there is no way to represent this in System.Reflection *yet*.
                // https://github.com/dotnet/runtime/issues/11354
                // https://github.com/dotnet/runtime/issues/69273

                /*
                    This code snippet is the creation of a delegate.
                    The function pointer is considered as a native int, so do I here.
                    IL_000a: ldnull
                    IL_000b: ldftn class C/D C::foo(class C/D)
                    IL_0011: newobj instance void class [System.Runtime]System.Func`2<class C/D, class C/D>::.ctor(object, native int)
                */
                result = ToSystemReflectionType(typeReference.Module.TypeSystem.IntPtr);
            } else {
                String systemReflectionFullName = typeReference.FullName;
                
                if (typeReference.IsNested){
                    systemReflectionFullName = systemReflectionFullName.Replace("/", "+"); 
                }

                result = assembly.GetType(systemReflectionFullName, true);
            }

            TypeMapping[typeReference] = result;

            return result;
        }
    }
}