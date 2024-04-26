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
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class HistoStochEMA : Indicator
	{
		//private StochasticsFast StochasticsFast1;
		private Stochastics StochasticsFast1;
		private EMA EMA1;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Histograma Estocastico com Media Movel";
				Name										= "HistoStochEMA";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				Stochastic_K					= 34;
				Stochastic_D					= 34;
				Ema								= 17;
				
				AddPlot(new Stroke(Brushes.DodgerBlue, 2),	PlotStyle.Bar,	NinjaTrader.Custom.Resource.NinjaScriptIndicatorDiff);
				AddLine(Brushes.DarkGray, 0,							NinjaTrader.Custom.Resource.NinjaScriptIndicatorZeroLine);
			}
			else if (State == State.Configure)
			{
			}
			
			else if (State == State.DataLoaded)
			{				
				//StochasticsFast1				= StochasticsFast(Close, Stochastic_D, Stochastic_K);
				StochasticsFast1				= Stochastics(Ema, Stochastic_D, Stochastic_K);
				EMA1							= EMA(StochasticsFast1.K, Ema);
				
			}
		}

		protected override void OnBarUpdate()
		{
			//Add your custom indicator logic here.
			//double input0	= Input[0];

			if (CurrentBar == 0)
			{
				Diff[0] = 0;
			}
			else
			{
				//double histogram0 = StochasticsFast1.D[1] - EMA1[1];
				
				Diff[0]			= StochasticsFast1.K[0] - EMA1[0];
			}
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Stochastic_K", Order=1, GroupName="Parameters")]
		public int Stochastic_K
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Stochastic_D", Order=2, GroupName="Parameters")]
		public int Stochastic_D
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Ema", Order=3, GroupName="Parameters")]
		public int Ema
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Diff
		{
			get { return Values[0]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private HistoStochEMA[] cacheHistoStochEMA;
		public HistoStochEMA HistoStochEMA(int stochastic_K, int stochastic_D, int ema)
		{
			return HistoStochEMA(Input, stochastic_K, stochastic_D, ema);
		}

		public HistoStochEMA HistoStochEMA(ISeries<double> input, int stochastic_K, int stochastic_D, int ema)
		{
			if (cacheHistoStochEMA != null)
				for (int idx = 0; idx < cacheHistoStochEMA.Length; idx++)
					if (cacheHistoStochEMA[idx] != null && cacheHistoStochEMA[idx].Stochastic_K == stochastic_K && cacheHistoStochEMA[idx].Stochastic_D == stochastic_D && cacheHistoStochEMA[idx].Ema == ema && cacheHistoStochEMA[idx].EqualsInput(input))
						return cacheHistoStochEMA[idx];
			return CacheIndicator<HistoStochEMA>(new HistoStochEMA(){ Stochastic_K = stochastic_K, Stochastic_D = stochastic_D, Ema = ema }, input, ref cacheHistoStochEMA);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.HistoStochEMA HistoStochEMA(int stochastic_K, int stochastic_D, int ema)
		{
			return indicator.HistoStochEMA(Input, stochastic_K, stochastic_D, ema);
		}

		public Indicators.HistoStochEMA HistoStochEMA(ISeries<double> input , int stochastic_K, int stochastic_D, int ema)
		{
			return indicator.HistoStochEMA(input, stochastic_K, stochastic_D, ema);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.HistoStochEMA HistoStochEMA(int stochastic_K, int stochastic_D, int ema)
		{
			return indicator.HistoStochEMA(Input, stochastic_K, stochastic_D, ema);
		}

		public Indicators.HistoStochEMA HistoStochEMA(ISeries<double> input , int stochastic_K, int stochastic_D, int ema)
		{
			return indicator.HistoStochEMA(input, stochastic_K, stochastic_D, ema);
		}
	}
}

#endregion
