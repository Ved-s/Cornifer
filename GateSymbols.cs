using Cornifer.Renderers;
using Cornifer.UI.Elements;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public ObjectProperty<Color> SplitterColor    = new("splitter", Color.White);
        public ObjectProperty<Color> LeftSymbolColor  = new("leftSymbol", Color.White);
        public ObjectProperty<Color> RightSymbolColor = new("rightSymbol", Color.White);
        public ObjectProperty<Color> LeftArrowColor   = new("leftArrow", Color.White);
        public ObjectProperty<Color> RightArrowColor  = new("rightArrow", Color.White);

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
            renderer.DrawTexture(Main.Pixel, center - splitterSize/2, null, splitterSize, SplitterColor.Value);

            if (LeftSymbolSprite is not null)
            {
                Vector2 spriteSize = LeftSymbolSprite.Frame.Size.ToVector2();

                Vector2 spritePos = WorldPosition + new Vector2(Size.X / 2 - 15.5f - spriteSize.X, Size.Y - 21 - spriteSize.Y / 2);
                renderer.DrawTexture(LeftSymbolSprite.Texture, spritePos, LeftSymbolSprite.Frame, null, LeftSymbolColor.Value);
            }

            if (RighSymbolSprite is not null)
            {
                Vector2 spriteSize = RighSymbolSprite.Frame.Size.ToVector2();

                Vector2 spritePos = WorldPosition + new Vector2(Size.X / 2 + 15.5f, Size.Y - 21 - spriteSize.Y / 2);
                renderer.DrawTexture(RighSymbolSprite.Texture, spritePos, RighSymbolSprite.Frame, null, RightSymbolColor.Value);
            }

            if (LeftArrowSprite is not null)
            {
                Vector2 spritePos = WorldPosition + new Vector2(Size.X / 2 + 22.5f, 0);
                renderer.DrawTexture(LeftArrowSprite.Texture, spritePos, LeftArrowSprite.Frame, null, RightArrowColor.Value);
            }

            if (RightArrowSprite is not null)
            {
                Vector2 spriteSize = RightArrowSprite.Frame.Size.ToVector2();

                Vector2 spritePos = WorldPosition + new Vector2(Size.X / 2 - 22.5f - spriteSize.X, 0);
                renderer.DrawTexture(RightArrowSprite.Texture, spritePos, RightArrowSprite.Frame, null, LeftArrowColor.Value);
            }
        }

        protected override void BuildInnerConfig(UIList list)
        {
            list.Elements.Add(new UIButton
            {
                Text = "Set splitter color",
                Height = 20,
            }.OnEvent(UIElement.ClickEvent, (btn, _) => Interface.ColorSelector.Show("Splitter color", SplitterColor.Value, (_, c) => SplitterColor.Value = c)));

            list.Elements.Add(new UIButton
            {
                Text = "Set left symbol color",
                Height = 20,
            }.OnEvent(UIElement.ClickEvent, (btn, _) => Interface.ColorSelector.Show("Left symbol color", LeftSymbolColor.Value, (_, c) => LeftSymbolColor.Value = c)));

            list.Elements.Add(new UIButton
            {
                Text = "Set right symbol color",
                Height = 20,
            }.OnEvent(UIElement.ClickEvent, (btn, _) => Interface.ColorSelector.Show("Right symbol color", RightSymbolColor.Value, (_, c) => RightSymbolColor.Value = c)));

            list.Elements.Add(new UIButton
            {
                Text = "Set left arrow color",
                Height = 20,
            }.OnEvent(UIElement.ClickEvent, (btn, _) => Interface.ColorSelector.Show("Left arrow color", LeftArrowColor.Value, (_, c) => LeftArrowColor.Value = c)));

            list.Elements.Add(new UIButton
            {
                Text = "Set right arrow color",
                Height = 20,
            }.OnEvent(UIElement.ClickEvent, (btn, _) => Interface.ColorSelector.Show("Right arrow color", RightArrowColor.Value, (_, c) => RightArrowColor.Value = c)));
        }

        protected override JsonNode? SaveInnerJson()
        {
            return new JsonObject()
                .SaveProperty(SplitterColor)
                .SaveProperty(LeftSymbolColor)
                .SaveProperty(RightSymbolColor)
                .SaveProperty(LeftArrowColor)
                .SaveProperty(RightArrowColor);
        }

        protected override void LoadInnerJson(JsonNode node)
        {
            SplitterColor.LoadFromJson(node);
            LeftSymbolColor.LoadFromJson(node);
            RightSymbolColor.LoadFromJson(node);
            LeftArrowColor.LoadFromJson(node);
            RightArrowColor.LoadFromJson(node);
        }
    }
}
