internal sealed record ReelContext(ulong Reel, ulong Bar, ulong Fish, ulong Playerbar);

internal sealed record OrderedReelContext(ReelContext Context, double BarX);
