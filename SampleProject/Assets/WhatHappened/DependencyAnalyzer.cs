// Author: Michael Camara
// Repository: https://github.com/michaeljcamara/WhatHappened

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

namespace WhatHappened {

    public class DependencyAnalyzer {

        // Table containing all types (as CustomTypes) in the assembly, referenced by name
        private static Dictionary<string, CustomType> customTypeLookup;

        public DependencyAnalyzer() {
            customTypeLookup = CreateLookupTable();

            foreach (CustomType customType in customTypeLookup.Values) {
                GatherDependencies(customType);
            }

            GatherTypesInFiles();
        }

        /// <summary>
        /// Create a CustomType for each assembly Type, and return a lookup table for each CustomType referenced by name
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, CustomType> CreateLookupTable() {
            Dictionary<string, CustomType> customTypeLookup = new Dictionary<string, CustomType>();
            Assembly assembly = Assembly.GetExecutingAssembly();
            Type[] types = assembly.GetTypes();

            foreach (Type currentType in types) {
                //Exclude any types in the System or Unity namespaces
                //TODO allow user configuration of which types/namespaces to exclude
                if (currentType.Namespace == null || (currentType.Namespace != null && !currentType.Namespace.Contains("Unity") && !currentType.Namespace.Contains("System") && !currentType.Namespace.Contains("WhatHappened"))) {
                    CustomType customType = new CustomType(currentType);
                    customTypeLookup.Add(customType.simplifiedFullName, customType);
                }
            }

            return customTypeLookup;
        }


        /// <summary>
        /// Find all unique dependencies for a given CustomType, including field, method, nested, inherited, 
        /// and generic types.  Then assign this set of dependencies to the given CustomType.
        /// </summary>
        private void GatherDependencies(CustomType customType) {

            // First find all unique assembly Types that this CustomType depends on
            HashSet<Type> dependencySet = new HashSet<Type>();
            dependencySet.UnionWith(GetFieldDependencies(customType.assemblyType));
            dependencySet.UnionWith(GetMethodDependencies(customType.assemblyType));
            dependencySet.UnionWith(GetNestedDependencies(customType.assemblyType));
            dependencySet.UnionWith(GetInheritedDependencies(customType.assemblyType));

            // Iterate through these Type dependencies, and find the corresponding valid CustomType from the lookup table
            HashSet<CustomType> customDependencySet = new HashSet<CustomType>();
            foreach (Type t in dependencySet) {

                //Ignore dependency with self
                if (t != customType.assemblyType) {
                    string simplifiedName = CustomType.SimplifyTypeFullName(t);

                    //Dont add dependencies that aren't CustomTypes (e.g. those in System or Unity namespaces, as previously filtered)
                    if (customTypeLookup.ContainsKey(simplifiedName)) {
                        customDependencySet.Add(customTypeLookup[simplifiedName]);
                    }
                }
            }

            // Assign this unique set of dependencies to the CustomType for easy access later
            customType.dependencies = customDependencySet;
        }

        /// <summary>
        /// Return the unique set of dependencies gathered from field (i.e. instance/member) variables
        /// of the given assembly Type
        /// </summary>
        private HashSet<Type> GetFieldDependencies(Type type) {

            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static); // Includes all?
            HashSet<Type> types = new HashSet<Type>();

            foreach (FieldInfo field in fields) {
                types.Add(field.FieldType);

                // Add any generic types included in this type
                if (field.FieldType.IsGenericType) {
                    types.UnionWith(GetAllNestedGenerics(field.FieldType));
                }
            }

            return types;
        }

        /// <summary>
        /// Return the unique set of dependencies gathered from each method, including return types, formal
        /// parameters, and local variables contained within the method body of the given assembly Type
        /// </summary>
        private HashSet<Type> GetMethodDependencies(Type type) {

            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly); // ONLY base, target     

            HashSet<Type> types = new HashSet<Type>();

