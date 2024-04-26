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
	public class EMA2 : Indicator
	{
		
		private EMA ema1;
		private EMA ema2;
		private EMA ema3;
		
		private bool compra;
		private bool venda;
		
		public Series<bool> boolcompra;
		public Series<bool> boolvenda;
		
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "EMA2";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				PeriodEMA1					= 8;
				PeriodEMA2					= 12;
				PeriodEMA3					= 17;
				EspacamentoEMA				= 0.20;
				
				AddPlot(Brushes.Fuchsia, "EMA_1");
				AddPlot(Brushes.Aqua, "EMA_2");
				AddPlot(Brushes.DodgerBlue, "EMA_3");
			}
			
		
			else if (State == State.Configure)
			{
				ema1 = EMA(PeriodEMA1);
				ema2 = EMA(PeriodEMA2);
				ema3 = EMA(PeriodEMA3);			
			}
			
			else if (State == State.DataLoaded)
			{				
				boolcompra = new Series<bool>(this);
				boolvenda = new Series<bool>(this);
			}
		}

		protected override void OnBarUpdate()
		{
			if(CurrentBar == 0) 
			{	
				EMA_1[0] = Input[0];
				EMA_2[0] = Input[0];
				EMA_3[0] = Input[0];
				return;
			}
			
			
			EMA_1[0] = ema1[0];
			EMA_2[0] = ema2[0];
			EMA_3[0] = ema3[0];
					
			BackBrush = null;
			
			if (ema1[0] > ema2[0] 
				&& ema2[0] > ema3[0]
				
				
				&& ema1[0] - ema2[0] >= EspacamentoEMA
				&& ema2[0] - ema3[0] >= EspacamentoEMA
				)
			{
				BackBrush  = Brushes.PaleGreen;
				compra = true;
				venda = false;
			}
			
			else if (ema1[0] < ema2[0] 
				&& ema2[0] < ema3[0]
				
				
				&& ema2[0] - ema1[0] >= EspacamentoEMA
				&& ema3[0] - ema2[0] >= EspacamentoEMA
				)
			{
				BackBrush  = Brushes.Pink;
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
		[Display(Name="PeriodEMA1", Description="EMA 1", Order=1, GroupName="Parameters")]
		public int PeriodEMA1
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="PeriodEMA2", Description="EMA 2", Order=2, GroupName="Parameters")]
		public int PeriodEMA2
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="PeriodEMA3", Description="EMA 3", Order=3, GroupName="Parameters")]
		public int PeriodEMA3
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0.01, double.MaxValue)]
		[Display(Name="Espacamento", Description="Espacamento entre EMAs", Order=4, GroupName="Parameters")]
		public double EspacamentoEMA
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> EMA_1
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> EMA_2
		{
			get { return Values[1]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> EMA_3
		{
			get { return Values[2]; }
		}
		
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private EMA2[] cacheEMA2;
		public EMA2 EMA2(int periodEMA1, int periodEMA2, int periodEMA3, double espacamentoEMA)
		{
			return EMA2(Input, periodEMA1, periodEMA2, periodEMA3, espacamentoEMA);
		}

		public EMA2 EMA2(ISeries<double> input, int periodEMA1, int periodEMA2, int periodEMA3, double espacamentoEMA)
		{
			if (cacheEMA2 != null)
				for (int idx = 0; idx < cacheEMA2.Length; idx++)
					if (cacheEMA2[idx] != null && cacheEMA2[idx].PeriodEMA1 == periodEMA1 && cacheEMA2[idx].PeriodEMA2 == periodEMA2 && cacheEMA2[idx].PeriodEMA3 == periodEMA3 && cacheEMA2[idx].EspacamentoEMA == espacamentoEMA && cacheEMA2[idx].EqualsInput(input))
						return cacheEMA2[idx];
			return CacheIndicator<EMA2>(new EMA2(){ PeriodEMA1 = periodEMA1, PeriodEMA2 = periodEMA2, PeriodEMA3 = periodEMA3, EspacamentoEMA = espacamentoEMA }, input, ref cacheEMA2);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.EMA2 EMA2(int periodEMA1, int periodEMA2, int periodEMA3, double espacamentoEMA)
		{
			return indicator.EMA2(Input, periodEMA1, periodEMA2, periodEMA3, espacamentoEMA);
		}

		public Indicators.EMA2 EMA2(ISeries<double> input , int periodEMA1, int periodEMA2, int periodEMA3, double espacamentoEMA)
		{
			return indicator.EMA2(input, periodEMA1, periodEMA2, periodEMA3, espacamentoEMA);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.EMA2 EMA2(int periodEMA1, int periodEMA2, int periodEMA3, double espacamentoEMA)
		{
			return indicator.EMA2(Input, periodEMA1, periodEMA2, periodEMA3, espacamentoEMA);
		}

		public Indicators.EMA2 EMA2(ISeries<double> input , int periodEMA1, int periodEMA2, int periodEMA3, double espacamentoEMA)
		{
			return indicator.EMA2(input, periodEMA1, periodEMA2, periodEMA3, espacamentoEMA);
		}
	}
}

#endregion
