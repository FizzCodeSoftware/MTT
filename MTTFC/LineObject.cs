namespace MTTFC
{
    public class LineObject
    {
        public string VariableName { get; set; }
        public string Type { get; set; }
        public string DifferentTypeToImport { get; set; }
        public bool IsArray { get; set; }
        public int ArrayDimensions { get; set; }
        public bool IsOptional { get; set; }
        public bool UserDefined { get; set; }
        public string UserDefinedImport { get; set; }
        public LineObject[] Container { get; set; }
        public bool IsContainer {
            get {
                return Container?.Length > 0;
            }
        }
    }
}