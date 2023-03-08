using System.Text.Json.Nodes;

namespace Cornifer.Structures
{
    public class GateRoomData
    {
        public bool Swapped;

        public string? TargetRegionName;

        public string? LeftRegionId;
        public string? RightRegionId;

        public string? LeftKarma;
        public string? RightKarma;

        public JsonObject SaveJson()
        {
            return new()
            {
                ["regLeft"] = LeftRegionId,
                ["regRight"] = RightRegionId,
                ["targetName"] = TargetRegionName
            };
        }

        public void LoadJson(JsonObject obj)
        {
            if (obj.TryGet("regLeft", out string? regLeft))
                LeftRegionId = regLeft;

            if (obj.TryGet("regRight", out string? regRight))
                RightRegionId = regRight;

            if (obj.TryGet("targetName", out string? targetName))
                TargetRegionName = targetName;
        }
    }
}