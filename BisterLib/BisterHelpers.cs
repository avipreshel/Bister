using Microsoft.CodeAnalysis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace BisterLib
{
    internal static class BisterHelpers
    {
        public static Lazy<List<string>> RunTimeAssemblyFilePath = new Lazy<List<string>>(() =>
        {
            var loadedModules = Process.GetCurrentProcess().Modules.Cast<ProcessModule>().ToList();
            ProcessModule mscorelib = loadedModules.FirstOrDefault(asm => asm.ModuleName == "mscorlib.dll");
            ProcessModule dotnetCore = loadedModules.FirstOrDefault(asm => asm.ModuleName == "System.Runtime.dll");
            if (mscorelib == null)
            {
                string coreLib = loadedModules.FirstOrDefault(asm => asm.ModuleName == "System.Private.CoreLib.dll").FileName;
                return new List<string>() { dotnetCore.FileName, coreLib };
            }
            else
            {
                return new List<string>() { mscorelib.FileName };
            }
        });

        public static Lazy<string> NetStandardAssemblyFilePath = new Lazy<string>(() =>
        {
            var loadedModules = Process.GetCurrentProcess().Modules.Cast<ProcessModule>().ToList();
            ProcessModule netStandard = loadedModules.FirstOrDefault(asm => asm.ModuleName == "netstandard.dll");
            return netStandard == null ? null : netStandard.FileName;
        });

        public static bool IsTopLevelInstanceDecleration(string instanceName)
        {
            return !instanceName.Contains(".") && !instanceName.Contains("[");
        }

        public static void GetAllReferencedAssemblies(Type objType, HashSet<Assembly> knownAssemblies)
        {
            knownAssemblies.Add(objType.Assembly);

            if (objType.IsGenericType)
            {
                foreach (var subType in objType.GenericTypeArguments)
                {
                    GetAllReferencedAssemblies(subType, knownAssemblies);
                }
            }

            foreach (var asm in objType.Assembly.GetReferencedAssemblies())
            {
                knownAssemblies.Add(Assembly.Load(asm));
            }

        }

        public static void GetAllDependentTypes(Type type, HashSet<Type> visited)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));


            // Process the type and its dependencies
            ProcessType(type, visited);
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

        public static string GetFriendlyGenericTypeName(Type type)
        {
            if (!type.IsGenericType)
            {
                return type.FullName;
            }

            string typeName = type.FullName;
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

        public static Type GetGenericAncestor(Type objType, Type genericTypeLookup)
        {
            Type currType = objType;
            do
            {
                if (currType.IsGenericType && currType.GetGenericTypeDefinition() == genericTypeLookup)
                    break;

                currType = currType.BaseType;

            } while (currType != null);
            if (currType == null)
            {
                throw new Exception($"{objType} does not inherit or implement {genericTypeLookup}");
            }
            return currType;
        }

        public static Type GetGenericInterface(Type objType, Type genericInterface)
        {
            var iType = objType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterface);
            if (iType == null)
            {
                throw new Exception($"{objType} does not inherit or implement {genericInterface}");
            }
            return iType;
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

                if (prop.DeclaringType.Namespace.StartsWith("System"))
                    continue;

                if (MightContainCircularReference(objType, prop.PropertyType))
                    continue;

                if (prop.PropertyType.FullName.StartsWith("System.Action") || prop.PropertyType.FullName.StartsWith("System.Func"))
                    continue;

                if (prop.Name == "Item" && prop.GetIndexParameters().Length > 0)
                    continue;

                yield return prop;
            }
        }

        public static string GetUsefulName(string instanceName)
        {
            return instanceName.Replace(".", "");
        }

        public static bool IsImplementingIEnumerable(Type objType)
        {
            return typeof(IEnumerable).IsAssignableFrom(objType);
        }

        public static bool IsPrimitive(Type type)
        {
            return type.IsPrimitive || type == typeof(decimal);
        }

        public static bool MightContainCircularReference(Type classType, Type propertyType)
        {
            if (classType == null || propertyType == null)
                throw new ArgumentNullException("classType or propertyType cannot be null.");

            if (propertyType.Namespace.StartsWith("System"))
                return false;

            // Case 1: Property is of the same type as the class
            if (propertyType == classType)
                return true;

            // Handle generic types (e.g., List<T>, IEnumerable<T>)
            Type propertyBaseType = propertyType;
            if (propertyType.IsGenericType)
            {
                // Get the generic type argument (e.g., T in List<T>)
                propertyBaseType = propertyType.GetGenericArguments()[0];
                if (propertyBaseType == classType)
                    return true;
            }

            // Case 2: Property type is derived from a shared base class
            // Check if property type is a subclass of classType or vice versa
            if (propertyBaseType.IsAssignableFrom(classType) || classType.IsAssignableFrom(propertyBaseType))
                return true;

            // Case 3: Check for shared interfaces
            var classInterfaces = classType.GetInterfaces();
            var propertyInterfaces = propertyBaseType.GetInterfaces();
            foreach (var classInterface in classInterfaces)
            {
                foreach (var propertyInterface in propertyInterfaces)
                {
                    if (classInterface == propertyInterface)
                        return true;
                }
            }

            // Case 4: Check for shared base class (other than object)
            Type currentClassBase = classType.BaseType;
            Type currentPropBase = propertyBaseType.BaseType;
            while (currentClassBase != null && currentPropBase != null)
            {
                if (currentClassBase == currentPropBase && currentClassBase != typeof(object))
                    return true;
                currentClassBase = currentClassBase.BaseType;
                currentPropBase = currentPropBase.BaseType;
            }

            return false;
        }


        public static bool TestGenericType(Type objType, Type genericType)
        {
            return genericType.IsAssignableFrom(objType) || objType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericType);
        }

        private static void ProcessType(Type type, HashSet<Type> visited)
        {
            // Stop if type is null, void, or already processed
            if (type == null || type == typeof(void))
                return;

            if (visited.Contains(type))
                return;

            visited.Add(type);

            // Process base type (stop at object)
            if (type.BaseType != null && type.BaseType != typeof(object))
                ProcessType(type.BaseType, visited);

            // Process implemented interfaces
            foreach (var interfaceType in type.GetInterfaces())
            {
                ProcessType(interfaceType, visited);
            }

            // Process public property types
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                ProcessType(property.PropertyType, visited);
            }

            // Handle array types
            if (type.IsArray)
            {
                ProcessType(type.GetElementType(), visited);
            }

            // Handle generic types
            if (type.IsGenericType)
            {

                // Process generic type definition (e.g., List<>)
                ProcessType(type.GetGenericTypeDefinition(), visited);
                // Process generic arguments (e.g., T in List<T>)
                foreach (var genericArg in type.GetGenericArguments())
                {
                    ProcessType(genericArg, visited);
                }
            }

            // Handle pointer or byref types
            if (type.IsByRef || type.IsPointer)
            {
                ProcessType(type.GetElementType(), visited);
            }
        }
    }

    public class AssemblyEqualityComparer : IEqualityComparer<Assembly>
    {
        public static readonly AssemblyEqualityComparer Instance = new AssemblyEqualityComparer();

        public bool Equals(Assembly x, Assembly y)
        {
            return x?.FullName == y?.FullName;
        }

        public int GetHashCode(Assembly obj)
        {
            return obj?.FullName.GetHashCode() ?? 0;
        }
    }
}