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
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class ROBOEstoMediasGalaticasv2 : Strategy
	{
		private StochasticsFast StochasticsFast1;
		private StochasticsFast StochasticsFast2;
		private StochasticsFast StochasticsFast3;
		private StochasticsFast StochasticsFast4;
		private StochasticsFast StochasticsFast5;
		private StochasticsFast StochasticsFast6;
		
		private StochasticsFast StochasticsFast17;
		private StochasticsFast StochasticsFast34;
		private StochasticsFast StochasticsFast72;
				
		private Momentum Momentum1;
		private EMA Momentum2;
		private EMA Momentum3;
		
		private EMA EMA1;
		private EMA EMA2;
		private EMA EMA3;
		private EMA EMA4;
		private EMA EMA5;
		
		private string  atmStrategyId			= string.Empty;
		private string  orderId					= string.Empty;
		
		private bool	isAtmStrategyCreated	= false;
		private bool	comprado				= false;
		private bool	vendido					= false;
		private bool    goodToGo				= true;
		
		private double preco;
		private double currAsk;
		private double currBid;
		private double currentPnL;
		private double dailyPnL;

		private int Cancelamento			= 7;
		private int DayGainStop				= 1200;
		private int DayLossStop				= 200;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"3 fractais em 3 tempos graficos 60, 15 e 4. Fractais acima de 61.8 ou abaixo de 38.2 confirmam tendencia nos tempos maiores. Entradas no grafico de 4 minutos. Compra pequena cruzando 38.2 e aumenta a mao cruzando 61.8. E o inverso para a venda.";
				Name										= "ROBOEstoMediasGalaticasv2";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 2700;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Day;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				IsInstantiatedOnEachOptimizationIteration	= true;							
			}
			else if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Minute, 15);
				AddDataSeries(Data.BarsPeriodType.Minute, 60);
			}
			else if (State == State.DataLoaded)
			{				
				//60m
				StochasticsFast1				= StochasticsFast(Closes[2], 12, 17);
				StochasticsFast2				= StochasticsFast(Closes[2], 12, 72);
				StochasticsFast3				= StochasticsFast(Closes[2], 12, 305);
				
				//15m
				StochasticsFast4				= StochasticsFast(Closes[1], 12, 17);
				StochasticsFast5				= StochasticsFast(Closes[1], 12, 72);
				StochasticsFast6				= StochasticsFast(Closes[1], 12, 305);
				
				//4R
				StochasticsFast17				= StochasticsFast(Close, 12, 17);
				StochasticsFast34				= StochasticsFast(Close, 12, 34);
				StochasticsFast72				= StochasticsFast(Close, 12, 72);
				
				EMA1							= EMA(Close, 8);
				EMA2							= EMA(Close, 17);
				EMA3							= EMA(Open, 17);
				EMA4							= EMA(Close, 34);
				EMA5							= EMA(StochasticsFast17.D, 8);
								
				Momentum1						= Momentum(14);
				Momentum2						= EMA(Momentum1, 2);
				Momentum3						= EMA(Momentum1, 8);
				
				EMA1.Plots[0].Brush = Brushes.Magenta;
				AddChartIndicator(EMA1);
				EMA2.Plots[0].Brush = Brushes.Cyan;
				AddChartIndicator(EMA2);
				EMA3.Plots[0].Brush = Brushes.DarkCyan;
				AddChartIndicator(EMA3);
				EMA4.Plots[0].Brush = Brushes.Orange;
				AddChartIndicator(EMA4);
				
               Draw.TextFixed(this,"Robo", "ROBOEstoMediasGalaticasv2", TextPosition.BottomLeft);
				
                StrategyReset();
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade)
				return;
			
			if (CurrentBars[0] < 72 || CurrentBars[1] < 305 || CurrentBars[2] < 17)
				return;
			
			// OnBarUpdate() will be called on incoming tick events on all Bars objects added to the strategy
			// We only want to process events on our primary Bars object (index = 0) which is set when adding
			// the strategy to a chart
			if (BarsInProgress != 0)
				return;
			
			// Make sure this strategy does not execute against historical data
			if(State == State.Historical)
				return;	
					
			if ((ToTime(Time[0]) <= 180000 && ToTime(Time[0]) >= 150000))
			{
				if (orderId.Length > 0)
				{
				  	AtmStrategyCancelEntryOrder(orderId);
					orderId = string.Empty;
				  	Print(Time[0] +" ROBOEstoMediasGalaticasv2 "+ Instrument.FullName + " Todas Ordens Canceladas");
				}
				if (atmStrategyId.Length > 0)
				{
					AtmStrategyClose(atmStrategyId);
					atmStrategyId = string.Empty;
					Print(Time[0] +" ROBOEstoMediasGalaticasv2 "+ Instrument.FullName + " Todas Posições Fechadas");
				}
			// Reset da Estrategia
				StrategyReset();
				return;
			}
			
			if(goodToGo)
        	{				
						//Meta Diaria
						if (dailyPnL <= (-DayLossStop))
							{
								Print(Time[0] +" ROBOEstoMediasGalaticasv2 "+ Instrument.FullName + " Meta Perda Diaria");
								goodToGo = false;
								return;
							}
						if (dailyPnL >= DayGainStop)
							{
								Print(Time[0] +" ROBOEstoMediasGalaticasv2 "+ Instrument.FullName + " Meta Ganho Diario");
								goodToGo = false;
								return;
							}
							
						//Medias Alinhadas
			            //Compra
						if 	((orderId.Length == 0 && atmStrategyId.Length == 0)
							
							&& (StochasticsFast1.K[0] > 50)  // 60m - 17
							
							&& (StochasticsFast4.K[0] > 50)  // 15m - 17
							&& (StochasticsFast5.K[0] > 50)  // 15m - 72
							&& (StochasticsFast6.K[0] > 50)  // 15m - 305
							 
							&& (Close[1] > Open[1])
							
							&& (EMA1[1] > EMA2[1])
							&& (EMA2[1] > EMA3[1])
							&& (EMA3[1] > EMA4[1])
							
							&& (EMA1[1] - EMA2[1] >= 0.5)
							
							&& (StochasticsFast17.D[1] < 60)
							&& (StochasticsFast17.D[1] > EMA5[1])
							)
						{
							isAtmStrategyCreated = false;  // reset atm strategy created check to false
							atmStrategyId = GetAtmStrategyUniqueId();
							orderId = GetAtmStrategyUniqueId();
							AtmStrategyCreate(OrderAction.Buy, OrderType.Limit, (GetCurrentBid(0) - 1 * TickSize), 0, TimeInForce.Gtc, orderId, "ATM_MediasAlinhadas", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
								//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
								if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
									isAtmStrategyCreated = true;
							});
								comprado = true;
								vendido = false;
								preco = (GetCurrentBid(0) - 1 * TickSize);
								Print(Time[0] + " " + Instrument.FullName + " Compra Medias Alinhadas");
						}
						// Venda
						if ((orderId.Length == 0 && atmStrategyId.Length == 0)
							
							&& (StochasticsFast1.K[0] < 50)  // 60m - 17
							
							&& (StochasticsFast4.K[0] < 50)  // 15m - 17
							&& (StochasticsFast5.K[0] < 50)  // 15m - 72
							&& (StochasticsFast6.K[0] < 50)  // 15m - 305
							
							&& (Close[1] < Open[1])
							
							&& (EMA1[1] < EMA2[1])
							&& (EMA2[1] < EMA3[1])
							&& (EMA3[1] < EMA4[1])
							
							&& (EMA2[1] - EMA1[1] >= 0.5)
							
							&& (StochasticsFast17.D[1] > 40)
							&& (StochasticsFast17.D[1] < EMA5[1])
							)
						{
							isAtmStrategyCreated = false;  // reset atm strategy created check to false
							atmStrategyId = GetAtmStrategyUniqueId();
							orderId = GetAtmStrategyUniqueId();
							AtmStrategyCreate(OrderAction.Sell, OrderType.Limit, (GetCurrentAsk(0) + 1 * TickSize), 0, TimeInForce.Gtc, orderId, "ATM_MediasAlinhadas", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
								//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
								if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
									isAtmStrategyCreated = true;
							});
								vendido = true;
								comprado = false;
								preco = (GetCurrentAsk(0) + 1 * TickSize);
								Print(Time[0] + " " + Instrument.FullName + " Venda Medias Alinhdas");
						}

						//Acumulo Stochastic
						// Compra
						if ((orderId.Length == 0 && atmStrategyId.Length == 0)
													
							&& (StochasticsFast17.D[2] <= 25)						//Acumulo EstoGalático 
							&& (StochasticsFast34.D[2] <= 25)
							&& (StochasticsFast72.D[2] <= 25)
							
							&& (StochasticsFast17.D[0] > StochasticsFast34.D[0])	//Cruzamento Estocastico
							
							&& (EMA2[2] - EMA1[2] <= 0.5)
							&& (EMA3[2] - EMA2[2] <= 0.7)
							&& (Open[1] == Open[2])									//Pivot
							&& (Close[1] > Open[1]) 								//Box a Favor
							
							&& (Momentum1[0] > Momentum3[0])
							) 
						{
							isAtmStrategyCreated = false;  // reset atm strategy created check to false
							atmStrategyId = GetAtmStrategyUniqueId();
							orderId = GetAtmStrategyUniqueId();
							AtmStrategyCreate(OrderAction.Buy, OrderType.Limit, Close[1], 0, TimeInForce.Day, orderId, "ATM_EstoGalatico", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
								//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
								if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
									isAtmStrategyCreated = true;
							});
								comprado = true;
								vendido = false;
								preco = Close[1];
								Print(Time[0] +" ROBOEstoMediasGalaticasv2 "+ Instrument.FullName + " Compra EstoGalatica");
						}
						// Venda
						if ((orderId.Length == 0 && atmStrategyId.Length == 0)
														
							&& (StochasticsFast17.D[2] >= 75)						//Acumulo EstoGalático 
							&& (StochasticsFast34.D[2] >= 75)
							&& (StochasticsFast72.D[2] >= 75)
							
							&& (StochasticsFast17.D[0] < StochasticsFast34.D[0])	//Cruzamento Estocastico
							
							&& (EMA1[2] - EMA2[2] <= 0.5) 
							&& (EMA2[2] - EMA3[2] <= 0.7)
							&& (Open[1] == Open[2])									//Pivot
							&& (Close[1] < Open[1]) 								//Box a Favor
							
							&& (Momentum2[0] < Momentum3[0])
							)
						{
							isAtmStrategyCreated = false;  // reset atm strategy created check to false
							atmStrategyId = GetAtmStrategyUniqueId();
							orderId = GetAtmStrategyUniqueId();
							AtmStrategyCreate(OrderAction.Sell, OrderType.Limit, Close[1], 0, TimeInForce.Day, orderId, "ATM_EstoGalatico", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
								//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
								if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
									isAtmStrategyCreated = true;
							});
								vendido = true;
								comprado = false;
								preco = Close[1];
								Print(Time[0] +" ROBOEstoMediasGalaticasv2 "+ Instrument.FullName + " Venda EstoGalatica");
						}

       		}
			
			if (!isAtmStrategyCreated)
				return;
		
			// Check for a pending entry order
			if (orderId.Length > 0)
			{
				string[] status = GetAtmStrategyEntryOrderStatus(orderId);

				// If the status call can't find the order specified, the return array length will be zero otherwise it will hold elements
				if (status.GetLength(0) > 0)
				{
					// If the order state is terminal, reset the order id value
					if (status[2] == "Filled" || status[2] == "Cancelled" || status[2] == "Rejected")
					{
						Print(Time[0] +" ROBOEstoMediasGalaticasv2 "+ Instrument.FullName + " State: " + status[2]);
						orderId = string.Empty;
					}
				}
			} 
			
			// If the strategy has terminated reset the strategy id
			else if (atmStrategyId.Length > 0 && GetAtmStrategyMarketPosition(atmStrategyId) == Cbi.MarketPosition.Flat)
			{
				ScreenUpdate();
				atmStrategyId = string.Empty;
			}

			//Cancelaqmento de Ordem nao executada 
			if (atmStrategyId.Length > 0)
			{
				
				CancelamentoDeOrdem();
			}
								
		}
		
		public void ScreenUpdate()
		{
			if (atmStrategyId.Length > 0)
				currentPnL = GetAtmStrategyRealizedProfitLoss(atmStrategyId);
			else
			 	currentPnL = 0;
			
			dailyPnL = dailyPnL + currentPnL;
			Draw.TextFixed(this,"Info", "Daily PnL = " + dailyPnL, TextPosition.BottomRight);
		}
		
		public void StrategyReset()
		{
			goodToGo = true;	
			dailyPnL = 0;
			currentPnL = 0;
			Draw.TextFixed(this,"Info", "Daily PnL = " + dailyPnL, TextPosition.BottomRight);
	
		}
		
		public void CancelamentoDeOrdem()
		{
				
				currAsk = GetCurrentAsk(0);
				currBid = GetCurrentBid(0);
					
					if (((GetAtmStrategyMarketPosition(atmStrategyId) == Cbi.MarketPosition.Flat) && (comprado == true && (currBid - Cancelamento * TickSize) >= preco)) 
						|| ((GetAtmStrategyMarketPosition(atmStrategyId) == Cbi.MarketPosition.Flat) && (vendido == true && (currAsk + Cancelamento * TickSize) <= preco)))
					{
							AtmStrategyCancelEntryOrder(orderId);
							Print(Time[0] +" ROBOEstoMediasGalaticasv2 "+ Instrument.FullName + " Ordem CANCELADA");
							atmStrategyId = string.Empty;
							orderId = string.Empty;
							comprado = false;
							vendido = false;
					}
		}
		
		#region ConnectionHandling
		protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs connectionStatusUpdate)
			{
			if(connectionStatusUpdate.Status == ConnectionStatus.Connected)
			  {
			    Print("ROBOEstoMediasGalaticasv2 "+ Instrument.FullName + " Connected at " + DateTime.Now);
				  if (atmStrategyId.Length > 0)
				  {
					AtmStrategyClose(atmStrategyId);
					atmStrategyId = string.Empty;
					Print(DateTime.Now +" ROBOEstoMediasGalaticasv2 "+ Instrument.FullName + " Todas ATMs Fechadas");
				  }
				  
				  if (orderId.Length > 0)
					{
						AtmStrategyCancelEntryOrder(orderId);
						orderId = string.Empty;
						Print(DateTime.Now +" ROBOEstoMediasGalaticasv2 "+ Instrument.FullName + " Todas Ordens Canceladas");
					}
				  
				  if (PositionAccount.MarketPosition != MarketPosition.Flat)
				  {
					if (PositionAccount.MarketPosition == MarketPosition.Long)
					{
						ExitLong();
					}
					else
					{
						ExitShort();
					}
					Print(DateTime.Now +" ROBOEstoMediasGalaticasv2 "+ Instrument.FullName + " Todas Posições Fechadas");
				  }
			  }
			  
			  else if(connectionStatusUpdate.Status == ConnectionStatus.ConnectionLost)
			  {
			    Print("ROBOEstoMediasGalaticasv2 "+ Instrument.FullName + " Connection lost at: " + DateTime.Now);
				if (orderId.Length > 0)
				{
				  AtmStrategyCancelEntryOrder(orderId);
				  orderId = string.Empty;
				  Print(DateTime.Now +" ROBOEstoMediasGalaticasv2 "+ Instrument.FullName + " Todas Ordens Canceladas");
				}
			  }
			}
        #endregion

	}
}
