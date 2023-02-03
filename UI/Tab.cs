using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI
{
    public abstract class Tab
    {
        public string Name = "";
        public abstract bool Selected { get; set; }
        public object? Tag;
    }
}
