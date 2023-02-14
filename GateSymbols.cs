using Cornifer.Renderers;
using Cornifer.UI.Elements;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Cornifer
{
    public class GateSymbols : SelectableIcon
    {
        public override Vector2 Size => new(107, 66);
        public override int ShadeSize => 5;
        public override int? ShadeCornerRadius => 6;
        public override bool LoadCreationForbidden => true;
        public override string? Name => "GateSymbols";

        AtlasSprite? LeftSymbolSprite;
        AtlasSprite? RighSymbolSprite;
        AtlasSprite? LeftArrowSprite;
        AtlasSprite? RightArrowSprite;

        public Color SplitterColor = Color.White;
        public Color LeftSymbolColor = Color.White;
        public Color RightSymbolColor = Color.White;
        public Color LeftArrowColor = Color.White;
        public Color RightArrowColor = Color.White;

        public GateSymbols()
        {
            LeftArrowSprite = GameAtlases.GetSpriteOrNull("Misc_ArrowLeft");
            RightArrowSprite = GameAtlases.GetSpriteOrNull("Misc_ArrowRight");
        }

        public GateSymbols(string left, string right) : this()
        {
            LeftSymbolSprite = GetSprite(left);
            RighSymbolSprite = GetSprite(right);
        }

        static AtlasSprite? GetSprite(string symbol)
        {
            string? name = symbol switch
            {
                "1" => "karma0",
                "2" => "karma1",
                "3" => "karma2",
                "4" => "karma3",
                "5" => "karma4",
                "R" => "Misc_KarmaR",
                _ => null
            };

            if (name is null)
                return null;

            if (GameAtlases.Sprites.TryGetValue(name, out AtlasSprite? sprite))
                return sprite;
            return null;
        }

        public override void DrawIcon(Renderer renderer)
        {
            Vector2 center = WorldPosition + Size / 2;

            Vector2 splitterSize = new(5, 64);
            renderer.DrawTexture(Main.Pixel, center - splitterSize/2, null, splitterSize, SplitterColor);

            if (LeftSymbolSprite is not null)
            {
                Vector2 spriteSize = LeftSymbolSprite.Frame.Size.ToVector2();

                Vector2 spritePos = WorldPosition + new Vector2(Size.X / 2 - 15.5f - spriteSize.X, Size.Y - 21 - spriteSize.Y / 2);
                renderer.DrawTexture(LeftSymbolSprite.Texture, spritePos, LeftSymbolSprite.Frame, null, LeftSymbolColor);
            }

            if (RighSymbolSprite is not null)
            {
                Vector2 spriteSize = RighSymbolSprite.Frame.Size.ToVector2();

                Vector2 spritePos = WorldPosition + new Vector2(Size.X / 2 + 15.5f, Size.Y - 21 - spriteSize.Y / 2);
                renderer.DrawTexture(RighSymbolSprite.Texture, spritePos, RighSymbolSprite.Frame, null, RightSymbolColor);
            }

            if (LeftArrowSprite is not null)
            {
                Vector2 spritePos = WorldPosition + new Vector2(Size.X / 2 + 22.5f, 0);
                renderer.DrawTexture(LeftArrowSprite.Texture, spritePos, LeftArrowSprite.Frame, null, RightArrowColor);
            }

            if (RightArrowSprite is not null)
            {
                Vector2 spriteSize = RightArrowSprite.Frame.Size.ToVector2();

                Vector2 spritePos = WorldPosition + new Vector2(Size.X / 2 - 22.5f - spriteSize.X, 0);
                renderer.DrawTexture(RightArrowSprite.Texture, spritePos, RightArrowSprite.Frame, null, LeftArrowColor);
            }
        }

        protected override void BuildInnerConfig(UIList list)
        {
            list.Elements.Add(new UIButton
            {
                Text = "Set splitter color",
                Height = 20,
            }.OnEvent(UIElement.ClickEvent, (btn, _) => Interface.ColorSelector.Show("Splitter color", SplitterColor, (_, c) => SplitterColor = c)));

            list.Elements.Add(new UIButton
            {
                Text = "Set left symbol color",
                Height = 20,
            }.OnEvent(UIElement.ClickEvent, (btn, _) => Interface.ColorSelector.Show("Left symbol color", LeftSymbolColor, (_, c) => LeftSymbolColor = c)));

            list.Elements.Add(new UIButton
            {
                Text = "Set right symbol color",
                Height = 20,
            }.OnEvent(UIElement.ClickEvent, (btn, _) => Interface.ColorSelector.Show("Right symbol color", RightSymbolColor, (_, c) => RightSymbolColor = c)));

            list.Elements.Add(new UIButton
            {
                Text = "Set left arrow color",
                Height = 20,
            }.OnEvent(UIElement.ClickEvent, (btn, _) => Interface.ColorSelector.Show("Left arrow color", LeftArrowColor, (_, c) => LeftArrowColor = c)));

            list.Elements.Add(new UIButton
            {
                Text = "Set right arrow color",
                Height = 20,
            }.OnEvent(UIElement.ClickEvent, (btn, _) => Interface.ColorSelector.Show("Right arrow color", RightArrowColor, (_, c) => RightArrowColor = c)));
        }

        protected override JsonNode? SaveInnerJson()
        {
            return new JsonObject
            {
                ["splitter"] = SplitterColor.PackedValue,
                ["leftSymbol"] = LeftSymbolColor.PackedValue,
                ["rightSymbol"] = RightSymbolColor.PackedValue,
                ["leftArrow"] = LeftArrowColor.PackedValue,
                ["rightArrow"] = RightArrowColor.PackedValue,
            };
        }

        protected override void LoadInnerJson(JsonNode node)
        {
            if (node.TryGet("splitter", out uint splitter))
                SplitterColor.PackedValue = splitter;

            if (node.TryGet("leftSymbol", out uint leftSymbol))
                LeftSymbolColor.PackedValue = leftSymbol;

            if (node.TryGet("rightSymbol", out uint rightSymbol))
                RightSymbolColor.PackedValue = rightSymbol;

            if (node.TryGet("leftArrow", out uint leftArrow))
                LeftArrowColor.PackedValue = leftArrow;

            if (node.TryGet("rightArrow", out uint rightArrow))
                RightArrowColor.PackedValue = rightArrow;
        }
    }
}
