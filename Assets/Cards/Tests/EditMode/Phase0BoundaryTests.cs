using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace Cards.Tests.EditMode
{
    public class Phase0BoundaryTests
    {
        private static readonly ForbiddenPattern[] RuntimeForbiddenPatterns =
        {
            new ForbiddenPattern(@"\bUnityEngine\b", "UnityEngine"),
            new ForbiddenPattern(@"\bMonoBehaviour\b", "MonoBehaviour"),
            new ForbiddenPattern(@"\bScriptableObject\b", "ScriptableObject"),
            new ForbiddenPattern(@"\bDebug\s*\.\s*Log(?:Warning|Error)?\b", "Debug.Log"),
            new ForbiddenPattern(@"\bWaitForSeconds\b", "WaitForSeconds"),
            new ForbiddenPattern(@"\bTransform\b", "Transform"),
            new ForbiddenPattern(@"\bMaterial\b", "Material")
        };

        [Test]
        public void CardsRuntimeAsmdef_EnablesNoEngineReferences()
        {
            string asmdefPath = GetProjectPath("Assets/Cards/Runtime/Cards.Runtime.asmdef");

            Assert.That(File.Exists(asmdefPath), Is.True, $"Missing asmdef: {asmdefPath}");

            string content = File.ReadAllText(asmdefPath);
            StringAssert.Contains("\"noEngineReferences\": true", content);
        }

        [Test]
        public void RuntimeSources_DoNotUseForbiddenUnityDependencies()
        {
            string runtimeRoot = GetProjectPath("Assets/Cards/Runtime");
            string projectRoot = GetProjectRoot();
            string[] files = Directory.GetFiles(runtimeRoot, "*.cs", SearchOption.AllDirectories);
            var violations = new List<string>();

            foreach (string file in files)
            {
                string sanitizedSource = StripComments(File.ReadAllText(file));
                string relativePath = Path.GetRelativePath(projectRoot, file).Replace("\\", "/");

                foreach (ForbiddenPattern pattern in RuntimeForbiddenPatterns)
                {
                    if (Regex.IsMatch(sanitizedSource, pattern.Regex))
                    {
                        violations.Add($"{relativePath}: {pattern.DisplayName}");
                    }
                }
            }

            Assert.That(violations, Is.Empty,
                "Runtime boundary violation(s):\n" + string.Join("\n", violations));
        }

        private static string GetProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static string GetProjectPath(string relativePath)
        {
            return Path.Combine(GetProjectRoot(), relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        }

        private static string StripComments(string source)
        {
            string withoutBlockComments = Regex.Replace(source, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            return Regex.Replace(withoutBlockComments, @"//.*$", string.Empty, RegexOptions.Multiline);
        }

        private readonly struct ForbiddenPattern
        {
            public ForbiddenPattern(string regex, string displayName)
            {
                Regex = regex;
                DisplayName = displayName;
            }

            public string Regex { get; }
            public string DisplayName { get; }
        }
    }
}
