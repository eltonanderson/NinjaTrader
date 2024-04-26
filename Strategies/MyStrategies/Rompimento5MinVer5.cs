                 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PTLRuntime.NETScript.Indicators;
using PTLRuntime.NETScript;

namespace Rompimento5MinVer5
{
    public class Rompimento5MinVer5 : NETStrategy
    {
        public Rompimento5MinVer5()
            : base()
        {
            #region Initialization
            base.ProjectName = "Rompimento5MinVer5";
            base.Comments = "Rompimento do candle 5 minutos - 0900h & 1030h";
            base.Version = "5.0";
            #endregion
        }

        #region Parameters
        [InputParameter("Magic number", 0, 1, 9999)]
        public int MagicNumber = 13;

        [InputParameter("Amount", 1, 0, 500)]
        public int Amount = 1;

        [InputParameter("Distancia", 2, 0, 500)]
        public int distancia = 1;
        
        [InputParameter("Primeira Entrada", 3, 0, 500)]
        public int primeira = 3;
        
        [InputParameter("Segunda Entrada", 4, 0, 500)]
        public int segunda = 23;
        
         [InputParameter("Terceira Entrada", 5, 0, 500)]
        public int terceira = 80;

        [InputParameter("Ordem", 6, new object[]
                        {
                            "Limit", OrdersType.Limit,
                            "Market", OrdersType.Market
                        })]
        public OrdersType tipo = OrdersType.Market;

        [InputParameter("SL", 7, 0, 500)]
        public int SL = 8;

        [InputParameter("TP", 8, 0, 500)]
        public int TP = 2;

        [InputParameter("Session end", 9)]
        public TimeSpan sessionEnd = new TimeSpan(18, 00, 0);
        
        
        #endregion
        
		TimeSpan time;

		double high, low;

        BarData curData;
        
		double contador;
        
		bool toTrade;
        
        string posId;

        public override void Init()
        {
            curData = CurrentData as BarData;
			toTrade = true;
			contador = 0;
        }

		public override void OnQuote()
		{
			time = CurrentData.Time().TimeOfDay;
			
			bool buy = high > 0 && Instruments.Current.LastQuote.Last > high + distancia * Point;
			bool sell = low > 0 && Instruments.Current.LastQuote.Last < low - distancia * Point;
				
			if (buy || sell)
			{
				var request = NewRequest((buy) ? Operation.Buy : Operation.Sell);
				if (request != null)
				{
					posId = Orders.Send(request);
					toTrade = false;
				}

				return;
			}
		}

		public override void NextBar()
		{
			if (time > sessionEnd)
			{
				contador = 0;
				return;
			}
						
			contador = contador + 1;
						
			if (contador == primeira || contador == segunda || contador == terceira)
			{
				high = curData.High(1);
				low = curData.Low(1);
				toTrade = true;
			}
			
			else
			{
				high = 0;
				low = 0;
			}
			
			Print("Time = " + time);
			Print("High = " + high);
			Print("Low = " + low);
			Print("Contador = " + contador);
			Print("Stop.... ");
		}

        public override void Complete()
        {
        }

        private NewOrderRequest NewRequest(Operation side)
        {
            NewOrderRequest request = null;

            if (Positions.GetPositionByOpenOrderId(posId) == null && toTrade)
            {
                request = new NewOrderRequest()
                {
                    Side = side,
                    Type = tipo,
                    Price = CurrentData.GetPrice(PriceType.Close, 1),
                    Amount = Amount,
                    Account = Accounts.Current,
                    Instrument = Instruments.Current,
                    StopLossOffset = SL * Point,
                    TakeProfitOffset = TP * Point,
                    MagicNumber = MagicNumber
                };
            }

            return request;
        }


    }
}
