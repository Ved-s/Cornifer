using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer
{
    public class Layer
    {
        public string Id;
        public string Name;

        public bool Special;

        public Layer(string id, string name, bool special)
        {
            Id = id;
            Name = name;
            Special = special;
        }

        public void DrawShade()
        {
            throw new NotImplementedException();
        }

        public void Draw() 
        {
            throw new NotImplementedException();
        }
    }
}
