﻿namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Is any item focused?
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class IsAnyItemFocused : Query
    {

        public bool Value { get; private set; }

        internal override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.IsAnyItemFocused();
        }
    }
}
