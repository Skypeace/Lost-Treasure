using System;
using GoogleMobileAds.Api;

namespace EasyMobile
{
    // For easy display in the inspector
    public enum BannerAdPositionEnum
    {
        Top,
        Bottom,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public sealed class BannerAdPosition
    {
        private readonly string name;

        private BannerAdPosition(string name)
        {
            this.name = name;
        }

        public static readonly BannerAdPosition Top = new BannerAdPosition("Top");
        public static readonly BannerAdPosition Bottom = new BannerAdPosition("Bottom");
        public static readonly BannerAdPosition TopLeft = new BannerAdPosition("TopLeft");
        public static readonly BannerAdPosition TopRight = new BannerAdPosition("TopRight");
        public static readonly BannerAdPosition BottomLeft = new BannerAdPosition("BottomLeft");
        public static readonly BannerAdPosition BottomRight = new BannerAdPosition("BottomRight");

        public AdPosition ToAdMobAdPosition()
        {
            switch (name)
            {
                case "Top":
                    return AdPosition.Top;
                case "Bottom":
                    return AdPosition.Bottom;
                case "TopLeft":
                    return AdPosition.TopLeft;
                case "TopRight":
                    return AdPosition.TopRight;
                case "BottomLeft":
                    return AdPosition.BottomLeft;
                case "BottomRight":
                    return AdPosition.BottomRight;
                default:
                    return AdPosition.Top;
            }
        }

        public static BannerAdPosition FromEnumPosition(BannerAdPositionEnum enumPos)
        {
            switch (enumPos)
            {
                case BannerAdPositionEnum.Top:
                    return BannerAdPosition.Top;
                case BannerAdPositionEnum.Bottom:
                    return BannerAdPosition.Bottom;
                case BannerAdPositionEnum.TopLeft:
                    return BannerAdPosition.TopLeft;
                case BannerAdPositionEnum.TopRight:
                    return BannerAdPosition.TopRight;
                case BannerAdPositionEnum.BottomLeft:
                    return BannerAdPosition.BottomLeft;
                case BannerAdPositionEnum.BottomRight:
                    return BannerAdPosition.BottomRight;
                default:
                    return BannerAdPosition.Top;
            }
        }
    }
}

