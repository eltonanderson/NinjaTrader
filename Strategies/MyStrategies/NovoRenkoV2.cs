#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;

#endregion

//This namespace holds Bars types in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.BarsTypes
{
	public class NovoRenkoV2 : BarsType
	{
		double barOpen;
        double barMax;
        double barMin;
		double fakeOpen=0;

		int    barDirection=0;
		double openOffset=0;
		double trendOffset=0;
		double reversalOffset=0;

		bool   maxExceeded=false;
		bool   minExceeded=false;

		double tickSize=0.01;
		
		
		protected override void OnStateChange()
		{
						
			
			
			if (State == State.SetDefaults)
			{
				Description							= @"Modified";
				Name								= "NovoRenkoV2";
				BarsPeriod							= new BarsPeriod { BarsPeriodType = (BarsPeriodType) 501, BarsPeriodTypeName = "NovoRenkoV2(501)", Value = 1 };
				//BarsPeriod							= new BarsPeriod { BarsPeriodType = (BarsPeriodType) 501, BarsPeriodTypeName = "NovoRenkoV2(Uni)", Value = 1 };
				
				//BarsPeriod							= new BarsPeriod { BarsPeriodType = (BarsPeriodType) 501, BarsPeriodTypeName = "UniRenko", Value = 1 };
				BuiltFrom							= BarsPeriodType.Tick;
				DaysToLoad							= 3;
				IsIntraday							= true;
			}
			else if (State == State.Configure)
			{
				Properties.Remove(Properties.Find("BaseBarsPeriodType",			false));
				Properties.Remove(Properties.Find("BaseBarsPeriodValue",		false));
				Properties.Remove(Properties.Find("PointAndFigurePriceType",	false));
				Properties.Remove(Properties.Find("ReversalType",				false));
				
				SetPropertyName("Value", "Tick Trend");
				//SetPropertyName("Value2", "Tick Reversal");
				//SetPropertyName("BaseBarsPeriodValue",  "Open Offset");
				
				//Name = "UniR T" + BarsPeriod.Value +"R" + BarsPeriod.Value2; // +"O" + BarsPeriod.BaseBarsPeriodValue;
				Name = "NovoRenkoV2 - " + BarsPeriod.Value +"R"; //+ BarsPeriod.Value2; // +"O" + BarsPeriod.BaseBarsPeriodValue;
				/// I kept the UI name short b/c as of NT8b6 the name space at the toolbar is very short. You would not see the values. Sim22.
			}
		}

		public override int GetInitialLookBackDays(BarsPeriod barsPeriod, TradingHours tradingHours, int barsBack)
		{		
			return 3;
		}

		protected override void OnDataPoint(Bars bars, double open, double high, double low, double close, DateTime time, long volume, bool isBar, double bid, double ask)
		{
			//### First Bar
			if (SessionIterator == null)
				SessionIterator = new SessionIterator(bars);
			
			bool isNewSession = SessionIterator.IsNewSession(time, isBar);
			if (isNewSession)
				SessionIterator.CalculateTradingDay(time, isBar);
			
			//if (bars.Count == 0 || bars.IsResetOnNewTradingDay && bars.SessionIterator.IsNewSession(time, isBar))			
			if (bars.Count == 0 || bars.IsResetOnNewTradingDay && isNewSession)
			//if (bars.Count == 0)// || bars.IsResetOnNewTradingDay && Bars.IsFirstBarOfSession)	
			{
				tickSize = bars.Instrument.MasterInstrument.TickSize;
				trendOffset = bars.BarsPeriod.Value * tickSize;
				reversalOffset = trendOffset * 2;
				//bars.BarPeriod.BaseBarsPeriodValue = bars.BarsPeriod.Value;	//### Remove to customize OpenOffset
				//openOffset = Math.Ceiling((double)bars.BarsPeriod.BaseBarsPeriodValue * 1) * tickSize;
				//openOffset = Math.Ceiling((double)bars.BarsPeriod.BaseBarsPeriodValue * 1) * tickSize;
				
				barOpen = close;
                barMax  = barOpen + (trendOffset * barDirection);
                barMin  = barOpen - (trendOffset * barDirection);
				
				AddBar(bars, barOpen, barOpen, barOpen, barOpen, time, volume);
			}
			//### Subsequent Bars
			else
			{
                maxExceeded  = bars.Instrument.MasterInstrument.Compare(close, barMax) > 0 ? true : false;
                minExceeded  = bars.Instrument.MasterInstrument.Compare(close, barMin) < 0 ? true : false;

                	//### Defined Range Exceeded?
                if ( maxExceeded || minExceeded )
                {
                    double thisClose = maxExceeded ? Math.Min(close, barMax) : minExceeded ? Math.Max(close, barMin) : close;
                    barDirection     = maxExceeded ? 1 : minExceeded ? -1 : 0;
                    fakeOpen = thisClose - (openOffset * barDirection);		//### Fake Open is halfway down the bar

                    	//### Close Current Bar
                    UpdateBar(bars, (maxExceeded ? thisClose : bars.GetHigh(bars.Count - 1)), (minExceeded ? thisClose : bars.GetLow(bars.Count - 1)), thisClose, time, volume);

                    	//### Add New Bar
					barOpen = close;
					barMax  = thisClose + ((barDirection>0 ? trendOffset : reversalOffset) );
					barMin  = thisClose - ((barDirection>0 ? reversalOffset : trendOffset) );

					AddBar(bars, fakeOpen, (maxExceeded ? thisClose : fakeOpen), (minExceeded ? thisClose : fakeOpen), thisClose, time, volume);
                }
                	//### Current Bar Still Developing
                else
                {
                    UpdateBar(bars, (close > bars.GetHigh(bars.Count - 1) ? close : bars.GetHigh(bars.Count - 1)), (close < bars.GetLow(bars.Count - 1) ? close : bars.GetLow(bars.Count - 1)), close, time, volume);
                }	
				
			}
			bars.LastPrice = close;
			
		}

		public override void ApplyDefaultBasePeriodValue(BarsPeriod period)
		{
		
		}

		public override void ApplyDefaultValue(BarsPeriod period)
		{
			period.BarsPeriodTypeName 	= "NovoRenkoV2";
			period.Value 				= 3;
			//period.Value2 				= 6;
			period.BaseBarsPeriodValue	= 0;
		}

		public override string ChartLabel(DateTime dateTime)
		{
			return dateTime.ToString("T", Core.Globals.GeneralOptions.CurrentCulture);
		}

		public override double GetPercentComplete(Bars bars, DateTime now)
		{
			//return 1.0d;
			return 0;
		}
		
	}
}
