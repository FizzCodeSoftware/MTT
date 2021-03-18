﻿namespace MTTFC
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;

    public static class ConvertServiceConverter
    {
        public static void Convert(ConvertService.LogAction log, List<ModelFile> models, string localConvertDir, PathStyle pathStyle, EnumValues enumValues, bool isAutoGeneratedTag)
        {
            log("Converting..");

            foreach (var file in models)
            {
                var directoryPath = Path.Combine(localConvertDir, file.Structure);

                var relativePath = pathStyle == PathStyle.Kebab
                    ? ConvertServiceHelper.ToKebabCasePath(file.Structure)
                    : file.Structure;

                DirectoryInfo di = Directory.CreateDirectory(Path.Combine(localConvertDir, relativePath));
                di.Create();

                string fileName = (pathStyle == PathStyle.Kebab ? ConvertServiceHelper.ToKebabCase(file.Name) : ConvertServiceHelper.ToCamelCase(file.Name)) + ".ts";
                log("Creating file {0}", fileName);
                string saveDir = Path.Combine(directoryPath, fileName);

                using var stream = GetStream(saveDir, 0);
                using StreamWriter f =
                    new StreamWriter(stream, System.Text.Encoding.UTF8, 1024, false);
                var importing = false;  //only used for formatting
                var imports = new List<string>();  //used for duplication

                if (isAutoGeneratedTag)
                {
                    f.WriteLine("/* Auto Generated */");
                    f.WriteLine();
                }

                if (file.IsEnum)
                {
                    f.WriteLine(
                        "export enum "
                        + file.Name
                        // + (String.IsNullOrEmpty(file.Inherits) ? "" : (" : " + file.Inherits)) //typescript doesn't extend enums like c#
                        + " {"
                        );

                    foreach (var obj in file.EnumObjects)
                    {
                        if (!String.IsNullOrEmpty(obj.Name))
                        {  //not an empty obj
                            var tsName = ConvertServiceHelper.ToCamelCase(obj.Name);
                            var str = tsName;
                            if (enumValues == EnumValues.Strings)
                            {
                                str += " = '" + tsName + "'";
                            }
                            else if (!obj.IsImplicit)
                            {
                                str += " = " + obj.Value;
                            }
                            str += ",";

                            f.WriteLine("    " + str);
                        }
                    }

                    f.WriteLine("}");
                }
                else
                {
                    foreach (var obj in file.Objects)
                    {
                        if (!String.IsNullOrEmpty(file.Inherits))
                        {
                            importing = true;

                            var import = "import { " + file.Inherits + " } from \""
                                + (pathStyle == PathStyle.Kebab ? ConvertServiceHelper.ToKebabCasePath(file.InheritenceStructure) : file.InheritenceStructure) + "\";";

                            if (!imports.Contains(import))
                            {
                                f.WriteLine(import);
                                imports.Add(import);
                            }
                        }

                        if (obj.UserDefined)
                        {
                            importing = true;
                            var import = "import { " + obj.Type + " } from \""
                                + (pathStyle == PathStyle.Kebab ? ConvertServiceHelper.ToKebabCasePath(obj.UserDefinedImport) : obj.UserDefinedImport) + "\";";

                            if (!imports.Contains(import))
                            {
                                f.WriteLine(import);
                                imports.Add(import);
                            }
                        }

                        if (obj.IsContainer)
                        {
                            foreach (LineObject innerObj in obj.Container)
                            {
                                if (innerObj.UserDefined)
                                {
                                    importing = true;
                                    var import = "import { " + innerObj.Type + " } from \""
                                        + (pathStyle == PathStyle.Kebab ? ConvertServiceHelper.ToKebabCasePath(innerObj.UserDefinedImport) : innerObj.UserDefinedImport) + "\";";

                                    if (!imports.Contains(import))
                                    {
                                        f.WriteLine(import);
                                        imports.Add(import);
                                    }
                                }
                            }
                        }
                    }

                    if (importing)
                    {
                        f.WriteLine("");
                    }

                    f.WriteLine(
                        "export interface "
                        + file.Name
                        + (String.IsNullOrEmpty(file.Inherits) ? "" : (" extends " + file.Inherits)) //if class has inheritance
                        + " {"
                        );

                    foreach (var obj in file.Objects)
                    {
                        if (obj.IsContainer)
                        {
                            var str =
                                ConvertServiceHelper.ToCamelCase(obj.VariableName)
                                + (obj.IsOptional ? "?" : String.Empty)
                                + ": "
                                + $"{obj.Type}<{obj.Container[0].Type}, {obj.Container[1].Type}>;";

                            f.WriteLine("    " + str);
                        }
                        else if (!String.IsNullOrEmpty(obj.VariableName))
                        {  //not an empty obj
                            var str =
                                ConvertServiceHelper.ToCamelCase(obj.VariableName)
                                + (obj.IsOptional ? "?" : String.Empty)
                                + ": "
                                + obj.Type
                                + (obj.IsArray ? "[]" : String.Empty)
                                + ";";

                            f.WriteLine("    " + str);
                        }
                    }

                    f.WriteLine("}");
                }
            }
        }

        private static FileStream GetStream(string saveDir, int iteration)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(saveDir));
                return new FileStream(saveDir, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.SequentialScan);
            }
            catch (UnauthorizedAccessException) when (iteration < 10)
            {
                Thread.Sleep(100 * (int)Math.Pow(2, iteration));
                return GetStream(saveDir, ++iteration);
            }
            catch (DirectoryNotFoundException) when (iteration < 10)
            {
                return GetStream(saveDir, ++iteration);
            }
        }
    }
}