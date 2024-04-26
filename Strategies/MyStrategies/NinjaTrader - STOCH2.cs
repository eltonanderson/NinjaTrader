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
	public class STOCH2 : Indicator
	{
		private StochasticsFast stoch1;
		private EMA ema1;
		private EMA ema2;
		
		private bool compra;
		private bool venda;
		
		public Series<bool> boolcompra;
		public Series<bool> boolvenda;
		
		protected override void OnStateChange()
		{
			
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "STOCH2";
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
				StochPeriodD				= 12;
				StochEMA					= 20;
				StochSignal					= 40;
				AddPlot(Brushes.Fuchsia, "StochD");
				AddPlot(Brushes.Aqua, "StochSig");
			}
			else if (State == State.Configure)
			{
				stoch1 = StochasticsFast(Close, StochPeriodD, StochEMA);
				ema1 = EMA(stoch1.D, StochEMA);
				ema2 = EMA(ema1, StochSignal);
			}
			
			else if (State == State.DataLoaded)
			{				
				boolcompra = new Series<bool>(this);
				boolvenda = new Series<bool>(this);
			}
		}

		protected override void OnBarUpdate()
		{
			StochD[0] = ema1[0];
			StochSig[0] = ema2[0];
			
			BackBrush = null;
			
			if (StochD[0]  > StochSig[0]
				&& StochD[1]  > StochSig[1]
				&& StochD[2]  > StochSig[2]
				)
			{
				BackBrush = Brushes.PaleGreen;
				compra = true;
				venda = false;
			}
			
			else if (StochD[0]  < StochSig[0]
				&& StochD[1]  < StochSig[1]
				&& StochD[2]  < StochSig[2]
				)
			{
				BackBrush = Brushes.Pink;
				compra = false;
				venda = true;
			}
			else
			{
				compra = false;
				venda = false;
			}
			
			boolcompra[0] = compra;
			boolvenda[0] = venda;
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="StochPeriodD", Description="Stoch Period D", Order=1, GroupName="Parameters")]
		public int StochPeriodD
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="StochEMA", Description="Stoch EMA", Order=2, GroupName="Parameters")]
		public int StochEMA
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="StochSignal", Description="Stoch Signal", Order=3, GroupName="Parameters")]
		public int StochSignal
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> StochD
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> StochSig
		{
			get { return Values[1]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private STOCH2[] cacheSTOCH2;
		public STOCH2 STOCH2(int stochPeriodD, int stochEMA, int stochSignal)
		{
			return STOCH2(Input, stochPeriodD, stochEMA, stochSignal);
		}

		public STOCH2 STOCH2(ISeries<double> input, int stochPeriodD, int stochEMA, int stochSignal)
		{
			if (cacheSTOCH2 != null)
				for (int idx = 0; idx < cacheSTOCH2.Length; idx++)
					if (cacheSTOCH2[idx] != null && cacheSTOCH2[idx].StochPeriodD == stochPeriodD && cacheSTOCH2[idx].StochEMA == stochEMA && cacheSTOCH2[idx].StochSignal == stochSignal && cacheSTOCH2[idx].EqualsInput(input))
						return cacheSTOCH2[idx];
			return CacheIndicator<STOCH2>(new STOCH2(){ StochPeriodD = stochPeriodD, StochEMA = stochEMA, StochSignal = stochSignal }, input, ref cacheSTOCH2);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.STOCH2 STOCH2(int stochPeriodD, int stochEMA, int stochSignal)
		{
			return indicator.STOCH2(Input, stochPeriodD, stochEMA, stochSignal);
		}

		public Indicators.STOCH2 STOCH2(ISeries<double> input , int stochPeriodD, int stochEMA, int stochSignal)
		{
			return indicator.STOCH2(input, stochPeriodD, stochEMA, stochSignal);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.STOCH2 STOCH2(int stochPeriodD, int stochEMA, int stochSignal)
		{
			return indicator.STOCH2(Input, stochPeriodD, stochEMA, stochSignal);
		}

		public Indicators.STOCH2 STOCH2(ISeries<double> input , int stochPeriodD, int stochEMA, int stochSignal)
		{
			return indicator.STOCH2(input, stochPeriodD, stochEMA, stochSignal);
		}
	}
}

#endregion
