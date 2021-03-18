﻿namespace MTTFC
{
    using System.Collections.Generic;

    public class ConvertServiceParameters
    {
        /// <summary>
        /// The current working directory for the convert process
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// The directory to save the ts models
        /// </summary>
        public string ConvertDirectory { get; set; }

        /// <summary>
        /// Comments at the top of each file that it was auto generated
        /// </summary>
        public bool IsAutoGeneratedTag { get; set; } = true; //default value if one is not provided;

        /// <summary>
        /// Determines whether to generate numeric or string values in typescript enums
        /// </summary>
        public EnumValues EnumValues { get; internal set; }

        /// <summary>
        /// Determines the naming style of the generated files and folders
        /// </summary>
        public PathStyle PathStyle { get; set; }
    }
}
