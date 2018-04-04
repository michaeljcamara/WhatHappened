using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace WhatHappened {
    public class CustomMethod {

        public int startLineNum, endLineNum;
        public int additions, deletions;
        public int totalChanges { get { return additions + deletions; } }
        public bool hasChanegd { get { return (totalChanges > 0); } }

        public MethodInfo info;

        public CustomMethod(MethodInfo info) {
            this.info = info;
            _regex = CreateMethodRegex();
        }

        private Regex _regex;
        public Regex regex { get { return _regex; } }
        private string methodSignature;

        public void ClearPreviousChanges() {
            additions = deletions = 0;
        }

        private Regex CreateMethodRegex() {

            System.Text.StringBuilder paramBuilder = new System.Text.StringBuilder("");
            foreach (ParameterInfo param in info.GetParameters()) {
                string simplifiedType = SimplifyTypeName(param.ParameterType);
                paramBuilder.Append(simplifiedType + @"\s*?" + param.Name + @"(\s*?,\s*?)??");
            }
            string simplifiedReturnType = SimplifyTypeName(info.ReturnType);
            string typeSpecifier = (info.DeclaringType.IsInterface) ? "interface" : "class";
            string methodPattern = @"(?<=\b" + typeSpecifier + @"\s+?" + info.DeclaringType.Name + @"\b) (?:.*?)(?<methodSig>" + simplifiedReturnType + @"\s+?" + info.Name + @"\s*?\(" + paramBuilder.ToString() + @"\s*?\))(?=\s*?{)";

            //// using .*? to allow for namespace specifiers, eg Michael.Classb vs ClassB. ALSO for generics, List<TypeHere> name

            //Debug.Log("Method: " + info.Name);
            //Debug.Log("  Full Method Pattern: " + methodPattern);
            Regex methodRegex = new Regex(methodPattern, RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

            return methodRegex;
        }

        private static Regex arrayRegex = new Regex(@"\[\]");
        /// <summary>
        /// Take a Type and return its name as it would be appear in source code, notably for primitive types.  E.g. change "Int32" type from System.Int32 to "int", as it would commonly be written in code.
        /// List of built-in types taken from: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/built-in-types-table
        /// </summary>
        private string SimplifyTypeName(Type primitive) {

            string startName = primitive.Name;
            string endName = "";

            if (primitive.IsArray || primitive.IsGenericType || primitive.IsByRef) {
                string splitNameFromContainer = @"(?<name>[^\[`]+)(?<ending>[\[`\&].*?$)";
                Match nameMatch = Regex.Match(primitive.Name, splitNameFromContainer);

                startName = nameMatch.Groups["name"].Captures[0].Value;
                endName = nameMatch.Groups["ending"].Captures[0].Value;
                endName = endName.Replace("[", @"\[");
                endName = endName.Replace("]", @"\]");
            }

            //Get alpha-numeric-Underscore NAME first, e.g List not List`1, Int32 not Int32[][][] or Int32[,,]
            switch (startName) {
                case "Byte":
                case "SByte":
                case "Char":
                case "Decimal":
                case "Double":
                case "Object":
                case "String":
                case "Void":
                    startName = startName.ToLower();
                    break;

                case "Boolean":
                    startName = "bool";
                    break;
                case "Single":
                    startName = "float";
                    break;
                case "Int32":
                    startName = "int";
                    break;
                case "UInt32":
                    startName = "uint";
                    break;
                case "Int64":
                    startName = "long";
                    break;
                case "UInt64":
                    startName = "ulong";
                    break;
                case "Int16":
                    startName = "short";
                    break;
                case "UInt16":
                    startName = "ushort";
                    break;
                default:
                    //Debug.Log(primitive + " is not a primitive type.");
                    break;
            }

            if (primitive.IsGenericType) {
                startName = SimplifyGenericType(primitive);
            }
            else if (primitive.IsArray) {
                startName += endName;
            }

            //TODO consider returning both "int" and "System.Int32" since technically either could be used

            if (primitive.Namespace == "") {
                return startName;
            }
            else {
                return @"(" + primitive.Namespace + @"\.)??" + startName;
            }

        }

        string SimplifyGenericType(Type generic) {

            string simplifiedName = generic.Name.Substring(0, generic.Name.IndexOf('`')) + @"\s*?\<\s*?";
            Type[] genericTypes = generic.GetGenericArguments();
            for (int i = 0; i < genericTypes.Length; i++) {
                simplifiedName += SimplifyTypeName(genericTypes[i]);
                if (i < genericTypes.Length - 1) {
                    simplifiedName += @"\s*?,\s*?";
                }
                else {
                    simplifiedName += @"\s*?>";
                }
            }

            return simplifiedName;
        }

        public override string ToString() {
            return info.Name;
        }

        public void SetSimplifiedMethodSignature(string sig) {
            methodSignature = Regex.Replace(sig, @"\s+", " "); //TODO assess perf
        }

        public string GetSimplifiedMethodSignature() {
            return methodSignature;
        }
    }
}