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

namespace BisterLib
{


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
                return serializer.DeserializeObj(blob);
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

        public byte[] Serialize<T>(T obj)
        {
            var serializer = (IBisterGenerated<T>)GenerateSerializationEngine(typeof(T));
            return serializer.Serialize(obj);
        }

        public byte[] Serialize(object obj)
        {
            var serializer = GenerateSerializationEngine(obj.GetType());
            return serializer.SerializeObj(obj);
        }

        #endregion

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
                sb.AppendLine(indentation + $"// Method : {callingMethod.DeclaringType.Name}.{callingMethod.Name}({objType.FullName})");
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

            string serializerTypeName = $"Serializer_{friendlyTypeName.Replace('.', '_').Replace('<', '_').Replace('>', '_')}";

            sb.Replace("___SERIALIZER_TYPE_NAME___", serializerTypeName);

            // If the user is trying to generate serializer for generic type, no need to have any "using"
            if (objType.Namespace.StartsWith("System"))
            {
                sb.Replace("<<<USINGS>>>", string.Empty);
            }
            else
            {
                sb.Replace("<<<USINGS>>>", $"using {objType.Namespace};");
            }

            EstimateInstanceSize(sb, GetRelevantProperties(objType));

            SerializerEntry(sb, objType);

            DeSerializerEntry(sb, objType);

            if (!string.IsNullOrEmpty(DebugPath))
            {
                File.WriteAllText(@"c:\temp\serialize.cs", sb.ToString());
            }

            Type genType = GenerateType(sb.ToString(), serializerTypeName, new List<Type>() { objType, typeof(IBisterGenerated) });

            var serializer = (IBisterGenerated)Activator.CreateInstance(genType);
            _typeToSerializer[objType] = serializer;
            return serializer;
        }

        private static void DeSerializerEntry(StringBuilderVerbose sb, Type objType)
        {
            var sbSerializer = new StringBuilderVerbose();
            string friendlyTypeName = Bister.GetFriendlyGenericTypeName(objType);
            string indentation = "\t\t\t";
            sbSerializer.AppendLine(indentation + $"{friendlyTypeName} instance = new {friendlyTypeName}();");
            
            //if (objType.IsGenericType)
            //{
            //    BisterDeserializer.DeSerializeGeneric(indentation, sbSerializer, objType, "instance");
            //}
            //else
            //{
            //    var props = objType.GetProperties();
            //    foreach (var prop in props)
            //    {
            //        var propAccessors = prop.GetAccessors();
            //        // only deal with properties that have get/set accessors
            //        if (propAccessors.Length > 0 && propAccessors.All(acc => acc.IsPublic))
            //        {
            //            if (prop.PropertyType.IsGenericType)
            //            {
            //                BisterDeserializer.DeSerializeGeneric(indentation, sbSerializer, prop.PropertyType, $"instance.{prop.Name}");
            //            }
            //            else
            //            {
            //                BisterDeserializer.PropertyDeserializer(indentation, prop, sbSerializer);
            //            }
            //        }
            //    }
            //}
            sbSerializer.AppendLine(indentation + "return instance;");
            sb.Replace("___DESERIALIZER_BODY___", sbSerializer.ToString());
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

            sbSerializerBody.AppendLine(indentation + "bw.Flush();");
            sbSerializerBody.AppendLine(indentation + "return ms.ToArray();");

            sb.Replace("___SERIALIZER_BODY___", sbSerializerBody.ToString());

            
            
        }

        private static void EstimateInstanceSize(StringBuilderVerbose sb, IEnumerable<PropertyInfo> props)
        {
            var sbSizeOfObject = new StringBuilderVerbose();
            sbSizeOfObject.Append($"0");
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
                else
                {
                    throw new NotImplementedException();
                }
                // Estimate 8 bytes per property...simple huristic

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
            return type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime);
        }

        private static Type GenerateType(string theCode, string expectedTypeName,List<Type> domainDependencies)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(theCode);
            
            if (domainDependencies==null)
            {
                domainDependencies = new List<Type>();
            }

            var dependencyFiles = domainDependencies.Select(t => MetadataReference.CreateFromFile(t.Assembly.Location)).ToList();

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
                    throw new Exception("CODEGEN ERROR:\n" + result.Diagnostics.First().ToString());
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
