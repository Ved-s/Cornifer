using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer
{
    public static class RegionColors
    {
        public static Color? GetMainColor(string region, string? subregion)
        {
            switch (region)
            {
                case "SU": return RGBColor(0x38C79E);
                case "HI": return RGBColor(0x75CED5);
                case "DS": return RGBColor(0xC73ADF);
                case "GW": return RGBColor(0x8DBD42);
                case "LF": return RGBColor(0x608C9E);
                case "CC": switch (subregion)
                {
                    case "The Gutter": return RGBColor(0x99661E);
                    default: return RGBColor(0xC53D0F);
                }
                case "SI": switch (subregion)
                {
                    case "Communications Array": return RGBColor(0xFF8FD2);
                    default: return RGBColor(0xFFD0AA);
                }
                case "SH": switch (subregion)
                {
                    case "Memory Crypts": return RGBColor(0x6464D3);
                    default: return RGBColor(0x515151);
                }
                case "SL": switch (subregion)
                {
                    case "The Precipice": return RGBColor(0xE7C164);
                    case "Looks to the Moon": return RGBColor(0x86C2FF);
                    default: return RGBColor(0xEDE5CC);
                }
                case "SB": switch (subregion)
                {
                    case "Filtration System": return RGBColor(0xDE061B);
                    case "Depths": return RGBColor(0xFFED00);
                    default: return RGBColor(0x7E4337);
                }
                case "UW": switch (subregion)
                {
                    case "The Leg": return RGBColor(0x9A1C32);
                    case "The Wall": return RGBColor(0xFFB447);
                    default: return RGBColor(0x886B57);
                }
                case "SS": switch (subregion)
                {
                    case "Five Pebbles (General Systems Bus)": return RGBColor(0x19E53F);
                    case "Five Pebbles (Recursive Transform Array)": return RGBColor(0x00FEFF);
                    case "Five Pebbles (Memory Conflux)": return RGBColor(0xEB9214);
                    case "Five Pebbles (Unfortunate Development)": return RGBColor(0xFF00FD);
                    default: return RGBColor(0x939393);
                }

                case "VS": switch (subregion)
                {
                    case "Sump Tunnel": return RGBColor(0x666256);
                    default: return RGBColor(0x75405C);
                }
                case "UG": return RGBColor(0x8FB572);
                case "OE": switch (subregion)
                {
                    case "Sunken Pier": return RGBColor(0x4A655E);
                    case "Journey's End": return RGBColor(0xEA9678);
                    case "Facility Roots (Western Intake)": return RGBColor(0x592712);
                    default: return RGBColor(0xD8AE8A);
                }
                case "LC": switch (subregion)
                {
                    case "The Floor": return RGBColor(0x7E7170);
                    case "12th Council Pillar, the House of Braids": return RGBColor(0x7F3339);
                    case "Atop the Tallest Tower": return RGBColor(0x7C9EB2);
                    default: return RGBColor(0xCAB8A5);
                }
                case "DM": switch (subregion)
                {
                    case "Looks to the Moon (Memory Conflux)": return RGBColor(0xB8BF99);
                    case "Luna": return RGBColor(0xCCC1A1);
                    case "Looks to the Moon (Neural Terminus)": return RGBColor(0x007CFF);
                    case "Looks to the Moon (Abstract Convergence Manifold)": return RGBColor(0xE5EA46);
                    case "Looks to the Moon (Vents)": return RGBColor(0x135965);
                    case "Struts": return RGBColor(0x446E8A);
                    default: return RGBColor(0x554C99);
                }
                case "LM": switch (subregion)
                {
                    case "The Precipice": return RGBColor(0xE7C164);
                    default: return RGBColor(0xD3F0B4);
                }
                case "RM": switch (subregion)
                {
                    case "The Rot (Depths)": return RGBColor(0x9C00FF);
                    case "Five Pebbles (Primary Cortex)": return RGBColor(0x19E53F);
                    case "The Rot (Cystic Conduit)": return RGBColor(0xEB9214);
                    case "Five Pebbles (Recursive Transform Array)": return RGBColor(0x00FEFF);
                    case "Five Pebbles (Linear Systems Rail)": return RGBColor(0x87FFCD);
                    default: return RGBColor(0xA1B5D3);
                }
                case "CL": switch (subregion)
                {
                    case "Frosted Cathedral": return RGBColor(0x5A5A5A);
                    case "The Husk": return RGBColor(0x8C91A5);
                    case "Five Pebbles": return RGBColor(0x927C5C);
                    default: return RGBColor(0x486660);
                }
                case "HR": return RGBColor(0x590E00);
                case "MS": switch (subregion)
                {
                    case "Submerged Superstructure (Vents)": return RGBColor(0x53A263);
                    case "Submerged Superstructure (The Heart)": return RGBColor(0x03FFF3);
                    case "Bitter Aerie": return RGBColor(0x8B92FF);
                    case "Auxiliary Transmission Array": return RGBColor(0xCDA1EB);
                    default: return RGBColor(0x7CC2F5);
                }
            }

            return null;
        }

        public static bool TryGetMainColor(string region, string? subregion, out Color color)
        {
            Color? v = GetMainColor(region, subregion);
            color = v ?? default;
            return v.HasValue;
        }

        static Color RGBColor(int rgb)
        {
            int r = (rgb >> 16) & 0xff;
            int g = (rgb >> 8) & 0xff;
            int b = rgb & 0xff;

            return new Color(r, g, b);
        }
    }
}
