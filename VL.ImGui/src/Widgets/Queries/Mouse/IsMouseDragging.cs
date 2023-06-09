﻿namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Is mouse dragging? (if lock_threshold &lt; -1.0f, uses io.MouseDraggingThreshold)
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries")]
    internal partial class IsMouseDragging : Widget
    {

        public ImGuiNET.ImGuiMouseButton Flags { private get; set; }

        public float Threshold { private get; set; } = -2.0f;

        public bool Value { get; private set; }

        protected override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.IsMouseDragging(Flags, Threshold);
        }
    }
}
