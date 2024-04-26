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
	public class EMAonEMAwithoutATMv4 : Strategy
	{
		private EMA EMA1;
		private EMA EMA2;
		private EMA EMA3;
		private EMA EMA4;
		private EMA EMA5;
		private EMA EMA6;
		
		private bool	entradaLonga			= false;
		private bool	entradaCurta			= false;
		private bool    goodToGo				= true;
		private bool	breakEven				= false;
		private bool	trailing				= false;

		private bool	disconnection			= false;
		
		private double dailyPnL;
		private double entryPrice;
		private double trailingPrice;
		private double _trailingStop;
		
		private double priorTradesCumProfit = 0.0;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Estrategia com ATM, só funciona em Real-Time data ou Market Replay.
																Ajustada para M6E Micro S&P.
																usando 2 tempos graficos de Renko ( 4R e 7R)
																";
				Name										= "EMAonEMAwithoutATMv4";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 2700;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				OrderFillResolutionType						= BarsPeriodType.Tick;
				OrderFillResolutionValue					= 1;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Day;
				TraceOrders									= true;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelCloseIgnoreRejects;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 2;
				ConnectionLossHandling 						= ConnectionLossHandling.KeepRunning;
				IncludeTradeHistoryInBacktest				= true;
				IncludeCommission 							= true;
				NumberRestartAttempts 						= 5;
				IsInstantiatedOnEachOptimizationIteration	= true;
				DaysToLoad 									= 3;
				
				MainEMA										= 9;
				SecEMA										= 34;
				CrossEMA									= 17;
				
				Quantidade									= 1;
				renkoMaior									= 6;

				DayGainStop									= 60;
				DayLossStop									= 30;
				
				longStopLossTicks 							= 14;
				longProfitTargetTicks 						= 22;
				shortStopLossTicks 							= 10;
				shortProfitTargetTicks 						= 18;
				
				longBreakEvenTrigger						= 8;
				longBreakEvenStep							= 3;
				shortBreakEvenTrigger						= 8;
				shortBreakEvenStep							= 3;
				
				trailingTrigger								= 2;
				trailingStep								= 4;
				trailingStop								= 4;
				
				OpenSession									= DateTime.Parse("14:00", System.Globalization.CultureInfo.InvariantCulture);
				CloseSession								= DateTime.Parse("19:00", System.Globalization.CultureInfo.InvariantCulture);
				
			}
			
			else if (State == State.Configure)
			{
				// Add a Renko
				
				AddRenko(Instrument.FullName, renkoMaior, MarketDataType.Last);
			}
			else if (State == State.DataLoaded)
			{				
				EMA1				= EMA(Close, MainEMA);
				EMA2				= EMA(EMA1, SecEMA);
				EMA3				= EMA(Close, CrossEMA);
				EMA4				= EMA(Open, CrossEMA);
				
				EMA5 				= EMA(Closes[1], 4);
				EMA6 				= EMA(Closes[1], 8);
				
				EMA1.Plots[0].Brush = Brushes.Cyan;
				AddChartIndicator(EMA1);
				EMA2.Plots[0].Brush = Brushes.DarkCyan;
				AddChartIndicator(EMA2);
				EMA3.Plots[0].Brush = Brushes.Magenta;
				AddChartIndicator(EMA3);
				EMA4.Plots[0].Brush = Brushes.DarkMagenta;
				AddChartIndicator(EMA4);
				
                Draw.TextFixed(this,"Robo", Name, TextPosition.BottomLeft);
				Draw.TextFixed(this,"Info", "Script Started", TextPosition.BottomRight);
				
				ClearOutputWindow();
			}
			/*else if (State == State.Terminated)
			{
				Print("Daily PnL = " + dailyPnL);
				Print("Robo " + Name +  " Parado");
			}*/
		}

		protected override void OnBarUpdate()
		{
			
			if (CurrentBar < BarsRequiredToTrade)
				return;

			if (BarsInProgress != 0)
				return;

			if(State == State.Historical)
				return;
				
			dailyPnL = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit - priorTradesCumProfit;
			Draw.TextFixed(this, "Info", "Daily PnL = " + dailyPnL, TextPosition.BottomRight);

								
			if ((Times[0][0].TimeOfDay > OpenSession.TimeOfDay) && (Times[0][0].TimeOfDay < CloseSession.TimeOfDay))
			{
				StrategyReset();
				return;
			}
			
			if (dailyPnL >= DayGainStop || dailyPnL <= -(DayLossStop))
			{
				Draw.TextFixed(this,"Robo", Name + " Meta de Ganho/Perda", TextPosition.BottomLeft);
				return;
			}
			
	#region GoodToGo	
		if(goodToGo)
			{				
						// Compra Longa
						if 	((Position.MarketPosition == MarketPosition.Flat)
							&& (EMA1[1] > EMA2[1]) 
							&& (EMA3[0] > EMA4[0]) 
							&& (EMA1[1] > EMA3[1]) 
							&& (Close[0] > Open[0]) 
							&& (Open[1] == Open[2])
							
							&& (Close[1] < EMA1[1])
							
							&& (EMA1[0] > EMA1[2])
							&& (EMA3[1] > EMA4[1])
							&& (EMA3[2] > EMA4[2])
							
							&& (EMA3[0] - EMA4[0] >= 2/3 * TickSize)
							
							&& (Closes[1][0] > Opens[1][0])
							&& (EMA5[0] > EMA6[0])
							)
						{
							Print ("------------------------------------------------");
							SetStopLoss(CalculationMode.Ticks, longStopLossTicks);
							SetProfitTarget(CalculationMode.Ticks, longProfitTargetTicks, true);
							
							EnterLongMIT(Quantidade, Close[0]);
							

							entradaLonga 	= true;
							entradaCurta 	= false;
														
							Print(Time[0] + " " + Instrument.FullName + " Compra Longa");
							Print ("------------------------------------------------");
						}
						// Venda Longa
						if ((Position.MarketPosition == MarketPosition.Flat)
							&& (EMA1[1] < EMA2[1]) 
							&& (EMA3[0] < EMA4[0]) 
							&& (EMA1[1] < EMA3[1]) 
							&& (Close[0] < Open[0]) 
							&& (Open[1] == Open[2]) 
							
							&& (Close[1] > EMA1[1])
							
							&& (EMA1[0] < EMA1[2])
							&& (EMA3[1] < EMA4[1])
							&& (EMA3[2] < EMA4[2])
							
							&& (EMA4[0] - EMA3[0] >= 2/3 * TickSize)
														
							&& (Closes[1][0] < Opens[1][0])
							&& (EMA5[0] < EMA6[0])
							)
						{
							Print ("------------------------------------------------");
							SetStopLoss(CalculationMode.Ticks, longStopLossTicks);
							SetProfitTarget(CalculationMode.Ticks, longProfitTargetTicks, true);
							
							EnterShortMIT(Quantidade, Close[0]);
							

							entradaLonga 	= true;
							entradaCurta 	= false;
														
							Print(Time[0] + " " + Instrument.FullName + " Venda Longa");
							Print ("------------------------------------------------");
						}
						
						// Compra Curta
						if ((Position.MarketPosition == MarketPosition.Flat)
							&& (EMA1[1] > EMA2[1]) 
							&& (EMA3[0] > EMA4[0]) 
							&& (EMA1[1] > EMA3[1]) 
							&& (Close[0] > Open[0]) 
							&& (Close[1] > Open[1])
							&& (Close[2] > Open[2])
							//&& (Close[3] < Open[3])
							
							&& (Closes[1][0] > Opens[1][0])
							&& (Closes[1][1] > Opens[1][1])
							&& (Closes[1][2] < Opens[1][2])
							&& (EMA5[0] > EMA6[0])
							&& (EMA5[1] > EMA6[1])
							&& (EMA5[2] > EMA6[2])
							)
						{
							Print ("------------------------------------------------");
							SetStopLoss(CalculationMode.Ticks, shortStopLossTicks);
							SetProfitTarget(CalculationMode.Ticks, shortProfitTargetTicks, true);
							
							EnterLongMIT(Quantidade, Close[0]);
							

							entradaLonga 	= false;
							entradaCurta 	= true;
														
							Print(Time[0] + " " + Instrument.FullName + " Compra Curta");
							Print ("------------------------------------------------");
						}
						// Venda Curta
						if ((Position.MarketPosition == MarketPosition.Flat)
							&& (EMA1[1] < EMA2[1]) 
							&& (EMA3[0] < EMA4[0]) 
							&& (EMA1[1] < EMA3[1]) 
							&& (Close[0] < Open[0]) 
							&& (Close[1] < Open[1])
							&& (Close[2] < Open[2])
							//&& (Close[3] > Open[3])						
							
							&& (Closes[1][0] < Opens[1][0])
							&& (Closes[1][1] < Opens[1][1])
							&& (Closes[1][2] > Opens[1][2])
							&& (EMA5[0] < EMA6[0])
							&& (EMA5[1] < EMA6[1])
							&& (EMA5[2] < EMA6[2])
							)
						{
							Print ("------------------------------------------------");
							SetStopLoss(CalculationMode.Ticks, shortStopLossTicks);
							SetProfitTarget(CalculationMode.Ticks, shortProfitTargetTicks, true);
							
							EnterShortMIT(Quantidade, Close[0]);
							

							entradaLonga 	= false;
							entradaCurta 	= true;
														
							Print(Time[0] + " " + Instrument.FullName + " Venda Curta");
							Print ("------------------------------------------------");
						}

        }
	#endregion
			
		}
	
	#region StopManagement
		
		protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
				{	
					if (entradaLonga)
					{
						
						
						if (breakEven && (marketDataUpdate.Price > 0) && (longBreakEvenTrigger > 0))
						{
							
							
							if(Position.MarketPosition == MarketPosition.Long && marketDataUpdate.Price >= (entryPrice + (longBreakEvenTrigger * TickSize)))
									{
										SetStopLoss(CalculationMode.Ticks, (longStopLossTicks - longBreakEvenStep));
										breakEven = false;
										trailing = true;
										trailingPrice = marketDataUpdate.Price;
										Print ("------------------------------------------------");
										Print ("BreakEven compra longa");
										Print("Preço de Entrada = " + entryPrice);										
										Print("Breakeven Trigger Price = " + marketDataUpdate.Price);
										Print ("------------------------------------------------");
									}
									
							if(Position.MarketPosition == MarketPosition.Short && marketDataUpdate.Price <= (entryPrice - (longBreakEvenTrigger * TickSize)))
									{
										SetStopLoss(CalculationMode.Ticks, (longStopLossTicks - longBreakEvenStep));
										breakEven = false;
										trailing = true;
										trailingPrice = marketDataUpdate.Price;
										Print ("------------------------------------------------");
										Print ("BreakEven venda longa");
										Print("Preço de Entrada = " + entryPrice);										
										Print("Breakeven Trigger Price = " + marketDataUpdate.Price);
										Print ("------------------------------------------------");
									}
						}
					}
					
					if (entradaCurta)
					{
						
												
						if (breakEven && (marketDataUpdate.Price > 0) && (shortBreakEvenTrigger > 0))
						{						
							
							
							if(Position.MarketPosition == MarketPosition.Long && marketDataUpdate.Price >= (entryPrice + (shortBreakEvenTrigger * TickSize)))
									{
										SetStopLoss(CalculationMode.Ticks, (shortStopLossTicks - shortBreakEvenStep));
										breakEven = false;
										trailing = true;
										trailingPrice = marketDataUpdate.Price;
										Print ("------------------------------------------------");
										Print ("BreakEven compra curta");
										Print("Preço de Entrada = " + entryPrice);										
										Print("Breakeven Trigger Price = " + marketDataUpdate.Price);
										Print ("------------------------------------------------");
									}
									
							if(Position.MarketPosition == MarketPosition.Short && marketDataUpdate.Price <= (entryPrice - shortBreakEvenTrigger * TickSize))
									{
										SetStopLoss(CalculationMode.Ticks, (shortStopLossTicks - shortBreakEvenStep));
										breakEven = false;
										trailing = true;
										trailingPrice = marketDataUpdate.Price;
										Print ("------------------------------------------------");
										Print ("BreakEven venda curta");
										Print("Preço de Entrada = " + entryPrice);
										Print("Breakeven Trigger Price = " + marketDataUpdate.Price);
										Print ("------------------------------------------------");
									}
						}
					}
					
					if (trailing && (marketDataUpdate.Price > 0) && (trailingTrigger > 0) 
						|| ((trailingTrigger > 0) && (longBreakEvenTrigger == 0) && entradaLonga) 
						|| ((trailingTrigger > 0) && (shortBreakEvenTrigger == 0) && entradaCurta)
						)
					{
						if (entradaLonga && (_trailingStop + longProfitTargetTicks - trailingTrigger - trailingStep < 0) 
							|| entradaCurta && (_trailingStop + shortProfitTargetTicks - trailingTrigger - trailingStep < 0)
							)
							return;
						
						if (Position.MarketPosition == MarketPosition.Long && marketDataUpdate.Price >= (trailingPrice + trailingTrigger * TickSize))
									{
										SetStopLoss(CalculationMode.Ticks, _trailingStop);
										trailingPrice = trailingPrice + (trailingTrigger * TickSize);
										_trailingStop = _trailingStop - trailingStep;
										
										Print ("------------------------------------------------");
										Print ("Trailing Stop moved " + trailingStep + " Ticks");
										Print("Novo Trailing Trigger Price = " + trailingPrice);
										Print("Novo Trailing Stop Value (Ticks) = " + _trailingStop);
										//Print("Market data Price = " + marketDataUpdate.Price);
										Print ("------------------------------------------------");
									}
									
						if (Position.MarketPosition == MarketPosition.Short && marketDataUpdate.Price <= (trailingPrice - trailingTrigger * TickSize))
									{
										SetStopLoss(CalculationMode.Ticks, _trailingStop);
										trailingPrice = trailingPrice - (trailingTrigger * TickSize);
										_trailingStop = _trailingStop - trailingStep;
										
										Print ("------------------------------------------------");
										Print ("Trailing Stop moved " + trailingStep + " Ticks");
										Print("Novo Trailing Trigger Price = " + trailingPrice);
										Print("Novo Trailing Stop Value (Ticks) = " + _trailingStop);
										//Print("Market data Price = " + marketDataUpdate.Price);
										Print ("------------------------------------------------");
									}
					}
									
					
				}
	#endregion
				
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
				{
						entryPrice = execution.Price;
						trailingPrice = entryPrice;
						_trailingStop = trailingStop;
					
				      	//Print ("------------------------------------------------");
						//Print("Preço de Execução = " + entryPrice);
						//Print("Quantidade = " + execution.Quantity);
						//Print("Hora da Execução = " + execution.Time);
						//Print ("------------------------------------------------");
				}
				
		protected override void OnPositionUpdate(Position position, double averagePrice, int quantity, MarketPosition marketPosition)
				{
						 if (position.MarketPosition == MarketPosition.Flat)
							  {
							    entradaLonga 	= false;
								entradaCurta 	= false;
								breakEven 		= false;
								trailing		= false;
								goodToGo		= true;
								  
								Print ("------------------------------------------------");
								Print(Time[0] + " " + Instrument.FullName + " Posição = Zerado");
								//Print("Rested Trailing Stop Value = " + _trailingStop);
								Print ("------------------------------------------------");
							  }
							  
						if (position.MarketPosition == MarketPosition.Long)
							  {
							    goodToGo		= false;
								breakEven 		= true;
								trailing		= false;
								  
								Print ("------------------------------------------------");
								Print(Time[0] + " " + Instrument.FullName + " Posição = Comprado");
								Print ("------------------------------------------------");
							  }
							  
						if (position.MarketPosition == MarketPosition.Short)
							  {
							    goodToGo		= false;
								breakEven 		= true;
								trailing		= false;
								  
								Print ("------------------------------------------------");
								Print(Time[0] + " " + Instrument.FullName + " Posição = Vendido");
								Print ("------------------------------------------------");
							  }
				} 
				
		protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string nativeError)
				{
							 
						if (order.OrderState == OrderState.Rejected)
							  {
							    Print ("------------------------------------------------");
								Print(Time[0] + " " + Instrument.FullName + " Ordem Rejeitada");
								Print ("------------------------------------------------");
							  }
							  
						if (order.OrderState == OrderState.Cancelled)
							  {
							    entradaLonga 	= false;
								entradaCurta 	= false;
								breakEven 		= false;
								trailing		= false;
								goodToGo		= true;
								  
								//_trailingStop = trailingStop;
								  
								Print ("------------------------------------------------");
								Print(Time[0] + " " + Instrument.FullName + " Ordem Cancelada");
								Print ("------------------------------------------------");
							  }
							  
						if (order.OrderState == OrderState.Filled)
							  {
							   // Print ("------------------------------------------------");
								//Print(Time[0] + " " + Instrument.FullName + " Ordem Totalmente Execudata");
								//Print ("------------------------------------------------");
							  }
				}

