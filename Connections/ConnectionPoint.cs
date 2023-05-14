using Cornifer.Json;
using Cornifer.MapObjects;
using Cornifer.Renderers;
using Cornifer.Structures;
using Cornifer.UI.Elements;
using Microsoft.Xna.Framework;
using System.Text.Json.Nodes;

namespace Cornifer.Connections
{
    public class ConnectionPoint : MapObject
    {
        public override string? Name => $"Connection_{Connection.Source.Name}_{Connection.Destination.Name}_{Connection.Points.IndexOf(this)}";

        public override bool CanSetActive => false;

        public override bool Active => Connection.Active;
        public override bool LoadCreationForbidden => true;
        public override bool NeedsSaving => false;

        public override bool AllowModifyLayer => false;

        protected override Layer DefaultLayer => Connection?.IsInRoomShortcut is true ? Main.InRoomConnectionsLayer : Main.ConnectionsLayer;

        public override Vector2 VisualOffset => -(VisualSize - Vector2.One) / 2;
        public override Vector2 VisualSize => new(13);

        public Connection Connection = null!;

        public ObjectProperty<bool> SkipPixelBefore = new("skipBefore", false);
        public ObjectProperty<bool> SkipPixelAfter = new("skipAfter", false);
        public ObjectProperty<bool> NoShadow = new("noShadow", false);

        public ConnectionPoint() 
        {
        }
        public ConnectionPoint(Connection connection)
        {
            Connection = connection;

            if (connection.IsInRoomShortcut)
                NoShadow.OriginalValue = true;
        }

        public JsonNode SaveJson()
        {
            return new JsonObject
            {
                ["x"] = ParentPosition.X,
                ["y"] = ParentPosition.Y,
            }.SaveProperty(SkipPixelBefore)
            .SaveProperty(SkipPixelAfter)
            .SaveProperty(NoShadow);
        }

        public void LoadJson(JsonNode node)
        {
            ParentPosition = JsonTypes.LoadVector2(node);
            SkipPixelBefore.LoadFromJson(node);
            SkipPixelAfter.LoadFromJson(node);
            NoShadow.LoadFromJson(node);
        }

        protected override void DrawSelf(Renderer renderer) { }

        protected override void BuildInnerConfig(UIList list)
        {
            list.Elements.Add(new UIButton
            {
                Text = "Skip pixel before",
                Height = 20,

                Selectable = true,
                Selected = SkipPixelBefore.Value,

                SelectedTextColor = Color.Black,
                SelectedBackColor = Color.White,

            }.OnEvent(UIElement.ClickEvent, (btn, _) => SkipPixelBefore.Value = btn.Selected));
            list.Elements.Add(new UIButton
            {
                Text = "Skip pixel after",
                Height = 20,

                Selectable = true,
                Selected = SkipPixelAfter.Value,

                SelectedTextColor = Color.Black,
                SelectedBackColor = Color.White,

            }.OnEvent(UIElement.ClickEvent, (btn, _) => SkipPixelAfter.Value = btn.Selected));
            list.Elements.Add(new UIButton
            {
                Text = "Disable shadow",
                Height = 20,

                Selectable = true,
                Selected = NoShadow.Value,

                SelectedTextColor = Color.Black,
                SelectedBackColor = Color.White,

            }.OnEvent(UIElement.ClickEvent, (btn, _) => NoShadow.Value = btn.Selected));

            Connection.BuildConfig(list);
        }

        public override string ToString()
        {
            return $"Point {Connection.Points.IndexOf(this)} in {Connection}";
        }
    }

}
