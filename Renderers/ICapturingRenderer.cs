using Cornifer.MapObjects;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.Renderers
{
    public interface ICapturingRenderer
    {
        void BeginCapture(Vector2 worldPos, Vector2 captureSize);
        void EndCapture();

        void BeginObjectCapture(MapObject obj, bool shade);
        void EndObjectCapture();
    }
}
