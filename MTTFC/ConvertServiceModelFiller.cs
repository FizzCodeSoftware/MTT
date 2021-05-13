namespace MTTFC
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    public static class ConvertServiceModelFiller
    {
        public static void BreakDown(List<ModelFile> models, string localWorkingDir, bool isModelInTSFileName)
        {
            foreach (var file in models)
            {
                foreach (var _line in file.Info)
                {
                    var line = StripComments(_line);

                    if (IsPreProcessorDirective(line))
                    {
                        continue;
                    }

                    var modLine = new List<string>(ExplodeLine(line));

                    // Check for correct structure
                    if ((StrictContains(line, "enum") && line.Contains("{")) || (StrictContains(line, "class") && line.Contains("{")))
                    {
                        throw new ArgumentException(string.Format("For parsing, C# DTO's must use curly braces on the next line\nin {0}.cs\n\"{1}\"", file.Name, _line));
                    }

                    // Enum declaration
                    if (StrictContains(line, "enum"))
                    {
                        if (modLine.Count > 2)
                        {
                            file.Inherits = modLine[^1];
                        }

                        file.IsEnum = true;

                        int value = 0;

                        foreach (var _enumLine in file.Info)
                        {
                            var enumLine = StripComments(_enumLine);

                            if (IsPreProcessorDirective(enumLine))
                            {
                                continue;
                            }

                            modLine = new List<string>(ExplodeLine(enumLine));

                            if (IsEnumObject(enumLine))
                            {
                                String name = modLine[0];
                                bool isImplicit = false;

                                if (modLine.Count > 1 && modLine[1] == "=")
                                {
                                    try
                                    {
                                        var tmpValue = modLine[2].Replace(",", "");
                                        if (tmpValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                                        {
                                            value = Convert.ToInt32(tmpValue, 16);
                                        }
                                        else
                                        {
                                            value = Int32.Parse(tmpValue);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        throw;
                                    }
                                }
                                else
                                {
                                    isImplicit = true;
                                }

                                var obj = new EnumObject()
                                {
                                    Name = name.Replace(",", ""),
                                    Value = value,
                                    IsImplicit = isImplicit
                                };

                                file.EnumObjects.Add(obj);
                            }
                        }

                        break;  //since enums are different we move onto the next file
                    }

                    // Class declaration
                    if (StrictContains(line, "class") && line.Contains(":"))
                    {
                        string inheritance = modLine[^1];

                        // Ignore interfaces by convention
                        if (!(inheritance.StartsWith("I") && inheritance.Length > 1 && char.IsUpper(inheritance[1])))
                        {
                            var commaIndex = inheritance.IndexOf(',');
                            if (commaIndex > 0)
                            {
                                inheritance = inheritance.Substring(0, commaIndex);
                            }
                            file.Inherits = inheritance;
                            file.InheritenceStructure = Find(models, inheritance, file, localWorkingDir)
                                + (isModelInTSFileName ? ".model" : "");

                            // If the class only contains inheritence we need a place holder obj
                            var obj = new LineObject();
                            file.Objects.Add(obj);
                        }
                    }

                    // Class property
                    if (StrictContains(line, "public") && !StrictContains(line, "class") && !IsContructor(line))
                    {
                        string type = modLine[0];
                        /** If the property is marked virtual, skip the virtual keyword. */
                        if (type.Equals("virtual"))
                        {
                            modLine.RemoveAt(0);
                            type = modLine[0];
                        }

                        bool isArray = CheckIsArray(type);

                        bool isOptional = CheckOptional(type);

                        bool isDic = CheckDictionary(type);

                        if (isDic)
                        {
                            string varName = modLine[1];

                            if (varName.EndsWith(";"))
                            {
                                varName = varName[0..^1];
                            }

                            var obj = new LineObject()
                            {
                                VariableName = varName,
                                Type = "Map",
                                IsArray = false,
                                IsOptional = isOptional,
                                UserDefined = false,
                                UserDefinedImport = "",
                                Container = new LineObject[2]
                            };

                            List<string> types = CleanType(type).Replace("Dictionary", String.Empty).Replace("IDictionary", String.Empty).Split(',').ToList();
                            types.ForEach(x => x.Trim());

                            int index = 0;
                            foreach (string t in types)
                            {
                                string innerType = CleanType(t);

                                var userDefinedImport = Find(models, innerType, file, localWorkingDir);
                                var isUserDefined = !String.IsNullOrEmpty(userDefinedImport);

                                obj.Container[index] = new LineObject()
                                {
                                    VariableName = "",
                                    Type = isUserDefined ? innerType : TypeOf(innerType),
                                    IsArray = false,
                                    IsOptional = false,
                                    UserDefined = isUserDefined,
                                    UserDefinedImport = userDefinedImport + (isModelInTSFileName ? ".model" : "")
                                };
                                index++;
                            }

                            file.Objects.Add(obj);
                        }
                        else
                        {
                            string newType;
                            bool isList = CheckList(type);
                            newType = isList ? type.Replace("List<", "").Replace(">", "") : CleanType(type);

                            var userDefinedImport = Find(models, newType, file, localWorkingDir);
                            var isUserDefined = !string.IsNullOrEmpty(userDefinedImport);

                            string varName = modLine[1];

                            if (varName.EndsWith(";"))
                            {
                                varName = varName[0..^1];
                            }

                            string typeToObject = isUserDefined ? newType : TypeOf(newType);

                            var obj = new LineObject()
                            {
                                VariableName = varName,
                                Type = typeToObject,
                                IsArray = isArray,
                                IsOptional = isOptional,
                                UserDefined = isUserDefined,
                                UserDefinedImport = userDefinedImport + (isModelInTSFileName ? ".model" : "")
                            };

                            file.Objects.Add(obj);
                        }
                    }
                }
            }
        }

        private static string StripComments(string line)
        {
            if (line.Contains("//"))
            {
                line = line.Substring(0, line.IndexOf("//"));
            }

            return line;
        }

        public static bool IsPreProcessorDirective(string str)
        {
            return Regex.IsMatch(str, @"^#\w+");
        }

        private static string TypeOf(string type)
        {
            switch (type)
            {
                case "byte":
                case "sbyte":
                case "decimal":
                case "double":
                case "float":
                case "int":
                case "uint":
                case "long":
                case "ulong":
                case "short":
                case "ushort":
                case "Byte":
                case "Decimal":
                case "Double":
                case "Int16":
                case "Int32":
                case "Int64":
                case "SByte":
                case "UInt16":
                case "UInt32":
                case "UInt64":
                    return "number";

                case "bool":
                case "Boolean":
                    return "boolean";

                case "string":
                case "char":
                case "String":
                case "Char":
                case "Guid":
                    return "string";

                case "DateTime":
                    return "Date";

                default: return "any";
            }
        }

        private static string[] ExplodeLine(string line)
        {
            var regex = new Regex("\\s*,\\s*");

            var l = regex.Replace(line, ",");

            return l
                .Replace("public", String.Empty)
                .Replace("static", String.Empty)
                .Replace("const", String.Empty)
                .Replace("readonly", String.Empty)
                .Trim()
                .Split(' ');
        }

        public static bool StrictContains(string str, string match)
        {
            string reg = "(^|\\s)" + match + "(\\s|$)";
            return Regex.IsMatch(str, reg);
        }

        private static bool IsEnumObject(string line)
        {
            return
                !String.IsNullOrWhiteSpace(line)
                && !StrictContains(line, "enum")
                && !StrictContains(line, "namespace")
                && !StrictContains(line, "using")
                && !IsContructor(line)
                && !line.Contains("{") && !line.Contains("}")
                && !line.Contains("[") && !line.Contains("]");
        }

        private static bool IsContructor(string line)
        {
            return !StrictContains(line, "new") && (line.Contains("()") || (line.Contains("(") && line.Contains(")")));
        }

        private static string Find(List<ModelFile> models, string query, ModelFile file, string localWorkingDir)
        {
            const string userDefinedImport = null;

            foreach (var f in models)
            {
                if (f.Name.Equals(query))
                {
                    var rel = GetRelativePathFromLocalPath(file.Structure, f.Structure, localWorkingDir);

                    return rel + ConvertServiceHelper.ToCamelCase(f.Name);
                }
            }

            return userDefinedImport;
        }

        private static string GetRelativePathFromLocalPath(string from, string to, string localWorkingDir)
        {
            var path1 = Path.Combine(localWorkingDir, from);
            var path2 = Path.Combine(localWorkingDir, to);
            path1 = path1.Replace("/", "\\");
            path2 = path2.Replace("/", "\\");

            if (!string.Equals(path1[^1..], "\\"))
            {
                path1 += "\\";
            }

            if (!string.Equals(path2[^1..], "\\"))
            {
                path2 += "\\";
            }

            var rel = GetRelativePath(path1, path2).Replace("\\", "/");

            if (!string.Equals(rel[..], "."))
            {
                rel = "./" + rel;
            }

            return rel;
        }

        private static string GetRelativePath(string from, string to)
        {
            var path1 = new Uri(from.Replace("\\", "/"));
            var path2 = new Uri(to.Replace("\\", "/"));

            var rel = path1.MakeRelativeUri(path2);

            return Uri.UnescapeDataString(rel.OriginalString);
        }

        private static bool CheckIsArray(string type)
        {
            return type.Contains("[]") ||
                type.Contains("ICollection") ||
                type.Contains("IEnumerable") ||
                type.Contains("IList") ||
                type.Contains("Array") ||
                type.Contains("Enumerable") ||
                type.Contains("Collection") ||
                type.StartsWith("List");
        }

        private static bool CheckOptional(string type)
        {
            return type.Contains("?");
        }

        private static bool CheckDictionary(string type)
        {
            return (type.Contains("Dictionary") || type.Contains("IDictionary")) && type.Contains("<") && type.Contains(">") && type.Contains(",");
        }

        private static bool CheckList(string type)
        {
            return type.StartsWith("List<");
        }

        private static string CleanType(string type)
        {
            return type.Replace("?", String.Empty)
                .Replace("[]", String.Empty)
                .Replace("ICollection", String.Empty)
                .Replace("IEnumerable", String.Empty)
                .Replace("IList", String.Empty)
                .Replace("<", String.Empty)
                .Replace(">", String.Empty);
        }
    }
}
