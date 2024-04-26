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
	public class EMAFastOBV3Rv2 : Strategy
	{
		
		private EMA EMA1;
		private EMA EMA2;
		private EMA EMA3;
		private EMA EMA4;

		private StochasticsFast StochasticsFast17;
		private EMA Stoch1712;
		
		private StochasticsFast StochasticsFast34;
		private EMA Stoch3412;
		
		private StochasticsFast StochasticsFast72;
		private EMA Stoch7212;
		private EMA Stoch7217;
		
		private OBV OBV1;
		private SMA OBV2;
		private EMA OBV3;
		
		private string  atmStrategyId			= string.Empty;
		private string  orderId					= string.Empty;
		
		private bool	isAtmStrategyCreated	= false;
		private bool	comprado				= false;
		private bool	vendido					= false;
		private bool    goodToGo				= true;
		private bool	ModoNormal				= true;
		private bool	ModoRecuperacao			= false;
		private bool	Consolidation			= false;
		
		private double preco;
		private double currAsk;
		private double currBid;
		private double currentPnL;
		private double dailyPnL;
		
		private int Distancia				= 2;
		private int Cancelamento			= 7;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"OBV com Estocastico e EMA";
				Name										= "EMAFastOBV3Rv2";
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
				
				DayGainStop				= 1800;
				DayLossStop				= 600;
			}
			
			else if (State == State.DataLoaded)
			{				
		
				EMA1				= EMA(Close, 9);
				EMA2				= EMA(Close, 12);
				
				EMA3				= EMA(Close, 17);
				EMA4				= EMA(Open, 17);
				
				StochasticsFast17	= StochasticsFast(Close, 1, 17);
				Stoch1712			= EMA(StochasticsFast17.K, 12);
				
				StochasticsFast34	= StochasticsFast(Close, 1, 34);
				Stoch3412			= EMA(StochasticsFast34.K, 12);
				
				StochasticsFast72	= StochasticsFast(Close, 1, 72);
				Stoch7212			= EMA(StochasticsFast72.K, 12);
				Stoch7217			= EMA(StochasticsFast72.K, 17);
					
				OBV1				= OBV(Close);
				OBV2				= SMA(OBV1, 42);
				OBV3				= EMA(OBV1, 4);
				
				EMA1.Plots[0].Brush = Brushes.Cyan;
				AddChartIndicator(EMA1);
				EMA2.Plots[0].Brush = Brushes.DarkCyan;
				AddChartIndicator(EMA2);
				
				EMA3.Plots[0].Brush = Brushes.Magenta;
				AddChartIndicator(EMA3);
				EMA4.Plots[0].Brush = Brushes.DarkMagenta;
				AddChartIndicator(EMA4);
		
				
				Draw.TextFixed(this,"Robo", "EMAFastOBV3Rv2", TextPosition.BottomLeft);
				
                StrategyReset();
			}
		}

			protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade)
				return;
			if(State == State.Historical)
				return;	

			if ((ToTime(Time[0]) <= 180000 && ToTime(Time[0]) >= 150000))
			{
				if (orderId.Length > 0)
				{
				  	AtmStrategyCancelEntryOrder(orderId);
					orderId = string.Empty;
				  	Print(Time[0] +" EMAFastOBV3Rv2 "+ Instrument.FullName + " Todas Ordens Canceladas");
				}
				if (atmStrategyId.Length > 0)
				{
					AtmStrategyClose(atmStrategyId);
					atmStrategyId = string.Empty;
					Print(Time[0] +" EMAFastOBV3Rv2 "+ Instrument.FullName + " Todas Posições Fechadas");
				}
			// Reset da Estrategia
				StrategyReset();
				return;
			}	

		#region GoodToGo
		if(goodToGo)
			{
					/*
					//Meta Diaria
					if (dailyPnL <= (-DayLossStop))
						{
							Print(Time[0] +" EMAFastOBV3Rv2 "+ Instrument.FullName + " Meta Perda Diaria");
							goodToGo = false;
							return;
						}
					if (dailyPnL >= DayGainStop)
						{
							Print(Time[0] +" EMAFastOBV3Rv2 "+ Instrument.FullName + " Meta Ganho Diario");
							goodToGo = false;
							return;
						}
					/*
					//Hora Sangrenta
					if ((ToTime(Time[0]) >= 90000 && ToTime(Time[0]) < 100000))
						{
								Print(Time[0] +" EMAFastOBV3Rv2 "+ Instrument.FullName + " Hora Sangrenta!");
												
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
					
					//Ultima Entrada
					if ((ToTime(Time[0]) >= 140000 && ToTime(Time[0]) < 150000))
					{
					    ScreenUpdate();
						Print(Time[0] +" EMAFastOBV3Rv2 "+ Instrument.FullName + " Fechamento Próximo");
						Print(Time[0] +" EMAFastOBV3Rv2 "+ Instrument.FullName + " Sem mais Entradas Hoje");

						if (orderId.Length > 0)
						{
						  	CancelamentoDeOrdem();
						}
		                goodToGo = false;
						return;
					}*/
				
				//Agulhada
				if ((ToTime(Time[0]) >= 180000 || ToTime(Time[0]) <= 150000))
					{
						// Compra
						if ((orderId.Length == 0 && atmStrategyId.Length == 0)
							&& (EMA1[1] > EMA2[1]) 								//EMAs
							&& (EMA1[2] > EMA2[2])								
							&& (EMA3[1] > EMA4[1]) 								//CrossEMAs
							&& (EMA3[2] > EMA4[2])								
							&& (EMA2[1] > EMA3[1])								//Leque EMAs
							&& (Open[1] == Open[2])								//Pivot
					
							&& (Close[1] >= EMA1[1])								//Agulhada
							&& (Close[2] <= EMA3[2])							
					
							&& (Close[0] > Open[0]) 							//Box Atual
							
							//&& (Stoch7212[1] > Stoch7217[1])					//Stochastics
							&& (Stoch1712[1] > Stoch7212[1])
							
							//&& (Stoch1712[2] <= 80)								//Acumulo EstoGalático 
							//&& (Stoch3412[2] <= 50)
							//&& (Stoch7212[2] <= 50)
							
							//&& (((Stoch1712[1] > Stoch3412[1]) 					//3 Estocasticos Alinhados
							//&& (Stoch3412[1] > Stoch7212[1]))  
							//|| ((Stoch1712[1] > Stoch7212[1]) 					// ou Stoch 17 e 34 acima do 72 + OBV Cruzado
							//&& (Stoch3412[1] > Stoch7212[1]) 
							//&& (OBV3[1] > OBV2[1]))) 	
							
							&& (OBV3[0] > OBV3[1])								//Inclinacao OBV
							&& (OBV3[1] > OBV3[2])
							//&& (OBV3[1] > OBV3[2])
							)
						{
							isAtmStrategyCreated = false;  // reset atm strategy created check to false
							atmStrategyId = GetAtmStrategyUniqueId();
							orderId = GetAtmStrategyUniqueId();
							AtmStrategyCreate(OrderAction.Buy, OrderType.Limit, Close[1], 0, TimeInForce.Day, orderId, "ATM_Agulhada", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
								//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
								if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
									isAtmStrategyCreated = true;
							});
								comprado = true;
								vendido = false;
								preco = Close[1];
								Print(Time[0] +" EMAFastOBV3Rv2 "+ Instrument.FullName + " Compra Agulhada");
						}
						// Venda
						if ((orderId.Length == 0 && atmStrategyId.Length == 0)
							&& (EMA1[1] < EMA2[1])								//EMAs	
							&& (EMA1[2] < EMA2[2])							
							&& (EMA3[1] < EMA4[1])								//CrossEMAs
							&& (EMA3[2] < EMA4[2])							
							&& (EMA2[1] < EMA3[1])								//Leque EMAs
							&& (Open[1] == Open[2])								//Pivot
					
							&& (Close[1] <= EMA1[1])								//Agulhada
							&& (Close[2] >= EMA3[2])							
					
							&& (Close[0] < Open[0]) 							//Box Atual
							
							//&& (Stoch7212[1] < Stoch7217[1])					//Stochastics
							&& (Stoch1712[1] < Stoch7212[1])
							
							//&& (Stoch1712[2] >= 20)								//Acumulo EstoGalático 
							//&& (Stoch3412[2] >= 50)
							//&& (Stoch7212[2] >= 50)
							
							//&& (((Stoch1712[1] < Stoch3412[1]) 					//3 Estocasticos Alinhados
							//&& (Stoch3412[1] < Stoch7212[1]))   
							//|| ((Stoch1712[1] < Stoch7212[1]) 					// ou Stoch 17 e 34 acima do 72 + OBV Cruzado
							//&& (Stoch3412[1] < Stoch7212[1]) 
							//&& (OBV3[1] < OBV2[1]))) 
							
							&& (OBV3[0] < OBV3[1])								//Inclinacao OBV
							&& (OBV3[1] < OBV3[2])
							//&& (OBV3[2] < OBV3[3])
							)
						{
							isAtmStrategyCreated = false;  // reset atm strategy created check to false
							atmStrategyId = GetAtmStrategyUniqueId();
							orderId = GetAtmStrategyUniqueId();
							AtmStrategyCreate(OrderAction.Sell, OrderType.Limit, Close[1], 0, TimeInForce.Day, orderId, "ATM_Agulhada", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
								//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
								if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
									isAtmStrategyCreated = true;
							});
								vendido = true;
								comprado = false;
								preco = Close[1];
								Print(Time[0] +" EMAFastOBV3Rv2 "+ Instrument.FullName + " Venda Agulhada");
						}
					}
					
				
					//Acumulo Stochastic
					if ((ToTime(Time[0]) >= 180000 || ToTime(Time[0]) <= 150000))
					{
						// Compra
						if ((orderId.Length == 0 && atmStrategyId.Length == 0)
													
							&& (Stoch1712[2] <= 25)								//Acumulo EstoGalático 
							&& (Stoch3412[2] <= 25)
							&& (Stoch7212[2] <= 25)
							
							&& (Stoch1712[0] > Stoch3412[0])					//Cruzamento Estocastico
							
							&& (EMA2[2] - EMA1[2] <= 0.35)
							&& (EMA3[2] - EMA2[2] <= 0.45)
							&& (Open[1] == Open[2])								//Pivot
							&& (Close[0] > Open[0]) 							//Box Atual
							
							&& (OBV3[0] > OBV3[1])								//Inclinacao OBV
							&& (OBV3[1] > OBV3[2])
							) 
						{
							isAtmStrategyCreated = false;  // reset atm strategy created check to false
							atmStrategyId = GetAtmStrategyUniqueId();
							orderId = GetAtmStrategyUniqueId();
							AtmStrategyCreate(OrderAction.Buy, OrderType.Market, Close[1], 0, TimeInForce.Day, orderId, "ATM_EstoGalatico", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
								//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
								if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
									isAtmStrategyCreated = true;
							});
								comprado = true;
								vendido = false;
								preco = Close[1];
								Print(Time[0] +" EMAFastOBV3Rv2 "+ Instrument.FullName + " Compra EstoGalatica");
						}
						// Venda
						if ((orderId.Length == 0 && atmStrategyId.Length == 0)
														
							&& (Stoch1712[2] >= 75)								//Acumulo EstoGalático 
							&& (Stoch3412[2] >= 75)
							&& (Stoch7212[2] >= 75)
							
							&& (Stoch1712[0] < Stoch3412[0])					//Cruzamento Estocastico
							
							&& (EMA1[2] - EMA2[2] <= 0.35) 
							&& (EMA2[2] - EMA3[2] <= 0.45)
							&& (Open[1] == Open[2])								//Pivot
							&& (Close[0] < Open[0]) 							//Box Atual
							
							&& (OBV3[0] < OBV3[1])								//Inclinacao OBV
							&& (OBV3[1] < OBV3[2])
							)
						{
							isAtmStrategyCreated = false;  // reset atm strategy created check to false
							atmStrategyId = GetAtmStrategyUniqueId();
							orderId = GetAtmStrategyUniqueId();
							AtmStrategyCreate(OrderAction.Sell, OrderType.Market, Close[1], 0, TimeInForce.Day, orderId, "ATM_EstoGalatico", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
								//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
								if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
									isAtmStrategyCreated = true;
							});
								vendido = true;
								comprado = false;
								preco = Close[1];
								Print(Time[0] +" EMAFastOBV3Rv2 "+ Instrument.FullName + " Venda EstoGalatica");
						}
					}
					
					//Tendencia
					if ((ToTime(Time[0]) >= 180000 || ToTime(Time[0]) <= 150000))
					{
						// Compra
						if ((orderId.Length == 0 && atmStrategyId.Length == 0)
							//&& (Stoch1712[2] >= 80)								//Tendencia no Estocastico 
							//&& (Stoch3412[2] >= 80)
							//&& (Stoch7212[2] >= 80)
													
							&& (Close[2] - EMA1[2] <= 0.7)
							//&& ((EMA1[2] - EMA2[2] >= 0.35) && (EMA1[2] - EMA2[2] <= 0.65))
							//&& ((EMA2[2] - EMA3[2] >= 0.65) && (EMA2[2] - EMA3[2] <= 0.85))
							
							&& (Open[1] == Open[2])								//Pivot
							&& (Close[0] > Open[0]) 							//Box Atual
							
							&& (OBV3[1] > OBV2[1])								//Cruzamento OBV
							
							&& (Stoch1712[1] > Stoch3412[1])
							&& (Stoch3412[1] > Stoch7212[1])
							&& (Stoch1712[2] - Stoch3412[2] >= 6)
							&& ((Stoch3412[2] - Stoch7212[2] >= 6) && (Stoch3412[2] - Stoch7212[2] <= 20))
							
							&& ((EMA1[2] - EMA2[2] >= 0.25) && (EMA1[2] - EMA2[2] <= 0.65))
							&& ((EMA2[2] - EMA3[2] >= 0.25) && (EMA2[2] - EMA3[2] <= 0.85))
							
							) 
						{
							isAtmStrategyCreated = false;  // reset atm strategy created check to false
							atmStrategyId = GetAtmStrategyUniqueId();
							orderId = GetAtmStrategyUniqueId();
							AtmStrategyCreate(OrderAction.Buy, OrderType.Limit, Close[1], 0, TimeInForce.Day, orderId, "ATM_EMAFastOBVTrend", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
								//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
								if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
									isAtmStrategyCreated = true;
							});
								comprado = true;
								vendido = false;
								preco = Close[1];
								Print(Time[0] +" EMAFastOBV3Rv2 "+ Instrument.FullName + " Compra Tendencia");
						}
						// Venda
						if ((orderId.Length == 0 && atmStrategyId.Length == 0)
							//&& (Stoch1712[2] <= 20)								//Tendencia no Estocastico 
							//&& (Stoch3412[2] <= 20)
							//&& (Stoch7212[2] <= 20)
													
							&& (EMA1[2] - Close[2] <= 0.7)
							//&& ((EMA2[2] - EMA1[2] >= 0.35) && (EMA2[2] - EMA1[2] <= 0.65))
							//&& ((EMA3[2] - EMA2[2] >= 0.65) && (EMA3[2] - EMA2[2] <= 0.85))
							
							&& (Open[1] == Open[2])								//Pivot
							&& (Close[0] < Open[0]) 							//Box Atual
							
							&& (OBV3[1] < OBV2[1])								//Cruzamento OBV
							
							&& (Stoch1712[1] < Stoch3412[1])
							&& (Stoch3412[1] < Stoch7212[1])
							&& (Stoch3412[2] - Stoch1712[2] >= 6)
							&& ((Stoch7212[2] - Stoch3412[2] >= 6) && (Stoch7212[2] - Stoch3412[2] <= 20))
							
							&& ((EMA2[2] - EMA1[2] >= 0.25) && (EMA2[2] - EMA1[2] <= 0.65))
							&& ((EMA3[2] - EMA2[2] >= 0.25) && (EMA3[2] - EMA2[2] <= 0.85))
							) 
						{
							isAtmStrategyCreated = false;  // reset atm strategy created check to false
							atmStrategyId = GetAtmStrategyUniqueId();
							orderId = GetAtmStrategyUniqueId();
							AtmStrategyCreate(OrderAction.Sell, OrderType.Limit, Close[1], 0, TimeInForce.Day, orderId, "ATM_EMAFastOBVTrend", atmStrategyId, (atmCallbackErrorCode, atmCallBackId) => {
								//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
								if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == atmStrategyId)
									isAtmStrategyCreated = true;
							});
								vendido = true;
								comprado = false;
								preco = Close[1];
								Print(Time[0] +" EMAFastOBV3Rv2 "+ Instrument.FullName + " Venda Tendencia");
						}
					}
			}
		
		#endregion
			
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
						Print(Time[0] +" EMAFastOBV3Rv2 "+ Instrument.FullName + " State: " + status[2]);
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
			
			/*if (comprado && CrossBelow(Stoch1712, Stoch3412, 1)
				|| vendido && CrossAbove(Stoch1712, Stoch3412, 1)
				|| (Open[1] == Open[2]) && (Open[2] == Open[3]) && (Open[3] == Open[4]) && (Open[4] == Open[5]) && (Open[5] == Open[6])
				)
				{
					if (atmStrategyId.Length > 0)
						{
							AtmStrategyClose(atmStrategyId);
							ScreenUpdate();
							atmStrategyId = string.Empty;
							comprado = false;
							vendido = false;
							Print(Time[0] +" EMAFastOBV3Rv2 "+ Instrument.FullName + " Condicao de Saida");
						}
				}*/

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
							Print(Time[0] +" EMAFastOBV3Rv2 "+ Instrument.FullName + " Ordem CANCELADA");
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
			    Print("EMAFastOBV3Rv2 "+ Instrument.FullName + " Connected at " + DateTime.Now);
				  if (atmStrategyId.Length > 0)
				  {
					AtmStrategyClose(atmStrategyId);
					atmStrategyId = string.Empty;
					Print(DateTime.Now +" EMAFastOBV3Rv2 "+ Instrument.FullName + " Todas ATMs Fechadas");
				  }
				  
				  if (orderId.Length > 0)
					{
						AtmStrategyCancelEntryOrder(orderId);
						orderId = string.Empty;
						Print(DateTime.Now +" EMAFastOBV3Rv2 "+ Instrument.FullName + " Todas Ordens Canceladas");
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
					Print(DateTime.Now +" EMAFastOBV3Rv2 "+ Instrument.FullName + " Todas Posições Fechadas");
				  }
			  }
			  
			  else if(connectionStatusUpdate.Status == ConnectionStatus.ConnectionLost)
			  {
			    Print("EMAFastOBV3Rv2 "+ Instrument.FullName + " Connection lost at: " + DateTime.Now);
				if (orderId.Length > 0)
				{
				  AtmStrategyCancelEntryOrder(orderId);
				  orderId = string.Empty;
				  Print(DateTime.Now +" EMAFastOBV3Rv2 "+ Instrument.FullName + " Todas Ordens Canceladas");
				}
			  }
			}
        #endregion
		
		#region Parametros
						
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Ganho Satisfatório", Order=1, GroupName="Parameters")]
		public double DayGainStop
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Perda Máxima", Order=2, GroupName="Parameters")]
		public double DayLossStop
		{ get; set; }
		#endregion
	}
}
