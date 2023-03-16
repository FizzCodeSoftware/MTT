using System;

namespace Example.Resources
{
    public class ObjectArrayAndDictionary
    {
		public Dictionary<string, int> Dict { get; set; }
		public object[] ObjArray { get; set; }
        public object[,] ObjArray2 { get; set; }
		public int[,] IntArray { get; init; }
    }
}
