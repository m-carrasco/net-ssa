using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using NetSsa.Reflection;
using NUnit.Framework;
using System.IO;
using System.Reflection;
using Mono.Cecil.Rocks;
using System.Diagnostics;
using NetSsa.Analyses;
using NetSsa.Instructions;
using System.Runtime.InteropServices;
using Mono.Reflection;

namespace UnitTest
{
    public class TypeMergeTests
    {
        private AssemblyDefinition _mscorlib = null;
        private AssemblyDefinition _dsa = null;
        private String _mscorlibPath = String.Empty;
        private String _dsaPath = String.Empty;

        [OneTimeSetUp]
        public void StartTest()
        {
            //TestContext.Error.WriteLine();
            Trace.Listeners.Add(new ConsoleTraceListener());
        }

        [SetUp]
        public void Setup()
        {
            _mscorlibPath = Path.Join(Directory.GetParent(Assembly.GetAssembly(typeof(Tests)).Location).FullName, "mscorlib.dll");
            _mscorlib = AssemblyDefinition.ReadAssembly(_mscorlibPath);
            _dsaPath = Path.Join(Directory.GetParent(Assembly.GetAssembly(typeof(Tests)).Location).FullName, "DSA.dll");
            _dsa = AssemblyDefinition.ReadAssembly(_dsaPath);
        }

        [TearDown]
        public void Exit()
        {
            _mscorlib.Dispose();
        }

        private MetadataLoadContext BuildMetadataLoadContextForMonoMscorlib(){
            string[] runtimeAssemblies = new string[] {_mscorlibPath};
            // Create the list of assembly paths consisting of runtime assemblies and the inspected assembly.
            var paths = new List<string>(runtimeAssemblies);

            // Create PathAssemblyResolver that can resolve assemblies using the created list.
            var resolver = new PathAssemblyResolver(paths);
            var context = new MetadataLoadContext(resolver, "mscorlib");
            if (context.CoreAssembly == null){
                throw new NotSupportedException("CoreAssembly cannot be null.");
            }
            return context;
        }

