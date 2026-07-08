namespace HuntAlerts.Windows;

internal static class AlertNotice
{
    // Bump this whenever the notice text changes so the popup shows again for
    // everyone (Configuration.AlertingNoticeVersion is compared against it).
    public const int Version = 2;

    public const string Headline = "The plugin is back up and fully operational.";

    public const string Body =
        "Good news: the data feed is live again, and hunt train and S-rank alerts are once more coming through in-game just like before.\n\n" +
        "Aether local trains will not be relayed. This is in part to reduce the stress of new conductors. Cross world trains will still be coming through.\n\n" +
        "Thank you to everyone who expressed their support and to the community members who have stepped up to provide crowdsourced data feeds.";
}
