using Cornifer.Renderers;
using Microsoft.Xna.Framework.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Cornifer.MapObjects
{
    public class MapImage : SimpleIcon
    {
        public override string? Name => "Image";

        public override bool SkipTextureSave => true;

        public MapImage() { }
        public MapImage(Image<Rgba32> image) 
        {
            Texture = new(Main.Instance.GraphicsDevice, image.Width, image.Height);
            int size = image.Width * image.Height;
            Rgba32[] buffer = ArrayPool<Rgba32>.Shared.Rent(size);
            image.Frames.RootFrame.CopyPixelDataTo(buffer.AsSpan(0, size));
            Texture.SetData(buffer, 0, size);
            Frame = new(0, 0, image.Width, image.Height);
            ArrayPool<Rgba32>.Shared.Return(buffer);
        }

        protected override JsonNode? SaveInnerJson(bool forCopy)
        {
            JsonNode? node = base.SaveInnerJson(forCopy) ?? new JsonObject();

            if (Texture is not null)
            {
                using MemoryStream ms = new();
                Texture.SaveAsPng(ms, Texture.Width, Texture.Height);
                node["image"] = Convert.ToBase64String(ms.AsSpan());
            }

            return node;
        }

        protected override void LoadInnerJson(JsonNode node, bool shallow)
        {
            base.LoadInnerJson(node, shallow);

            if (node.TryGet("image", out string? image)) 
            {
                int utf8len = Encoding.UTF8.GetByteCount(image);
                byte[] utf8buf = ArrayPool<byte>.Shared.Rent(utf8len);
                Span<byte> utf8 = utf8buf.AsSpan().Slice(0, utf8len);
                utf8len = Encoding.UTF8.GetBytes(image, utf8);
                utf8 = utf8.Slice(0, utf8len);

                int b64len = Base64.GetMaxDecodedFromUtf8Length(utf8len);
                byte[] imgbuf = ArrayPool<byte>.Shared.Rent(b64len);
                Span<byte> img = imgbuf.AsSpan().Slice(0, b64len);
                Base64.DecodeFromUtf8(utf8, img, out _, out int imglen);

                using MemoryStream ms = new(imgbuf, 0, imglen);
                Texture = Texture2D.FromStream(Main.Instance.GraphicsDevice, ms);
                Frame = new(0, 0, Texture.Width, Texture.Height);
            }
        }
    }
}