        [Test]
        public void MergeTypesTest()
        {
            MetadataLoadContext metadataLoadContext = BuildMetadataLoadContextForMonoMscorlib();
            TypeAdapter typeAdapter = new TypeAdapter(metadataLoadContext);

            TypeDefinition t0 = _mscorlib.MainModule.GetType("System.Collections.Generic.IList`1");
            TypeDefinition t1 = _mscorlib.MainModule.GetType("System.Collections.Generic.List`1");
            TypeDefinition t2 = _mscorlib.MainModule.GetType("System.Exception");

            TypeReference t3 = t0.MakeGenericInstanceType(new TypeReference[] {t2});
            TypeReference t4 = t1.MakeGenericInstanceType(new TypeReference[] {t2});
    
            Type mergeType = ClassHierarchy.GetLowestCommonAncestor(typeAdapter.ToSystemReflectionType(t3), typeAdapter.ToSystemReflectionType(t4));
            Assert.AreEqual("System.Collections.Generic.IList`1[System.Exception]", mergeType.ToString());

            TypeReference t5 = _mscorlib.MainModule.GetType("System.Runtime.ExceptionServices.ExceptionDispatchInfo");
            TypeReference t6 = t0.MakeGenericInstanceType(new TypeReference[] {t5});
            TypeReference t7 = t1.MakeGenericInstanceType(new TypeReference[] {t5});

            // Merge t6 and t7
            mergeType = ClassHierarchy.GetLowestCommonAncestor(typeAdapter.ToSystemReflectionType(t6), typeAdapter.ToSystemReflectionType(t7));
            Assert.AreEqual("System.Collections.Generic.IList`1[System.Runtime.ExceptionServices.ExceptionDispatchInfo]", mergeType.ToString());

            TypeReference t8 = _mscorlib.MainModule.GetType("System.Threading.CancellationCallbackInfo/WithSyncContext");
            TypeReference t9 = _mscorlib.MainModule.GetType("System.Threading.CancellationCallbackInfo");

            // Merge t8 and t9
            mergeType = ClassHierarchy.GetLowestCommonAncestor(typeAdapter.ToSystemReflectionType(t8), typeAdapter.ToSystemReflectionType(t9));
            Assert.AreEqual("System.Threading.CancellationCallbackInfo", mergeType.ToString());

            TypeReference t10 = _mscorlib.MainModule.GetType("System.Threading.Tasks.Task`1");
            TypeReference t11 = _mscorlib.MainModule.GetType("System.Boolean");

            TypeReference t12 = t10.MakeGenericInstanceType(new TypeReference[] {t11});
            TypeReference t13 = _mscorlib.MainModule.GetType("System.Threading.SemaphoreSlim/TaskNode");
            
            // Merge t12 and t13
            mergeType = ClassHierarchy.GetLowestCommonAncestor(typeAdapter.ToSystemReflectionType(t12), typeAdapter.ToSystemReflectionType(t13));
            Assert.AreEqual("System.Threading.Tasks.Task`1[System.Boolean]", mergeType.ToString());

            TypeReference t14 = _mscorlib.MainModule.GetType("System.Collections.Generic.IEqualityComparer`1");
            TypeReference t15 = _mscorlib.MainModule.GetType("System.Collections.Generic.EqualityComparer`1");

            var gp = t14.GenericParameters.Single();
            t14 = t14.MakeGenericInstanceType(new TypeReference[] {gp});
            t15 = t15.MakeGenericInstanceType(new TypeReference[] {gp});

            // Merge t14 and t15
            mergeType = ClassHierarchy.GetLowestCommonAncestor(typeAdapter.ToSystemReflectionType(t14), typeAdapter.ToSystemReflectionType(t15));
            Assert.AreEqual("System.Collections.Generic.IEqualityComparer`1[T]", mergeType.ToString());
        }

        [Test]
        public void LowestCommonAncestorTest()
        {
            MetadataLoadContext metadataLoadContext = BuildMetadataLoadContextForMonoMscorlib();
            TypeAdapter typeAdapter = new TypeAdapter(metadataLoadContext);
            LowestCommonAncestor lowestCommonAncestor = new LowestCommonAncestor(typeAdapter);

            TypeDefinition t0 = _mscorlib.MainModule.GetType("System.Collections.Generic.IList`1");
            TypeDefinition t1 = _mscorlib.MainModule.GetType("System.Collections.Generic.List`1");
            TypeDefinition t2 = _mscorlib.MainModule.GetType("System.Exception");

            TypeReference t3 = t0.MakeGenericInstanceType(new TypeReference[] {t2});
            TypeReference t4 = t1.MakeGenericInstanceType(new TypeReference[] {t2});
            TypeReference commonType = lowestCommonAncestor.GetLowestCommonAncestor(t3, t4);

            Assert.AreEqual("System.Collections.Generic.IList`1<System.Exception>", commonType.FullName);

            TypeReference t5 = _mscorlib.MainModule.GetType("System.Collections.Generic.IEqualityComparer`1");
            TypeReference t6 = _mscorlib.MainModule.GetType("System.Collections.Generic.EqualityComparer`1");

            var gp = t5.GenericParameters.Single();
            t5 = t5.MakeGenericInstanceType(new TypeReference[] {gp});
            t6 = t6.MakeGenericInstanceType(new TypeReference[] {gp});

            commonType = lowestCommonAncestor.GetLowestCommonAncestor(t5, t6);
            Assert.AreEqual("System.Collections.Generic.IEqualityComparer`1<T>", commonType.FullName);

            TypeReference t7 = _mscorlib.MainModule.GetType("System.Threading.Tasks.Task`1");
            TypeReference t8 = _mscorlib.MainModule.GetType("System.Boolean");

            TypeReference t9 = t7.MakeGenericInstanceType(new TypeReference[] {t8});
            TypeReference t10 = _mscorlib.MainModule.GetType("System.Threading.SemaphoreSlim/TaskNode");

            commonType = lowestCommonAncestor.GetLowestCommonAncestor(t9, t10);
            Assert.AreEqual("System.Threading.Tasks.Task`1<System.Boolean>", commonType.FullName);
        }

