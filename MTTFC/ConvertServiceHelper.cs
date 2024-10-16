namespace MTTFC
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;

    public static class ConvertServiceHelper
    {
        public static string GetWorkingDirectory(ConvertService.LogAction log, string workingDirectory)
        {
            string localWorkingDir;
            var dir = Directory.GetCurrentDirectory();

            if (string.IsNullOrEmpty(workingDirectory))
            {
                log("Using Default Working Directory {0}", dir);
                localWorkingDir = dir;
            }
            else
            {
                var localdir = Path.Combine(dir, workingDirectory);

                if (!Directory.Exists(localdir))
                {
                    log("Working Directory does not exist {0}, creating..", localdir);
                    Directory.CreateDirectory(localdir).Create();
                    localWorkingDir = localdir;
                }
                else
                {
                    log("Working Directory {0}", localdir);
                    localWorkingDir = localdir;
                }
            }

            return localWorkingDir;
        }

        public static string GetConvertDirectory(ConvertService.LogAction log, string convertDirectory)
        {
            var dir = Directory.GetCurrentDirectory();

            if (string.IsNullOrEmpty(convertDirectory))
            {
                log("Using Default Convert Directory {0} - this does not always update", dir);
                return dir;
            }
            else
            {
                var localdir = Path.Combine(dir, convertDirectory);

                if (!Directory.Exists(localdir))
                {
                    log("Convert Directory does not exist {0}, creating..", localdir);
                    Directory.CreateDirectory(localdir).Create();
                }
                else
                {
                    log("Convert Directory {0}", localdir);
                }

                return localdir;
            }
        }

        private static void DeleteDirectory(string path, int iteration)
        {
            foreach (string directory in Directory.GetDirectories(path))
            {
                DeleteDirectory(directory, 0);
            }

            try
            {
                Directory.Delete(path, true);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                if (iteration >= 10)
                {
                    throw;
                }
                Thread.Sleep(100 * (int)Math.Pow(2, iteration));
                DeleteDirectory(path, ++iteration);
            }
        }

        public static string ToCamelCase(string str)
        {
            if (string.IsNullOrEmpty(str) || char.IsLower(str, 0))
                return str;

            bool isCaps = true;

            foreach (var c in str)
            {
                if (char.IsLetter(c) && char.IsLower(c))
                    isCaps = false;
            }

            if (isCaps)
                return str.ToLower();

            return char.ToLowerInvariant(str[0]) + str[1..];
        }

        public static string ToKebabCasePath(string path)
        {
            return string.Join("/", path.Split('/').Select(segment => ToKebabCase(segment)));
        }

        public static string ToKebabCase(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            var words = new List<string>();
            var wordStart = 0;
            int i;
            for (i = 1; i < str.Length; i++)
            {
                if (char.IsUpper(str[i]))
                {
                    words.Add(str[wordStart..i]);
                    wordStart = i;
                }
            }
            words.Add(str[wordStart..i]);

            return string.Join("-", words.Where(w => !string.IsNullOrEmpty(w)).Select(w => w.ToLower()));
        }
    }
}
