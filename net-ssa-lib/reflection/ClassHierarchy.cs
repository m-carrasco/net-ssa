using System;
namespace NetSsa.Reflection
{
    public class ClassHierarchy{

        // GetLowestCommonAncestor finds the closest common class or interface
        // between 'a' and 'b'. The procedure can have more than one correct answer.
        // This is due to interfaces. The current implementation is based on:
        // https://gitlab.ow2.org/asm/asm/-/blob/c5b540441b239ebf090292e69e93155d90cf2626/asm/src/main/java/org/objectweb/asm/ClassWriter.java#L1035
        public static Type GetLowestCommonAncestor(Type a, Type b) {
            if (a.IsAssignableFrom(b)) {
                return a;
            }

            if (b.IsAssignableFrom(a)) {
                return b;
            }

            // Stop here otherwise the next loop crashes.
            // An interface doesn't implement System.Object explicitly.
            if (a.IsInterface || b.IsInterface){
                return typeof(Object).GetType();
            }

            // Find common ancestor following the class hierarchy
            Type commonClass = a;
            do {
                commonClass = commonClass.BaseType;
            } while (!commonClass.IsAssignableFrom(b));

            // Finding the lowest common ancestor for interfaces is a bit
            // tricky because there is no single root in the hierarchy. 
            // The following code could be a solution. However, not sure
            // if it is really necessary. It should be tested more properly. 
            /*
            var aAllInterfaces = a.GetInterfaces().ToHashSet();
            var bAllInterfaces = b.GetInterfaces().ToHashSet();
            var commonInterfaces = bAllInterfaces.Intersect(aAllInterfaces);
            
            // Let A and B be two different common interfaces.
            // If A <- B, then we only want to keep B.
            // A <- B is read as A can be assigned from B.

            ISet<Type> toRemove = new HashSet<Type>();
            foreach (var intA in commonInterfaces.ToList()) {
                foreach (var intB in commonInterfaces.ToList()){
                    if (intB.GetInterfaces().Any(intf => intA.IsAssignableFrom(intB))){
                        toRemove.Add(intA);
                        break;
                    }
                }
            }
            interfaceAncestor = commonInterfaces.Except(toRemove).ToHashSet();
            */

            return commonClass;
        }
        public static Type GetLowestCommonAncestor(Type[] types) {
            if (types.Length == 0){
                throw new ArgumentException("Array size cannot be zero");
            }

            foreach (Type tr in types){
                if (tr == null){
                    throw new ArgumentNullException("Array contains null reference");
                }
            }
            if (types.Length == 1){
                return types[0];
            }

            Type lca = types[0];
            for (int i=1; i < types.Length; i++){
                lca = GetLowestCommonAncestor(lca, types[i]);
            }
            return lca;
        }
    }
}