#region StrategyReset
			public void StrategyReset()
			{
				entradaLonga 	= false;
				entradaCurta 	= false;
				goodToGo		= true;
				
				//Print(Time[0] + " " + Instrument.FullName + " Reset");
				
				if (Bars.IsFirstBarOfSession) 
					priorTradesCumProfit = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
				else
					priorTradesCumProfit = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
				
				dailyPnL = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit - priorTradesCumProfit;
				Draw.TextFixed(this,"Info", "Daily PnL = " + dailyPnL, TextPosition.BottomRight);
				Draw.TextFixed(this,"Robo", Name, TextPosition.BottomLeft);
			}
#endregion
		
#region ConnectionHandling
			protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs connectionStatusUpdate)
			{
			if(connectionStatusUpdate.Status == ConnectionStatus.Connected)
			  {
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
					Print(Time[0] + " " + Instrument.FullName + " Todas Posições Fechadas");
					goodToGo = false;
				  }
				  
				  if (disconnection)
				  {
					Print(Time[0] + " " + Instrument.FullName + " Robo Parado por Perda de Conexão");
					SetState(State.Terminated);
    				return;
				  }
			  }
			  
			  else if(connectionStatusUpdate.Status == ConnectionStatus.ConnectionLost)
			  {
				  disconnection = true;			
			  }
			}
