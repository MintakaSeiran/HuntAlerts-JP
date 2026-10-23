using System;
using System.Linq;
using Dalamud.Game.Chat;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.DalamudServices;
using ECommons.IPC;
using ECommons.Logging;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HuntAlerts.Services;

namespace HuntAlerts.Helpers;

internal static class TravelRetarget
{
    private static string source = "", destination = "";
    private static TravelRetargetGate? gate;
    private static HuntTrainMessage? pending;
    internal static bool IsPending => gate != null;

    internal static void Enable()
    {
        Svc.Framework.Update += Update;
        Svc.Chat.ChatMessage += OnChat;
    }

    internal static void Disable()
    {
        Svc.Framework.Update -= Update;
        Svc.Chat.ChatMessage -= OnChat;
        Utilities.CancelTeleport();
        Clear();
    }

    internal static void Track(string from, string to)
    {
        source = from;
        destination = to;
    }

    internal static void EndTravel()
    {
        if (!IsPending) source = destination = "";
    }

    private static string CurrentWorld => Svc.Objects.LocalPlayer?.CurrentWorld.ValueNullable?.Name.ExtractText() ?? "";
    private static bool Loading => !Svc.ClientState.IsLoggedIn || Svc.Objects.LocalPlayer == null
        || Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51];

    private static bool SameDc(string a, string b) => WorldData.TryGetWorld(a, out var first)
        && WorldData.TryGetWorld(b, out var second) && !string.IsNullOrEmpty(first.Datacenter)
        && first.Datacenter == second.Datacenter;

    internal static void Request(HuntTrainMessage entry)
    {
        try
        {
            if (IsPending)
            {
                if (!Loading && CurrentWorld == source && SameDc(source, entry.huntWorld))
                {
                    pending = entry;
                    Svc.Chat.Print($"取消確認後の移動先を {entry.huntWorld} / {entry.startZone} に変更しました。");
                }
                else Svc.Chat.Print("取消確認中は同じDC内の移動先だけ選択できます。");
                return;
            }
            if (!Utilities.TeleportInProgress && !ECommonsIPC.Lifestream.IsBusy()
                && !Svc.Condition[ConditionFlag.WaitingToVisitOtherWorld] && !Loading)
            {
                Start(entry);
                return;
            }
            if (!Utilities.TeleportInProgress || Loading || CurrentWorld != source
                || !SameDc(source, destination) || !SameDc(source, entry.huntWorld) || !CanCancelQueue())
            {
                Svc.Chat.Print("切替できません。HuntAlertsが開始した同一DC内の転送予約状態を開き、順番待ち中に選択してください。転送準備完了後は切替できません。");
                return;
            }
            if (Svc.ClientState.ClientLanguage != Dalamud.Game.ClientLanguage.Japanese)
            {
                Svc.Chat.Print("予約切替は現在、日本語クライアントの取消通知に対応しています。");
                return;
            }

            pending = entry;
            gate = new(Environment.TickCount64);
            Utilities.CancelTeleport();
            // Abort stops Lifestream's task queue, not the game's world-visit reservation.
            Svc.PluginInterface.GetIpcSubscriber<object>("Lifestream.Abort").InvokeAction();
            if (!CancelQueue())
            {
                Fail("転送予約状態が変化したため、切替を中止しました。");
                return;
            }
            Svc.Chat.Print("転送予約の取消を確認しています。確認後に最後に選んだモブへ移動します。");
        }
        catch (Exception ex)
        {
            PluginLog.Error($"[TravelRetarget] {ex}");
            Fail("移動切替に失敗しました。転送予約とLifestreamの状態を手動で確認してください。");
        }
    }

    private static unsafe bool CanCancelQueue()
    {
        if (!Svc.Condition[ConditionFlag.WaitingToVisitOtherWorld]
            || !GenericHelpers.TryGetAddonByName<AtkUnitBase>("WorldTravelFinderStatus", out var addon)
            || !GenericHelpers.IsAddonReady(addon) || !addon->IsVisible || addon->AtkValuesCount < 6) return false;
        // Reject unexpected layouts before interpreting union members as strings.
        for (var i = 1; i <= 3; i++)
            if ((addon->AtkValues[i].Type != AtkValueType.String && addon->AtkValues[i].Type != AtkValueType.ManagedString)
                || addon->AtkValues[i].String.Value == null) return false;
        var master = new AddonMaster.WorldTravelFinderStatus(addon);
        var button = master.CancelButton;
        return master.StartingWorldString == source && master.DestinationWorldString == destination
            && master.PositionInQueueString.Any(c => char.IsDigit(c) && c != '0')
            && button != null && button->IsEnabled && button->AtkResNode != null && button->AtkResNode->IsVisible();
    }

    private static unsafe bool CancelQueue()
    {
        if (Loading || CurrentWorld != source || !CanCancelQueue()) return false;
        if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("WorldTravelFinderStatus", out var addon)) return false;
        new AddonMaster.WorldTravelFinderStatus(addon).Cancel();
        return true;
    }

    private static unsafe bool QueueVisible() =>
        GenericHelpers.TryGetAddonByName<AtkUnitBase>("WorldTravelFinderStatus", out var addon) && addon->IsVisible;

    private static void OnChat(IChatMessage message)
    {
        if (message.LogKind == XivChatType.SystemMessage
            && string.IsNullOrEmpty(message.OriginalSender.ExtractText())
            && message.OriginalMessage.ExtractText().Trim() == "申請が取り消されました。")
        {
            if (gate != null) gate.Acknowledge();
            else if (!string.IsNullOrEmpty(source))
            {
                // A manual cancellation must not leave a stale request eligible for retargeting.
                Utilities.CancelTeleport();
                source = destination = "";
            }
        }
    }

    private static void Update(IFramework framework)
    {
        if (gate == null) return;
        try
        {
            if (!HuntAlerts.C.LifestreamIntegration || !ECommonsIPC.Lifestream.Available)
            {
                Fail("Lifestream連携が無効になったため、移動切替を中止しました。");
                return;
            }
            var result = gate.Evaluate(Environment.TickCount64, CurrentWorld == source, Loading,
                QueueVisible(), Svc.Condition[ConditionFlag.WaitingToVisitOtherWorld],
                Utilities.TeleportInProgress, ECommonsIPC.Lifestream.IsBusy());
            if (result == RetargetResult.Failed)
                Fail("予約取消を安全に確認できなかったため再申請しません。転送予約とLifestreamを確認してください。");
            else if (result == RetargetResult.Ready)
            {
                var next = pending;
                Clear();
                if (next != null) Start(next);
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error($"[TravelRetarget] {ex}");
            Fail("予約取消の確認に失敗したため、移動切替を中止しました。");
        }
    }

    private static void Start(HuntTrainMessage entry) => Utilities.ExecuteTeleport(entry.huntWorld,
        entry.startLocation, entry.startLocationAetheryteId, entry.startZone, entry.locationCoords,
        entry.instance, entry.openmaponArrival, true);

    private static void Fail(string reason)
    {
        Clear();
        Svc.Chat.Print(reason);
    }

    private static void Clear()
    {
        gate = null;
        pending = null;
        source = destination = "";
    }
}
