namespace HD2_Helper;

internal static class HangulInputPolicy
{
    // Low-level hooks report left/right modifier virtual keys (A0-A5).
    // Pressing or releasing Shift must not commit the syllable before ㄲ/ㅆ.
    internal static bool PreservesComposition(uint key) =>
        key is >= 0x10 and <= 0x12 or >= 0xA0 and <= 0xA5 or 0x09;
}
