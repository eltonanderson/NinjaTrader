//
// Copyright (C) 2017, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
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
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// PVT (On Balance Volume) is a running total of volume. It shows if volume is flowing into
	/// or out of a security. When the security closes higher than the previous close, all
	/// of the day's volume is considered up-volume. When the security closes lower than the
	/// previous close, all of the day's volume is considered down-volume.
	/// </summary>
	public class PVT : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= NinjaTrader.Custom.Resource.NinjaScriptIndicatorDescriptionOBV;
				Name						= "PVT";
				IsSuspendedWhileInactive	= true;
				DrawOnPricePanel			= false;

				AddPlot(Brushes.Goldenrod, NinjaTrader.Custom.Resource.NinjaScriptIndicatorNameOBV);
			}
			else if (State == State.Historical)
			{
				if (Calculate == Calculate.OnPriceChange)
				{
					Draw.TextFixed(this, "NinjaScriptInfo", string.Format(NinjaTrader.Custom.Resource.NinjaScriptOnPriceChangeError, Name), TextPosition.BottomRight);
					Log(string.Format(NinjaTrader.Custom.Resource.NinjaScriptOnPriceChangeError, Name), LogLevel.Error);
				}
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar == 0)
				Value[0] = 0;
			else
			{
				double close0	= Close[0];
				double close1	= Close[1];

				if (close1 != 0)
					Value[0] = Value[1] + ((close0 - close1)/close1 * Volume[0]);
				else
					Value[0] = 0;
			}
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PVT[] cachePVT;
		public PVT PVT()
		{
			return PVT(Input);
		}

		public PVT PVT(ISeries<double> input)
		{
			if (cachePVT != null)
				for (int idx = 0; idx < cachePVT.Length; idx++)
					if (cachePVT[idx] != null &&  cachePVT[idx].EqualsInput(input))
						return cachePVT[idx];
			return CacheIndicator<PVT>(new PVT(), input, ref cachePVT);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PVT PVT()
		{
			return indicator.PVT(Input);
		}

		public Indicators.PVT PVT(ISeries<double> input )
		{
			return indicator.PVT(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PVT PVT()
		{
			return indicator.PVT(Input);
		}

		public Indicators.PVT PVT(ISeries<double> input )
		{
			return indicator.PVT(input);
		}
	}
}

#endregion
