namespace MTTRunner
{
    using System;

    using MTTFC;

    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Assuming testing...");

                args = new string[2] { "", "" };

                //works for vs code
                args[0] = "../../example/Resources";
                args[1] = "../../example/models";
            }

            Program.StartService(args);
        }

        public static void StartService(string[] args)
        {
            var convertService = new ConvertService((logString, logArgs) => Console.WriteLine(logString, logArgs),
                new ConvertServiceParameters()
                {
                    WorkingDirectory = args[0],
                    ConvertDirectory = args[1]
                });

            convertService.Execute();
        }
    }
}
