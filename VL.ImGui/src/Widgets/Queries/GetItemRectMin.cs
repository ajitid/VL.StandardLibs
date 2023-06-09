﻿using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Get upper-left bounding rectangle of the last item (screen space)
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class GetItemRectMin : Widget
    {

        public Vector2 Value { get; private set; }

        protected override void UpdateCore(Context context)
        {
            var size = ImGuiNET.ImGui.GetItemRectMin();
            Value = ImGuiConversion.ToVLHecto(size);
        }
    }
}
