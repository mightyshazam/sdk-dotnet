using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReflectionAnalysisPoC
{
    /* ***********
     * Everything here is prototypes and experiements.
     * The final implementation will not look like this!
     *********** */
    
    [Workflow(nameof(EatAsync))]
    public interface IWalking
    {
        [WorkflowSignal("DrinkWater")]
        public Task DrinkAsync();

        [WorkflowSignal("EatFood")]
        public Task EatAsync();

        public Task EatAsync(string food = null);

        public Task GoAsync();
    }

    [Workflow(null)]
    public interface IHiking : IWalking
    {
        [WorkflowSignal("DrinkWater4")]
        public new Task DrinkAsync();
    }

    public interface IBackpacking : IWalking
    {
        [WorkflowSignal("EatFood2")]
        public new Task EatAsync();
    }

    public interface ITouring<TBar> : IHiking, IBackpacking
    {
        Task<T> DoStuffAsync<T>(int x, T y, TBar z, byte q = 5);
    }

    public class Mountaineering : IBackpacking, IHiking
    {
        [WorkflowSignal("DrinkWater2")]
        public virtual Task DrinkAsync()
        {
            throw new NotImplementedException();
        }

        public virtual Task GoAsync()
        {
            throw new NotImplementedException();
        }
        
        public virtual Task EatAsync()
        {
            throw new NotImplementedException();
        }

        public Task EatAsync(string food)
        {
            throw new NotImplementedException();
        }
    }

    public class Climbing : Mountaineering, IBackpacking
    {
        [WorkflowSignal("DrinkWater3")]
        public override Task DrinkAsync()
        {
            throw new NotImplementedException();
        }

        Task IBackpacking.EatAsync()
        {
            throw new NotImplementedException();
        }
    }


    public class Touring : ITouring<double>
    {
        public Task<T> DoStuffAsync<T>(int x, T y, double z, byte q = 5)
        {
            throw new NotImplementedException();
        }

        public Task DrinkAsync()
        {
            throw new NotImplementedException();
        }

        public Task EatAsync()
        {
            throw new NotImplementedException();
        }

        public Task EatAsync(string food = null)
        {
            throw new NotImplementedException();
        }

        public Task GoAsync()
        {
            throw new NotImplementedException();
        }
    }

    public class AttributeInfo
    {
        public Attribute Attribute { get; }
        public ICustomAttributeProvider Target { get; }

        public AttributeInfo(Attribute attribute, ICustomAttributeProvider target)
        {
            Attribute = attribute;
            Target = target;
        }

        public T GetTarget<T>() where T : class
        {
            return Target as T;
        }
    }

    internal class Program
    {
        internal static void Main(string[] _)
        {
            Console.WriteLine();

            Type backpackingType = typeof(IBackpacking);
            //backpackingType.

            PrintAttributes(backpackingType);

            PrintAttributes(typeof(IHiking));

            PrintAttributes(typeof(Mountaineering));

            PrintAttributes(typeof(Climbing));

            PrintWorkflowAttributeMap(typeof(Climbing));

            PrintWorkflowAttributeMap(typeof(ITouring<>));

            PrintWorkflowAttributeMap(typeof(Touring));

            Console.WriteLine("-----------");
        }

        private static void PrintWorkflowAttributeMap(Type type)
        {
            Console.WriteLine();
            Console.WriteLine("----------- ----------- -----------");
            Console.WriteLine($"TYPE: {type.FullName}");

            Console.WriteLine();
            Console.WriteLine($"Interfaces:");
            Type[] typeIfaces= type.GetInterfaces();
            foreach(Type iface in typeIfaces)
            {
                Console.WriteLine($"    {iface.FullName}");
                PrintMethods(iface.GetMethods(), "        ");
            }

            Console.WriteLine();
            Console.WriteLine($"Inheritance:");
            Type baseType = type;
            while (baseType != null)
            {
                Console.WriteLine($"    {baseType.FullName}");
                PrintMethods(baseType.GetMethods(), "        ");

                baseType = baseType.BaseType;
            }

            Console.WriteLine();
            Console.WriteLine($"Methods:");
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
            MethodInfo[] typeMethods = type.GetMethods(flags);
            //PrintMethods(typeMethods, "    ");

            var methodAttributes = new Dictionary<MethodInfo, List<AttributeInfo>>();
            foreach (MethodInfo method in typeMethods)
            {
                var methodAttrs = new List<AttributeInfo>();
                methodAttributes.Add(method, methodAttrs);

                object[] detectedMethodAttrs = method.GetCustomAttributes(inherit: false);
                foreach (object mAttr in detectedMethodAttrs)
                {
                    if (mAttr is Attribute methAttr)
                    {
                        methodAttrs.Add(new AttributeInfo(methAttr, method));
                    }
                }

                Console.WriteLine($"    \"{method.Name}\";"
                                + $"\t\t ({GetMethodVisibility(method)});"
                                + $"\t Declared: {method.DeclaringType.Name};"
                                + $"\t Reflected: {method.ReflectedType.Name}");

                string indent = "    ";
                MethodInfo currMethod = method;
                MethodInfo methodBaseDef = currMethod.GetBaseDefinition();
                while (methodBaseDef != null && methodBaseDef != currMethod)
                {
                    object[] detectedBaseMethodAttrs = methodBaseDef.GetCustomAttributes(inherit: false);
                    foreach (object mAttr in detectedBaseMethodAttrs)
                    {
                        if (mAttr is Attribute methAttr)
                        {
                            methodAttrs.Add(new AttributeInfo(methAttr, methodBaseDef));
                        }
                    }

                    indent = indent + "    ";
                    Console.WriteLine($"{indent}-> \"{methodBaseDef.Name}\";"
                                + $"\t\t ({GetMethodVisibility(method)});"
                                + $"\t Declared: {methodBaseDef.DeclaringType.Name};"
                                + $"\t Reflected: {methodBaseDef.ReflectedType.Name}");

                    currMethod = methodBaseDef;
                    methodBaseDef = currMethod.GetBaseDefinition();
                }                
            }

            Console.WriteLine();
            Console.WriteLine($"Iface maps (Type={type.Name}):");

            var ifaceMethodsToImplement = new Dictionary<string, List<MethodInfo>>();

            if (type.IsInterface)
            {
                Console.WriteLine("Type is an Iface. Building Specification Merge Map instead of Implementation Map.");

                var methods = new List<MethodInfo>();
                foreach(MethodInfo m in type.GetMethods())
                {
                    methods.Add(m);
                }

                foreach (Type iface in typeIfaces)
                {
                    foreach (MethodInfo m in iface.GetMethods())
                    {
                        methods.Add(m);
                    }
                }

                foreach (MethodInfo method in methods)
                {
                    string methodSignature = WriteMethodSignature(method);

                    List<MethodInfo> signatureMethods = GetOrAddNew(ifaceMethodsToImplement, methodSignature);
                    signatureMethods.Add(method);

                    List<AttributeInfo> currMethodAttrs = GetOrAddNew(methodAttributes, method);
                    object[] detectedMethodAttrs = method.GetCustomAttributes(inherit: false);
                    foreach (object mAttr in detectedMethodAttrs)
                    {
                        if (mAttr is Attribute methAttr)
                        {
                            currMethodAttrs.Add(new AttributeInfo(methAttr, method));
                        }
                    }
                }
            }
            else
            {
                foreach (Type iface in typeIfaces)
                {
                    Console.WriteLine();
                    Console.WriteLine($"    Iface map for {iface.FullName}:");

                    InterfaceMapping ifaceMap = type.GetInterfaceMap(iface);
                    for (int i = 0; i < ifaceMap.TargetMethods.Length; i++)
                    {
                        MethodInfo typeMethod = ifaceMap.TargetMethods[i];
                        MethodInfo ifaceMethod = ifaceMap.InterfaceMethods[i];

                        List<AttributeInfo> attrs = GetOrAddNew(methodAttributes, typeMethod);
                        
                        object[] ifaceMethodAttrs = ifaceMethod.GetCustomAttributes(inherit: false);
                        foreach (object mAttr in ifaceMethodAttrs)
                        {
                            if (mAttr is Attribute methAttr)
                            {
                                attrs.Add(new AttributeInfo(methAttr, ifaceMethod));
                            }
                        }

                        Console.WriteLine($"        Type Method:  \"{WriteMethodSignature(typeMethod)}\";"
                                    + $"\t\t Declared: {typeMethod.DeclaringType.Name};"
                                    + $"\t Reflected: {typeMethod.ReflectedType.Name}"
                                    + $"\t ({attrs.Count} attributes so far)");

                        Console.WriteLine($"        Iface Method: \"{WriteMethodSignature(ifaceMethod)})\";"
                                    + $"\t\t Declared: {ifaceMethod.DeclaringType.Name};"
                                    + $"\t Reflected: {ifaceMethod.ReflectedType.Name}"
                                    + $"\t ({ifaceMethodAttrs.Length} attributes)");
                    }
                }
            }

            if (type.IsInterface)
            {
                Console.WriteLine();
                Console.WriteLine($"Attribute Map (Iface Type={type.Name}):");

                foreach (KeyValuePair<string, List<MethodInfo>> methodOverloads in ifaceMethodsToImplement)
                {
                    Console.WriteLine();
                    
                    if (methodOverloads.Value.Count > 1)
                    {
                        Console.WriteLine($"    \"{methodOverloads.Key}\" has {methodOverloads.Value.Count} ambigious overloads:");
                        Console.WriteLine("    This will be a PROBLEM is and only if more than one has Workflow-relevant attributes.");
                    }
                    else
                    {
                        Console.WriteLine($"    {methodOverloads.Key}:");
                    }

                    foreach (MethodInfo overload in methodOverloads.Value)
                    {
                        List<AttributeInfo> attributes = methodAttributes[overload];
                        PrintAttributeMapEntry(overload, attributes, "        ", fullSignature: false);
                    }
                }
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine($"Attribute Map (Non-Iface Type={type.Name}):");
                foreach (KeyValuePair<MethodInfo, List<AttributeInfo>> methodData in methodAttributes)
                {
                    Console.WriteLine();
                    PrintAttributeMapEntry(methodData.Key, methodData.Value, "    ", fullSignature: true);
                }
            }
        }

        private static void PrintAttributeMapEntry(MethodInfo method, List<AttributeInfo> attributes, string prefix, bool fullSignature)
        {
            Console.WriteLine($"{prefix}\"{(fullSignature ? WriteMethodSignature(method) : method.Name)}\";"
                                    + $"\t\t ({GetMethodVisibility(method)});"
                                    + $"\t Declared: {method.DeclaringType.Name};"
                                    + $"\t Reflected: {method.ReflectedType.Name}");

            if (attributes == null || attributes.Count == 0)
            {
                Console.WriteLine($"{prefix}    -");
                return;
            }

            foreach (AttributeInfo attrInfo in attributes)
            {
                Attribute attr = attrInfo.Attribute;

                Console.Write($"{prefix}    [{attr.GetType().Name}]");

                if (attr is WorkflowAttribute wfAttr)
                {
                    Console.Write($" MainMethod=\"{wfAttr.MainMethod}\"");
                }

                if (attr is WorkflowQueryAttribute wfQuAttr)
                {
                    Console.Write($" QueryTypeName=\"{wfQuAttr.QueryTypeName}\"");
                }

                if (attr is WorkflowSignalAttribute wfSgAttr)
                {
                    Console.Write($" SignalTypeName=\"{wfSgAttr.SignalTypeName}\"");
                }

                if (attrInfo.Target is MethodInfo targetMethod)
                {
                    Console.Write($" (on {targetMethod.DeclaringType}::{WriteMethodSignature(targetMethod)})");
                }
                else if (attrInfo.Target is MemberInfo targetMember)
                {
                    Console.Write($" (on {targetMember.Name})");
                }                    

                Console.WriteLine();
            }
        }


        private static string WriteMethodSignature(MethodInfo method)
        {
            var signature = new StringBuilder();
            signature.Append(method.Name);
            
            int openBracePos = signature.Length;
            signature.Append('(');

            int index = 0;
            int genericIndex = 0;
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                if (index++ > 0)
                {
                    signature.Append(", ");
                }

                Type parameterType = parameter.ParameterType;
                if (parameterType.IsGenericParameter)
                {
                    signature.Append('T');                    
                    signature.Append(++genericIndex);
                }
                else
                {
                    signature.Append(parameter.ParameterType.FullName);
                }                
            }

            if (genericIndex > 0)
            {
                signature.Insert(openBracePos, genericIndex);
                signature.Insert(openBracePos, '`');
            }

            signature.Append(')');
            return signature.ToString();
        }

        private static void PrintMethods(MethodInfo[] methods, string prefix)
        {
            foreach (MethodInfo method in methods)
            {
                Console.WriteLine($"{prefix}\"{method.Name}\";"
                                + $"\t\t ({GetMethodVisibility(method)});"
                                + $"\t Declared: {method.DeclaringType.Name};"
                                + $"\t Reflected: {method.ReflectedType.Name}");
            }
        }

        private static TValue GetOrAddNew<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
        {
            if (!dictionary.TryGetValue(key, out TValue value))
            {
                value = new TValue();
                dictionary.Add(key, value);
            }

            return value;
        }

        private static string GetMethodVisibility(MethodBase method)
        {
            if (method.IsPublic) { return "Public"; }
            if (method.IsAssembly) { return "Assembly"; }
            if (method.IsFamilyAndAssembly) { return "FamilyAndAssembly"; }
            if (method.IsFamily) { return "Family"; }
            if (method.IsFamilyOrAssembly) { return "FamilyOrAssembly"; }
            if (method.IsPrivate) { return "Private"; }
            return "Unknown";
        }

        private static void PrintAttributes(Type type)
        {
            Console.WriteLine();
            Console.WriteLine($"{type.FullName} {{");

            Console.WriteLine($"    Type Attributes {{");
            Attribute[] attrs = Attribute.GetCustomAttributes(type, inherit: true);
            foreach (Attribute attr in attrs)
            {
                WorkflowAttribute fwAttr = attr as WorkflowAttribute;
                if (fwAttr != null)
                {
                    Console.WriteLine($"        {fwAttr.GetType().Name} (default={fwAttr.IsDefaultAttribute()}), MainMethod=\"{fwAttr.MainMethod}\"");
                }
                else
                {
                    Console.WriteLine($"        {attr.GetType().Name} (default={attr.IsDefaultAttribute()})");
                }
            }
            Console.WriteLine($"    }}");


            Console.WriteLine($"    Methods {{");
            MethodInfo[] methods = type.GetMethods();
            foreach(MethodInfo method in methods)
            {
                Console.WriteLine($"        {method.Name} {{");

                Attribute[] methodAttrs = Attribute.GetCustomAttributes(method, inherit: true);
                foreach (Attribute attr in methodAttrs)
                {
                    WorkflowSignalAttribute sgAttr = attr as WorkflowSignalAttribute;
                    if (sgAttr != null)
                    {
                        Console.WriteLine($"            {sgAttr.GetType().Name} (default={sgAttr.IsDefaultAttribute()}), SignalTypeName=\"{sgAttr.SignalTypeName}\"");
                    }
                    else
                    {
                        Console.WriteLine($"            {attr.GetType().Name} (default={attr.IsDefaultAttribute()})");
                    }
                }

                Console.WriteLine($"        }}");
            }

            Console.WriteLine($"    }}");

            Console.WriteLine("}");
        }

        private static void PrintAttributes2(Type type)
        {
            Console.WriteLine();
            Console.WriteLine($"{type.FullName} {{");

            Attribute[] attrs = (Attribute[]) type.GetCustomAttributes(typeof(Attribute), inherit: true);
            foreach (Attribute attr in attrs)
            {
                Console.WriteLine($"    {attr.GetType().Name} (default={attr.IsDefaultAttribute()})");
            }

            Console.WriteLine("}");
        }
    }
}
