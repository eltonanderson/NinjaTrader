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
namespace NinjaTrader.NinjaScript.Indicators.MyIndicators
{
    public class OBV2 : Indicator
    {
        private OBV obv1;
        private SMA sma1;

        private bool compra;
        private bool venda;

        public Series<bool> boolcompra;
        public Series<bool> boolvenda;


        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Enter the description for your new custom Indicator here.";
                Name = "OBV2";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                //Disable this property if your indicator requires custom values that cumulate with each new market data event. 
                //See Help Guide for additional information.
                IsSuspendedWhileInactive = true;

                EspacamentoOBV = 20;
                PeriodOBV = 15;


                AddPlot(Brushes.Fuchsia, "_OBV");
                AddPlot(Brushes.Aqua, "SMA_OBV");
            }
            else if (State == State.Configure)
            {
                obv1 = OBV(Close);

                sma1 = SMA(obv1, PeriodOBV);
            }

            else if (State == State.DataLoaded)
            {
                boolcompra = new Series<bool>(this);
                boolvenda = new Series<bool>(this);
            }
        }

        protected override void OnBarUpdate()
        {

            _OBV[0] = obv1[0] / 100;
            SMA_OBV[0] = sma1[0] / 100;

            BackBrush = null;

            if (_OBV[0] > SMA_OBV[0]
                && _OBV[0] - SMA_OBV[0] >= EspacamentoOBV
                )
            {
                BackBrush = Brushes.PaleGreen;
                compra = true;
                venda = false;
            }

            else if (_OBV[0] < SMA_OBV[0]
                && SMA_OBV[0] - _OBV[0] >= EspacamentoOBV
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
        [Display(Name = "PeriodOBV", Description = "SMA OBV", Order = 1, GroupName = "Parameters")]
        public int PeriodOBV
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Espacamento", Description = "Espacamento entre OBV e SMA", Order = 2, GroupName = "Parameters")]
        public int EspacamentoOBV
        { get; set; }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> _OBV
        {
            get { return Values[0]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> SMA_OBV
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
		private MyIndicators.OBV2[] cacheOBV2;
		public MyIndicators.OBV2 OBV2(int periodOBV, int espacamentoOBV)
		{
			return OBV2(Input, periodOBV, espacamentoOBV);
		}

		public MyIndicators.OBV2 OBV2(ISeries<double> input, int periodOBV, int espacamentoOBV)
		{
			if (cacheOBV2 != null)
				for (int idx = 0; idx < cacheOBV2.Length; idx++)
					if (cacheOBV2[idx] != null && cacheOBV2[idx].PeriodOBV == periodOBV && cacheOBV2[idx].EspacamentoOBV == espacamentoOBV && cacheOBV2[idx].EqualsInput(input))
						return cacheOBV2[idx];
			return CacheIndicator<MyIndicators.OBV2>(new MyIndicators.OBV2(){ PeriodOBV = periodOBV, EspacamentoOBV = espacamentoOBV }, input, ref cacheOBV2);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MyIndicators.OBV2 OBV2(int periodOBV, int espacamentoOBV)
		{
			return indicator.OBV2(Input, periodOBV, espacamentoOBV);
		}

		public Indicators.MyIndicators.OBV2 OBV2(ISeries<double> input , int periodOBV, int espacamentoOBV)
		{
			return indicator.OBV2(input, periodOBV, espacamentoOBV);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MyIndicators.OBV2 OBV2(int periodOBV, int espacamentoOBV)
		{
			return indicator.OBV2(Input, periodOBV, espacamentoOBV);
		}

		public Indicators.MyIndicators.OBV2 OBV2(ISeries<double> input , int periodOBV, int espacamentoOBV)
		{
			return indicator.OBV2(input, periodOBV, espacamentoOBV);
		}
	}
}

#endregion
