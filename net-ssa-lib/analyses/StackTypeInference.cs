using System;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Cecil.Cil;
using NetSsa.Instructions;

namespace NetSsa.Analyses
{
    public class StackType {
        public enum StackTypeKind{
            Int32,
            Int64,
            NativeInt,
            NativeFloat,
            NativeObjectRef,
            NativeManagedPointer,
            GenericParameter,
            UserDefineValueType,
        }

        public static readonly StackType StackTypeInt32 = new StackType(StackTypeKind.Int32);
        public static readonly StackType StackTypeInt64 = new StackType(StackTypeKind.Int64);
        public static readonly StackType StackTypeNativeInt = new StackType(StackTypeKind.NativeInt);
        public static readonly StackType StackTypeNativeFloat = new StackType(StackTypeKind.NativeFloat);
        public static readonly StackType StackTypeUnknownNativeManagedPointer = new StackType(StackTypeKind.NativeManagedPointer);
        public static readonly StackType StackTypeNullObjectRef = NullStackType.Singleton;
        public static readonly StackType StackTypeUnknownObjectRef = new StackType(StackTypeKind.NativeObjectRef);

        public StackTypeKind Kind;
        public TypeReference UserDefinedValueType;
        public GenericParameter GenericParameter;
        public ByReferenceType ManagedPointerType;

        protected StackType(StackTypeKind k){
            Kind = k;
        }

        protected StackType(TypeReference userType){
            Kind = StackTypeKind.UserDefineValueType;
            UserDefinedValueType = userType;
        }

        public static StackType CreateManagedPointer(ByReferenceType type){
            return new StackType(StackTypeKind.NativeManagedPointer) { ManagedPointerType = type };
        }

        public static StackType CreateUserDefined(TypeReference type){
            return new StackType(StackTypeKind.UserDefineValueType) { UserDefinedValueType = type };
        }

        public static StackType CreateGenericParameter(GenericParameter type){
            return new StackType(StackTypeKind.GenericParameter) { GenericParameter = type};
        }

        public virtual Boolean IsNullStackType() {
            return false;
        }

        public virtual Boolean IsBoxStackType() {
            return false;
        }

        public override string ToString()
        {
            switch (this.Kind){
                case StackType.StackTypeKind.Int32:
                case StackType.StackTypeKind.Int64:
                case StackType.StackTypeKind.NativeInt:
                case StackType.StackTypeKind.NativeFloat:
                    return this.Kind.ToString();
                case StackType.StackTypeKind.GenericParameter:
                    return this.Kind.ToString() + "<" + this.GenericParameter + ">";
                case StackType.StackTypeKind.UserDefineValueType:
                    return this.Kind.ToString() + "<" + this.UserDefinedValueType + ">";
                case StackType.StackTypeKind.NativeManagedPointer:
                {
                    if (this.Equals(StackTypeUnknownNativeManagedPointer)){
                        return "UnknownManagedPointer";
                    }

                    return this.Kind.ToString() + "<" + this.ManagedPointerType.FullName + ">";
                }
                case StackType.StackTypeKind.NativeObjectRef:
                {
                    if (this.Equals(StackTypeUnknownObjectRef)){
                        return "UnknownObjectRef";
                    }

                    if (this.Equals(StackTypeNullObjectRef)){
                        return "NullObjectRef";
                    }

                    if (this is AssemblyObjectReferenceStackType assemblyReference){
                        return "ObjectRef<" + assemblyReference.AssemblyTypeReference.FullName + ">";
                    }

                    if (this is BoxStackType boxStackType){
                        return "Box<" + boxStackType.BoxedType.FullName + ">";
                    }

                    throw new NotSupportedException("Unhandled NativeObjectRef");
                }
                default:
                    throw new ArgumentException("Unhandled case");
            }
        }
    }

    public class AssemblyObjectReferenceStackType : StackType {
        public TypeReference AssemblyTypeReference;

        public AssemblyObjectReferenceStackType(TypeReference type) : base(StackTypeKind.NativeObjectRef) {
            AssemblyTypeReference = type;
        }
    }

    public class NullStackType : StackType{
        public static readonly NullStackType Singleton = new NullStackType();
        private NullStackType() : base(StackTypeKind.NativeObjectRef) {}
        public override Boolean IsNullStackType() {
            return true;
        }
    }

    public class BoxStackType : StackType{
        public TypeReference BoxedType;
        public Boolean IsNullable;
        public BoxStackType(TypeReference boxed, bool isNullable = false) : base(StackTypeKind.NativeObjectRef) {
            BoxedType = boxed;
            IsNullable = isNullable;
        }
        public override Boolean IsBoxStackType() {
            return true;
        }
    }

    public class StackTypeInference {
        private IRBody _body;
        private ModuleDefinition _module;
        private MethodDefinition _method;
        private TypeSystem _typeSystem;
        private bool _mergeTypes;
        LowestCommonAncestor _lca;

        public StackTypeInference(IRBody body){
            _body = body;
            _method = body.CilBody.Method;
            _module = _method.Module;
            _typeSystem = _module.TypeSystem;
            _mergeTypes = false;
        }

        public StackTypeInference(IRBody body, LowestCommonAncestor lca) : this(body) {
            if (lca == null){
                throw new ArgumentNullException("LowestCommonAncestor cannot be null");
            }
            _mergeTypes = true;
            _lca = lca;
        }
        
        private static bool BasicStackTypeEquality(StackType a, StackType b){
            if (a.Kind == b.Kind){
                switch (a.Kind){
                    case StackType.StackTypeKind.Int32:
                    case StackType.StackTypeKind.Int64:
                    case StackType.StackTypeKind.NativeInt:
                    case StackType.StackTypeKind.NativeFloat:
                    case StackType.StackTypeKind.NativeManagedPointer:
                    case StackType.StackTypeKind.NativeObjectRef:
                        return true;
                    case StackType.StackTypeKind.GenericParameter:
                        return a.GenericParameter.Equals(b.GenericParameter);
                    case StackType.StackTypeKind.UserDefineValueType:
                        return StackTypeInference.AreEqual(a.UserDefinedValueType, b.UserDefinedValueType);
                }
            }
            return false;
        }

