using System;

[Serializable]
public struct PayoutEntryKey : IEquatable<PayoutEntryKey>
{
    public EPayType Channel;
    public int TierId;

    public PayoutEntryKey(EPayType channel, int tierId)
    {
        Channel = channel;
        TierId = tierId;
    }

    public string StorageKey => $"{(int)Channel}_{TierId}";

    public bool Equals(PayoutEntryKey other) => Channel == other.Channel && TierId == other.TierId;

    public override bool Equals(object obj) => obj is PayoutEntryKey other && Equals(other);

    public override int GetHashCode() => ((int)Channel * 397) ^ TierId;

    public static bool TryParse(string storageKey, out PayoutEntryKey key)
    {
        key = default;
        if (string.IsNullOrEmpty(storageKey)) return false;
        var parts = storageKey.Split('_');
        if (parts.Length != 2) return false;
        if (!int.TryParse(parts[0], out int channel)) return false;
        if (!int.TryParse(parts[1], out int tierId)) return false;
        key = new PayoutEntryKey((EPayType)channel, tierId);
        return true;
    }
}
