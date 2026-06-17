using System;
using System.Collections.Generic;
using System.Linq;

namespace Client.Services.Fishing;

internal sealed class TranquilityController
{
    private const double HitYMin = 0.78;
    private const double HitYMax = 0.90;
    private const long KeyCooldownMs = 30;

    private readonly ReelLocator locator;
    private readonly HashSet<ulong> hitNotes = new();
    private readonly Dictionary<string, long> lastKeySentAt = new();

    public TranquilityController(ReelLocator locator)
    {
        this.locator = locator;
    }

    public void Reset()
    {
        global::MouseInput.LeftUp();
        hitNotes.Clear();
        lastKeySentAt.Clear();
        DebugLog.Write("Tranquility.Reset", "cleared");
    }

    public void Update()
    {
        global::MouseInput.LeftUp();
        DebugLog.Write("Tranquility.Update", "tick");

        var root = locator.GetTranquilityRoot();
        if (root == 0)
        {
            DebugLog.Write("Tranquility.Update", "no root");
            return;
        }

        var container = locator.GetTranquilityLaneContainer(root);
        if (container == 0)
        {
            DebugLog.Write("Tranquility.Update", "no container");
            return;
        }

        var seen = new HashSet<ulong>();
        for (var index = 1; index <= 4; index++)
        {
            var lane = locator.GetTranquilityLane(container, index);
            if (lane == 0)
            {
                DebugLog.Write("Tranquility.Update", $"lane={index} missing");
                continue;
            }

            var key = locator.GetTranquilityLaneKey(root, lane, index);
            if (key.Length == 0)
            {
                DebugLog.Write("Tranquility.Update", $"lane={index} key=missing");
                continue;
            }

            DebugLog.Write("Tranquility.Update", $"lane={index} key={key} notes={locator.ReadChildren(lane).Count}");

            foreach (var noteAddr in locator.ReadChildren(lane))
            {
                if (!string.Equals(locator.ReadName(noteAddr), "Note", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(locator.ReadClass(noteAddr), "ImageLabel", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                seen.Add(noteAddr);
                if (hitNotes.Contains(noteAddr))
                {
                    continue;
                }

                if (!locator.IsVisible(noteAddr))
                {
                    DebugLog.Write("Tranquility.Update", $"lane={index} note={noteAddr:X} hidden");
                    continue;
                }

				var sy = locator.ReadFramePositionScale(noteAddr).Y;
                DebugLog.Write("Tranquility.Update", $"lane={index} note={noteAddr:X} sy={sy:0.000}");
                if (sy >= HitYMin && sy <= HitYMax)
                {
                    PressLaneKey(key, noteAddr);
                }
            }
        }

        var stale = hitNotes.Where(note => !seen.Contains(note)).ToList();
        foreach (var note in stale)
        {
            hitNotes.Remove(note);
        }
    }

    private void PressLaneKey(string key, ulong noteAddr)
    {
        var now = Environment.TickCount64;
        if (lastKeySentAt.TryGetValue(key, out var lastSentAt) &&
            lastSentAt != 0 && now - lastSentAt < KeyCooldownMs)
        {
            DebugLog.Write("Tranquility.PressLaneKey", $"key={key} note={noteAddr:X} skipped=cooldown remaining={KeyCooldownMs - (now - lastSentAt)}ms");
            return;
        }

		DebugLog.Write("Tranquility.PressLaneKey", $"key={key} note={noteAddr:X} sending");
		NativeKeyboard.PressKey(key[0]);
		lastKeySentAt[key] = now;
		hitNotes.Add(noteAddr);
		DebugLog.Write("Tranquility.PressLaneKey", $"key={key} note={noteAddr:X} sent");
	}
}