        private static bool FullStackTypeEquality(StackType a, StackType b){
            if (a.Kind == b.Kind){
                switch (a.Kind){
                    case StackType.StackTypeKind.Int32:
                    case StackType.StackTypeKind.Int64:
                    case StackType.StackTypeKind.NativeInt:
                    case StackType.StackTypeKind.NativeFloat:
                        return true;
                    case StackType.StackTypeKind.NativeObjectRef:
                    {
                        // This handles unknown and null stack types
                        if (a == b){
                            return true;
                        }

                        if (a is BoxStackType boxA &&
                            b is BoxStackType boxB){
                            return AreEqual(boxA.BoxedType, boxB.BoxedType);
                        }

                        if (a is AssemblyObjectReferenceStackType assemblyTypeA &&
                            b is AssemblyObjectReferenceStackType assemblyTypeB){
                            return AreEqual(assemblyTypeA.AssemblyTypeReference, assemblyTypeB.AssemblyTypeReference);
                        }
                        
                        return false;
                    }
                    case StackType.StackTypeKind.NativeManagedPointer:
                        return AreEqual(a.ManagedPointerType, b.ManagedPointerType);
                    case StackType.StackTypeKind.GenericParameter:
                        return a.GenericParameter.Equals(b.GenericParameter);
                    case StackType.StackTypeKind.UserDefineValueType:
                        return StackTypeInference.AreEqual(a.UserDefinedValueType, b.UserDefinedValueType);
                }
            }
            return false;
        }

        public IDictionary<Register, StackType> Type(){
            IDictionary<Register, StackType> typing = new Dictionary<Register, StackType>();
            bool extraIteration = false;

            do
            {
                foreach (TacInstruction tacInstruction in _body.Instructions){
                    if (tacInstruction.Result is Register register){
                        StackType type = GetType(tacInstruction, register, typing, out extraIteration);
                        if (type != null){
                            if (typing.TryGetValue(register, out StackType definedType)){
                                // This check is not strictly required. It is actually an invariant or assertion.
                                if (!BasicStackTypeEquality(type,definedType)){
                                    throw new Exception("Unexpected mismatch: " + tacInstruction + " " +  type.Kind + " " + definedType.Kind);
                                }
                            } else{
                                typing[register] = type;
                            }
                        }
                    }
                }
                // Phi instructions may require more than one iteration.
                // The algorithm is iterating until it can be checked that 
                // all incoming values of a phi are the same type.
                // To the best of my knowledge, knowing the type of one incoming value
                // is enough. However, I think it is better to enforce this. 
            } while (extraIteration);

            return typing;
        }

