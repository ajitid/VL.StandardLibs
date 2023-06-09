﻿namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Submit one header cell manually (rarely used)
    /// </summary>
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal partial class TableHeader : Widget
    {

        public string? Label { private get; set; }

        protected override void UpdateCore(Context context)
        {
            if (context.IsInBeginTables)
                ImGuiNET.ImGui.TableHeader(Context.GetLabel(this, Label));
        }
    }
}