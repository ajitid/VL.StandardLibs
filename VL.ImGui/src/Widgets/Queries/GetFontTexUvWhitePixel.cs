﻿using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Get UV coordinate for a while pixel, useful to draw custom shapes via the ImDrawList API
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class GetFontTexUvWhitePixel : Widget
    {

        public Vector2 Value { get; private set; }

        protected override void UpdateCore(Context context)
        {
            var size = ImGuiNET.ImGui.GetFontTexUvWhitePixel();
            Value = ImGuiConversion.ToVLHecto(size);
        }
    }
}
