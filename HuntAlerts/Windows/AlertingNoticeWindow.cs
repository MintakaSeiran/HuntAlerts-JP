using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace HuntAlerts.Windows;

public class AlertingNoticeWindow : Window
{
    public AlertingNoticeWindow() : base("HuntAlerts - Notice", ImGuiWindowFlags.NoCollapse)
    {
        Size            = new Vector2(480, 360);
        SizeCondition   = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(380, 240),
            MaximumSize = new Vector2(900, 1200),
        };
    }

    public override void Draw()
    {
        Components.Icon(FontAwesomeIcon.Users, Theme.NoticeGoodText);
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Text, Theme.NoticeGoodText);
        ImGui.TextUnformatted("Alerting Restored");
        ImGui.PopStyleColor();
        ImGui.Separator();
        ImGui.Spacing();

        var footerHeight = ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y * 2;
        var midHeight    = System.Math.Max(60f, ImGui.GetContentRegionAvail().Y - footerHeight);

        if (ImGui.BeginChild("##noticeBody", new Vector2(0, midHeight), false))
        {
            ImGui.PushTextWrapPos();

            ImGui.PushStyleColor(ImGuiCol.Text, Theme.NoticeGoodText);
            ImGui.TextUnformatted(AlertNotice.Headline);
            ImGui.PopStyleColor();
            ImGui.Spacing();

            ImGui.PushStyleColor(ImGuiCol.Text, Theme.Text);
            ImGui.TextUnformatted(AlertNotice.Body);
            ImGui.PopStyleColor();

            ImGui.PopTextWrapPos();
        }
        ImGui.EndChild();

        ImGui.Separator();
        if (Components.ActionButton(FontAwesomeIcon.Check, "Understood", ButtonRole.Success))
            IsOpen = false;
    }
}
