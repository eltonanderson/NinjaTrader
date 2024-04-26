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
	public class RenkoFastv2 : Strategy
	{
		private StochasticsFast StochasticsFast1;
		private EMA EMA1;
		private OBV PVT1;
		private SMA SMA1;
		
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
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"PVT com Estocastico";
				Name										= "RenkoFastv2";
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
				
				Distancia				= 1;
				Cancelamento			= 9;
				DayGainStop				= 1000;
				DayLossStop				= 1000;
			}
			
			else if (State == State.DataLoaded)
			{				
				StochasticsFast1	= StochasticsFast(Close, 1, 72);
				EMA1				= EMA(StochasticsFast1.K, 17);
				PVT1				= OBV(Close);
				SMA1				= SMA(PVT1, 50);
				
				Draw.TextFixed(this,"Robo", "RenkoFastv2", TextPosition.BottomLeft);
				
                StrategyReset();
			}
		}

			protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade)
				return;

			if(State == State.Historical)
				return;	

			if ((ToTime(Time[0]) >= 150000 && ToTime(Time[0]) <= 190000))
			{
				if (orderId.Length > 0)
				{
				  	AtmStrategyCancelEntryOrder(orderId);
					orderId = string.Empty;
				  	Print(DateTime.Now +" RenkoFastv2 "+ Instrument.FullName + " Todas Ordens Canceladas");
				}
				if (atmStrategyId.Length > 0)
				{
					AtmStrategyClose(atmStrategyId);
					atmStrategyId = string.Empty;
					Print(DateTime.Now +" RenkoFastv2 "+ Instrument.FullName + " Todas Posições Fechadas");
				}
			// Reset da Estrategia
				StrategyReset();
				return;
			}	

	#region GoodToGo
	if(goodToGo)
		{
			//Condicao de saida
			if ((comprado && (StochasticsFast1.K[0] < EMA1[0])) || (vendido && (StochasticsFast1.K[0] > EMA1[0])))
			{
				AtmStrategyClose(atmStrategyId);
				atmStrategyId = string.Empty;
				Print(DateTime.Now +" RenkoFastv2 "+ Instrument.FullName + " Posicao Fechada");
			}

			//Meta Diaria
			if (dailyPnL <= (-DayLossStop))
				{
					Print(DateTime.Now +" RenkoFastv2 "+ Instrument.FullName + " Meta Perda Diaria");
					goodToGo = false;
					return;
				}
			if (dailyPnL >= DayGainStop)
				{
					Print(DateTime.Now +" RenkoFastv2 "+ Instrument.FullName + " Meta Ganho Diario");
					goodToGo = false;
					return;
				}
			/*
			//Hora Sangrenta
			if ((Times[0][0].TimeOfDay >= OpenBloodHour.TimeOfDay) && (Times[0][0].TimeOfDay <= CloseBloodHour.TimeOfDay))
				{
						//ScreenUpdate();
						Print(DateTime.Now +" RenkoFastv2 "+ Instrument.FullName + " Hora Sangrenta!");
										
						if (orderId.Length > 0)
							{
							  	CancelamentoDeOrdem();
							}
						
						if (atmStrategyId.Length > 0 && GetAtmStrategyMarketPosition(atmStrategyId) == Cbi.MarketPosition.Flat)
							{
								ScreenUpdate();
								atmStrategyId = string.Empty;
							}
						return;
				}
			*/
			
			//Ultima Entrada
			if ((ToTime(Time[0]) >= 140000 && ToTime(Time[0]) < 150000))
			{
			    ScreenUpdate();
				Print(DateTime.Now +" RenkoFastv2 "+ Instrument.FullName + " Fechamento Próximo");
				Print(DateTime.Now +" RenkoFastv2 "+ Instrument.FullName + " Sem mais Entradas Hoje");

				if (orderId.Length > 0)
				{
				  	CancelamentoDeOrdem();
				}
                goodToGo = false;
				return;
			}
		
			// Compra
			if ((orderId.Length == 0 && atmStrategyId.Length == 0)
				&& (StochasticsFast1.K[0] > EMA1[0])
				 && (PVT1[0] > SMA1[0]))
			{
				isAtmStrategyCreated = false;  // reset atm strategy created check to false
				atmStrategyId = GetAtmStrategyUniqueId();
				orderId = GetAtmStrategyUniqueId();
				AtmStrategyCreate(OrderAction.Buy, OrderType.Limit, (GetCurrentBid(0) - Distancia * TickSize), 0, TimeInForce.Day, orderId, "ATM_BreakEven2Stages", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
					//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
					if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
						isAtmStrategyCreated = true;
				});
					comprado = true;
					vendido = false;
					preco = (GetCurrentBid(0) - Distancia * TickSize);
			}
			// Venda
			if ((orderId.Length == 0 && atmStrategyId.Length == 0)
				&& (StochasticsFast1.K[0] < EMA1[0])
				 && (PVT1[0] < SMA1[0]))
			{
				isAtmStrategyCreated = false;  // reset atm strategy created check to false
				atmStrategyId = GetAtmStrategyUniqueId();
				orderId = GetAtmStrategyUniqueId();
				AtmStrategyCreate(OrderAction.Sell, OrderType.Limit, (GetCurrentAsk(0) + Distancia * TickSize), 0, TimeInForce.Day, orderId, "ATM_BreakEven2Stages", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
					//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
					if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
						isAtmStrategyCreated = true;
				});
					vendido = true;
					comprado = false;
					preco = (GetCurrentAsk(0) + Distancia * TickSize);
			}
		}
		#endregion
		
		#region Gerenciamento de ATM
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
						Print(DateTime.Now +" RenkoFastv2 "+ Instrument.FullName + " State: " + status[2]);
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
		#endregion
		
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
							Print(DateTime.Now +" RenkoFastv2 "+ Instrument.FullName + " Ordem CANCELADA");
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
			    Print(DateTime.Now + " RenkoFastv2 "+ Instrument.FullName + " Connected at " + DateTime.Now);
				  if (atmStrategyId.Length > 0)
				  {
					AtmStrategyClose(atmStrategyId);
					atmStrategyId = string.Empty;
					Print(DateTime.Now +" RenkoFastv2 "+ Instrument.FullName + " Todas ATMs Fechadas");
				  }
				  
				  if (orderId.Length > 0)
					{
						AtmStrategyCancelEntryOrder(orderId);
						orderId = string.Empty;
						Print(DateTime.Now +" RenkoFastv2 "+ Instrument.FullName + " Todas Ordens Canceladas");
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
					Print(DateTime.Now +" RenkoFastv2 "+ Instrument.FullName + " Todas Posições Fechadas");
				  }
			  }
			  
			  else if(connectionStatusUpdate.Status == ConnectionStatus.ConnectionLost)
			  {
			    Print(DateTime.Now +" RenkoFastv2 "+ Instrument.FullName + " Connection lost at: " + DateTime.Now);
				if (orderId.Length > 0)
				{
				  AtmStrategyCancelEntryOrder(orderId);
				  orderId = string.Empty;
				  Print(DateTime.Now +" RenkoFastv2 "+ Instrument.FullName + " Todas Ordens Canceladas");
				}
			  }
			}
        #endregion
		
		#region Parametros
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Distancia (Preço Limite)", Order=1, GroupName="Parameters")]
		public int Distancia
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Cancelamento (Se Nao Executado)", Order=2, GroupName="Parameters")]
		public int Cancelamento
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Ganho Satisfatório", Order=3, GroupName="Parameters")]
		public double DayGainStop
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Perda Máxima", Order=4, GroupName="Parameters")]
		public double DayLossStop
		{ get; set; }
		#endregion
	}
}
