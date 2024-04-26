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
    public class STOCH2 : Indicator
    {
        private StochasticsFast _sto1;
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
                Name = "STOCH2";
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
                StochPeriodD = 17;
                StochEMA = 12;
                StochSignal = 17;
                AddPlot(Brushes.Fuchsia, "StochD");
                AddPlot(Brushes.Aqua, "StochSig");
            }
            else if (State == State.Configure)
            {
                _sto1 = StochasticsFast(Close, StochPeriodD, StochEMA);
                _ema1 = EMA(_sto1.D, StochEMA);
                _ema2 = EMA(_ema1, StochSignal);
            }

            else if (State == State.DataLoaded)
            {
                boolLongSeries = new Series<bool>(this);
                boolShortSeries = new Series<bool>(this);
            }
        }

        protected override void OnBarUpdate()
        {
            StochD[0] = _ema1[0];
            StochSig[0] = _ema2[0];

            BackBrush = null;

            if (StochD[0] > StochSig[0]
                && StochD[1] > StochSig[1]
                && StochD[2] > StochSig[2]
                )
            {
                BackBrush = Brushes.PaleGreen;
                _longCondition = true;
                _shortCondition = false;
            }

            else if (StochD[0] < StochSig[0]
                && StochD[1] < StochSig[1]
                && StochD[2] < StochSig[2]
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
        [Display(Name = "StochPeriodD", Description = "Stoch Period D", Order = 1, GroupName = "Parameters")]
        public int StochPeriodD
        { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "StochEMA", Description = "Stoch EMA", Order = 2, GroupName = "Parameters")]
        public int StochEMA
        { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "StochSignal", Description = "Stoch Signal", Order = 3, GroupName = "Parameters")]
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
		private MyIndicators.STOCH2[] cacheSTOCH2;
		public MyIndicators.STOCH2 STOCH2(int stochPeriodD, int stochEMA, int stochSignal)
		{
			return STOCH2(Input, stochPeriodD, stochEMA, stochSignal);
		}

		public MyIndicators.STOCH2 STOCH2(ISeries<double> input, int stochPeriodD, int stochEMA, int stochSignal)
		{
			if (cacheSTOCH2 != null)
				for (int idx = 0; idx < cacheSTOCH2.Length; idx++)
					if (cacheSTOCH2[idx] != null && cacheSTOCH2[idx].StochPeriodD == stochPeriodD && cacheSTOCH2[idx].StochEMA == stochEMA && cacheSTOCH2[idx].StochSignal == stochSignal && cacheSTOCH2[idx].EqualsInput(input))
						return cacheSTOCH2[idx];
			return CacheIndicator<MyIndicators.STOCH2>(new MyIndicators.STOCH2(){ StochPeriodD = stochPeriodD, StochEMA = stochEMA, StochSignal = stochSignal }, input, ref cacheSTOCH2);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MyIndicators.STOCH2 STOCH2(int stochPeriodD, int stochEMA, int stochSignal)
		{
			return indicator.STOCH2(Input, stochPeriodD, stochEMA, stochSignal);
		}

		public Indicators.MyIndicators.STOCH2 STOCH2(ISeries<double> input , int stochPeriodD, int stochEMA, int stochSignal)
		{
			return indicator.STOCH2(input, stochPeriodD, stochEMA, stochSignal);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MyIndicators.STOCH2 STOCH2(int stochPeriodD, int stochEMA, int stochSignal)
		{
			return indicator.STOCH2(Input, stochPeriodD, stochEMA, stochSignal);
		}

		public Indicators.MyIndicators.STOCH2 STOCH2(ISeries<double> input , int stochPeriodD, int stochEMA, int stochSignal)
		{
			return indicator.STOCH2(input, stochPeriodD, stochEMA, stochSignal);
		}
	}
}

#endregion
