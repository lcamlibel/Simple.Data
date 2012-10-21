using System;

namespace Simple.Data.Ado
{
    internal class FunctionNameConverter : IFunctionNameConverter
    {
        #region IFunctionNameConverter Members

        public string ConvertToSqlName(string simpleFunctionName)
        {
            if (simpleFunctionName.Equals("length", StringComparison.InvariantCultureIgnoreCase))
            {
                return "len";
            }
            if (simpleFunctionName.Equals("average", StringComparison.InvariantCultureIgnoreCase))
            {
                return "avg";
            }
            return simpleFunctionName;
        }

        #endregion
    }
}