using Cornifer.UI.Elements;
using Cornifer.UI.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI.Modals
{
    public class TextFormatting : Modal<TextFormatting, Empty>
    {
        static (bool format, string text)[] FormattingInfo = new[]
        {
            (false,
            "Cornifer supports text formatting similar to BBCode.\n" +
            "Format consists of tags, formatted [tagName:tagData]tagContent[/tagName] or just [tagName:tagData].\n" +
            "Tags can be inside other tags. If tag is never closed, it will apply to the rest of the text.\n" +
            "Current tag list:\n"),

            (false, ""),
            (false,
            "[c:RRGGBB] Colored text\n" +
            "Color data can be RRGGBBAA, RRGGBB, RGB, or single grayscale hex letter."),
            (true, "\\[c:f00\\]Red text\\[/c\\] - [c:f00]Red text[/c]"),

            (false, ""),
            (false,
            "[s:RRGGBB] Shaded text\n" +
            "Shade color data can be RRGGBBAA, RRGGBB, RGB, or single grayscale hex letter."),
            (true, "\\[s:0\\]Shaded text\\[/s\\] - [s:0]Shaded text[/s]"),

            (false, ""),
            (false,
            "[ns] Non-Shaded text\n" +
            "Removes text shade."),
            (true, "\\[s:0\\]Shaded and \\[ns\\]non-shaded\\[/ns\\] text\\[/s\\] - [s:0]Shaded and [ns]non-shaded[/ns] text[/s]"),

            (false, ""),
            (false,
            "[i] Italic text\n" +
            "Makes text appear italic."),
            (true, "\\[i\\]Italic text\\[/i\\] - [i]Italic text[/i]"),

            (false, ""),
            (false,
            "[b] Bold text\n" +
            "Makes text appear bold."),
            (true, "\\[b\\]Bold text\\[/b\\] - [b]Bold text[/b]"),

            (false, ""),
            (false,
            "[u] Underlined text\n" +
            "Makes text underlined."),
            (true, "\\[u\\]Underlined text\\[/u\\] - [u]Underlined text[/u]"),

            (false, ""),
            (false,
            "[sc:float] Scaled text\n" +
            "Scales text."),
            (true, "\\[sc:0.5\\]Small text\\[/sc\\] and \\[sc:2\\]big text\\[/sc\\] - [sc:0.5]Small text[/sc] and [sc:2]big text[/sc]"),

            (false, ""),
            (false,
            "[a:float] Aligned text\n" +
            "Makes text aligned with text before by some value."),
            (true, "\\[sc:2\\]Big text,\\[/sc\\] normal \\[a:.6\\]and aligned\\[/a\\] - [sc:2]Big text,[/sc] normal [a:.6]and aligned[/a]"),

            (false, ""),
            (false,
            "[ic:name] [ic:name:color] Icon (this tag does not need to be closed)\n" +
            "Draws icons, found in \"Add icons to map\" menu."),
            (true, "Slugcat \\[ic:Slugcat_White\\] and their bat \\[ic:batSymbol:0\\] - Slugcat [ic:Slugcat_White] and their bat [ic:batSymbol:0]"),

            (false, ""),
            (false,
            "[ds:color] Dropshadow\n" +
            "Adds dropshadow effect to text and icons."),
            (true, "\\[ds:555\\]Text\\[/ds\\] - [ds:555]Text[/ds]"),
        };

        public TextFormatting() 
        {
            Top = new(0, .5f, -.5f);
            Left = new(0, .5f, -.5f);

            Width = new(0, .9f);
            Height = new(0, .9f);

            Margin = 5;
            Padding = 5;

            Visible = ModalVisible;

            UIList list;

            Elements = new(this)
            {
                new UILabel
                {
                    Top = 10,
                    Height = 20,
                    Text = "Text formatting",
                    TextAlign = new(.5f)
                },
                new UIList
                {
                    Top = 35,
                    Height = new(-60, 1),
                }.Assign(out list),
                new UIButton
                {
                    Top = new(-20, 1),
                    Left = new(0, .5f, -.5f),
                    Width = 80,
                    Height = 20,
                    Text = "Close",
                    TextAlign = new(.5f)
                }.OnEvent(UIElement.ClickEvent, (_, _) => ReturnResult(new()))
            };

            foreach (var (formatted, text) in FormattingInfo)
            {
                if (text == "")
                {
                    list.Elements.Add(new UIElement { Height = 20 });
                    continue;
                }

                if (formatted)
                {
                    list.Elements.Add(new UIFormattedLabel
                    {
                        Height = 0,
                        Width = 0,

                        Text = text,
                    });
                }
                else
                {
                    list.Elements.Add(new UILabel
                    {
                        WordWrap = true,
                        Height = 0,
                        Width = 0,

                        Text = text,
                    });
                }
            }
        }
    }
}
