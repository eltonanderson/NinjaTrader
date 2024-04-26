using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Code;
using NTRes.NinjaTrader.Gui.NinjaScript.OutputWindow;

namespace NinjaTrader.NinjaScript.OptimizationFitnesses
{
    public class TradeStationIndex : OptimizationFitness
    {

        private const int MinimumTrades = 30;
		private const int MaximunTrades = 120;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
                Name = "Trade Station Index";
        }

        protected override void OnCalculatePerformanceValue(StrategyBase strategy)
        {

            try
            {

                var dd = Math.Abs(GetDrawDown(strategy));

                if (dd == 0)
                    dd = 1;

                var coef = GetNetProfit(strategy) * GetWins(strategy) / dd;

                if (double.IsInfinity(coef) || double.IsNaN(coef) || double.IsPositiveInfinity(coef) || double.IsNegativeInfinity(coef))
                    coef = 0;


                if (strategy.SystemPerformance.AllTrades.Count < MinimumTrades 
					|| strategy.SystemPerformance.AllTrades.Count > MaximunTrades 
					|| strategy.SystemPerformance.AllTrades.TradesPerformance.ProfitFactor < 2.0
					//|| (double)strategy.SystemPerformance.AllTrades.WinningTrades.TradesCount / strategy.SystemPerformance.AllTrades.TradesCount < 50.0
					)
                    coef = 10;
				else 
					Value = coef * strategy.SystemPerformance.AllTrades.TradesPerformance.ProfitFactor;
                //Value = coef;

                
            }
            catch (Exception e)
            {
                Print("Somehting crashed in the optimization algo" + e);
                Output.Process("Somehting crashed in the optimization algo" + e, PrintTo.OutputTab2);
            }
            
        }



        private double GetNetProfit(StrategyBase strategy)
        {
            return strategy.SystemPerformance.AllTrades.TradesPerformance.NetProfit;
        }

        private double GetDrawDown(StrategyBase strategy)
        {
            return strategy.SystemPerformance.AllTrades.TradesPerformance.Currency.Drawdown;
        }

        private int GetWins(StrategyBase strategy)
        {
            return strategy.SystemPerformance.AllTrades.WinningTrades.TradesCount;
        }
    }
}