#endregion

#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="EMA Principal", Order=1, GroupName="Indicadores")]
		public int MainEMA
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="EMA Secundaria", Order=2, GroupName="Indicadores")]
		public int SecEMA
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="EMA (Abertura - Fechamento)", Order=3, GroupName="Indicadores")]
		public int CrossEMA
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Quantidade", Description="Quantidade de Contratos", Order=6, GroupName="Parameters")]
		public int Quantidade
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Tempo Gráfico Maior (Renko)", Description="Tempo Gráfico Maior", Order=7, GroupName="Parameters")]
		public int renkoMaior
		{ get; set; }
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Open Session", Description="Horario de Inicio", Order=7, GroupName="Horário")]
		public DateTime OpenSession
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Close Session", Description="Horario de Fechamento", Order=8, GroupName="Horário")]
		public DateTime CloseSession
		{ get; set; }		
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Inicio Hora Sangrenta", Description="Horario de Inicio", Order=9, GroupName="Horário")]
		public DateTime OpenBloodHour
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Termino Hora Sangrenta", Description="Horario de Fechamento", Order=10, GroupName="Horário")]
		public DateTime CloseBloodHour
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="LastEntry", Description="Proximo ao Fechamento", Order=11, GroupName="Horário")]
		public DateTime LastEntry
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Ganho Satisfatório", Order=12, GroupName="Meta Diária")]
		public double DayGainStop
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Perda Máxima", Order=13, GroupName="Meta Diária")]
		public double DayLossStop
		{ get; set; }
		
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Stop Longo", Order=14, GroupName="Alvos")]
		public int longStopLossTicks
		{ get; set; }
			
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Alvo Longo", Order=15, GroupName="Alvos")]
		public int longProfitTargetTicks
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Stop Curto", Order=16, GroupName="Alvos")]
		public int shortStopLossTicks
		{ get; set; }
			
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Alvo Curto", Order=17, GroupName="Alvos")]
		public int shortProfitTargetTicks
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Long BreakEven Trigger", Description="Distancia de acionamento do Breakeven", Order=8, GroupName="BreakEven")]
		public int longBreakEvenTrigger
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Long BreakEven Step", Description="Distancia do passo do Breakeven", Order=8, GroupName="BreakEven")]
		public int longBreakEvenStep
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Short BreakEven Trigger", Description="Distancia de acionamento do Breakeven", Order=8, GroupName="BreakEven")]
		public int shortBreakEvenTrigger
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Short BreakEven Step", Description="Distancia do passo do Breakeven", Order=8, GroupName="BreakEven")]
		public int shortBreakEvenStep
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Trailing Trigger", Order=8, GroupName="Trailing Stop")]
		public int trailingTrigger
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Trailing Step", Order=8, GroupName="Trailing Stop")]
		public int trailingStep
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Trailing Stop", Order=8, GroupName="Trailing Stop")]
		public int trailingStop
		{ get; set; }

#endregion

	}
}