        private StackType GetType(TacInstruction tacInstruction, Register reg, IDictionary<Register, StackType> typing, out bool requireExtraIteration){
            
            requireExtraIteration = false;

            if (tacInstruction is BytecodeInstruction bytecodeInstruction){
                OpCode op = bytecodeInstruction.OpCode;

                switch (op.Code){
                    case Code.Ldelem_U1:
                    case Code.Ldelem_U2:
                    case Code.Ldelem_U4:
                    case Code.Ldelem_I1:
                    case Code.Ldelem_I2:
                    case Code.Ldelem_I4:
                    case Code.Ldc_I4_M1:
                    case Code.Ldc_I4_0:
                    case Code.Ldc_I4_1:
                    case Code.Ldc_I4_2:
                    case Code.Ldc_I4_3:
                    case Code.Ldc_I4_4:
                    case Code.Ldc_I4_5:
                    case Code.Ldc_I4_6:
                    case Code.Ldc_I4_7:
                    case Code.Ldc_I4_8:
                    case Code.Ldc_I4_S:
                    case Code.Ldc_I4:
                        return StackType.StackTypeInt32;
                    case Code.Ldelem_I8:
                    case Code.Ldc_I8:
                    case Code.Ldind_I8:
                        return StackType.StackTypeInt64;
                    case Code.Ldc_R4:
                    case Code.Ldc_R8:
                    case Code.Conv_R4:
                    case Code.Conv_R8:
                    case Code.Conv_R_Un:
                    case Code.Ldelem_R4:
                    case Code.Ldelem_R8:
                    case Code.Ldind_R4:
                    case Code.Ldind_R8:
                        return StackType.StackTypeNativeFloat;
                    case Code.Ldnull:
                        return StackType.StackTypeNullObjectRef;
                    case Code.Box:
                        return GetBoxType((TypeReference)bytecodeInstruction.EncodedOperand);
                    case Code.Ldstr:
                        return new AssemblyObjectReferenceStackType(_typeSystem.String);
                    case Code.Newarr:
                        return new AssemblyObjectReferenceStackType(TypeReferenceRocks.MakeArrayType((TypeReference)bytecodeInstruction.EncodedOperand));
                    case Code.Ldind_Ref:
                    {
                        Register addrReg = (Register)bytecodeInstruction.Operands[0];
                        if (!typing.ContainsKey(addrReg)){
                            requireExtraIteration = true;
                            return null;
                        }

                        StackType addrType = typing[addrReg];
                        if (addrType.Kind == StackType.StackTypeKind.NativeInt)
                        {
                            return StackType.StackTypeUnknownObjectRef;
                        }
                        else if (addrType.Kind == StackType.StackTypeKind.NativeManagedPointer){
                            // Consider IntermediateType of the element pointed by the managed ptr
                            if (addrType.Equals(StackType.StackTypeUnknownNativeManagedPointer)){
                                return StackType.StackTypeUnknownObjectRef;
                            }

                            return new AssemblyObjectReferenceStackType(GetIntermediateType(addrType.ManagedPointerType.ElementType));
                        }
                        
                        throw new NotSupportedException("Unhandled case for Ldind_ref");
                    }
                    case Code.Ldelem_Ref:
                    {
                        Register arrayReg = (Register)bytecodeInstruction.Operands[0];
                        if (!typing.ContainsKey(arrayReg)){
                            requireExtraIteration = true;
                            return null;
                        }
                        StackType arrayType = typing[arrayReg];

                        if (arrayType.Equals(StackType.StackTypeUnknownObjectRef)){
                            return StackType.StackTypeUnknownObjectRef;
                        }

                        AssemblyObjectReferenceStackType assemblyStackType = (AssemblyObjectReferenceStackType)arrayType;
                        ArrayType refArrayType = (ArrayType)assemblyStackType.AssemblyTypeReference;
                        return new AssemblyObjectReferenceStackType(GetIntermediateType(refArrayType.ElementType));
                    }
                    case Code.Conv_U:
                    case Code.Conv_I:
                    case Code.Conv_Ovf_I:
                    case Code.Conv_Ovf_U:
                    case Code.Conv_Ovf_I_Un:
                    case Code.Conv_Ovf_U_Un:
                    case Code.Ldind_I:
                    case Code.Ldlen:
                    case Code.Localloc:
                        return StackType.StackTypeNativeInt;
                    case Code.Conv_Ovf_I1:
                    case Code.Conv_Ovf_I2:
                    case Code.Conv_Ovf_I4:
                    case Code.Conv_Ovf_U1:
                    case Code.Conv_Ovf_U2:
                    case Code.Conv_Ovf_U4:
                    case Code.Conv_Ovf_I1_Un:
                    case Code.Conv_Ovf_I2_Un:
                    case Code.Conv_Ovf_I4_Un:
                    case Code.Conv_Ovf_U1_Un:
                    case Code.Conv_Ovf_U2_Un:
                    case Code.Conv_Ovf_U4_Un:
                    case Code.Conv_I1:
                    case Code.Conv_I2:
                    case Code.Conv_I4:
                    case Code.Conv_U1:
                    case Code.Conv_U2:
                    case Code.Conv_U4:
                    case Code.Sizeof:
                    case Code.Ldind_U1:
                    case Code.Ldind_U2:
                    case Code.Ldind_U4:
                    case Code.Ldind_I1:
                    case Code.Ldind_I2:
                    case Code.Ldind_I4:
                    case Code.Cgt:
                    case Code.Cgt_Un:
                    case Code.Ceq:
                    case Code.Clt:
                    case Code.Clt_Un:
                        return StackType.StackTypeInt32;
                    case Code.Conv_U8:
                    case Code.Conv_I8:
                    case Code.Conv_Ovf_I8:
                    case Code.Conv_Ovf_U8:
                        return StackType.StackTypeInt64;
                    case Code.Ldarg_0:
                    case Code.Ldarg_1:
                    case Code.Ldarg_2:
                    case Code.Ldarg_3:
                    case Code.Ldarg_S:
                    case Code.Ldloc_0:
                    case Code.Ldloc_1:
                    case Code.Ldloc_2:
                    case Code.Ldloc_3:
                    case Code.Ldloc_S:
                        return GetStackTypeFromTypeReference(GetIntermediateType(GetTypeFromMemoryVariable(bytecodeInstruction)));
                    case Code.Ldloca:
                    case Code.Ldloca_S:
                    case Code.Ldarga:
                    case Code.Ldarga_S:
                    {
                        return StackType.CreateManagedPointer(TypeReferenceRocks.MakeByReferenceType(GetTypeFromMemoryVariable(bytecodeInstruction)));
                    }
                    case Code.Ldelema:
                    {
                        return StackType.CreateManagedPointer(TypeReferenceRocks.MakeByReferenceType((TypeReference)bytecodeInstruction.EncodedOperand));
                    }
                    case Code.Ldsflda:
                    case Code.Ldflda:
                    {
                        FieldReference fieldReference = (FieldReference)bytecodeInstruction.EncodedOperand;
                        TypeReference intermediateType = GetIntermediateType(ResolveParameters(fieldReference.FieldType, (p) => ResolveGenericParameterAsArgument(p, fieldReference)));
                        return StackType.CreateManagedPointer(new ByReferenceType(intermediateType));
                    }
                    case Code.Ldfld:
                    case Code.Ldsfld:
                    {
                        FieldReference fieldReference = (FieldReference)bytecodeInstruction.EncodedOperand;
                        return GetStackTypeFromTypeReference(GetIntermediateType(ResolveParameters(fieldReference.FieldType, (p) => ResolveGenericParameterAsArgument(p, fieldReference))));
                    }
                    case Code.Newobj:
                    {
                        var ctor = (MethodReference)bytecodeInstruction.EncodedOperand;
                        // We must check either ctor is a value or reference type
                        // This is done in GetStackTypeFromTypeReference.
                        return GetStackTypeFromTypeReference(ctor.DeclaringType);
                    }
                    case Code.Ldelem_Any:
                    {
                        return GetStackTypeFromTypeReference((TypeReference)bytecodeInstruction.EncodedOperand);
                    }
                    case Code.Call:
                    case Code.Callvirt:{
                        MethodReference method = (MethodReference)bytecodeInstruction.EncodedOperand;
                        return GetStackTypeFromTypeReference(ResolveParameters(method.ReturnType, (p) => ResolveGenericParameterAsArgument(p, method))); 
                    }
                    case Code.Ret: {
                        return GetStackTypeFromTypeReference(_method.ReturnType);
                    }
                    case Code.And:
                    case Code.Or:
                    case Code.Xor:
                    case Code.Rem_Un:
                    case Code.Div_Un:
                    {
                        Register reg0 = (Register)bytecodeInstruction.Operands[0];
                        Register reg1 = (Register)bytecodeInstruction.Operands[1];

                        if (!typing.ContainsKey(reg0) || !typing.ContainsKey(reg1)){
                            requireExtraIteration = true;
                            return null;
                        }

                        return GetIntegerOperationType(typing[reg0], typing[reg1]);
                    }
                    case Code.Not:
                    {
                        Register reg0 = (Register)bytecodeInstruction.Operands[0];
                        if (!typing.ContainsKey(reg0)){
                            requireExtraIteration = true;
                            return null;
                        }
                        return GetIntegerOperationType(typing[reg0], typing[reg0]);
                    }
                    case Code.Neg:
                    {
                        Register reg0 = (Register)bytecodeInstruction.Operands[0];
                        if (!typing.ContainsKey(reg0)){
                            requireExtraIteration = true;
                            return null;
                        }
                        return GetUnaryNumeraticOperationsType(typing[reg0]);
                    }
                    case Code.Shl:
                    case Code.Shr:
                    case Code.Shr_Un:
                    {
                        Register reg0 = (Register)bytecodeInstruction.Operands[0];
                        Register reg1 = (Register)bytecodeInstruction.Operands[1];
                        if (!typing.ContainsKey(reg0) || !typing.ContainsKey(reg1)){
                            requireExtraIteration = true;
                            return null;
                        }
                        return GetShiftOperationsType(typing[reg0], typing[reg1]);
                    }
                    case Code.Add:
                    case Code.Div:
                    case Code.Rem:
                    case Code.Sub:
                    case Code.Mul:
                    {
                        Register reg0 = (Register)bytecodeInstruction.Operands[0];
                        Register reg1 = (Register)bytecodeInstruction.Operands[1];

                        if (!typing.ContainsKey(reg0) || !typing.ContainsKey(reg1)){
                            requireExtraIteration = true;
                            return null;
                        }

                        var reg0Type = typing[reg0];
                        var reg1Type = typing[reg1];

                        return GetBinaryNumericOperationType(reg0Type, reg1Type, op.Code);
                    }
                    case Code.Add_Ovf:
                    case Code.Add_Ovf_Un:
                    case Code.Mul_Ovf:
                    case Code.Mul_Ovf_Un:
                    case Code.Sub_Ovf:
                    case Code.Sub_Ovf_Un:
                    {
                        Register reg0 = (Register)bytecodeInstruction.Operands[0];
                        Register reg1 = (Register)bytecodeInstruction.Operands[1];

                        if (!typing.ContainsKey(reg0) || !typing.ContainsKey(reg1)){
                            requireExtraIteration = true;
                            return null;
                        }

                        var reg0Type = typing[reg0];
                        var reg1Type = typing[reg1];

                        return GetOverflowArithmeticOperationType(reg0Type, reg1Type, op.Code);
                    }
                    case Code.Dup:
                    {
                        Register reg0 = (Register)bytecodeInstruction.Operands[0];

                        if (!typing.ContainsKey(reg0)){
                            requireExtraIteration = true;
                            return null;
                        }

                        return typing[(Register)bytecodeInstruction.Operands[0]];
                    }
                    case Code.Castclass:
                        return GetStackTypeFromTypeReference((TypeReference)bytecodeInstruction.EncodedOperand);
                    case Code.Ldftn:
                    case Code.Ldvirtftn:
                    {
                        return StackType.StackTypeNativeInt;
                    }
                    case Code.Ldobj:{
                        
                        return GetStackTypeFromTypeReference(GetIntermediateType((TypeReference)bytecodeInstruction.EncodedOperand));
                    }

                    case Code.Ldtoken:
                    {
                        Object encodedOperand = bytecodeInstruction.EncodedOperand;
                        TypeReference handle = null;

                        if (encodedOperand is TypeReference typeRef){
                            handle = _typeSystem.Boolean.Module.GetType("System.RuntimeTypeHandle");
                        } else if (encodedOperand is MethodReference methodRef){
                            handle = _typeSystem.Boolean.Module.GetType("System.RuntimeMethodHandle");
                        } else if (encodedOperand is FieldReference fieldRef){
                            handle = _typeSystem.Boolean.Module.GetType("System.RuntimeFieldHandle");
                        } else {
                            throw new NotImplementedException("Unexpected encoded operand type: " + encodedOperand.GetType());
                        }

                        if (handle == null){
                            throw new NullReferenceException("TypeReference to handle was not found.");
                        }

                        return GetStackTypeFromTypeReference(handle);
                    }
                    case Code.Refanytype:
                        return StackType.StackTypeUnknownObjectRef;
                    case Code.Arglist:
                        return GetStackTypeFromTypeReference(_typeSystem.Boolean.Module.GetType("System.RuntimeArgumentHandle") ?? throw new NullReferenceException("System.RuntimeArgumentHandle not found"));
                    case Code.Isinst:
                        return GetStackTypeFromTypeReference((TypeReference)bytecodeInstruction.EncodedOperand);
                    case Code.Unbox:
                        return StackType.CreateManagedPointer(TypeReferenceRocks.MakeByReferenceType((TypeReference)bytecodeInstruction.EncodedOperand));
                    case Code.Unbox_Any:
                        return GetStackTypeFromTypeReference(GetIntermediateType((TypeReference)bytecodeInstruction.EncodedOperand));
                    default:
                        throw new NotSupportedException("Unexpected opcode: " + tacInstruction + " in method: " + _body.CilBody.Method.FullName); 
                }
            } else if (tacInstruction is PhiInstruction phiInstruction) {
                
                var incomingRegisters = phiInstruction.Operands.Select(i => (Register)i);
                var definedRegisters = incomingRegisters.Where(r => typing.ContainsKey(r)).ToList();
                requireExtraIteration = definedRegisters.Count < incomingRegisters.Count();
                // It cannot be computed yet.
                if (definedRegisters.Count == 0){
                    return null;
                }
                
                return MergeTypes(definedRegisters, typing);
            }

            throw new ArgumentException("Unhandled argument");
        }

