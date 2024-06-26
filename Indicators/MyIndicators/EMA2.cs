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
    public class EMA2 : Indicator
    {

        private EMA _ema1;
        private EMA _ema2;
        private EMA _ema3;

        private bool _longCondition;
        private bool _shortCondition;

        public Series<bool> boolLongSeries;
        public Series<bool> boolShortSeries;


        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Enter the description for your new custom Indicator here.";
                Name = "EMA2";
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
                PeriodEMA1 = 8;
                PeriodEMA2 = 12;
                PeriodEMA3 = 17;
                EspacamentoEMA = 0.10;

                AddPlot(Brushes.Fuchsia, "EMA_1");
                AddPlot(Brushes.Aqua, "EMA_2");
                AddPlot(Brushes.DodgerBlue, "EMA_3");
            }


            else if (State == State.Configure)
            {
                _ema1 = EMA(PeriodEMA1);
                _ema2 = EMA(PeriodEMA2);
                _ema3 = EMA(PeriodEMA3);
            }

            else if (State == State.DataLoaded)
            {
                boolLongSeries = new Series<bool>(this);
                boolShortSeries = new Series<bool>(this);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar == 0)
            {
                EMA_1[0] = Input[0];
                EMA_2[0] = Input[0];
                EMA_3[0] = Input[0];
                return;
            }


            EMA_1[0] = _ema1[0];
            EMA_2[0] = _ema2[0];
            EMA_3[0] = _ema3[0];

            BackBrush = null;

            if (_ema1[0] > _ema2[0]
                && _ema2[0] > _ema3[0]


                && _ema1[0] - _ema2[0] >= EspacamentoEMA
                && _ema2[0] - _ema3[0] >= EspacamentoEMA
                )
            {
                BackBrush = Brushes.PaleGreen;
                _longCondition = true;
                _shortCondition = false;
            }

            else if (_ema1[0] < _ema2[0]
                && _ema2[0] < _ema3[0]


                && _ema2[0] - _ema1[0] >= EspacamentoEMA
                && _ema3[0] - _ema2[0] >= EspacamentoEMA
                )
            {
                BackBrush = Brushes.Pink;
                _longCondition = false;
                _shortCondition = true;
            }

            else
            {
                _longCondition = false;
                _shortCondition = false;
            }

            boolLongSeries[0] = _longCondition;
            boolShortSeries[0] = _shortCondition;
        }

        #region Properties
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "PeriodEMA1", Description = "EMA 1", Order = 1, GroupName = "Parameters")]
        public int PeriodEMA1
        { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "PeriodEMA2", Description = "EMA 2", Order = 2, GroupName = "Parameters")]
        public int PeriodEMA2
        { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "PeriodEMA3", Description = "EMA 3", Order = 3, GroupName = "Parameters")]
        public int PeriodEMA3
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Espacamento", Description = "Espacamento entre EMAs", Order = 4, GroupName = "Parameters")]
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
		private MyIndicators.EMA2[] cacheEMA2;
		public MyIndicators.EMA2 EMA2(int periodEMA1, int periodEMA2, int periodEMA3, double espacamentoEMA)
		{
			return EMA2(Input, periodEMA1, periodEMA2, periodEMA3, espacamentoEMA);
		}

		public MyIndicators.EMA2 EMA2(ISeries<double> input, int periodEMA1, int periodEMA2, int periodEMA3, double espacamentoEMA)
		{
			if (cacheEMA2 != null)
				for (int idx = 0; idx < cacheEMA2.Length; idx++)
					if (cacheEMA2[idx] != null && cacheEMA2[idx].PeriodEMA1 == periodEMA1 && cacheEMA2[idx].PeriodEMA2 == periodEMA2 && cacheEMA2[idx].PeriodEMA3 == periodEMA3 && cacheEMA2[idx].EspacamentoEMA == espacamentoEMA && cacheEMA2[idx].EqualsInput(input))
						return cacheEMA2[idx];
			return CacheIndicator<MyIndicators.EMA2>(new MyIndicators.EMA2(){ PeriodEMA1 = periodEMA1, PeriodEMA2 = periodEMA2, PeriodEMA3 = periodEMA3, EspacamentoEMA = espacamentoEMA }, input, ref cacheEMA2);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MyIndicators.EMA2 EMA2(int periodEMA1, int periodEMA2, int periodEMA3, double espacamentoEMA)
		{
			return indicator.EMA2(Input, periodEMA1, periodEMA2, periodEMA3, espacamentoEMA);
		}

		public Indicators.MyIndicators.EMA2 EMA2(ISeries<double> input , int periodEMA1, int periodEMA2, int periodEMA3, double espacamentoEMA)
		{
			return indicator.EMA2(input, periodEMA1, periodEMA2, periodEMA3, espacamentoEMA);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MyIndicators.EMA2 EMA2(int periodEMA1, int periodEMA2, int periodEMA3, double espacamentoEMA)
		{
			return indicator.EMA2(Input, periodEMA1, periodEMA2, periodEMA3, espacamentoEMA);
		}

		public Indicators.MyIndicators.EMA2 EMA2(ISeries<double> input , int periodEMA1, int periodEMA2, int periodEMA3, double espacamentoEMA)
		{
			return indicator.EMA2(input, periodEMA1, periodEMA2, periodEMA3, espacamentoEMA);
		}
	}
}

#endregion
