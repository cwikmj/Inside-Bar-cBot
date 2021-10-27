using System;
using cAlgo.API;
using cAlgo.API.Internals;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class InsideBar : Indicator
    {
        [Output("Inside Up", LineColor = "Cornsilk", PlotType = PlotType.Points, Thickness = 5)]
        public IndicatorDataSeries Above { get; set; }
        [Output("Inside Down", LineColor = "Cornsilk", PlotType = PlotType.Points, Thickness = 5)]
        public IndicatorDataSeries Below { get; set; }

        public override void Calculate(int index)
        {
            var motherHigh = Bars.HighPrices.Last(2);
            var motherLow = Bars.LowPrices.Last(2);
            var motherOpen = Bars.OpenPrices.Last(2);
            var motherClose = Bars.ClosePrices.Last(2);

            var childHigh = Bars.HighPrices.Last(1);
            var childLow = Bars.LowPrices.Last(1);
            var childOpen = Bars.OpenPrices.Last(1);
            var childClose = Bars.ClosePrices.Last(1);

            if (childHigh < motherHigh && childLow > motherLow && Math.Abs(motherOpen - motherClose) > Math.Abs(childOpen - childClose))
            {
                if ((Math.Abs(motherClose - motherOpen) / Math.Abs(motherHigh - motherClose)) > 0.5 && (Math.Abs(childClose - childOpen) / Math.Abs(childHigh - childLow)) > 0.5)
                {
                    DrawPoint(index);
                }
            }
        }

        private void DrawPoint(int index)
        {
            Above[index - 2] = Bars.HighPrices[index - 2] + 0.001;
            Below[index - 2] = Bars.LowPrices[index - 2] - 0.001;
        }
    }
}