        private StackType MergeTypes(IList<Register> definedRegisters, IDictionary<Register, StackType> typing){
            Register firstDefinedRegister = definedRegisters[0];
            if (!definedRegisters.All(r => BasicStackTypeEquality(typing[r], typing[firstDefinedRegister]))){
                throw new Exception("Mismatch between types in phi.");
            }

            // If all types are object references, ignore null stack type because it can match any other type.
            if (definedRegisters.All(r => typing[r].Kind == StackType.StackTypeKind.NativeObjectRef)){
                foreach (var r in definedRegisters.Where(r => typing[r].IsNullStackType()).ToList()){
                    definedRegisters.Remove(r);
                }

                if (definedRegisters.Count == 0){
                    return StackType.StackTypeNullObjectRef;
                }

                firstDefinedRegister = definedRegisters[0];
            }

            if (!definedRegisters.All(r => FullStackTypeEquality(typing[r], typing[definedRegisters[0]]))){
                switch (typing[firstDefinedRegister].Kind){
                    case StackType.StackTypeKind.NativeManagedPointer:
                        return StackType.StackTypeUnknownNativeManagedPointer;
                    case StackType.StackTypeKind.NativeObjectRef:
                    {
                        if (!_mergeTypes)
                            return StackType.StackTypeUnknownObjectRef;

                        IEnumerable<StackType> stackTypes = definedRegisters.Select(r => typing[r]);

                        if (stackTypes.Any(st => st.Equals(StackType.StackTypeUnknownObjectRef))){
                            return StackType.StackTypeUnknownObjectRef;
                        }

                        TypeReference[] typeReferences = stackTypes.Select(st => {
                            if (st is BoxStackType){
                                return _typeSystem.Object;
                            } else{
                                AssemblyObjectReferenceStackType assembly = (AssemblyObjectReferenceStackType)st;
                                return assembly.AssemblyTypeReference;
                            }
                        }).ToArray();
                        return new AssemblyObjectReferenceStackType(_lca.GetLowestCommonAncestor(typeReferences));
                    }
                    default:
                        throw new NotSupportedException("Unhandled case for unknown type");
                }
            }

            return typing[firstDefinedRegister];
        }

