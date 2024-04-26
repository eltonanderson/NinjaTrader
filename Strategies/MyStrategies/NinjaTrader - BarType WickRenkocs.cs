// 
// Copyright (C) 2019, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel;
using NinjaTrader;
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.BarsTypes
{
	public class WickRenko : BarsType
	{
		private			double			offset;
		private			double			renkoHigh;
		private			double			renkoLow;
		private			double			maxima;
		private			double			minima;

		public override void ApplyDefaultBasePeriodValue(BarsPeriod period) {}

		public override void ApplyDefaultValue(BarsPeriod period) {period.Value = 3;}

		public override string ChartLabel(DateTime time) { return time.ToString(Name, Core.Globals.GeneralOptions.CurrentCulture); }

		public override int GetInitialLookBackDays(BarsPeriod period, TradingHours tradingHours, int barsBack) { return 5; }

		public override double GetPercentComplete(Bars bars, DateTime now) { return 0; }

		public override bool IsRemoveLastBarSupported { get { return true; } }

		protected override void OnDataPoint(Bars bars, double open, double high, double low, double close, DateTime time, long volume, bool isBar, double bid, double ask)
		{
			if (SessionIterator == null)
				SessionIterator = new SessionIterator(bars);

			offset = bars.BarsPeriod.Value * bars.Instrument.MasterInstrument.TickSize;
			bool isNewSession = SessionIterator.IsNewSession(time, isBar);
			if (isNewSession)
				SessionIterator.GetNextSession(time, isBar);

			if (bars.Count == 0 || bars.IsResetOnNewTradingDay && isNewSession)
			{
				if (bars.Count > 0)
				{
					// Close out last bar in session and set open == close
					double		lastBarClose	= bars.GetClose(bars.Count - 1);
					DateTime	lastBarTime		= bars.GetTime(bars.Count - 1);
					long		lastBarVolume	= bars.GetVolume(bars.Count - 1);
					RemoveLastBar(bars);
					AddBar(bars, lastBarClose, lastBarClose, lastBarClose, lastBarClose, lastBarTime, lastBarVolume);
				}

				renkoHigh	= close + offset;
				renkoLow	= close - offset;

				isNewSession = SessionIterator.IsNewSession(time, isBar);
				if (isNewSession)
					SessionIterator.GetNextSession(time, isBar);

				AddBar(bars, close, close, close, close, time, volume);
				bars.LastPrice = close;

				return;
			}

			double		barOpen		= bars.GetOpen(bars.Count - 1);
			double		barClose	= bars.GetClose(bars.Count - 1);
			double		barHigh		= bars.GetHigh(bars.Count - 1);
			double		barLow		= bars.GetLow(bars.Count - 1);
			long		barVolume	= bars.GetVolume(bars.Count - 1);
			DateTime	barTime		= bars.GetTime(bars.Count - 1);

			if (renkoHigh.ApproxCompare(0.0) == 0 || renkoLow.ApproxCompare(0.0) == 0)
			{
				if (bars.Count == 1)
				{
					renkoHigh	= barOpen + offset;
					renkoLow	= barOpen - offset;
				}
				else if (bars.GetClose(bars.Count - 2) > bars.GetOpen(bars.Count - 2))
				{
					renkoHigh	= bars.GetClose(bars.Count - 2) + offset;
					renkoLow	= bars.GetClose(bars.Count - 2) - offset * 2;
				}
				else
				{
					renkoHigh	= bars.GetClose(bars.Count - 2) + offset * 2;
					renkoLow	= bars.GetClose(bars.Count - 2) - offset;
				}
			}

			if (close.ApproxCompare(renkoHigh) > 0)
			{
				minima = Math.Min(barClose, close);
				
				if (barHigh.ApproxCompare(barClose) != 0)
				{
					RemoveLastBar(bars);
					//AddBar(bars, renkoHigh - offset, Math.Max(renkoHigh - offset, renkoHigh), Math.Min(renkoHigh - offset, renkoHigh), renkoHigh, barTime, barVolume);
					AddBar(bars, Math.Min(barClose, renkoHigh - offset), renkoHigh, minima, renkoHigh, barTime, barVolume);

				}

				renkoLow	= renkoHigh - 2.0 * offset;
				renkoHigh	= renkoHigh + offset;
				
				
				isNewSession = SessionIterator.IsNewSession(time, isBar);
				if (isNewSession)
					SessionIterator.GetNextSession(time, isBar);

				while (close.ApproxCompare(renkoHigh) > 0)	// Add empty bars to fill gap if price jumps
				{
					//AddBar(bars, renkoHigh - offset, Math.Max(renkoHigh - offset, renkoHigh), Math.Min(renkoHigh - offset, renkoHigh), renkoHigh, time, 0);
					AddBar(bars, barHigh, renkoHigh, Math.Min(renkoHigh - offset, renkoHigh), renkoHigh, time, 0);
					renkoLow	= renkoHigh - 2.0 * offset;
					renkoHigh	= renkoHigh + offset;
				}

				// Add final partial bar
				//AddBar(bars, renkoHigh - offset, Math.Max(renkoHigh - offset, close), Math.Min(renkoHigh - offset, close), close, time, volume);
				AddBar(bars, barHigh, renkoHigh, minima, renkoHigh, time, volume);
	
			}
			else
				if (close.ApproxCompare(renkoLow) < 0)
				{
					maxima = Math.Max(barClose, close);
					
					if (barLow.ApproxCompare(barClose) != 0	)
					{
						RemoveLastBar(bars);
						//AddBar(bars, renkoLow + offset, Math.Max(renkoLow + offset, renkoLow), Math.Min(renkoLow + offset, renkoLow), renkoLow, barTime, barVolume);
						AddBar(bars, Math.Max(barClose, renkoLow + offset), renkoLow, maxima, renkoLow, barTime, barVolume);
					}

					renkoHigh	= renkoLow + 2.0 * offset;
					renkoLow	= renkoLow - offset;

					isNewSession = SessionIterator.IsNewSession(time, isBar);
					if (isNewSession)
						SessionIterator.GetNextSession(time, isBar);

					while (close.ApproxCompare(renkoLow) < 0)	// Add empty bars to fill gap if price jumps
					{
						//AddBar(bars, renkoLow + offset, Math.Max(renkoLow + offset, renkoLow), Math.Min(renkoLow + offset, renkoLow), renkoLow, time, 0);
						AddBar(bars, barLow, renkoLow, Math.Min(renkoLow + offset, renkoLow), renkoLow, time, 0);
						renkoHigh	= renkoLow + 2.0 * offset;
						renkoLow	= renkoLow - offset;
					}

					// Add final partial bar
					//AddBar(bars, renkoLow + offset, Math.Max(renkoLow + offset, close), Math.Min(renkoLow + offset, close), close, time, volume);
					AddBar(bars, barLow, renkoLow, maxima, renkoLow, time, volume);
				}
				else
					UpdateBar(bars, close, close, close, time, volume);

			bars.LastPrice	= close;
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name				= "WickRenko";
				BarsPeriod			= new BarsPeriod { BarsPeriodType = (BarsPeriodType) 444, BarsPeriodTypeName = "WickRenko(444)", Value = 1 };
				BuiltFrom			= BarsPeriodType.Tick;
				DaysToLoad			= 3;
				DefaultChartStyle	= Gui.Chart.ChartStyleType.CandleStick;
				IsIntraday			= true;
				IsTimeBased			= false;
			}
			else if (State == State.Configure)
			{

				Properties.Remove(Properties.Find("BaseBarsPeriodType",			true));
				Properties.Remove(Properties.Find("BaseBarsPeriodValue",		true));
				Properties.Remove(Properties.Find("PointAndFigurePriceType",	true));
				Properties.Remove(Properties.Find("ReversalType",				true));
				Properties.Remove(Properties.Find("Value2",						true));

				SetPropertyName("Value", "Renko Box");
				Name = "WickRenko - " + BarsPeriod.Value +"R"; 
			}
		}
	}
}
