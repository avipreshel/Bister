using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BisterLib
{
    public static class BisterConsts
    {
        public static readonly FieldInfo ExceptionHResult = typeof(Exception).GetField("_HResult", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo ExceptionSource = typeof(Exception).GetField("_source", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo ExceptionMessage = typeof(Exception).GetField("_message", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo ExceptionStackTrace = typeof(Exception).GetField("_stackTraceString", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly string NullStr = "_NUL_";
    }

    public class Bister : IBister
    {


        static Lazy<IBister> _lazy = new Lazy<IBister>(() => new Bister());
        public static IBister Instance => _lazy.Value;

        ConcurrentDictionary<Type, IBisterGenerated> _typeToSerializer = new ConcurrentDictionary<Type, IBisterGenerated>();
        ConcurrentDictionary<Type, int> _typeToMaxSize = new ConcurrentDictionary<Type, int>();

        /// <summary>
        /// If not empty, will dump the latest serialization class into the path, e.g. c:\temp\gen.cs
        /// </summary>
        public string DebugPath { get; set; } = string.Empty;

        public bool IsDebug => !string.IsNullOrEmpty(DebugPath);

        private Bister() { }

        #region IBister

        public object Deserialize(BinaryReader br, Type objType)
        {
            var serializer = GenerateSerializationEngine(objType);
            return serializer.Deserialize(br);
        }
        
        public T Deserialize<T>(byte[] blob)
        {
            return (T)Deserialize(blob, typeof(T));
        }

        public object Deserialize(byte[] blob, Type objType)
        {
            using (MemoryStream stream = new MemoryStream(blob))
            {
                using (BinaryReader br = new BinaryReader(stream))
                {
                    return Deserialize(br, objType);
                }
            }
        }

        

        public byte[] Serialize<T>(T instance)
        {
            return Serialize(instance, typeof(T));
        }

        public byte[] Serialize(object instance, Type objType)
        {
            var serializer = GenerateSerializationEngine(objType);
            int size = 1024;
            _typeToMaxSize.TryGetValue(objType, out size);

            using (var ms = new MemoryStream(size))
            {
                using (var bw = new BinaryWriter(ms))
                {
                    serializer.Serialize(instance, bw);
                    byte[] blob = ms.ToArray();
                    _typeToMaxSize[objType] = Math.Max(size, blob.Length);
                    return blob;
                }
            }
        }

        public void Serialize(object instance, Type objType, BinaryWriter bw)
        {
            var serializer = GenerateSerializationEngine(objType);
            serializer.Serialize(instance, bw);
        }

        #endregion

        public static bool TestGenericType(Type objType,Type genericType)
        {
            return genericType.IsAssignableFrom(objType) || objType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericType);
        }

        public static Type GetGenericAncestor(Type objType, Type genericTypeLookup)
        {
            Type currType = objType;
            while (!(currType.IsGenericType && currType.GetGenericTypeDefinition() == genericTypeLookup))
            {
                currType = currType.BaseType;
                if (currType == null)
                {
                    throw new Exception($"{objType} does not inherit or implement {genericTypeLookup}");
                }
            }
            return currType;
        }

        public static void IncreaseIndent(ref string indent)
        {
            indent = indent + "\t";
        }

        public static void DecreaseIndent(ref string indent)
        {
            indent = indent.Substring(0,indent.Length-1);
        }

        public static bool IsTopLevelInstanceDecleration(string instanceName)
        {
            return !instanceName.Contains(".") && !instanceName.Contains("[");
        }

        string ReadTemplateFromResource()
        {
            string[] resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            string clientProxyResourceName = resourceNames.First(res => res.Contains("BisterTemplate"));
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(clientProxyResourceName))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static void PrintMethodName(StringBuilderVerbose sb, string indentation, Type objType = null)
        {
            var callingMethod = (new StackTrace()).GetFrame(1).GetMethod();
            if (objType == null)
            {
                sb.AppendLine(indentation + $"// Method : {callingMethod.DeclaringType.Name}.{callingMethod.Name}()");
            }
            else
            {
                sb.AppendLine(indentation + $"// Method : {callingMethod.DeclaringType.Name}.{callingMethod.Name}({Bister.GetFriendlyGenericTypeName(objType)})");
            }
        }

        public static string GetFriendlyGenericTypeName(Type type)
        {
            if (!type.IsGenericType)
            {
                return type.FullName;
            }

            string typeName = type.Name;
            int backtickIndex = typeName.IndexOf('`');
            if (backtickIndex > 0)
            {
                typeName = typeName.Remove(backtickIndex);
            }

            Type[] genericArguments = type.GetGenericArguments();
            string[] genericArgumentNames = new string[genericArguments.Length];
            for (int i = 0; i < genericArguments.Length; i++)
            {
                genericArgumentNames[i] = GetFriendlyGenericTypeName(genericArguments[i]);
            }

            return $"{typeName}<{string.Join(", ", genericArgumentNames)}>";
        }

        IBisterGenerated GenerateSerializationEngine(Type objType)
        {
            if (_typeToSerializer.TryGetValue(objType, out IBisterGenerated serTmp))
            {
                return serTmp;
            }

            var sb = new StringBuilderVerbose(true);
            sb.Append(ReadTemplateFromResource());

            string friendlyTypeName = GetFriendlyGenericTypeName(objType);
            sb.Replace("___TYPE_NAME___", friendlyTypeName);

            string serializerTypeName = $"Serializer_{friendlyTypeName.Replace(" ", string.Empty).Replace(',', '_').Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace("[]", "_1DARR_")}";

            sb.Replace("___SERIALIZER_TYPE_NAME___", serializerTypeName);

            EstimateInstanceSize(sb, objType);

            SerializerEntry(sb, objType);

            DeSerializerEntry(sb, objType);

           

            Type genType = GenerateType(sb, serializerTypeName, new List<Type>() { objType, typeof(IBisterGenerated) });

            var serializer = (IBisterGenerated)Activator.CreateInstance(genType);
            _typeToSerializer[objType] = serializer;
            return serializer;
        }

        private static void DeSerializerEntry(StringBuilderVerbose sb, Type objType)
        {
            var sbDeserializer = new StringBuilderVerbose();
            
            string indentation = "\t\t\t";

            sbDeserializer.AppendLine(indentation + $"{Bister.GetFriendlyGenericTypeName(objType)} instance;");

            BisterDeserializer.DeserializeAnyType(sbDeserializer, indentation, "instance", objType);

            sbDeserializer.AppendLine(indentation + "return instance;");
            sb.Replace("___DESERIALIZER_BODY___", sbDeserializer.ToString());
        }


        public static string BinaryWriterMethod(TypeCode typeCode,string instanceName)
        {
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    return $"bw.Write((bool){instanceName})";
                case TypeCode.Char:
                    return $"bw.Write((char){instanceName})";
                case TypeCode.Byte:
                    return $"bw.Write((byte){instanceName})";
                case TypeCode.SByte:
                    return $"bw.Write((sbyte){instanceName})";
                case TypeCode.Int16:
                    return $"bw.Write((short){instanceName})";
                case TypeCode.UInt16:
                    return $"bw.Write((ushort){instanceName})";
                case TypeCode.Int32:
                    return $"bw.Write((int){instanceName})";
                case TypeCode.UInt32:
                    return $"bw.Write((uint){instanceName})";
                case TypeCode.Int64:
                    return $"bw.Write((long){instanceName})";
                case TypeCode.UInt64:
                    return $"bw.Write((ulong){instanceName})";
                case TypeCode.Single:
                    return $"bw.Write((float){instanceName})";
                case TypeCode.Double:
                    return $"bw.Write((double){instanceName})";
                case TypeCode.Decimal:
                    return $"bw.Write((decimal){instanceName})";
                case TypeCode.String:
                    return $"bw.Write((string){instanceName})";
                case TypeCode.DateTime:
                    return $"bw.Write({instanceName}.Ticks == 0 ? 0 : (long){instanceName}.ToFileTime())";
                default:
                    throw new NotImplementedException();
            }
        }

        public static string GetUsefulName(string instanceName)
        {
            return instanceName.Replace(".", "");
        } 

     
        public static string BinaryReaderMethod(TypeCode typeCode)
        {
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    return "ReadBoolean()";
                case TypeCode.Char:
                    return "ReadChar()";
                case TypeCode.Byte:
                    return "ReadByte()";
                case TypeCode.SByte:
                    return "ReadSByte()";
                case TypeCode.Int16:
                    return "ReadInt16()";
                case TypeCode.UInt16:
                    return "ReadUInt16()";
                case TypeCode.Int32:
                    return "ReadInt32()";
                case TypeCode.UInt32:
                    return "ReadUInt32()";
                case TypeCode.Int64:
                    return "ReadInt64()";
                case TypeCode.UInt64:
                    return "ReadUInt64()";
                case TypeCode.Single:
                    return "ReadSingle()";
                case TypeCode.Double:
                    return "ReadDouble()";
                case TypeCode.Decimal:
                    return "ReadDecimal()";
                case TypeCode.String:
                    return "ReadString()";
                case TypeCode.DateTime:
                    return "ReadInt64()";
                default:
                    throw new NotImplementedException();
            }
        }

        private static void SerializerEntry(StringBuilderVerbose sb, Type objType)
        {
            var sbSerializerBody = new StringBuilderVerbose(Bister.Instance.IsDebug);

            string indentation = "\t\t\t";

            BisterSerializer.SerializeAnyType(sbSerializerBody, indentation, "instance", objType);

            sb.Replace("___SERIALIZER_BODY___", sbSerializerBody.ToString());

            
            
        }

        /// <summary>
        /// This requires major overhead to work in recursive manner
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="objType"></param>
        private static void EstimateInstanceSize(StringBuilderVerbose sb,Type objType)
        {
            var sbSizeOfObject = new StringBuilderVerbose();
            sbSizeOfObject.Append($"0");

            if (typeof(Exception).IsAssignableFrom(objType))
            {
                sbSizeOfObject.Append($"+instance.Message.Length+(instance.StackTrace == null? 0 : instance.StackTrace.Length)+(instance.Source == null? 0 : instance.Source.Length)+8");
            }
            else if (typeof(IDictionary).IsAssignableFrom(objType))
            {
                sbSizeOfObject.Append($"+instance.Count * 32");
            }
            else
            {
                IEnumerable<PropertyInfo> props = GetRelevantProperties(objType);

                foreach (var prop in props)
                {

                    if (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(Decimal))
                    {
                        sbSizeOfObject.Append($"+{Marshal.SizeOf(prop.PropertyType)}");
                    }
                    else if (prop.PropertyType.IsEnum)
                    {
                        sbSizeOfObject.Append($"+8");
                    }
                    else if (prop.PropertyType == typeof(string))
                    {
                        sbSizeOfObject.Append($"+instance.{prop.Name}.Length");
                    }
                    else if (prop.PropertyType == typeof(DateTime))
                    {
                        sbSizeOfObject.Append($"+8");
                    }
                    else if (prop.PropertyType == typeof(TimeSpan))
                    {
                        sbSizeOfObject.Append($"+8");
                    }
                    else if (prop.PropertyType.IsArray)
                    {
                        Type arrayItemType = prop.PropertyType.GetElementType();
                        if (arrayItemType.IsPrimitive || arrayItemType == typeof(Decimal))
                        {
                            sbSizeOfObject.Append($"+instance.{prop.Name}.Length * {Marshal.SizeOf(arrayItemType)}");
                        }
                        else
                        {
                            sbSizeOfObject.Append($"+instance.{prop.Name}.Length * 8");
                        }
                    }
                    else if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType))
                    {
                        int size = prop.PropertyType.GenericTypeArguments.Sum(t => t.IsPrimitive ? Marshal.SizeOf(t) : 8);
                        sbSizeOfObject.Append($"+{size}");
                    }
                    else if (prop.PropertyType == typeof(object))
                    {
                        sbSizeOfObject.Append($"+8");
                    }
                    else if (typeof(Exception).IsAssignableFrom(prop.PropertyType))
                    {
                        sbSizeOfObject.Append($"+8+instance.Message.Length+instance.StackTrace.Length");
                    }
                    else
                    {
                        sbSizeOfObject.Append($"+8"); // Unknown...
                    }
                    // Estimate 8 bytes per property...simple huristic

                }
            }
            

            sb.Replace("___BINARY_WRITER_BUFFER_SIZE___", $"(int)Math.Ceiling(1.5f * ({sbSizeOfObject.ToString()}))");
        }

        

        /// <summary>
        /// Returns list of properties which have a public getter and setter
        /// </summary>
        /// <param name="objType"></param>
        /// <returns></returns>
        public static IEnumerable<PropertyInfo> GetRelevantProperties(Type objType)
        {
            foreach (var prop in objType.GetProperties())
            {
                if (!prop.CanRead || !prop.CanWrite)
                    continue;

                if (!prop.GetMethod.IsDefined(typeof(CompilerGeneratedAttribute), false))
                    continue;

                if (!prop.SetMethod.IsDefined(typeof(CompilerGeneratedAttribute), false))
                    continue;

                if (prop.PropertyType.FullName.StartsWith("System.Action") || prop.PropertyType.FullName.StartsWith("System.Func"))
                    continue;

                if (prop.Name == "Item" && prop.GetIndexParameters().Length > 0)
                    continue;

                yield return prop;
            }
        }

        public static bool IsImplementingIEnumerable(Type objType)
        {
            return typeof(IEnumerable).IsAssignableFrom(objType);
        }

        public static bool IsPrimitive(Type type)
        {
            return type.IsPrimitive || type == typeof(decimal);
        }

        public static List<Assembly> GetDependentAssemblies(Type type)
        {
            var assemblies = new List<Assembly>();
            var visitedAssemblies = new HashSet<Assembly>();

            void AddAssembly(Assembly assembly)
            {
                if (assembly != null && visitedAssemblies.Add(assembly))
                {
                    assemblies.Add(assembly);
                    foreach (var referencedAssembly in assembly.GetReferencedAssemblies())
                    {
                        AddAssembly(Assembly.Load(referencedAssembly));
                    }
                }
            }

            AddAssembly(type.Assembly);
            return assemblies;
        }

        public static List<Type> GetDependentTypes(Type type)
        {
            var dependentTypes = new HashSet<Type>();
            var visitedTypes = new HashSet<Type>();

            void AddType(Type t)
            {
                if (visitedTypes.Contains(t))
                    return;

                visitedTypes.Add(t);

                if (t.IsGenericType)
                {
                    foreach (var gt in t.GetGenericArguments())
                    {
                        AddType(gt);
                    }
                }

                if (string.IsNullOrEmpty(t.FullName) || t.FullName.StartsWith("System"))
                    return;

                if (t != type)
                {
                    dependentTypes.Add(t);
                }


                foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    AddType(field.FieldType);
                }
                foreach (var property in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    AddType(property.PropertyType);
                }
                foreach (var method in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    AddType(method.ReturnType);
                    foreach (var parameter in method.GetParameters())
                    {
                        AddType(parameter.ParameterType);
                    }
                }
            }

            AddType(type);
            return dependentTypes.ToList();
        }

        private Type GenerateType(StringBuilderVerbose sb, string expectedTypeName, List<Type> domainDependencies)
        {
            var objType = domainDependencies.First();
            

            if (domainDependencies == null)
            {
                domainDependencies = new List<Type>();
            }

            var subDependencies = GetDependentTypes(objType)
                .Distinct();
            
            domainDependencies.AddRange(subDependencies);

            // If the user is trying to generate serializer for generic type, no need to have any "using"
            if (objType.Namespace.StartsWith("System"))
            {
                StringBuilder sbUsings = new StringBuilder();
                sbUsings.AppendLine($"using {typeof(IBisterGenerated).Namespace};");
                sb.Replace("<<<USINGS>>>", sbUsings.ToString());
            }
            else
            {
                StringBuilder sbUsings = new StringBuilder();
                sbUsings.AppendLine($"using {typeof(IBisterGenerated).Namespace};");
                sbUsings.AppendLine($"using {objType.Namespace};");
                foreach (var ns in subDependencies.Select(t => t.Namespace).Distinct())
                {
                    if (ns != objType.Namespace)
                    {
                        sbUsings.AppendLine($"using {ns};");
                    }
                    
                }
                sb.Replace("<<<USINGS>>>", sbUsings.ToString());
            }

            if (IsDebug)
            {
                File.WriteAllText($@"c:\temp\bister.cs", sb.ToString()); // always dump the latest under the same file name. It's useful for debugging.
                string friendlyFilename = expectedTypeName.Replace("<", "[").Replace(">", "]");
                File.WriteAllText($@"c:\temp\{friendlyFilename}.cs", sb.ToString());
            }

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sb.ToString());

            var dependencyFiles = domainDependencies
                .Select(t => MetadataReference.CreateFromFile(t.Assembly.Location))
                .Distinct()
                .ToList();

            // Compile the syntax tree into an assembly
            CSharpCompilation compilation = CSharpCompilation.Create(
                $"{expectedTypeName}.asm",
                new[] { syntaxTree },
                new[] {
                    MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51").Location),
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(Path.GetDirectoryName(typeof(object).Assembly.Location) + @"\System.Runtime.dll"),
                    MetadataReference.CreateFromFile(Path.GetDirectoryName(typeof(object).Assembly.Location) + @"\System.Collections.dll"),
                    MetadataReference.CreateFromFile(typeof(Enum).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(ICollection<>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Collections.Hashtable).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Collection<>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(IEnumerable<>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(IDictionary<,>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Dictionary<,>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(MemoryStream).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(BinaryReader).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(ReadOnlySpan<>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(BinaryWriter).Assembly.Location)
                    //Marshal
                },
                new CSharpCompilationOptions(
                    outputKind: OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: Bister.Instance.IsDebug ? OptimizationLevel.Release : OptimizationLevel.Debug,
                    platform: Platform.X64
                ))
                .AddReferences(dependencyFiles);

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    StringBuilder sbErrors = new StringBuilder();
                    foreach (var error in result.Diagnostics)
                    {
                        sbErrors.AppendLine($"{error}");
                    }
                    File.WriteAllText(@"c:\temp\bister.cs", sb.ToString());
                    File.AppendAllText(@"c:\temp\bister.cs", Environment.NewLine + sbErrors.ToString());

                    throw new Exception($"CODEGEN ERROR {sbErrors}");
                }


                ms.Seek(0, SeekOrigin.Begin);
                Assembly assembly = Assembly.Load(ms.ToArray());

                // Instantiate the class using reflection

                Type myClassType = assembly.GetTypes().First(t => t.Name.Contains(expectedTypeName));

                return myClassType;
            }
        }

        
    }
}