        // Recursively visit all generic parameters in the given 'type' and replace them
        // with non-generic-parameter types 
        public static TypeReference ResolveParameters(TypeReference type, Func<GenericParameter, TypeReference> resolver){
            if (type is GenericParameter genericParameter){
                return resolver(genericParameter);
            }

            // Composite types (recursion)

            if (type is ArrayType arrayType){
                TypeReference element = arrayType.ElementType;
                TypeReference resolved = ResolveParameters(element, resolver);
                return resolved == element ? type : new ArrayType(resolved, arrayType.Rank);
            }

            if (type is ByReferenceType byReferenceType){
                TypeReference element = byReferenceType.ElementType;
                TypeReference resolved = ResolveParameters(element, resolver);
                return resolved == element ? type : new ByReferenceType(resolved);
            }

            if (type is OptionalModifierType optType){
                TypeReference element = optType.ElementType;
                TypeReference resolved = ResolveParameters(element, resolver);
                return resolved == element ? type : new OptionalModifierType(optType.ModifierType, resolved);
            }

            if (type is RequiredModifierType reqType){
                TypeReference element = reqType.ElementType;
                TypeReference resolved = ResolveParameters(element, resolver);
                return resolved == element ? type : new RequiredModifierType(reqType.ModifierType, resolved);
            }

            if (type is GenericInstanceType genericInstance){
                var resolvedGas = genericInstance.GenericArguments.Select(ga => ResolveParameters(ga, resolver)).ToList();

                bool changed = false;
                for (int i=0; i < resolvedGas.Count; i++){
                    if (resolvedGas[i] != genericInstance.GenericArguments[i]){
                        changed = true;
                        break;
                    }
                }

                if (changed){
                    GenericInstanceType res = new GenericInstanceType(genericInstance.ElementType);
                    foreach (var ga in resolvedGas){
                        res.GenericArguments.Add(ga);
                    }    
                    return res;
                }

                return type;
            }

            return type;
        }

        private static TypeReference ResolveGenericParameterAsArgument(GenericParameter genericParameter, FieldReference fieldReference){
            // https://groups.google.com/g/mono-cecil/c/m_hv7mHXpCI
            
            IGenericInstance genericInstance = null;
            switch (genericParameter.Type){
                case GenericParameterType.Type:
                    {
                        genericInstance = (GenericInstanceType)fieldReference.DeclaringType;
                        break;
                    }
                default:
                    throw new NotSupportedException("Something wrong happened");
            }

            return genericInstance.GenericArguments[genericParameter.Position];
        }        

        private static TypeReference ResolveGenericParameterAsArgument(GenericParameter genericParameter, MethodReference methodReference){
            // https://groups.google.com/g/mono-cecil/c/m_hv7mHXpCI
            
            IGenericInstance genericInstance = null;
            switch (genericParameter.Type){
                case GenericParameterType.Type:
                    {
                        genericInstance = (GenericInstanceType)methodReference.DeclaringType;
                        break;
                    }
                case GenericParameterType.Method:
                    {
                        genericInstance = (GenericInstanceMethod)methodReference;
                        break;
                    }
                default:
                    throw new NotSupportedException("Something wrong happened");
            }

            return genericInstance.GenericArguments[genericParameter.Position];
        }        

