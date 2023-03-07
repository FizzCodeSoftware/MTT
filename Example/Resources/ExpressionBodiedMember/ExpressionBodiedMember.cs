using System;

namespace Example.Resources
{
    public class ExpressionBodiedMember
    {
        public int IdNormal { get; set; }
		public string IdString  => IdNormal.ToString();
    }
}