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
    public class Bister : IBister
    {
        static Lazy<IBister> _lazy = new Lazy<IBister>(() => new Bister());
        public static IBister Instance => _lazy.Value;

        ConcurrentDictionary<Type, IBisterTypeSerializer> _typeToSerializer = new ConcurrentDictionary<Type, IBisterTypeSerializer>();
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

        public void RegisterSerializer(Type objType, IBisterTypeSerializer serializer)
        {
            _typeToSerializer[objType] = serializer;
        }

        public bool IsKnownType(Type objType)
        {
            return _typeToSerializer.ContainsKey(objType);
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

        public static void IncreaseIndent(ref string indent)
        {
            indent = indent + "\t";
        }

        public static void DecreaseIndent(ref string indent)
        {
            indent = indent.Substring(0,indent.Length-1);
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
                sb.AppendLine(indentation + $"// Method : {callingMethod.DeclaringType.Name}.{callingMethod.Name}({BisterHelpers.GetFriendlyGenericTypeName(objType)})");
            }
        }

        IBisterTypeSerializer GenerateSerializationEngine(Type objType)
        {
            if (_typeToSerializer.TryGetValue(objType, out IBisterTypeSerializer serTmp))
            {
                return serTmp;
            }

            var sb = new StringBuilderVerbose(true);
            sb.Append(ReadTemplateFromResource());

            string friendlyTypeName = BisterHelpers.GetFriendlyGenericTypeName(objType);
            sb.Replace("___TYPE_NAME___", friendlyTypeName);

            string serializerTypeName = $"Serializer_{friendlyTypeName.Replace(" ", string.Empty).Replace(',', '_').Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace("[]", "_1DARR_")}";

            sb.Replace("___SERIALIZER_TYPE_NAME___", serializerTypeName);

            EstimateInstanceSize(sb, objType);

            SerializerEntry(sb, objType);

            DeSerializerEntry(sb, objType);

           

            Type genType = GenerateSerializerForType(sb, serializerTypeName, objType);

            var serializer = (IBisterTypeSerializer)Activator.CreateInstance(genType);
            _typeToSerializer[objType] = serializer;
            return serializer;
        }

        private static void DeSerializerEntry(StringBuilderVerbose sb, Type objType)
        {
            var sbDeserializer = new StringBuilderVerbose();
            
            string indentation = "\t\t\t";

            sbDeserializer.AppendLine(indentation + $"{BisterHelpers.GetFriendlyGenericTypeName(objType)} instance;");

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
                    return $"bw.Write({instanceName}.ToBinary())";
                default:
                    throw new NotImplementedException();
            }
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
                IEnumerable<PropertyInfo> props = BisterHelpers.GetRelevantProperties(objType);

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

        private Type GenerateSerializerForType(StringBuilderVerbose sb, string expectedTypeName, Type objType)
        {
            var allAsm = new HashSet<Assembly>(AssemblyEqualityComparer.Instance);
            BisterHelpers.GetAllReferencedAssemblies(objType, allAsm);
            allAsm.Add(typeof(IBister).Assembly); // Include self

            var allDepends = allAsm
                .SelectMany(asm => asm.GetExportedTypes())
                .ToList();


            StringBuilder sbUsings = new StringBuilder();
            foreach (var ns in allDepends.Select(t => t.Namespace).Distinct())
            {
                if (ns!= null)
                {
                    sbUsings.AppendLine($"using {ns};");
                }
                    
            }
            sb.Replace("<<<USINGS>>>", sbUsings.ToString());

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sb.ToString());

            var dependencyFileNames = allAsm
                .Select(asm => asm.Location)
                .Distinct()
                .ToList();

            
            dependencyFileNames.AddRange(BisterHelpers.RunTimeAssemblyFilePath.Value);

            if (BisterHelpers.NetStandardAssemblyFilePath.Value != null)
            {
                dependencyFileNames.Add(BisterHelpers.NetStandardAssemblyFilePath.Value);
            }

            List<PortableExecutableReference> dependencyReferences = dependencyFileNames
                .Distinct()
                .Select(file => MetadataReference.CreateFromFile(file))
                .ToList();

            // Create a compilation object
            CSharpCompilation compilation = CSharpCompilation.Create(
                $"{expectedTypeName}.asm",
                new[] { syntaxTree },
                dependencyReferences,
                new CSharpCompilationOptions(
                    outputKind: OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: Bister.Instance.IsDebug ? OptimizationLevel.Release : OptimizationLevel.Debug,
                    platform: Platform.X64, publicSign: false, reportSuppressedDiagnostics: false
                ));

            using (var ms = new MemoryStream())
            {
                // Compile
                EmitResult result = compilation.Emit(ms);

                if (result.Success)
                {
                    if (IsDebug)
                    {
                        if (!Directory.Exists(DebugPath))
                        {
                            Directory.CreateDirectory(DebugPath);
                        }
                        File.WriteAllText(Path.Combine(DebugPath,"bister.cs"), sb.ToString()); // always dump the latest under the same file name. It's useful for debugging.
                        string friendlyFilename = expectedTypeName.Replace("<", "[").Replace(">", "]");
                        File.WriteAllText(Path.Combine(DebugPath, $"{friendlyFilename}.cs"), sb.ToString());
                    }
                }
                else
                {
                    StringBuilder sbErrors = new StringBuilder();
                    foreach (var error in result.Diagnostics)
                    {
                        sbErrors.AppendLine($"{error}");
                    }
                    File.WriteAllText(@"c:\temp\bister.cs", sb.ToString());
                    File.AppendAllText(@"c:\temp\bister.cs", "\n********* COMPILATION ERRORS *******\n" + sbErrors.ToString());

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