        // ECMA-CIL: Table III.2: Binary Numeric Operations 
        private StackType GetBinaryNumericOperationType(StackType a, StackType b, Code c){
            if (a == null){
                throw new ArgumentNullException("First argument cannot be null.");
            }
            if (b == null){
                throw new ArgumentNullException("Second argument cannot be null.");
            }
            

            switch (a.Kind){
                case StackType.StackTypeKind.Int32:
                {
                    switch(b.Kind) {
                        case StackType.StackTypeKind.Int32:
                            return StackType.StackTypeInt32;
                        case StackType.StackTypeKind.NativeInt:
                            return StackType.StackTypeNativeInt;
                        case StackType.StackTypeKind.NativeManagedPointer:
                            if (c == Code.Add){
                                return b;
                            }
                            break;
                    }
                    break;
                }
                case StackType.StackTypeKind.Int64:
                {
                    switch(b.Kind){
                        case StackType.StackTypeKind.Int64:
                            return StackType.StackTypeInt64;
                    }
                    break;
                }
            
                case StackType.StackTypeKind.NativeInt:
                {
                    switch (b.Kind){
                        case StackType.StackTypeKind.NativeInt:
                        case StackType.StackTypeKind.Int32:
                            return StackType.StackTypeNativeInt;
                        case StackType.StackTypeKind.NativeManagedPointer:
                            if (c == Code.Add){
                                return b;
                            }
                            break;
                    }
                    break;
                }
                case StackType.StackTypeKind.NativeFloat:
                {
                    switch (b.Kind){
                        case StackType.StackTypeKind.NativeFloat:
                            return StackType.StackTypeNativeFloat;
                    }
                    break;
                }
                case StackType.StackTypeKind.NativeManagedPointer:
                {
                    if (c == Code.Add || c == Code.Sub){
                        switch (b.Kind){
                            case StackType.StackTypeKind.Int32:
                            case StackType.StackTypeKind.NativeInt:
                                return a;
                        }

                        if (c == Code.Sub){
                            if (b.Kind == StackType.StackTypeKind.NativeManagedPointer){
                                return a;
                            }
                        }
                    }
                    break;
                }
            }

            throw new NotImplementedException("Unhandled case: " + a.Kind + " " + b.Kind + " " + c);
        }

        // ECMA-CIL: Table III.7: Overflow Arithmetic Operations 
        private StackType GetOverflowArithmeticOperationType(StackType a, StackType b, Code c){
            if (a == null){
                throw new ArgumentNullException("First argument cannot be null.");
            }
            if (b == null){
                throw new ArgumentNullException("Second argument cannot be null.");
            }
            

            switch (a.Kind){
                case StackType.StackTypeKind.Int32:
                {
                    switch(b.Kind) {
                        case StackType.StackTypeKind.Int32:
                            return StackType.StackTypeInt32;
                        case StackType.StackTypeKind.NativeInt:
                            return StackType.StackTypeNativeInt;
                        case StackType.StackTypeKind.NativeManagedPointer:
                            if (c == Code.Add_Ovf_Un){
                                return b;
                            }
                            break;
                    }
                    break;
                }
                case StackType.StackTypeKind.Int64:
                {
                    switch(b.Kind){
                        case StackType.StackTypeKind.Int64:
                            return StackType.StackTypeInt64;
                    }
                    break;
                }
            
                case StackType.StackTypeKind.NativeInt:
                {
                    switch (b.Kind){
                        case StackType.StackTypeKind.NativeInt:
                        case StackType.StackTypeKind.Int32:
                            return StackType.StackTypeNativeInt;
                        case StackType.StackTypeKind.NativeManagedPointer:
                            if (c == Code.Add_Ovf_Un){
                                return b;
                            }
                            break;
                    }
                    break;
                }
                case StackType.StackTypeKind.NativeManagedPointer:
                {
                    if (c == Code.Add_Ovf_Un || c == Code.Sub_Ovf_Un){
                        switch (b.Kind){
                            case StackType.StackTypeKind.Int32:
                            case StackType.StackTypeKind.NativeInt:
                                return a;
                        }

                        if (c == Code.Sub_Ovf_Un){
                            if (b.Kind == StackType.StackTypeKind.NativeManagedPointer){
                                return StackType.StackTypeNativeInt;
                            }
                        }
                    }
                    break;
                }
            }

            throw new NotImplementedException("Unhandled case");
        }

        // ECMA-CIL: Table III.6: Shift Operations 
        private StackType GetShiftOperationsType(StackType a, StackType b){
            if (a == null){
                throw new ArgumentNullException("First argument cannot be null.");
            }
            if (b == null){
                throw new ArgumentNullException("Second argument cannot be null.");
            }

            switch (a.Kind){
                case StackType.StackTypeKind.Int32:
                {
                    switch (b.Kind){
                        case StackType.StackTypeKind.Int32:
                        case StackType.StackTypeKind.NativeInt:
                            return StackType.StackTypeInt32;
                    }
                    break;
                }
                case StackType.StackTypeKind.Int64:
                {
                    switch (b.Kind){
                        case StackType.StackTypeKind.Int32:
                        case StackType.StackTypeKind.NativeInt:
                            return StackType.StackTypeInt64;
                    }
                    break;
                }
                case StackType.StackTypeKind.NativeInt:
                {
                    switch (b.Kind){
                        case StackType.StackTypeKind.Int32:
                        case StackType.StackTypeKind.NativeInt:
                            return StackType.StackTypeNativeInt;
                    }
                    break;
                }
            }


            throw new NotImplementedException("Unhandled case");
        }

        private StackType GetUnaryNumeraticOperationsType(StackType a) {
            if (a == null){
                throw new ArgumentNullException("First argument cannot be null.");
            }

            switch (a.Kind){
                case StackType.StackTypeKind.Int32:
                    return StackType.StackTypeInt32;
                case StackType.StackTypeKind.Int64:
                    return StackType.StackTypeInt64;
                case StackType.StackTypeKind.NativeInt:
                    return StackType.StackTypeNativeInt;
                case StackType.StackTypeKind.NativeFloat:
                    return StackType.StackTypeNativeFloat;
            }

            throw new NotImplementedException("Unhandled case");
        }

