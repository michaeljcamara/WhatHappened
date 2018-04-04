using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Linq;
using System.IO;

namespace WhatHappened {
    public class FileParser {
        private string RemoveCommentsAndStrings(CustomFile file) {

            string text = File.ReadAllText(file.info.FullName);

            string lineCommentPattern = @"(?<=^.*)(//.*?$)";
            //string lineCommentPattern = @"(?<=^.*)(//.*)(?!.*\*/$)"; //TODO Exclude if */ at end
            Regex lineCommentRegex = new Regex(lineCommentPattern, RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

            //string blockCommentPattern = @"/\* #start with /*
            //                    (.*? #follow with any number of chars (NOT \n)
            //                    (?<newline>\n)*) #then any num consecutive \n 
            //                    *? #then repeat finding these occurences within /**/ boundary
            //                    \*/ #end with */
            //                    "; //works, 1000* faster with .*?
            //                       //Consider a way for lookahead, performance saver

            //string blockCommentPattern = @"/\*.*?\*/";
            string blockCommentPattern = @"/\*.*?\*/"; // TODO trying to exclude escaped backslash

            Regex blockCommentRegex = new Regex(blockCommentPattern, RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Singleline);

            //TODO include literal @ strings
            string stringPattern = @"(?<=^.*)(?<!\\)(""[^""]*?"")"; //YES, each match = string, replace w/"". IGNORE ESCAPED quotes
                                                                    //string testStringPattern = @"^((?<start>[^""]*?)(?<stringliteral>""[^""]*?"")(?<end>[^""]*?))*?$";  //Separate captures into named groups, so can access string literals
            Regex stringRegex = new Regex(stringPattern, RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);


            //REPLACE FILE CONTENTS
            text = stringRegex.Replace(text, "");
            text = blockCommentRegex.Replace(text, delegate (Match m) {
                int numNewlines = m.Value.Count(x => x == '\n');
                System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder("");
                for (int i = 0; i < numNewlines; i++) {
                    stringBuilder.Append("\n");
                }
                return stringBuilder.ToString();
            });

            text = lineCommentRegex.Replace(text, "");

            return text;
        }

        public List<CustomType> ExtractTypesFromFile(CustomFile file) {

            List<CustomType> typesInFile = new List<CustomType>();

            //Debug.LogError("**STARTING FILE: " + file.name);

            string text = RemoveCommentsAndStrings(file);

            string typePattern = @"\b(?:class|interface)(?:\s+?)(?<type>[_\w]+?\b)(?=[^{]*?{)"; // WORKS!!
            Regex classRegex = new Regex(typePattern, RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

            int currentLineNum = 1; // +1 since the leading text does not capture the \n from the current line
            int currentIndex = 0; // Index of text string where current match begins
            CustomType previousType = null; // Keep track of the previous match's type

            foreach (Match classMatch in classRegex.Matches(text)) {

                currentLineNum += text.Substring(currentIndex, classMatch.Index - currentIndex).Count(x => x == '\n');
                currentIndex = classMatch.Index;

                int startLineNum = currentLineNum, endLineNum = 0;

                for (int i = classMatch.Index, numOpenBraces = 0, numClosedBraces = 0, numNewLines = 0; i < text.Length; i++) {
                    switch (text[i]) {
                        case '{':
                            numOpenBraces++;
                            break;
                        case '}':
                            numClosedBraces++;
                            break;
                        case '\n':
                            numNewLines++;
                            break;
                    }

                    if (numOpenBraces == numClosedBraces && numOpenBraces != 0) {
                        endLineNum = currentLineNum + numNewLines; // +1 since array is zero-indexed but line numbers are 1-indexed
                        break;
                    }
                }

                // Get the name of the class as it appears in the text
                System.Text.StringBuilder typeString = new System.Text.StringBuilder("");
                typeString.Append(classMatch.Groups["type"].Captures[0].Value);

                // Handle nested types (e.g. private classes nested in other classes, potentially many nested levels)
                //TODO clean this up with newer helper methods for nested classes
                bool foundParent = false;
                do {
                    // No outer type detected, i.e. match is not nested
                    if (previousType == null) {
                        foundParent = true;
                    }
                    //Check if match is nested in previous matched type
                    else if (startLineNum >= previousType.startLineNum && endLineNum <= previousType.endLineNum) {
                        Type parent = previousType.type;
                        while (parent.IsNested) {
                            typeString.Insert(0, parent.Name + "+");
                            parent = parent.DeclaringType;
                        }
                        typeString.Insert(0, parent.Name + "+");
                        foundParent = true;
                    }
                    //Not nested in previous matched type. Check if nested in that type's parent (if it exists)
                    else {
                        if (previousType.type.DeclaringType != null) {
                            previousType = DependencyAnalyzer.GetCustomTypeFromString(SimplifyTypeFullName(previousType.type.DeclaringType));
                        }
                        else {
                            previousType = null;
                        }
                    }
                } while (!foundParent);

                // Get the CustomType associated with the string found in text (modified if type was nested to match desired format of key)
                CustomType currentType = DependencyAnalyzer.GetCustomTypeFromString(typeString.ToString());
                if (currentType == null) {
                    Debug.LogError("Could not find " + typeString + " in customTypeLookup");
                }

                typesInFile.Add(currentType);
                currentType.SetFile(file);

                previousType = currentType;
                currentType.startLineNum = startLineNum;
                currentType.endLineNum = endLineNum;

                //Debug.LogError("Class matched: " + classString + ", StartLineNum = " + currentType.startLineNum + ", EndLineNum = " + currentType.endLineNum);

                int numMatchesFound = 0;

                foreach (CustomMethod method in currentType.GetMethods()) {
                    //Debug.Log("Looking for method: " + method.info);
                    Regex methodRegex = method.regex;
                    //Debug.Log("Regex is: " + methodRegex.ToString());

                    Match methodMatch = methodRegex.Match(text);
                    if (methodMatch.Success) {
                        //Debug.Log("Found match: " + methodMatch);
                        //Debug.LogWarning("    Method Signature: " + methodMatch.Groups["methodSig"].Captures[0].Value);
                        numMatchesFound++;
                    }
                    else {
                        Debug.LogError("!!DID NOT FIND: " + method.info.Name);
                    }

                    method.SetSimplifiedMethodSignature(methodMatch.Groups["methodSig"].Value);

                    currentLineNum = currentType.startLineNum + text.Substring(classMatch.Index, methodMatch.Groups["methodSig"].Index - classMatch.Index).Count(x => x == '\n');

                    method.startLineNum = currentLineNum;

                    for (int i = methodMatch.Groups["methodSig"].Index, numOpenBraces = 0, numClosedBraces = 0, numNewLines = 0; i < text.Length; i++) {
                        switch (text[i]) {
                            case '{':
                                numOpenBraces++;
                                break;
                            case '}':
                                numClosedBraces++;
                                break;
                            case '\n':
                                numNewLines++;
                                break;
                        }

                        if (numOpenBraces == numClosedBraces && numOpenBraces != 0) {
                            method.endLineNum = currentLineNum + numNewLines;
                            break;
                        }
                    } // end method brace matching

                    //RENABLE
                    //if (method.endLineNum == 0) {
                    //    Debug.LogError("Could not find end line number for " + method.info.Name);
                    //}
                    //else {
                    //    //Debug.LogError("Start/End Line Nums for " + method.info.Name + ": " + method.startLineNum + ", " + method.endLineNum);
                    //    Debug.LogError("    Method matched: " + method.info.Name + ", StartLineNum = " + method.startLineNum + ", EndLineNum = " + method.endLineNum);
                    //}


                } // end CustomMethod iteration


                //REENABLE
                //if (numMatchesFound == currentType.methods.Count) {
                //    Debug.LogError("**FOUND ALL METHODS IN " + currentType + ": " + numMatchesFound + " out of " + currentType.methods.Count);
                //}
                //else {
                //    Debug.LogError("!!DID NOT FIND ALL METHODS IN " + currentType + ": " + numMatchesFound + " out of " + currentType.methods.Count);
                //}

                // Reset linenum/index back to the currentType before moving onto next type
                currentLineNum = currentType.startLineNum;
                currentIndex = classMatch.Index;
            }

            return typesInFile;
        }

        private string SimplifyTypeFullName(Type t) {
            return t.FullName.Replace(t.Namespace + ".", "");
        }
    }
}