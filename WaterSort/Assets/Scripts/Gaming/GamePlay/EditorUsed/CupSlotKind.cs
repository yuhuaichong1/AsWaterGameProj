using XrCode;

namespace AsGame.Data
{
    public enum CupSlotKind
    {
        广告瓶 = 0,
        锁瓶 = 1,
        空槽 = 2,
        空瓶 = 3,
        普通瓶 = 4
    }

    public static class CupSlotKindUtility
    {
        public static readonly string[] EditorToolbarLabels =
        {
            "广告瓶", "锁瓶", "空槽", "空瓶", "普通瓶"
        };

        public static CupSlotKind GetKind(CupData cup)
        {
            if (cup == null) return CupSlotKind.普通瓶;
            if (cup.isNull != 0) return CupSlotKind.空槽;
            if (cup.isVideo != 0) return CupSlotKind.广告瓶;
            if (cup.isLock != 0) return CupSlotKind.锁瓶;
            if (cup.isEmptyCup != 0) return CupSlotKind.空瓶;
            return CupSlotKind.普通瓶;
        }

        public static void SetKind(CupData cup, CupSlotKind kind)
        {
            if (cup == null) return;

            cup.isNull = 0;
            cup.isVideo = 0;
            cup.isLock = 0;
            cup.isEmptyCup = 0;
            cup.lockColor = 0;
            cup.lockNums = 0;

            switch (kind)
            {
                case CupSlotKind.空槽:
                    cup.isNull = 1;
                    cup.colors?.Clear();
                    cup.whNums = 0;
                    break;
                case CupSlotKind.广告瓶:
                    cup.isVideo = 1;
                    break;
                case CupSlotKind.锁瓶:
                    cup.isLock = 1;
                    break;
                case CupSlotKind.空瓶:
                    cup.isEmptyCup = 1;
                    cup.colors?.Clear();
                    cup.whNums = 0;
                    break;
            }
        }

        public static string GetKindShortTag(CupData cup) => GetKind(cup) switch
        {
            CupSlotKind.空槽 => "空槽",
            CupSlotKind.广告瓶 => "广告",
            CupSlotKind.锁瓶 => "锁",
            CupSlotKind.空瓶 => "空瓶",
            _ => ""
        };

        public static bool ParticipatesInWaterRefresh(CupData cup) =>
            cup != null && cup.isNull == 0 && cup.isVideo == 0 && cup.isEmptyCup == 0;

        public static bool ShowInLevelPreview(CupData cup) =>
            cup != null && cup.isNull == 0;
    }
}