        // ECMA-CIL: Table III.5: Integer Operations
        private static StackType GetIntegerOperationType(StackType a, StackType b){
            if (a == null){
                throw new ArgumentNullException("First argument cannot be null.");
            }
            if (b == null){
                throw new ArgumentNullException("Second argument cannot be null.");
            }

            switch (a.Kind){
                case StackType.StackTypeKind.Int32:
                {
                    switch (b.Kind){
                        case StackType.StackTypeKind.Int32:
                            return StackType.StackTypeInt32;
                        case StackType.StackTypeKind.NativeInt:
                            return StackType.StackTypeNativeInt;
                    }
                    break;
                }
                case StackType.StackTypeKind.Int64:
                {
                    if (b.Kind == StackType.StackTypeKind.Int64){
                        return StackType.StackTypeInt64;
                    }
                    break;
                }
                case StackType.StackTypeKind.NativeInt:
                {
                    switch (b.Kind){
                        case StackType.StackTypeKind.Int32:
                            return StackType.StackTypeNativeInt;
                        case StackType.StackTypeKind.NativeInt:
                            return StackType.StackTypeNativeInt;
                    }

                    break;
                }
            }

            throw new NotImplementedException("Unhandled case");
        }

        private TypeReference GetTypeFromMemoryVariable(BytecodeInstruction inst){
            MemoryVariable memVar = (MemoryVariable)inst.Operands[0];
            return memVar.Type;
        }


        private StackType GetBoxType(TypeReference typeToken){
            // I'm not sure if this is the correct way to check for Nullable.
            // At least, this is how cecil does it.

            bool isNullable = IsTypeOf(typeToken, "System", "Nullable`1");
            bool isGenericParameter = typeToken is GenericParameter;

            if (isGenericParameter){
                return new BoxStackType(typeToken);
            }

            if (isNullable){
                return new BoxStackType(typeToken.GetElementType(), true);
            } else{
                TypeDefinition def = typeToken.Resolve();
                if (def.IsValueType){
                    return new BoxStackType(typeToken);
                } else{
                    return new AssemblyObjectReferenceStackType(typeToken);
                }
            }
        }

        public static bool IsTypeOf (TypeReference self, string @namespace, string name)
        {
            return self.Name == name
                && self.Namespace == @namespace;
        }

        private StackType GetStackTypeFromTypeReference(TypeReference typeReference)
        {
            /*
                ECMA-CIL:
                    In addition to CTS type extensibility, it is possible to emit custom modifiers into
                    member signatures (see Types in Partition II). The CLI will honor these modifiers for
                    purposes of method overloading and hiding, as well as for binding, but will not
                    enforce any of the language-specific semantics. These modifiers can reference the
                    return type or any parameter of a method, or the type of a field. They come in two
                    kinds: required modifiers that anyone using the member must understand in order to
                    correctly use it, and optional modifiers that can be ignored if the modifier is not
                    understood. 
            */

            if (typeReference.IsRequiredModifier || typeReference.IsOptionalModifier){
                IModifierType modifierType = (IModifierType)typeReference;
                return GetStackTypeFromTypeReference(modifierType.ElementType);
            }

            if (typeReference.IsArray) {
                return new AssemblyObjectReferenceStackType(typeReference);
            }

            if (typeReference.IsGenericParameter)
            {
                return StackType.CreateGenericParameter((GenericParameter)typeReference);
            }

            // The intermediate type is the type that is actually in the evaluation stack
            // CLI semantics guarantee that typeReference is automatically transformed into its intermediate type
            TypeReference intermediateType = GetIntermediateType(typeReference);

            if (intermediateType.IsByReference){
                return StackType.CreateManagedPointer((ByReferenceType)intermediateType);
            }

            if (AreEqual(_typeSystem.Int32, intermediateType)){
                return StackType.StackTypeInt32;
            }

            if (AreEqual(_typeSystem.Int64, intermediateType)){
                return StackType.StackTypeInt64;
            }

            if (AreEqual(_typeSystem.IntPtr, intermediateType) || intermediateType.IsPointer){
                return StackType.StackTypeNativeInt;
            }

            if (AreEqual(GetVerificationFloatType(_typeSystem), intermediateType)){
                return StackType.StackTypeNativeFloat;
            }

            TypeDefinition def = typeReference.Resolve();
            if (def.IsValueType){
                return StackType.CreateUserDefined(typeReference);
            } else {
                return new AssemblyObjectReferenceStackType(typeReference);
            }
        }

        public static TypeReference GetUnderlyingType(TypeReference type){

            if (type == null){
                throw new ArgumentNullException();
            }

            /*
                The underlying type of a type T is the following:
                    1. If T is an enumeration type, then its underlying type is the underlying type declared in
                        the enumeration’s definition.
                    2. Otherwise, the underlying type is itself.
            */

            // TODO: Not sure if we should actually ignore the RequiredModifierType and process its element type directly (similarly, optionalmodifiertype)
            if (type is ArrayType || type is ByReferenceType || type is GenericInstanceType || type is RequiredModifierType || type is OptionalModifierType){
                return type;
            }

            // TODO: Is this correct? Anyway, this looks like a pathological case
            if (type is GenericParameter genericParam) {
                if (genericParam.HasConstraints){
                    foreach (GenericParameterConstraint gpc in genericParam.Constraints){
                        TypeReference constraintType = gpc.ConstraintType;
                        TypeDefinition constraintTypeDef = constraintType.Resolve();
                        if (constraintTypeDef.IsEnum) {
                            constraintTypeDef.GetEnumUnderlyingType();
                        }
                    }
                }

                return type;
            }

            TypeDefinition def = type.Resolve();
            if (def.IsEnum){
                return def.GetEnumUnderlyingType();
            }

            return type;
        }

