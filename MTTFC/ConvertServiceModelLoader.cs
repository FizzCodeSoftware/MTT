namespace MTTFC
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public static class ConvertServiceModelLoader
    {
        public static List<ModelFile> GetModels(string directory, string localWorkingDir)
        {
            var models = new List<ModelFile>();

            var files = Directory.GetFiles(directory);
            var dirs = Directory.GetDirectories(directory);

            foreach (var dir in dirs)
            {
                string d = dir.Replace(directory, string.Empty);

                if (!string.IsNullOrEmpty(d))
                {
                    models.AddRange(GetModels(dir, localWorkingDir));
                }
            }

            var workingUri = new Uri(EnsureTrailingSlash(localWorkingDir));
            var dirUri = new Uri(EnsureTrailingSlash(directory));
            var relativePath = workingUri.MakeRelativeUri(dirUri).OriginalString;
            foreach (var file in files)
            {
                var model = GetModel(file, relativePath);
                models.Add(model);
            }

            return models;
        }

        private static string EnsureTrailingSlash(string str)
        {
            if (!str.EndsWith("/") && !str.EndsWith("\\"))
            {
                str += "\\";
            }
            return str;
        }

        private static ModelFile GetModel(string file, string structure = "")
        {
            structure = structure.Replace(@"\", "/");
            string[] explodedDir = file.Replace(@"\", "/").Split('/');

            string fileName = explodedDir[^1];

            string[] fileInfo = File.ReadAllLines(file);

            return new ModelFile()
            {
                Name = ToPascalCase(fileName.Replace(".cs", String.Empty)),
                Info = fileInfo,
                Structure = structure
            };
        }

        private static string ToPascalCase(string str)
        {
            if (String.IsNullOrEmpty(str) || Char.IsUpper(str, 0))
                return str;

            return Char.ToUpperInvariant(str[0]) + str[1..];
        }
    }
}
