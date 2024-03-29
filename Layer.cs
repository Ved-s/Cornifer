﻿using Cornifer.MapObjects;
using Cornifer.Renderers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer
{
    public class Layer
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public virtual bool Special { get; set; }
        public bool Visible = true;
        public bool DefaultVisibility = true;

        public Layer(string id, string name, bool special, bool defaultVisibility)
        {
            Id = id;
            Name = name;
            Special = special;
            Visible = defaultVisibility;
            DefaultVisibility = defaultVisibility;
        }

        public virtual void Update() { }

        public virtual void DrawShade(Renderer renderer, Predicate<MapObject>? predicate = null)
        {
            foreach (MapObject obj in Main.WorldObjectLists)
                if (predicate?.Invoke(obj) is null or true)
                    obj.DrawShade(renderer, this);
        }

        public virtual void Draw(Renderer renderer, Predicate<MapObject>? predicate = null) 
        {
            foreach (MapObject obj in Main.WorldObjectLists)
                if (predicate?.Invoke(obj) is null or true)
                    obj.Draw(renderer, this);
        }

        public virtual void DrawGuides(Renderer renderer) { }
    }

    public class ConnectionsLayer : Layer 
    {
        public bool InRoomConnections { get; }

        public override bool Special => true;

        public ConnectionsLayer(bool inRoomConnections, bool defaultVisibility) : base(
            inRoomConnections ? "inroomconnections" : "connections",
            inRoomConnections ? "In-Room Connections" : "Connections",
            true, defaultVisibility)
        {
            InRoomConnections = inRoomConnections;
        }

        public override void DrawShade(Renderer renderer, Predicate<MapObject>? predicate = null)
        {
            Main.Region?.Connections?.DrawShadows(renderer, !InRoomConnections, InRoomConnections, predicate);
        }

        public override void Draw(Renderer renderer, Predicate<MapObject>? predicate = null)
        {
            Main.Region?.Connections?.DrawConnections(renderer, true,  !InRoomConnections, InRoomConnections, predicate);
            Main.Region?.Connections?.DrawConnections(renderer, false, !InRoomConnections, InRoomConnections, predicate);
        }

        public override void DrawGuides(Renderer renderer)
        {
            Main.Region?.Connections?.DrawGuideLines(renderer, !InRoomConnections, InRoomConnections);
        }
    }
}
