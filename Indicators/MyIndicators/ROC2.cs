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
    public class ROC2 : Indicator
    {
        private ROC _roc1;
        private EMA _ema1;
        private EMA _ema2;

        private bool _longCondition;
        private bool _shortCondition;

        public Series<bool> boolLongSeries;
        public Series<bool> boolShortSeries;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Enter the description for your new custom Indicator here.";
                Name = "ROC2";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                //Disable this property if your indicator requires custom values that cumulate with each new market data event. 
                //See Help Guide for additional information.
                IsSuspendedWhileInactive = true;
                PeriodROC = 4;
                emaROC = 8;
                AddPlot(Brushes.Fuchsia, "CurrROC");
                AddPlot(Brushes.Aqua, "EmaROC");
            }
            else if (State == State.Configure)
            {
                _ema1 = EMA(Close, emaROC);
                _roc1 = ROC(_ema1, PeriodROC);
                _ema2 = EMA(_roc1, emaROC);
            }

            else if (State == State.DataLoaded)
            {
                boolLongSeries = new Series<bool>(this);
                boolShortSeries = new Series<bool>(this);
            }
        }

        protected override void OnBarUpdate()
        {
            CurrROC[0] = _roc1[0] * 1000;
            EmaROC[0] = _ema2[0] * 1000;

            BackBrush = null;

            if (CurrROC[0] > EmaROC[0]
                && CurrROC[1] > EmaROC[1]
                && CurrROC[2] > EmaROC[2]
                )
            {
                BackBrush = Brushes.PaleGreen;
                _longCondition = true;
                _shortCondition = false;
            }

            else if (CurrROC[0] < EmaROC[0]
                && CurrROC[1] < EmaROC[1]
                && CurrROC[2] < EmaROC[2]
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
        [Display(Name = "PeriodROC", Description = "Period ROC", Order = 1, GroupName = "Parameters")]
        public int PeriodROC
        { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "EmaROC", Description = "EMA ROC", Order = 2, GroupName = "Parameters")]
        public int emaROC
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
		private MyIndicators.ROC2[] cacheROC2;
		public MyIndicators.ROC2 ROC2(int periodROC, int emaROC)
		{
			return ROC2(Input, periodROC, emaROC);
		}

		public MyIndicators.ROC2 ROC2(ISeries<double> input, int periodROC, int emaROC)
		{
			if (cacheROC2 != null)
				for (int idx = 0; idx < cacheROC2.Length; idx++)
					if (cacheROC2[idx] != null && cacheROC2[idx].PeriodROC == periodROC && cacheROC2[idx].emaROC == emaROC && cacheROC2[idx].EqualsInput(input))
						return cacheROC2[idx];
			return CacheIndicator<MyIndicators.ROC2>(new MyIndicators.ROC2(){ PeriodROC = periodROC, emaROC = emaROC }, input, ref cacheROC2);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MyIndicators.ROC2 ROC2(int periodROC, int emaROC)
		{
			return indicator.ROC2(Input, periodROC, emaROC);
		}

		public Indicators.MyIndicators.ROC2 ROC2(ISeries<double> input , int periodROC, int emaROC)
		{
			return indicator.ROC2(input, periodROC, emaROC);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MyIndicators.ROC2 ROC2(int periodROC, int emaROC)
		{
			return indicator.ROC2(Input, periodROC, emaROC);
		}

		public Indicators.MyIndicators.ROC2 ROC2(ISeries<double> input , int periodROC, int emaROC)
		{
			return indicator.ROC2(input, periodROC, emaROC);
		}
	}
}

#endregion
