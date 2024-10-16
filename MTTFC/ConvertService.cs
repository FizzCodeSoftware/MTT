﻿namespace MTTFC
{
    public class ConvertService
    {
        /// <summary>
        /// A delegate to support logging
        /// </summary>
        /// <param name="s">The string to log.</param>
        /// <param name="args">The arguments.</param>
        public delegate void LogAction(string s, params object[] args);

        private LogAction Log { get; }

        private readonly ConvertServiceParameters OriginalParameters;

        public ConvertService(LogAction log, ConvertServiceParameters parameters)
        {
            OriginalParameters = parameters;
            Log = log;
        }

        public bool Execute()
        {
            Log("Starting MTT ConvertService");
            var localWorkingDir = ConvertServiceHelper.GetWorkingDirectory(Log, OriginalParameters.WorkingDirectory);
            var localConvertDir = ConvertServiceHelper.GetConvertDirectory(Log, OriginalParameters.ConvertDirectory);

            var parameters = new ConvertServiceParameters()
            {
                WorkingDirectory = localWorkingDir,
                ConvertDirectory = localConvertDir,
                ConvertToType = OriginalParameters.ConvertToType,
                EnumValues = OriginalParameters.EnumValues,
                PathStyle = OriginalParameters.PathStyle,
                IsAutoGeneratedTag = OriginalParameters.IsAutoGeneratedTag,
                IsModelInTSFileName = OriginalParameters.IsModelInTSFileName,
                Extends = OriginalParameters.Extends,
                Implements = OriginalParameters.Implements,
            };

            var models = ConvertServiceModelLoader.GetModels(parameters.WorkingDirectory, parameters.WorkingDirectory);

            ConvertServiceModelFiller.BreakDown(models, parameters.WorkingDirectory, parameters.IsModelInTSFileName);
            ConvertServiceConverter.Convert(Log, models, parameters);

            Log("Finished MTT ConvertService");
            return true;
        }
    }
}
