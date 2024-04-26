                      using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using PTLRuntime.NETScript;
using System.Linq;
using PTLRuntime.NETScript.Indicators;
using System.Timers;

namespace PVT_MACDsemi
{
    /// <summary>
    /// PVT_MACD
    /// 
    /// </summary>
    public class PVT_MACDsemi : NETStrategy
    {
        public PVT_MACDsemi()
            : base()
        {
			#region Initialization
           
            base.ProjectName = "PVT_MACDsemi";
            #endregion 
        }
        
        public int MagicNumber = 13;

        [InputParameter("SL", 0, 0, 500)]
        public int SL = 7;

        [InputParameter("TP", 0, 0, 500)]
        public int TP = 18;

        [InputParameter("Amount", 0, 0.1, 100, 1, 0.1)]
        public int Amount = 1;
        
        [InputParameter("MACD Diff", 0, 0.1, 100, 1, 0.1)]
        public double MACDDiff = 0.2;

        [InputParameter("Session start", 0)]
        public TimeSpan sessionStart = new TimeSpan(09, 03, 0);

        [InputParameter("Session end", 0)]
        public TimeSpan sessionEnd = new TimeSpan(17, 0, 0);
        
        public int Period = 17;
        public int FastEMA = 17;
        public int SlowEMA = 72;
        public int SignalSMA = 34;
        
		public bool OrdemEnviada;
		public bool MACDbuy;
		public bool MACDsell;
        
		Indicator MACD, mediaPVT;
		
		int maxPeriod;
		
		//Timer timer;
		
		string posId;
        /// <summary>
        /// This function will be called after creating
        /// </summary>
		public override void Init()
		{
			MACD = Indicators.iMACD(CurrentData, FastEMA, SlowEMA, SignalSMA, PriceType.Close);
			mediaPVT = Indicators.iCustom("MediaPVT", CurrentData);
			
			maxPeriod = new int[] {2, Period, FastEMA, SlowEMA, SignalSMA }.Max();
			
			OrdemEnviada = false;
			MACDbuy = false;
			MACDsell = false;
			
			/*timer = new Timer(1000);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();*/
		}        
 
		/// <summary>
		/// Entry point. This function is called when new quote comes 
		/// </summary>
		
		//public override void NextBar()			
		public override void OnQuote()
		{
			if (CurrentData.HistoryCount < maxPeriod || !IsActiveSession || OrdemEnviada)
				return;
			
			var v0 = mediaPVT.GetValue(0, 1);
			var v1 = mediaPVT.GetValue(1, 1);
			
			var v0_ = MACD.GetValue(0, 1) - MACD.GetValue(1, 1);
			var v1_ = MACD.GetValue(0, 2) - MACD.GetValue(1, 2);
			
			var fec1 = CurrentData.GetPrice(PriceType.Close, 1);
			var abe1 = CurrentData.GetPrice(PriceType.Open, 1);
			
			var fec2 = CurrentData.GetPrice(PriceType.Close, 2);
			var abe2 = CurrentData.GetPrice(PriceType.Open, 2);
			
			var mACDDiff = v0_ - v1_;
			
			if (mACDDiff >= 0.2)
			{
				MACDbuy = true;
				MACDsell = false;
			}
			if (mACDDiff <= -(0.2))
			{
				MACDsell = true;
				MACDbuy = false;
			}
			if (mACDDiff < 0.2 && mACDDiff > -(0.2))
			{
				MACDbuy = false;
				MACDsell = false;
			}
			
			
			bool buy = v0 > v1 && v0_ > v1_ && fec1 > abe1 && MACDbuy && abe2 > fec2;
			bool sell = v0 < v1 && v0_ < v1_ && fec1 < abe1 && MACDsell && abe2 < fec2;
			
			if (buy || sell)
			{
				var request = NewRequest((buy) ? Operation.Buy : Operation.Sell);
				if (request != null)
				{
					posId = Orders.Send(request);
					
					OrdemEnviada = true;
				}
				
				return;
			}
			
			//ClosePosition(posId);
		}
        
		/*public override void OnQuote()
		{
			
		ClosePositionBySession();
		
		}*/
        /// <summary>
        /// This function will be called before removing
        /// </summary>
		public override void Complete()
		{
			/*timer.Stop();
            timer.Elapsed -= Timer_Elapsed;
            timer.Dispose();
            timer = null;*/
			
			OrdemEnviada = false;
            
			MACD.Dispose();
			MACD = null;
			mediaPVT.Dispose();
			mediaPVT = null;
		} 
		
		#region
		/*private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ClosePositionBySession();
        }*/
		
		bool IsActiveSession
		{
			get
			{
				var time = CurrentData.Time().TimeOfDay;
				return (sessionStart < sessionEnd) ? (time >= sessionStart && time <= sessionEnd) : (time >= sessionStart || time <= sessionEnd);
			}
		}
		
		/// <summary>
        /// builds request for Market order with given operation side
        /// </summary>
        /// <param name="side">operation side</param>
        /// <returns>Market order request</returns>
		private NewOrderRequest NewRequest(Operation side)
		{
			NewOrderRequest request = null;
			
			if (Positions.GetPositionByOpenOrderId(posId) == null)
			{
				request = new NewOrderRequest()
				{
					Side = side,
                    MarketRange = 30,
                    Type = OrdersType.Limit,
                    Amount = Amount,
                    Account = Accounts.Current,
                    Instrument = Instruments.Current,
                    StopLossOffset = SL * Point,
                    TakeProfitOffset = TP * Point,
                    MagicNumber = MagicNumber,
                    Price = CurrentData.GetPrice(PriceType.Close, 1)
				};
			}
			
			return request;
		}
		
		void ClosePosition(string Id)
		{
			var pos = Positions.GetPositionByOpenOrderId(Id);
			
			if (pos != null)
			{
				pos.Close();
			}
		}	
			                
		void ClosePositionBySession()
		{
			if (!IsActiveSession && posId != null)
				ClosePosition(posId);
		}
		#endregion
     }
}
