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
	public class ROC2 : Indicator
	{
		private ROC roc1;
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
				Name										= "ROC2";
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
				
				PeriodROC									= 4;
				emaROC										= 8;
				EspacamentoROC								= 0;
				
				AddPlot(Brushes.Fuchsia, "CurrROC");
				AddPlot(Brushes.Aqua, "EmaROC");
			}
			else if (State == State.Configure)
			{
				ema1 = EMA(Close, emaROC);
				roc1 = ROC(ema1, PeriodROC);
				ema2 = EMA(roc1, emaROC);
			}
			
			else if (State == State.DataLoaded)
			{				
				boolcompra = new Series<bool>(this);
				boolvenda = new Series<bool>(this);
			}
		}

		protected override void OnBarUpdate()
		{
			CurrROC[0] = roc1[0] * 1000;
			EmaROC[0] = ema2[0] *1000;
			
			BackBrush = null;
			
			if (CurrROC[0]  > EmaROC[0]
				&& CurrROC[1]  > EmaROC[1]
				&& CurrROC[2]  > EmaROC[2]
				&& CurrROC[0]  - EmaROC[0] >= EspacamentoROC
				)
			{
				BackBrush = Brushes.PaleGreen;
				compra = true;
				venda = false;
			}
			
			else if (CurrROC[0]  < EmaROC[0]
				&& CurrROC[1]  < EmaROC[1]
				&& CurrROC[2]  < EmaROC[2]
				&& EmaROC[0]  - CurrROC[0] >= EspacamentoROC
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
		[Display(Name="PeriodROC", Description="Period ROC", Order=1, GroupName="Parameters")]
		public int PeriodROC
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="EmaROC", Description="EMA ROC", Order=2, GroupName="Parameters")]
		public int emaROC
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Espacamento", Description="Espacamento entre ROC e EMA ROC", Order=3, GroupName="Parameters")]
		public int EspacamentoROC
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> CurrROC
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> EmaROC
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
		private ROC2[] cacheROC2;
		public ROC2 ROC2(int periodROC, int emaROC, int espacamentoROC)
		{
			return ROC2(Input, periodROC, emaROC, espacamentoROC);
		}

		public ROC2 ROC2(ISeries<double> input, int periodROC, int emaROC, int espacamentoROC)
		{
			if (cacheROC2 != null)
				for (int idx = 0; idx < cacheROC2.Length; idx++)
					if (cacheROC2[idx] != null && cacheROC2[idx].PeriodROC == periodROC && cacheROC2[idx].emaROC == emaROC && cacheROC2[idx].EspacamentoROC == espacamentoROC && cacheROC2[idx].EqualsInput(input))
						return cacheROC2[idx];
			return CacheIndicator<ROC2>(new ROC2(){ PeriodROC = periodROC, emaROC = emaROC, EspacamentoROC = espacamentoROC }, input, ref cacheROC2);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ROC2 ROC2(int periodROC, int emaROC, int espacamentoROC)
		{
			return indicator.ROC2(Input, periodROC, emaROC, espacamentoROC);
		}

		public Indicators.ROC2 ROC2(ISeries<double> input , int periodROC, int emaROC, int espacamentoROC)
		{
			return indicator.ROC2(input, periodROC, emaROC, espacamentoROC);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ROC2 ROC2(int periodROC, int emaROC, int espacamentoROC)
		{
			return indicator.ROC2(Input, periodROC, emaROC, espacamentoROC);
		}

		public Indicators.ROC2 ROC2(ISeries<double> input , int periodROC, int emaROC, int espacamentoROC)
		{
			return indicator.ROC2(input, periodROC, emaROC, espacamentoROC);
		}
	}
}

#endregion
