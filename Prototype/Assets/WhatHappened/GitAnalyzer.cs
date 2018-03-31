using LibGit2Sharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace WhatHappened {
    public class GitAnalyzer {

        private Repository repo;
        private List<Commit> _commitList;
        public List<Commit> commitList { get { return _commitList; } }


        public GitAnalyzer() {
            DirectoryInfo gitDir = new DirectoryInfo(Path.GetFullPath("."));
            bool isFound = false;
            while (!isFound) {
                DirectoryInfo[] dirs = gitDir.GetDirectories(".git");

                if (dirs.Length == 0) {
                    if (gitDir.Parent == null) {
                        Debug.LogError("!!Could not find Git directory working backwards, abort!");
                    }
                    else {
                        gitDir = gitDir.Parent;
                    }
                }
                else {
                    isFound = true;
                    gitDir = dirs[0];
                }
            }
            Debug.Log("This is the git dir: " + gitDir.FullName);
            repo = new Repository(gitDir.FullName);

            _commitList = new List<Commit>();
            foreach (Commit c in repo.Commits) {
                commitList.Add(c);
            }
        }

        public void DiffFile(CustomFile file, int commitIndex) {
            if (file == null) {
                Debug.LogError("TRYING TO DIFf NULL FILE");
                return;
            }
            file.ClearPreviousChanges();

            // Return if user selected "NO SELECTION" index
            if (commitIndex == -1) {
                return;
            }

            LibGit2Sharp.Tree chosenTree2 = commitList[commitIndex].Tree;

            CompareOptions compareOptions = new CompareOptions();
            compareOptions.ContextLines = 0; // if 0, only include modified lines in diff (has + or - at start of line). 3 is default
            compareOptions.IncludeUnmodified = true;
            compareOptions.InterhunkLines = 0;  // if Int.MAX then all changes are in ONE hunk
            ExplicitPathsOptions pathOptions = new ExplicitPathsOptions();

            List<string> chosenFilePaths = new List<string>() { file.relPath };

            Patch patch = repo.Diff.Compare<Patch>(chosenTree2, DiffTargets.WorkingDirectory, new List<string> { file.relPath }, pathOptions, compareOptions);
            PatchEntryChanges changes = patch[file.relPath]; //WORKS if correct relpath
            string patchText = changes.Patch; // If no changes b/w commits, then "", or if notIncludeModified, null changes
            file.SetDiffText(patchText);
            Debug.LogError("***THEPATCH: " + patchText);

            // WORKS  all hunk in one long string
            string diffPattern = @"\@\@\s\-(?:\d+)(?:,\d+)??\s\+(?<start>\d+)(?:,\d+)??\s\@\@
                            (?:[^\n]*?\n)(?<changes>.*?)(?=\n^@ | $\Z)
                            ";
            //TODO figure out way to sep chunks via regex
            //string diffPattern = @"\@\@\s\-(?:\d+)(?:,\d+)??\s\+(?<start>\d+)(?:,\d+)??\s\@\@
            //                    (?:[^\n]*?\n)(?<changes>^[\+]|[\-].*?$)+(?=\n^@ | $\Z)
            //                    ";
            Regex diffRegex = new Regex(diffPattern, RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Multiline);

            foreach (Match m in diffRegex.Matches(patchText)) {
                //Debug.LogError("Found match: " + m);
                //Debug.LogError("   Hunk is:-" + m.Groups["changes"].Captures[0].Value + "-");
                //Debug.LogError("     LineNum is: " + m.Groups["start"].Captures[0].Value);
                string hunk = m.Groups["changes"].Captures[0].Value;
                int lineNum = Int32.Parse(m.Groups["start"].Captures[0].Value);

                System.Text.StringBuilder fileString = new System.Text.StringBuilder("");
                StringReader reader = new StringReader(hunk);
                string line;
                while ((line = reader.ReadLine()) != null) {

                    CustomType typeAtLine = file.GetTypeByLineNumber(lineNum);
                    //Debug.Log(" Type at line: " + typeAtLine);
                    CustomMethod methodAtLine = null;
                    if (typeAtLine != null) {
                        methodAtLine = typeAtLine.GetMethodAtLineNum(lineNum);
                    }

                    switch (line[0]) {
                        case '+':
                            if (typeAtLine != null) {
                                if (methodAtLine != null) {
                                    typeAtLine.additionsInMethods++;
                                    methodAtLine.additions++;
                                }
                                else {
                                    typeAtLine.additionsOutsideMethods++;
                                }
                            }
                            lineNum++;
                            break;
                        case '-':
                            if (typeAtLine != null) {
                                if (methodAtLine != null) {
                                    typeAtLine.deletionsInMethods++;
                                    methodAtLine.deletions++;
                                }
                                else {
                                    typeAtLine.deletionsOutsideMethods++;
                                }
                            }
                            break;
                        default:
                            Debug.LogError("Unknown hunk format for: " + hunk);
                            break;
                    }

                }
            } // End iterate hunk matches


            List<CustomType> typesInFile = file.GetTypesInFile();

            foreach (CustomType t in typesInFile) {
                Debug.LogError("Total changes in " + t + ": " + t + ", inMethods: " + t.totalChangesInMethods + ", outMethods: " + t.totalChangesOutsideMethods);

                foreach (CustomMethod m in t.methods) {
                    Debug.LogWarning("   Total changes in " + m + ": " + (m.additions + m.deletions) + "(Adds: " + m.additions + ", Dels: " + m.deletions + ")");
                }
            }

        }

    }
}