            foreach (MethodInfo method in methods) {

                // Parameter dependencies
                ParameterInfo[] parameters = method.GetParameters();
                foreach (ParameterInfo parameter in parameters) {
                    types.Add(parameter.ParameterType);

                    // Add any generic types in this type
                    if (parameter.ParameterType.IsGenericType) {
                        types.UnionWith(GetAllNestedGenerics(parameter.ParameterType));
                    }
                }

                // Return type dependencies
                if (method.ReturnType != typeof(void)) {
                    types.Add(method.ReturnType);

                    // Add any generic types in this type
                    if (method.ReturnType.IsGenericType) {
                        types.UnionWith(GetAllNestedGenerics(method.ReturnType));
                    }
                }

                // Local dependencies
                IList<LocalVariableInfo> localVars = methods[0].GetMethodBody().LocalVariables;
                foreach (LocalVariableInfo local in localVars) {
                    types.Add(local.LocalType);

                    if (local.LocalType.IsGenericType) {
                        types.UnionWith(GetAllNestedGenerics(local.LocalType));
                    }
                }
            }

            return types;
        }

        /// <summary>
        /// Return the unique set of dependencies gathered from each nested type (e.g. nested classes)
        /// in the given assembly Type
        /// </summary>
        private HashSet<Type> GetNestedDependencies(Type type) {
            Type[] nested = type.GetNestedTypes(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            HashSet<Type> types = new HashSet<Type>();

            foreach (Type t in nested) {
                types.Add(t);
                if (t.IsGenericType) {
                    types.UnionWith(GetAllNestedGenerics(t));
                }
            }

            return types;
        }

        /// <summary>
        /// Return the unique set of dependencies gathered from any inherited interfaces or inherited class
        /// from the given assembly Type
        /// </summary>
        private HashSet<Type> GetInheritedDependencies(Type type) {
            Type[] interfaces = type.GetInterfaces();
            Type baseType = type.BaseType;

            HashSet<Type> types = new HashSet<Type>();
            types.Add(baseType);

            if (baseType.IsGenericType) {
                types.UnionWith(GetAllNestedGenerics(baseType));
            }

            foreach (Type i in interfaces) {
                types.Add(i);
                if (i.IsGenericType) {
                    types.UnionWith(GetAllNestedGenerics(i));
                }
            }

            return types;
        }

        /// <summary>
        /// Return the unique set of dependencies gathered from all generic types in the given
        /// assembly Type.  Uses recursion to further find all nested generic Types beyond the first.
        /// </summary>
        private HashSet<Type> GetAllNestedGenerics(Type type) {
            HashSet<Type> types = new HashSet<Type>();

            // Add any generic types in this type, searching recursively for ALL generics in definition
            // E.g. capture class A in:  Dictionary< Dictionary<int, A>, int> dict;
            foreach (Type genType in type.GetGenericArguments()) {
                types.Add(genType);
                types.UnionWith(GetAllNestedGenerics(genType));
            }

            return types;
        }

        /// <summary>
        /// Iterate through each C# file (.cs MIME type), parsing each using the FileParser class to find
        /// all appropriate CustomTypes contained within them.  A CustomFile object is created for each
        /// FileInfo object found in the project directory, which is populated with the list of CustomTypes
        /// found within it.
        /// </summary>
        private void GatherTypesInFiles() {
            DirectoryInfo assetDir = new System.IO.DirectoryInfo(Application.dataPath);
            FileInfo[] csFiles = assetDir.GetFiles("*.cs", System.IO.SearchOption.AllDirectories);
            FileParser parser = new FileParser();
            Regex whatHappenedRegex = new Regex(@"^.*Assets.*WhatHappened.*$", RegexOptions.IgnoreCase);
            foreach (FileInfo f in csFiles) {
                if (whatHappenedRegex.IsMatch(f.FullName)) {
                    continue;
                }

                CustomFile customFile = new CustomFile(f);

                // Parse file text and record all CustomTypes in CustomFile object
                //Note: each CustomType also retains a reference to the CustomFile it resides in
                List<CustomType> typesInFile = parser.MatchTypesInFile(customFile);
                customFile.types = typesInFile;
            }
        }

        public static CustomType GetCustomTypeFromString(string typeName) {
            if (customTypeLookup.ContainsKey(typeName)) {
                return customTypeLookup[typeName];
            }
            else {
                return null;
            }
        }

        public Dictionary<string, CustomType>.ValueCollection GetAllCustomTypes() {
            return customTypeLookup.Values;
        }

        //TODO CONSTRUCTOR DEPS
    }
}