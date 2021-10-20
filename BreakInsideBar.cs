using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class BreakInsideBar : Robot
    {
        [Parameter("Lot Size", DefaultValue = 0.2, MinValue = 0.1, Step = 0.1)]
        public double Quantity { get; set; }

        private AverageTrueRange atr;
        private LeastSquares lsq;

        protected override void OnStart()
        {
            atr = Indicators.AverageTrueRange(8, MovingAverageType.Exponential);
            lsq = Indicators.GetIndicator<LeastSquares>(89);
            PendingOrders.Created += OnPendingOrdersCreated;
            PendingOrders.Filled += OnPendingOrdersFilled;
            Positions.Closed += OnPositionsClosed;
        }

        protected override void OnBar()
        {
            if (IsPositionOpenByType(TradeType.Buy) || IsPositionOpenByType(TradeType.Sell))
            {
                TrailSL();
            }
            else
                InsideBarSetup();
        }
        // = = = = = = = = = = = = = = = = = = = = = = = =
        // MANAGE INSIDE BAR RECOGNITION
        private void InsideBarSetup()
        {
            // variable declaration
            var motherHigh = Bars.HighPrices.Last(2);
            var motherLow = Bars.LowPrices.Last(2);
            var motherOpen = Bars.OpenPrices.Last(2);
            var motherClose = Bars.ClosePrices.Last(2);

            var childHigh = Bars.HighPrices.Last(1);
            var childLow = Bars.LowPrices.Last(1);
            var childOpen = Bars.OpenPrices.Last(1);
            var childClose = Bars.ClosePrices.Last(1);
            if (PendingOrders.Count == 0 && !IsPositionOpenByType(TradeType.Buy) && !IsPositionOpenByType(TradeType.Sell))
            {
                // body size at least 50% of the whole candle
                if ((Math.Abs(motherClose - motherOpen) / Math.Abs(motherHigh - motherClose)) > 0.5 && (Math.Abs(childClose - childOpen) / Math.Abs(childHigh - childLow)) > 0.5)
                {
                    // mother greater than the child + child within mother's range + mother at least 9 pips
                    if (motherHigh > childHigh && motherLow < childLow && Math.Abs(motherOpen - motherClose) > Math.Abs(childOpen - childClose) && Math.Abs(motherHigh - motherLow) >= 9 * Symbol.PipSize)
                    {
                        PlaceStop();
                    }
                }
            }
        }

        // = = = = = = = = = = = = = = = = = = = = = = = =
        // PRINT LOG msg when PENDING CREATED
        void OnPendingOrdersCreated(PendingOrderCreatedEventArgs obj)
        {
            var price = obj.PendingOrder.TargetPrice;
            Print("CREATED PENDING AT price - ", price);
        }

        // = = = = = = = = = = = = = = = = = = = = = = = =
        // PRINT LOG msg when ORDER FILLED and SET SL/TP
        void OnPendingOrdersFilled(PendingOrderFilledEventArgs obj)
        {
            if (IsPositionOpenByType(TradeType.Buy))
            {
                var buyPositions = Positions.FindAll("InsideBar", SymbolName, TradeType.Buy);
                foreach (Position position in buyPositions)
                    if (position.StopLoss == null && position.TakeProfit == null)
                    {
                        double minSL = Bars.LowPrices.LastValue;
                        for (int i = 0; i < 5; i++)
                        {
                            double last = Bars.LowPrices.Last(i);
                            if (last < minSL)
                            {
                                minSL = last;
                            }
                        }
                        ModifyPosition(position, minSL, position.EntryPrice + 4 * GetAverageATR());
                        Print("OPENED BUY - - SL: {0} TP: {1}", position.StopLoss.Value, position.TakeProfit.Value);
                        Print("SL DIST.: {0} TP DIST.: {1}", Math.Round((position.StopLoss.Value - position.EntryPrice), Symbol.Digits), Math.Round((position.TakeProfit.Value - position.EntryPrice), Symbol.Digits));
                    }
            }
            if (IsPositionOpenByType(TradeType.Sell))
            {
                var sellPositions = Positions.FindAll("InsideBar", SymbolName, TradeType.Sell);
                foreach (Position position in sellPositions)
                    if (position.StopLoss == null && position.TakeProfit == null)
                    {
                        double maxSL = Bars.HighPrices.LastValue;
                        for (int i = 0; i < 5; i++)
                        {
                            double last = Bars.HighPrices.Last(i);
                            if (last > maxSL)
                            {
                                maxSL = last;
                            }
                        }
                        ModifyPosition(position, maxSL, position.EntryPrice - 4 * GetAverageATR());
                        Print("OPENED SELL - - SL: {0} TP: {1}", position.StopLoss.Value, position.TakeProfit.Value);
                        Print("SL DIST.: {0} TP DIST.: {1}", Math.Round((position.EntryPrice - position.StopLoss.Value), Symbol.Digits), Math.Round((position.EntryPrice - position.TakeProfit.Value), Symbol.Digits));
                    }
            }
        }

        // = = = = = = = = = = = = = = = = = = = = = = = =
        // MANAGING SL + TP
        private void TrailSL()
        {
            if (IsPositionOpenByType(TradeType.Buy))
            {
                var buyPositions = Positions.FindAll("InsideBar", SymbolName, TradeType.Buy);
                foreach (Position position in buyPositions)
                    if (position.StopLoss != null && position.Pips > 20)
                    {
                        double distance = Symbol.Bid - position.EntryPrice;
                        double newBuyTP = Math.Round(5 * GetAverageATR() * Symbol.PipSize, Symbol.Digits);
                        if (position.EntryPrice + distance > position.StopLoss)
                        {
                            ModifyPosition(position, position.EntryPrice + 0.1 * distance * Symbol.PipSize, Symbol.Ask + newBuyTP);
                            Print("NEW SL/TP {0} {1}", position.StopLoss.Value, position.TakeProfit.Value);
                        }
                    }
            }

            if (IsPositionOpenByType(TradeType.Sell))
            {
                var sellPositions = Positions.FindAll("InsideBar", SymbolName, TradeType.Sell);
                foreach (Position position in sellPositions)
                    if (position.StopLoss != null && position.Pips > 20)
                    {
                        double distance = position.EntryPrice - Symbol.Bid;
                        double newSellTP = Math.Round(5 * GetAverageATR() * Symbol.PipSize, Symbol.Digits);
                        if (position.EntryPrice - distance < position.StopLoss)
                        {
                            ModifyPosition(position, position.EntryPrice - 0.1 * distance * Symbol.PipSize, Symbol.Bid - newSellTP);
                            Print("NEW SL/TP {0} {1}", position.StopLoss.Value, position.TakeProfit.Value);
                        }
                    }
            }
        }

        // = = = = = = = = = = = = = = = = = = = = = = = =
        // GET last ATR results to have a reliable MARKET MOVEMENT RANGE
        private double GetAverageATR()
        {
            double avgAtr = 0;
            double sumAtr = 0;
            for (int i = 1; i < 10; i++)
            {
                sumAtr += atr.Result.Last(i);
            }
            avgAtr = Math.Round(sumAtr / 10, Symbol.Digits);
            return avgAtr;
        }

        // = = = = = = = = = = = = = = = = = = = = = = = =
        // PRINT LOG msg when POSITION CLOSED
        void OnPositionsClosed(PositionClosedEventArgs obj)
        {
            Print("CLOSED - Net Result:", LastResult.Position.NetProfit);
        }

        // = = = = = = = = = = = = = = = = = = = = = = = =
        // check if any OPENED positions
        private bool IsPositionOpenByType(TradeType type)
        {
            var p = Positions.FindAll("InsideBar", SymbolName, type);
            if (p.Count() >= 1)
            {
                return true;
            }
            return false;
        }

        // = = = = = = = = = = = = = = = = = = = = = = = =
        // request to place BUY STOP + SELL STOP
        private void PlaceStop()
        {
            var LotSize = Symbol.QuantityToVolumeInUnits(Quantity);
            double insideHigh = Bars.HighPrices.Last(2) + 1 * Symbol.PipSize;
            double insidelow = Bars.LowPrices.Last(2) - 1 * Symbol.PipSize;
            if (Bars.ClosePrices.Last(1) > lsq.irrlse.Last(1))
            {
                PlaceStopOrder(TradeType.Buy, SymbolName, LotSize, insideHigh, "InsideBar", null, null, Server.Time.AddHours(13));
            }
            if (Bars.ClosePrices.Last(1) < lsq.irrlse.Last(1))
            {
                PlaceStopOrder(TradeType.Sell, SymbolName, LotSize, insidelow, "InsideBar", null, null, Server.Time.AddHours(13));
            }
        }
    }
}
