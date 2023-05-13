using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.Structures
{
    public class DiamondPlacement
    {
        const string FilePath = "Assets/diamondplacement.txt";
        public static readonly Vector2 DiamondSize = new(9);
        public static readonly Vector2 DiamondRowMove = new(5, 5);
        public static readonly Vector2 DiamondColumnMove = new(-5, 5);

        public Vector2 Size { get; private set; }
        public Vector2[] Positions { get; private set; }

        public static DiamondPlacement[] Placements { get; private set; } = Array.Empty<DiamondPlacement>();
        public static Vector2 MaxSize { get; private set; } = Vector2.Zero;

        public DiamondPlacement(Vector2[] positions)
        {
            Positions = positions;

            if (positions.Length == 0)
            {
                Size = Vector2.Zero;
                return;
            }

            Vector2 tl = positions[0];
            Vector2 br = positions[0] + DiamondSize;

            for (int i = 1; i < positions.Length; i++)
            {
                tl.X = Math.Min(tl.X, positions[i].X);
                tl.Y = Math.Min(tl.Y, positions[i].Y);

                br.X = Math.Max(br.X, positions[i].X + DiamondSize.X);
                br.Y = Math.Max(br.Y, positions[i].Y + DiamondSize.Y);
            }

            tl.Floor();
            br.Floor();

            Size = br - tl;
            Vector2 center = tl + Size / 2;

            for (int i = 0; i < positions.Length; i++)
            {
                Vector2 pos = positions[i] - center;
                pos.Floor();
                positions[i] = pos;
            }
        }

        public static void Load()
        {
            string filePath = Path.Combine(Main.MainDir, FilePath);

            if (!File.Exists(filePath))
                return;

            List<Vector2> positions = new List<Vector2>();
            List<DiamondPlacement> placements = new();

            foreach (string line in File.ReadLines(filePath))
            {
                if (line.StartsWith("//") || line.Length == 0 || string.IsNullOrWhiteSpace(line))
                    continue;

                Vector2 rowStart = Vector2.Zero;
                Vector2 pos = Vector2.Zero;
                positions.Clear();

                foreach (string row in line.Split('|'))
                {
                    for (int i = 0; i < row.Length; i++)
                    {
                        char c = row[i];

                        if (c >= '0' && c <= '9')
                        {
                            int v = c - '0';

                            while (positions.Count < v + 1)
                                positions.Add(Vector2.Zero);

                            positions[v] = pos;
                        }
                        pos += DiamondRowMove;
                    }
                    rowStart += DiamondColumnMove;
                    pos = rowStart;
                }

                DiamondPlacement placement = new(positions.ToArray());
                placements.Add(placement);
                MaxSize = new(Math.Max(MaxSize.X, placement.Size.X), Math.Max(MaxSize.Y, placement.Size.Y));
            }

            Placements = placements.ToArray();
        }
    }
}
