﻿namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Width of item given pushed settings and current cursor position. NOT necessarily the width of last item unlike most 'Item' functions.
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class CalcItemWidth : Widget
    {

        public float Value { get; private set; }

        protected override void UpdateCore(Context context)
        {
            Value = ImGuiNET.ImGui.CalcItemWidth().ToVLHecto();
        }
    }
}
