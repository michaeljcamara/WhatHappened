using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq; //TODO any way to avoid "using system.linq" for just this one extension method? other way to do
using LibGit2Sharp;

namespace WhatHappened {

    public class DependencyAnalyzer {

        //public Dictionary<CustomType, HashSet<CustomType>> dependencyTable;
        public Assembly assembly; // TODO REMOVE THIS EVENTUALLY
        private static Dictionary<string, CustomType> customTypeLookup;

        public static CustomType GetCustomTypeFromString(string typeName) {
            if (customTypeLookup.ContainsKey(typeName)) {
                return customTypeLookup[typeName];
            }
            else {
                return null;
            }
        }

        //public CustomType GetCustomTypeFromString(string typeName) {
        //    if (customTypeLookup.ContainsKey(typeName)) {
        //        return customTypeLookup[typeName];
        //    }
        //    else {
        //        return null;
        //    }
        //}

        public Dictionary<string, CustomType>.ValueCollection GetAllCustomTypes() {
            return customTypeLookup.Values;
        }

        //TODO IMPORTANT: Check if public/private classes WITHIN a class are captured by the GetALlTypes method
        //YES: CurrentType: ClassB+anotherClass1 //TODO need to handle this corrrectly, indicate private class within ClassB
        //TODO: Need to consider datastructures of types, like List<SomeType> or SomeType[]
        //List: System.Collections.Generic.List`1[CustomMethod]
        //TODO consider partial namespace prefixes, e.g. Int32 instead of System.Int32...umm, or need better example
        //TODO If dependency is generic type, get genericParams, add to dependency list
        //TODO need to add nested types to dep list

        HashSet<Type> GetFieldDependencies(Type type) {

            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static); // Includes all?
            HashSet<Type> types = new HashSet<Type>();

            foreach (FieldInfo field in fields) {
                types.Add(field.FieldType);
                //Debug.Log("In GetFieldDeps, cur field = " + field + ", name = " + field.Name + ", fieldType = " + field.FieldType); //In GetFieldDeps, cur field = System.String authorOfChange, fieldType = System.String

                //Debug.LogError("  Field name: " + field.FieldType.Name + ", fullname: " + field.FieldType.FullName + ", isNested(has+): " + field.FieldType.IsNested + ", namespace: " + field.FieldType.Namespace + ", FULLTYPE: " + field.FieldType);

                // Add any generic types included in this type
                if (field.FieldType.IsGenericType) {
                    types.UnionWith(GetAllNestedGenerics(field.FieldType));
                }
            }

            return types;
        }

        HashSet<Type> GetMethodDependencies(Type type) {
            //Consider, adding separate data structure for EACH method, so can 

            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly); // ONLY base, target     

            HashSet<Type> types = new HashSet<Type>();

            foreach (MethodInfo method in methods) {

                // Parameter dependencies
                ParameterInfo[] parameters = method.GetParameters();
                foreach (ParameterInfo parameter in parameters) {
                    //types.Add(parameter.Member.MemberType.GetType()); //TODO CHECK, REPLACED THIS
                    types.Add(parameter.ParameterType); //TODO CHECK, REPLACED THIS

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

        HashSet<Type> GetNestedDependencies(Type type) {
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

        HashSet<Type> GetInheritedDependencies(Type type) {
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

        HashSet<Type> GetAllNestedGenerics(Type type) {
            HashSet<Type> types = new HashSet<Type>();

            // Add any generic types in this type, searching recursively for ALL generics in definition
            // E.g. capture class A in:  Dictionary< Dictionary<int, A>, int> dict;
            foreach (Type genType in type.GetGenericArguments()) {
                types.Add(genType);
                types.UnionWith(GetAllNestedGenerics(genType));
            }

            return types;
        }

        public DependencyAnalyzer() {

            //TODO CONSTRUCTOR DEPS
            assembly = Assembly.GetExecutingAssembly();
            customTypeLookup = new Dictionary<string, CustomType>(); //NEWW

            Type[] types = assembly.GetTypes();

            foreach (Type currentType in types) {
                //if ((currentType.Namespace != null && !currentType.Namespace.Contains("Unity") && !currentType.Namespace.Contains("System")) || (currentType != typeof(CustomType) && currentType != typeof(DependencyAnalyzer))) {
                if (currentType.Namespace == null || (currentType.Namespace != null && !currentType.Namespace.Contains("Unity") && !currentType.Namespace.Contains("System") && !currentType.Namespace.Contains("WhatHappened"))) { 
                    CustomType customType = new CustomType(currentType);
                    customTypeLookup.Add(customType.simplifiedFullName, customType);
                }
            } 

            foreach (CustomType customType in customTypeLookup.Values) {
                HashSet<Type> dependencySet = new HashSet<Type>();

                dependencySet.UnionWith(GetFieldDependencies(customType.type));
                dependencySet.UnionWith(GetMethodDependencies(customType.type));
                dependencySet.UnionWith(GetNestedDependencies(customType.type));
                dependencySet.UnionWith(GetInheritedDependencies(customType.type));

                HashSet<CustomType> customDependencySet = new HashSet<CustomType>();
                foreach (Type t in dependencySet) {

                    //Ignore dependency with self
                    if (t != customType.type) {
                        string simplifiedName = SimplifyTypeFullName(t);

                        //Dont add dependencies that aren't CustomTypes (e.g. those in System or Unity namespaces, as previously filtered)
                        if (customTypeLookup.ContainsKey(simplifiedName)) {
                            customDependencySet.Add(customTypeLookup[simplifiedName]);
                        }

                    }
                }

                customType.SetDependencies(customDependencySet);
            }

            //Debug.LogWarning("Datapath is: " + Application.dataPath);
            DirectoryInfo assetDir = new System.IO.DirectoryInfo(Application.dataPath);
            FileInfo[] csFiles = assetDir.GetFiles("*.cs", System.IO.SearchOption.AllDirectories);

            FileParser parser = new FileParser();
            //Debug.LogWarning("Iterating all .cs files in Assets");
            foreach (FileInfo f in csFiles) {
                if (f.FullName.Contains("WhatHappened")) {
                    //Debug.LogWarning("Skipping File: " + f);
                    continue;
                }
                //Debug.LogWarning("f: " + f + ", name: " + f.Name + ", fullName: " + f.FullName + ", length: " + f.Length);
                //output: f: D:\User\Documents\CMPSC\600\SeniorThesisPrototype\Prototype\Assets\Scripts\ClassC.cs, name: ClassC.cs, fullName: D:\User\Documents\CMPSC\600\SeniorThesisPrototype\Prototype\Assets\Scripts\ClassC.cs, length: 276

                CustomFile customFile = new CustomFile(f);

                List<CustomType> typesInFile = parser.ExtractTypesFromFile(customFile);
                customFile.SetTypesInFile(typesInFile);
            }

        }

        



        void OnEnable() {
            Debug.LogError("ENABLING!!");
        }

        void OnDisable() {
            Debug.LogError("DISABLING!");
        }

        void OnReset() {
            Debug.LogError("ON RESET");
        }

        void Reset() {
            Debug.LogError("Reset");
        }

        private string SimplifyTypeFullName(Type t) {
            return t.FullName.Replace(t.Namespace + ".", "");
        }
    }
}