using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Simple.Data
{
    public class SimpleFunction
    {
        private readonly ReadOnlyCollection<object> _args;
        private readonly string _name;

        public SimpleFunction(string name, IEnumerable<object> args)
        {
            _name = name;
            _args = args.ToList().AsReadOnly();
        }

        public string Name
        {
            get { return _name; }
        }

        public ReadOnlyCollection<object> Args
        {
            get { return _args; }
        }
    }
}