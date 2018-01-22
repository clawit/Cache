namespace Cache.Fody
{
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Rocks;
    using Mono.Collections.Generic;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class CecilExtensions
    {
        public static bool IsBoxingRequired(this TypeReference typeReference, TypeReference expectedType)
        {
            if (expectedType.IsValueType && string.Equals(typeReference.FullName, expectedType.FullName))
            {
                // Boxing is never required if type is expected
                return false;
            }

            if (typeReference.IsValueType || typeReference.IsGenericParameter)
            {
                return true;
            }

            return false;
        }

        public static IEnumerable<MethodDefinition> AbstractMethods(this TypeDefinition type)
        {
            return type.Methods.Where(x => x.IsAbstract);
        }

        public static IEnumerable<MethodDefinition> ConcreteMethods(this TypeDefinition type)
        {
            return type.Methods.Where(x => !x.IsAbstract && x.HasBody && !IsEmptyConstructor(x));
        }

        static bool IsEmptyConstructor(this MethodDefinition method)
        {
            return method.Name == ".ctor" &&
                   method.Body.Instructions.Count(x => x.OpCode != OpCodes.Nop) == 3;
        }

        public static bool IsInterceptor(this TypeReference type)
        {
            return type.Name == "CacheLogger";
        }
        public static bool IsInstanceConstructor(this MethodDefinition methodDefinition)
        {
            return methodDefinition.IsConstructor && !methodDefinition.IsStatic;
        }

        public static void InsertBefore(this MethodBody body, Instruction target, Instruction instruction)
        {
            body.Instructions.InsertBefore(target, instruction);
        }

        public static void InsertBefore(this Collection<Instruction> instructions, Instruction target, Instruction instruction)
        {
            var index = instructions.IndexOf(target);
            instructions.Insert(index, instruction);
        }

        public static string MethodName(this MethodDefinition method)
        {
            if (method.IsConstructor)
            {
                return $"{method.DeclaringType.Name}{method.Name} ";
            }
            return $"{method.DeclaringType.Name}.{method.Name} ";
        }

        public static void Insert(this MethodBody body, int index, IEnumerable<Instruction> instructions)
        {
            instructions = instructions.Reverse();
            foreach (var instruction in instructions)
            {
                body.Instructions.Insert(index, instruction);
            }
        }

        public static void Add(this MethodBody body, params Instruction[] instructions)
        {
            foreach (var instruction in instructions)
            {
                body.Instructions.Add(instruction);
            }
        }

        public static bool IsYield(this MethodDefinition method)
        {
            if (method.ReturnType == null)
            {
                return false;
            }
            if (!method.ReturnType.Name.StartsWith("IEnumerable"))
            {
                return false;
            }
            var stateMachinePrefix = $"<{method.Name}>";
            var nestedTypes = method.DeclaringType.NestedTypes;
            return nestedTypes.Any(x => x.Name.StartsWith(stateMachinePrefix));
        }

        public static CustomAttribute GetAsyncStateMachineAttribute(this MethodDefinition method)
        {
            return method.CustomAttributes.FirstOrDefault(_ => _.AttributeType.Name == "AsyncStateMachineAttribute");
        }

        public static bool IsAsync(this MethodDefinition method)
        {
            return GetAsyncStateMachineAttribute(method) != null;
        }

        public static bool IsLeaveInstruction(this Instruction instruction)
        {
            return instruction.OpCode == OpCodes.Leave || instruction.OpCode == OpCodes.Leave_S;
        }

        public static MethodDefinition Method(this TypeDefinition type, string name)
        {
            var method = type.Methods.FirstOrDefault(x => x.Name == name);
            if (method == null)
            {
                throw new Exception($"Could not find method '{name}' on type {type.FullName}.");
            }
            return method;
        }

        public static MethodDefinition Method(this TypeDefinition type, string name, params string[] parameters)
        {
            var method = type.Methods.FirstOrDefault(x =>
            {
                return x.Name == name &&
                       parameters.Length == x.Parameters.Count &&
                       x.Parameters.Select(y => y.ParameterType.Name).SequenceEqual(parameters);
            });
            if (method == null)
            {
                throw new Exception($"Could not find method '{name}' on type {type.FullName}.");
            }
            return method;
        }

        public static TypeDefinition Type(this List<TypeDefinition> types, string name)
        {
            var type = types.FirstOrDefault(x => x.Name == name);
            if (type == null)
            {
                throw new Exception($"Could not find type '{name}'.");
            }
            return type;
        }

        public static MethodDefinition GetPropertyGet(this TypeDefinition typeDefinition, string propertyName)
        {
            return typeDefinition.Properties.Where(x => x.Name == propertyName).Select(x => x.GetMethod).SingleOrDefault();
        }

        public static MethodDefinition GetInheritedPropertyGet(this TypeDefinition baseType, string propertyName)
        {
            MethodDefinition methodDefinition = baseType.GetPropertyGet(propertyName);

            if (methodDefinition == null && baseType.BaseType != null)
            {
                return baseType.BaseType.Resolve().GetInheritedPropertyGet(propertyName);
            }

            if (methodDefinition == null && baseType.BaseType == null)
            {
                return null;
            }

            if (methodDefinition.IsPrivate)
            {
                return null;
            }

            return methodDefinition;
        }

        public static MethodReference MakeHostInstanceGeneric(this MethodReference self, params TypeReference[] args)
        {
            MethodReference reference = new MethodReference(self.Name, self.ReturnType, self.DeclaringType.MakeGenericInstanceType(args))
            {
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis,
                CallingConvention = self.CallingConvention,
            };

            foreach (ParameterDefinition parameter in self.Parameters)
            {
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
            }

            foreach (GenericParameter genericParam in self.GenericParameters)
            {
                reference.GenericParameters.Add(new GenericParameter(genericParam.Name, reference));
            }

            return reference;
        }

        public static int AddVariable(this MethodDefinition method, TypeReference typeReference)
        {
            method.Body.Variables.Add(method.Module.ImportVariable(typeReference));

            return method.Body.Variables.Count - 1;
        }

        public static VariableDefinition ImportVariable(this ModuleDefinition module, TypeReference typeReference)
        {
            return new VariableDefinition(module.ImportReference(typeReference));
        }

        public static Instruction AppendLdstr(this Instruction instruction, ILProcessor processor, string str)
        {
            return instruction.Append(processor.Create(OpCodes.Ldstr, str), processor);
        }

        public static Instruction Append(this Instruction instruction, Instruction instructionAfter, ILProcessor processor)
        {
            processor.InsertAfter(instruction, instructionAfter);

            return instructionAfter;
        }

        public static Instruction AppendLdcI4(this Instruction instruction, ILProcessor processor, int value)
        {
            return instruction.Append(processor.Create(OpCodes.Ldc_I4, value), processor);
        }

        public static Instruction AppendDup(this Instruction instruction, ILProcessor processor)
        {
            return instruction.Append(processor.Create(OpCodes.Dup), processor);
        }

        public static Instruction AppendLdloc(this Instruction instruction, ILProcessor processor, int index)
        {
            return instruction.Append(processor.Create(OpCodes.Ldloc, index), processor);
        }

        public static MethodReference ImportMethod(this ModuleDefinition module, MethodDefinition methodDefinition)
        {
            return module.ImportReference(methodDefinition);
        }

        public static Instruction Prepend(this Instruction instruction, Instruction instructionBefore, ILProcessor processor)
        {
            processor.InsertBefore(instruction, instructionBefore);

            return instructionBefore;
        }

        public static Instruction AppendStloc(this Instruction instruction, ILProcessor processor, int index)
        {
            return instruction.Append(processor.Create(OpCodes.Stloc, index), processor);
        }

        public static Instruction AppendLdarg(this Instruction instruction, ILProcessor processor, int index)
        {
            return instruction.Append(processor.Create(OpCodes.Ldarg, index), processor);
        }

        public static Instruction AppendBoxIfNecessary(this Instruction instruction, ILProcessor processor, TypeReference typeReference)
        {
            if (typeReference.IsValueType || typeReference.IsGenericParameter)
            {
                return instruction.Append(processor.Create(OpCodes.Box, typeReference), processor);
            }

            return instruction;
        }

        public static Instruction AppendLd(this Instruction instruction, ILProcessor processor, CustomAttributeArgument argument)
        {
            // Tested with the following types (see ECMA-334, 24.1.3 attribute parameter types)
            // bool, byte, char, double, float, int, long, sbyte, short, string, uint, ulong, ushort.
            // object
            // System.Type
            // An enum type, provided it has public accessibility and the types in which it is nested (if any) also have public accessibility (§17.2)
            // Single-dimensional arrays of the above types
            TypeReference argumentType = argument.Type;

            switch (argumentType.MetadataType)
            {
                case MetadataType.ValueType:
                    if (argumentType.Resolve().IsEnum == false)
                    {
                        throw new ArgumentException("Type Enum expected.", "argument");
                    }

                    // Get underlying Enum type
                    argumentType = argument.Type.Resolve().Fields.First(field => field.Name == "value__").FieldType;
                    break;

                case MetadataType.Object:
                    return instruction.AppendLd(processor, (CustomAttributeArgument)argument.Value);

                case MetadataType.Array:
                    CustomAttributeArgument[] values = (CustomAttributeArgument[])argument.Value;

                    instruction = instruction
                        .AppendLdcI4(processor, values.Length)
                        .Append(processor.Create(OpCodes.Newarr, argument.Type.GetElementType()), processor);

                    TypeReference arrayType = argument.Type.GetElementType();

                    for (int i = 0; i < values.Length; i++)
                    {
                        instruction = instruction
                            .AppendDup(processor)
                            .AppendLdcI4(processor, i)
                            .AppendLd(processor, values[i]);

                        if (arrayType == ReferenceFinder.Weaver.ModuleDefinition.TypeSystem.Object)
                        {
                            instruction = instruction.AppendBoxIfNecessary(processor, ((CustomAttributeArgument)values[i].Value).Type);
                        }
                        else if (argumentType.Resolve().IsEnum)
                        {
                            // Get underlying Enum type
                            arrayType = argument.Type.Resolve().Fields.First(field => field.Name == "value__").FieldType;
                        }

                        if (arrayType.IsValueType)
                        {
                            switch (arrayType.MetadataType)
                            {
                                case MetadataType.Boolean:
                                case MetadataType.SByte:
                                case MetadataType.Byte:
                                    instruction = instruction.Append(processor.Create(OpCodes.Stelem_I1), processor);
                                    break;

                                case MetadataType.Char:
                                case MetadataType.Int16:
                                case MetadataType.UInt16:
                                    instruction = instruction.Append(processor.Create(OpCodes.Stelem_I2), processor);
                                    break;

                                case MetadataType.Int32:
                                case MetadataType.UInt32:
                                    instruction = instruction.Append(processor.Create(OpCodes.Stelem_I4), processor);
                                    break;

                                case MetadataType.Int64:
                                case MetadataType.UInt64:
                                    instruction = instruction.Append(processor.Create(OpCodes.Stelem_I8), processor);
                                    break;

                                case MetadataType.Single:
                                    instruction = instruction.Append(processor.Create(OpCodes.Stelem_R4), processor);
                                    break;

                                case MetadataType.Double:
                                    instruction = instruction.Append(processor.Create(OpCodes.Stelem_R8), processor);
                                    break;

                                default:
                                    throw new ArgumentException("Unrecognized array value type.", "argument");
                            }
                        }
                        else
                        {
                            instruction = instruction.Append(processor.Create(OpCodes.Stelem_Ref), processor);
                        }
                    }

                    return instruction;
            }

            switch (argumentType.MetadataType)
            {
                case MetadataType.Boolean:
                case MetadataType.SByte:
                    return instruction.Append(processor.Create(OpCodes.Ldc_I4_S, Convert.ToSByte(argument.Value)), processor);

                case MetadataType.Int16:
                    return instruction.Append(processor.Create(OpCodes.Ldc_I4, Convert.ToInt16(argument.Value)), processor);

                case MetadataType.Int32:
                    return instruction.Append(processor.Create(OpCodes.Ldc_I4, Convert.ToInt32(argument.Value)), processor);

                case MetadataType.Int64:
                    return instruction.Append(processor.Create(OpCodes.Ldc_I8, Convert.ToInt64(argument.Value)), processor);

                case MetadataType.Byte:
                    return instruction.Append(processor.Create(OpCodes.Ldc_I4, Convert.ToInt32(argument.Value)), processor);

                case MetadataType.UInt16:
                    unchecked
                    {
                        return instruction.Append(processor.Create(OpCodes.Ldc_I4, (Int16)(UInt16)argument.Value), processor);
                    }

                case MetadataType.Char:
                    return instruction.Append(processor.Create(OpCodes.Ldc_I4, Convert.ToChar(argument.Value)), processor);

                case MetadataType.UInt32:
                    unchecked
                    {
                        return instruction.Append(processor.Create(OpCodes.Ldc_I4, (Int32)(UInt32)argument.Value), processor);
                    }

                case MetadataType.UInt64:
                    unchecked
                    {
                        return instruction.Append(processor.Create(OpCodes.Ldc_I8, (Int64)(UInt64)argument.Value), processor);
                    }

                case MetadataType.Single:
                    return instruction.Append(processor.Create(OpCodes.Ldc_R4, Convert.ToSingle(argument.Value)), processor);

                case MetadataType.Double:
                    return instruction.Append(processor.Create(OpCodes.Ldc_R8, Convert.ToDouble(argument.Value)), processor);

                case MetadataType.String:
                    return instruction.Append(processor.Create(OpCodes.Ldstr, (string)argument.Value), processor);

                case MetadataType.Class:
                    if (argumentType.Resolve().IsDefinition == false)
                    {
                        throw new ArgumentException("Type Type expected.", "argument");
                    }

                    return
                        instruction
                            .Append(processor.Create(OpCodes.Ldtoken, (TypeReference)argument.Value), processor)
                            .Append(processor.Create(OpCodes.Call, ReferenceFinder.Weaver.ModuleDefinition.ImportMethod(
                                ReferenceFinder.SystemTypeGetTypeFromHandleMethod)), processor);

                default:
                    throw new ArgumentException("Unrecognized attribute parameter type.", "argument");
            }
        }

        		public static MethodReference MakeGeneric(this MethodReference method, params TypeReference[] arguments)
		{
			if (method.GenericParameters.Count != arguments.Length)
			{
				throw new ArgumentException("Invalid number of generic type arguments supplied");
			}

			if (arguments.Length == 0)
			{
				return method;
			}

			GenericInstanceMethod genericTypeReference = new GenericInstanceMethod(method);

			foreach (TypeReference argument in arguments)
			{
				genericTypeReference.GenericArguments.Add(argument);
			}

			return genericTypeReference;
		}
    }
}
