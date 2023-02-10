using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Cornifer
{
    public static class JsonTypes
    {
        public static JsonNode SaveVector2(Vector2 vector)
        {
            return new JsonObject
            {
                ["x"] = vector.X,
                ["y"] = vector.Y,
            };
        }

        public static Vector2 LoadVector2(JsonNode node)
        {
            Vector2 vec = default;
            if (node.TryGet("x", out float x))
                vec.X = x;
            if (node.TryGet("y", out float y))
                vec.Y = y;
            return vec;
        }

        public static JsonNode SaveRectangle(Rectangle rectangle)
        {
            return new JsonObject
            {
                ["x"] = rectangle.X,
                ["y"] = rectangle.Y,
                ["w"] = rectangle.Width,
                ["h"] = rectangle.Height,
            };
        }

        public static Rectangle LoadRectangle(JsonNode node)
        {
            Rectangle rect = default;
            if (node.TryGet("x", out int x))
                rect.X = x;
            if (node.TryGet("y", out int y))
                rect.Y = y;
            if (node.TryGet("w", out int w))
                rect.Width = w;
            if (node.TryGet("h", out int h))
                rect.Height = h;
            return rect;
        }


    }
}