        [Test]
        // Unfortunately, I have no way to debug the issue for osx
        [Platform(Exclude="MacOsX")]
        public void MergeTypeMscorlib(){
            // Unfortunately, I have no way to debug the issue for osx
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)){
                Assert.Pass();
            }

            MetadataLoadContext metadataLoadContext = BuildMetadataLoadContextForMonoMscorlib();
            TypeAdapter typeAdapter = new TypeAdapter(metadataLoadContext);
            LowestCommonAncestor lowestCommonAncestor = new LowestCommonAncestor(typeAdapter);
            Process(_mscorlib, lowestCommonAncestor);
        }

        private static void Process(AssemblyDefinition assembly, LowestCommonAncestor lowestCommonAncestor){
            foreach (TypeDefinition t in assembly.MainModule.GetTypes())
            {
                foreach (MethodDefinition m in t.Methods)
                {
                    if (!m.HasBody)
                        continue;

                    IRBody irBody = Unstacker.Compute(m.Body);
                    // This call is optional.
                    Ssa.Compute(irBody);
                    // This analysis is optional and it requires SSA
                    StackTypeInference analysis = new StackTypeInference(irBody, lowestCommonAncestor);
                    IDictionary<Register, StackType> stackTypes = analysis.Type();

                    foreach (TacInstruction ins in irBody.Instructions)
                    {
                        if (ins.Result is Register register){
                            StackType stackType = stackTypes[register];
                            // This is a valid stack type. However, we know it is not present
                            // in the current mscorlib.
                            Assert.AreNotEqual(StackType.StackTypeUnknownNativeManagedPointer, stackType);
                            if (ins is BytecodeInstruction bytecodeInstruction){
                                if (bytecodeInstruction.OpCode != Mono.Cecil.Cil.OpCodes.Refanytype){
                                    // StackTypeUnknownObjectRef can be a valid type. However, it is not
                                    // present in the our test libraries
                                    Assert.AreNotEqual(StackType.StackTypeUnknownObjectRef, stackType);
                                }
                            } else if (ins is PhiInstruction){
                                // StackTypeUnknownObjectRef can be a valid type. However, it is not
                                // present in the our test libraries
                                Assert.AreNotEqual(StackType.StackTypeUnknownObjectRef, stackType);
                            }
                        }
                    }
                }
            }
        }

        public static MetadataLoadContext BuildMetadataLoadContextCurrentRuntime(string dllpath){
            // Get the array of runtime assemblies.
            string[] runtimeAssemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
            
            // Create the list of assembly paths consisting of runtime assemblies and the inspected assembly.
            var paths = new List<string>(runtimeAssemblies);
            paths.Add(dllpath);

            // Create PathAssemblyResolver that can resolve assemblies using the created list.
            var resolver = new PathAssemblyResolver(paths);
            var context = new MetadataLoadContext(resolver);

            if (context.CoreAssembly == null){
                throw new NotSupportedException("CoreAssembly cannot be null.");
            }

            return context;
        }

        [Test]
        public void MergeTypeDSA(){
            // Unfortunately, I have no way to debug the issue for osx
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)){
                Assert.Pass();
            }

            MetadataLoadContext metadataLoadContext = BuildMetadataLoadContextCurrentRuntime(_dsaPath);
            TypeAdapter typeAdapter = new TypeAdapter(metadataLoadContext);
            LowestCommonAncestor lowestCommonAncestor = new LowestCommonAncestor(typeAdapter);

            Process(_dsa, lowestCommonAncestor);
        }
    }
}