        /*
            ECMA CIL: In other words the reduced type ignores the semantic differences between enumerations
            and the signed and unsigned integer types; treating these types the same if they have the same
            number of bits
        */
        public static TypeReference GetReducedType(TypeReference type){
            /*
                The reduced type of a type T is the following:
                    1. If the underlying type of T is:
                        a. int8, or unsigned int8, then its reduced type is int8.
                        b. int16, or unsigned int16, then its reduced type is int16.
                        c. int32, or unsigned int32, then its reduced type is int32.
                        d. int64, or unsigned int64, then its reduced type is int64.
                        e. native int, or unsigned native int, then its reduced type is native int.
                    2. Otherwise, the reduced type is itself. 
            */

            if (type == null){
                throw new ArgumentNullException();
            }

            ModuleDefinition module = type.Module;
            TypeSystem typeSystem = module.TypeSystem;
            TypeReference underlyingType = GetUnderlyingType(type);

            if (AreEqual(typeSystem.SByte, underlyingType) || AreEqual(typeSystem.Byte, underlyingType)){
                return typeSystem.SByte;
            }

            if (AreEqual(typeSystem.Int16, underlyingType) || AreEqual(typeSystem.UInt16, underlyingType)){
                return typeSystem.Int16;
            }

            if (AreEqual(typeSystem.Int32, underlyingType) || AreEqual(typeSystem.UInt32, underlyingType)){
                return typeSystem.Int32;
            }

            if (AreEqual(typeSystem.Int64, underlyingType) || AreEqual(typeSystem.UInt64, underlyingType)){
                return typeSystem.Int64;
            }

            if (AreEqual(typeSystem.IntPtr, underlyingType) || AreEqual(typeSystem.UIntPtr, underlyingType)){
                return typeSystem.IntPtr;
            }

            return type;
        }

        /*
            ECMA CIL: In other words the verification type ignores the semantic differences between
            enumerations, characters, booleans, the signed and unsigned integer types, and managed pointers
            to any of these; treating these types the same if they have the same number of bits or point to
            types with the same number of bits
        */
        public static TypeReference GetVerificationType(TypeReference type){
            /*
                The verification type (§III.1.8.1.2.1) of a type T is the following:
                    1. If the reduced type of T is:
                        a. int8 or bool, then its verification type is int8.
                        b. int16 or character, then its verification type is int16.
                        c. int32 then its verification type is int32.
                        d. int64 then its verification type is int64.
                        e. native int, then its verification type is native int.
                    2. If T is a managed pointer type S& and the reduced type of S is:
                        a. int8 or bool, then its verification type is int8&.
                        b. int16 or character, then its verification type is int16&.
                        c. int32, then its verification type is int32&.
                        d. int64, then its verification type is int64&.
                        e. native int, then its verification type is native int&.
                    3. Otherwise, the verification type is itself
            */

            if (type == null){
                throw new ArgumentNullException();
            }

            ModuleDefinition module = type.Module;
            TypeSystem typeSystem = module.TypeSystem;
            TypeReference reducedType = GetReducedType(type);

            if (AreEqual(typeSystem.SByte, reducedType) || AreEqual(typeSystem.Boolean, reducedType)){
                return typeSystem.SByte;
            }

            if (AreEqual(typeSystem.Int16, reducedType) || AreEqual(typeSystem.Char, reducedType)){
                return typeSystem.Int16;
            }

            if (AreEqual(typeSystem.Int32, reducedType)){
                return typeSystem.Int32;
            }

            if (AreEqual(typeSystem.Int64, reducedType)){
                return typeSystem.Int64;
            }

            if (AreEqual(typeSystem.IntPtr, reducedType)){
                return typeSystem.IntPtr;
            }

            if (type is ByReferenceType byReferenceType) {
                TypeReference elementType = byReferenceType.ElementType;

                if (elementType is ByReferenceType){
                    throw new System.Exception("The element type of a ByReferenceType cannot be a ByReferenceType");
                }
                
                return new ByReferenceType(GetVerificationType(elementType));
            }

            return type;
        }

        /*
            ECMA CIL: The intermediate type is similar to the verification type in stack state according to the table
            in III.1.8.1.2.1, differing only for floating-point types. The intermediate type of a type T may
            have a different representation and meaning than T.
        */
        public static TypeReference GetIntermediateType(TypeReference type){
            /*
                The intermediate type of a type T is the following:
                    1. If the verification type of T is int8, int16, or int32, then its intermediate type is int32.
                    2. If the verification type of T is a floating-point type then its intermediate type is F (§III.1.1.1).
                    3. Otherwise, the intermediate type is the verification type of T. 
            */

            /*
                Floating-point numbers: See also Partition I, Handling of Floating Point Datatypes.
                    Storage locations for floating-point numbers (statics, array elements, and fields of
                    classes) are of fixed size. The supported storage sizes are float32 and float64.
                    Everywhere else (on the evaluation stack, as arguments, as return types, and as local
                    variables) floating-point numbers are represented using an internal floating-point
                    type. In each such instance, the nominal type of the variable or expression is either
                    float32 or float64, but its value might be represented internally with additional
                    range and/or precision.
            */

            // m-carrasco: I haven't read the floating-point standard used in ECMA-CLI. 
            // I'm assuming that float64 can be used as the internal floating-point type F

            if (type == null){
                throw new ArgumentNullException();
            }

            ModuleDefinition module = type.Module;
            TypeSystem typeSystem = module.TypeSystem;
            TypeReference verificationType = GetVerificationType(type);


            if (AreEqual(typeSystem.SByte, verificationType) || AreEqual(typeSystem.Int16, verificationType) || AreEqual(typeSystem.Int32, verificationType)){
                return typeSystem.Int32;
            }

            if (AreEqual(typeSystem.Single, verificationType) || AreEqual(typeSystem.Double, verificationType)){
                return GetVerificationFloatType(typeSystem);
            }

            return verificationType;
        }
        
        public static TypeReference GetVerificationFloatType(TypeSystem typeSystem){
            return typeSystem.Double;
        }

        private static bool AreEqual(TypeReference a, TypeReference b){
            if (a == null || b == null){
                throw new ArgumentNullException();
            }
            return a.Namespace == b.Namespace && a.Name == b.Name;
        }
    }
}