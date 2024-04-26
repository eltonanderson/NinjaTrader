using PTLRuntime.NETScript;
using PTLRuntime.NETScript.Indicators;
using System.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Box4RWick
{
    public class Box4RWick : NETStrategy
    {
        public Box4RWick()
            : base()
        {
            base.ProjectName = "Box4RWick";
            base.Version = "Version 1";
            base.Comments = "Baseado na agressao de um box de renko calculado pelo PVT atual menos o passado";
        }

        [InputParameter("Caixa de Renko", 0, 1, 999)]
        public int caixa = 4;

        [InputParameter("Magic Number", 1, 1, 999)]
        public int MagicNumber = 33;

        [InputParameter("Lote", 2, 1, 999)]
        public int Lote = 1;

        [InputParameter("StopLoss", 3, 1, 999)]
        public int StopLoss = 5;

        [InputParameter("Take Profit", 4, 1, 999)]
        public int TakeProfit = 1;
        
        [InputParameter("Ordem", 5, new object[]
                        {
                        	"Limit", OrdersType.Limit,
                        	"Market", OrdersType.Market
                        })]
        public OrdersType tipo = OrdersType.Limit;

        [InputParameter("Inicio das operacoes", 6, 1, 999)]
        public TimeSpan sessionStart = new TimeSpan(11, 15, 00);

        [InputParameter("Termino das operacoes", 7, 1, 999)]
        public TimeSpan sessionEnd = new TimeSpan(19, 00, 00);

        private string posId;
        private State state;
		private Position pos, closedPosition;
		
        BarData curData;

        public override void Init()
        {
            curData = CurrentData as BarData;

            state = State.ExitMarket;
        }

        public override void NextBar()
        {
            if (!IsActiveSession)
                return;
            
            closedPosition = Positions.GetClosedPositionById(posId);

			if (closedPosition != null)
			{
				state = State.ExitMarket;
			}

            double Clo = curData.Close(1);
            double preClo = curData.Close(2);
            double preClo2 = curData.Close(3);
            double Abe = curData.Open(2);
            double preAbe2 = curData.Open(3);
            double Max = curData.High(1);
            double Min = curData.Low(1);
            
			bool pivot = Clo == preAbe2 || Clo == preClo2 && Abe == preAbe2;

            bool buy  = (Max - Min) - caixa > 0 && Clo > preClo && !pivot;
            bool sell = (Max - Min) - caixa > 0 && Clo < preClo && !pivot;
            
            if (state == State.EnteredBuy && sell || state == State.EnteredShort && buy)
            {
                ClosePosition(posId);
                state = State.ExitMarket;
            }
            
            if (buy)
            {
            	//pos = Positions.GetPositionByOpenOrderId(posId);

                if (state != State.EnteredBuy)
                {
                    NewOrderRequest request = new NewOrderRequest()
                    {
                        Side = Operation.Buy,
                        MarketRange = 30,
                        Type = tipo,
                        Price = CurrentData.GetPrice(PriceType.Close, 1),
                        Amount = Lote,
                        Account = Accounts.Current,
                        Instrument = Instruments.Current,
                        StopLossOffset = StopLoss * Point,
                        TakeProfitOffset = TakeProfit * Point,
                        MagicNumber = MagicNumber
                    };
                    posId = Orders.Send(request);
                    state = State.EnteredBuy;
					
                }
            }

            else if (sell)
            {
                var position = Positions.GetPositionByOpenOrderId(posId);

                if (state != State.EnteredShort)
                {
                    NewOrderRequest request = new NewOrderRequest()
                    {
                        Side = Operation.Sell,
                        MarketRange = 30,
                        Type = tipo,
                        Price = CurrentData.GetPrice(PriceType.Close, 1),
                        Amount = Lote,
                        Account = Accounts.Current,
                        Instrument = Instruments.Current,
                        StopLossOffset = StopLoss * Point,
                        TakeProfitOffset = TakeProfit * Point,
                        MagicNumber = MagicNumber
                    };
                    posId = Orders.Send(request);
                    state = State.EnteredShort;
					
                }
            }

        }

        public override void OnQuote()
        {
            ClosePositionBySession();
            
			Order[] All_ord = Orders.GetOrders();
			
			foreach (var ord in All_ord)
			{
				var entrada = ord.Price;
				Print("Ordem entrada = " + entrada);
				Print("Lado da ordem = " + ord.Side);
				
				if (ord.Side == Operation.Buy && CurrentData.GetPrice(PriceType.Close, 0) > entrada + 3 * Point || 
				    ord.Side == Operation.Sell && CurrentData.GetPrice(PriceType.Close, 0) < entrada - 3 * Point)
					Orders.CancelAll();
			}

        }

        public override void Complete()
        {
        }
        
        #region Utils
        
        bool IsActiveSession
        {
            get
            {
                var time = CurrentData.Time().TimeOfDay;
                return (sessionStart < sessionEnd) ? (time >= sessionStart && time <= sessionEnd) : (time >= sessionStart || time <= sessionEnd);
            }
        }

        void ClosePositionBySession()
        {
			if (!IsActiveSession && posId != null)
				ClosePosition(posId);
        }

        void ClosePosition(string Id)
        {
            pos = Positions.GetPositionByOpenOrderId(Id);

            if (pos != null)
            {
                pos.Close();
				state = State.ExitMarket;
            }
        }

        internal enum State
        {
            EnteredBuy,
            EnteredShort,
            ExitMarket
        }
        #endregion
    }
}
