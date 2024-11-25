using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Microsoft.VisualBasic;
using System.IO;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.ComponentModel.Design;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using GeneratedNS;

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


        static Lazy<IBister> _lazy = new Lazy<IBister>(()=> new Bister());
        public static IBister Instance => _lazy.Value;

        Dictionary<Type, IBisterGenerated> _typeToSerializer = new Dictionary<Type, IBisterGenerated>();

        /// <summary>
        /// If not empty, will dump the latest serialization class into the path, e.g. c:\temp\gen.cs
        /// </summary>
        public string DebugPath { get; set; } = string.Empty;

        public bool IsDebug => string.IsNullOrEmpty(DebugPath);

        private Bister() { }

        Type ReadTypeFromBlob(byte[] buffer)
        {
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    string typeName = reader.ReadString();
                    return Type.GetType(typeName);
                }
            }
        }

        #region IBister

        public object Deserialize(byte[] blob, Type objType)
        {
            try
            {
                IBisterGenerated serializer = GenerateSerializationEngine(objType);

                using (MemoryStream stream = new MemoryStream(blob))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        return serializer.Deserialize(reader);
                    }
                }
            }
            catch (EndOfStreamException ex)
            {
                throw new Exception($"Failed to deserialize. Buffer contains data which does not match the expected type {objType}", ex);
            }
        }

        public T Deserialize<T>(byte[] blob)
        {
            return (T)Deserialize(blob, typeof(T));
        }

        public byte[] Serialize<T>(T instance)
        {
            var serializer = GenerateSerializationEngine(typeof(T));

            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    serializer.Serialize(instance, bw);
                    bw.Flush();
                    return ms.ToArray();
                }
            }
        }

        public byte[] Serialize(object instance)
        {
            if (instance == null)
            {
                throw new Exception("instance is null. Bister cannot serialize anonymous null object as the concrete type is unknown");
            }
            else
            {
                var serializer = GenerateSerializationEngine(instance.GetType());

                using (var ms = new MemoryStream())
                {
                    using (var bw = new BinaryWriter(ms))
                    {
                        serializer.Serialize(instance, bw);
                        bw.Flush();
                        return ms.ToArray();
                    }
                }
            }
        }

        public void Serialize(object instance, BinaryWriter bw)
        {
            if (instance == null)
            {
                throw new Exception("instance is null. Bister cannot serialize anonymous null object as the concrete type is unknown");
            }
            else
            {
                var serializer = GenerateSerializationEngine(instance.GetType());
                serializer.Serialize(instance, bw);
            }
        }


        public object Deserialize(BinaryReader br, Type objType)
        {
            var serializer = GenerateSerializationEngine(objType);
            return serializer.Deserialize(br);
        }

        #endregion

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

            string serializerTypeName = $"Serializer_{friendlyTypeName.Replace(" ", string.Empty).Replace(',', '_').Replace('.', '_').Replace('<', '_').Replace('>', '_')}";

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
                Type keyType = objType.GenericTypeArguments[0];
                Type valType = objType.GenericTypeArguments[1];
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
            return objType.GetProperties().Where(prop => prop.GetAccessors().Length > 0 && prop.GetAccessors().All(acc => acc.IsPublic)).ToList();
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

                if (t.FullName.StartsWith("System"))
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
                sb.Replace("<<<USINGS>>>", string.Empty);
            }
            else
            {
                StringBuilder sbUsings = new StringBuilder();
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

            if (!string.IsNullOrEmpty(DebugPath))
            {
                File.WriteAllText($@"c:\temp\serialize.cs", sb.ToString()); // always dump the latest under the same file name. It's useful for debugging.
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
                    optimizationLevel: string.IsNullOrEmpty(Bister.Instance.DebugPath) ? OptimizationLevel.Release : OptimizationLevel.Debug,
                    platform: Platform.X64
                ))
                .AddReferences(dependencyFiles);

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    throw new Exception($"CODEGEN ERROR for {expectedTypeName}:\n" + result.Diagnostics.First().ToString());
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
