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
		public class WickRenkoBox3 : Strategy
		{
			private int StopLossTicks;
			private int ProfitTargetTicks;
			
			private bool condicaoDeCompra = false;
			private bool condicaoDeVenda = false;

			private bool novaOrdem = false;
			private bool novoTP = false;
			private bool isMIT = true;

			protected override void OnStateChange()
			{
				if (State == State.SetDefaults)
				{
					Description									= @"Entradas baseadas na sombra do renko";
					Name										= "WickRenkoBox3";
					Calculate									= Calculate.OnEachTick;
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
					TraceOrders									= true;
					RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelCloseIgnoreRejects;
					StopTargetHandling							= StopTargetHandling.PerEntryExecution;
					BarsRequiredToTrade							= 20;
					ConnectionLossHandling 						= ConnectionLossHandling.KeepRunning;
					IncludeTradeHistoryInBacktest				= false;

					// Disable this property for performance gains in Strategy Analyzer optimizations
					// See the Help Guide for additional information
					IsInstantiatedOnEachOptimizationIteration	= true;
					
					Quantidade 					= 1;
					RenkoBox					= 4;
					PT							= 5;
					SL							= 9;
				}

				else if (State == State.Configure)
				{
					StopLossTicks = SL;
					ProfitTargetTicks = PT;				
				}

				else if (State == State.DataLoaded)
				{				
				    Draw.TextFixed(this,"Robo", Name, TextPosition.BottomLeft);
					ClearOutputWindow();  
				}
			}

			protected override void OnBarUpdate()
				{
					if (CurrentBar < BarsRequiredToTrade)
					return;
				
					// OnBarUpdate() will be called on incoming tick events on all Bars objects added to the strategy
					// We only want to process events on our primary Bars object (index = 0) which is set when adding
					// the strategy to a chart
					if (BarsInProgress != 0)
						return;
					
					// Make sure this strategy does not execute against historical data
					if(State == State.Historical)
						return;
										
					if (!(ToTime(Time[0]) > 070000 && ToTime(Time[0]) < 130000))
							return;

					if (IsFirstTickOfBar && Position.MarketPosition == MarketPosition.Flat)  //Condicao de Entrada
						{
							condicaoDeCompra = ((Close[1] > Open[1]) 
												&& (Close[2] > Open[2]) 
												&& (Open[1] - Low[1] >= RenkoBox * TickSize) 
												);
								
								
							condicaoDeVenda = ((Close[1] < Open[1]) 
												&& (Close[2] < Open[2]) 
												&& (High[1] - Open[1] >= RenkoBox * TickSize)
												);
							
							if (condicaoDeCompra || condicaoDeVenda)
								{
									novaOrdem = true;
									
									Print ("------------------------------------------------");
									Print ("Condicao de Compra = " + condicaoDeCompra);
									Print ("Condicao de Venda = " + condicaoDeVenda);
									
								}
							//Print ("test" + ToTime(Time[0]));	
						}	
				}
			protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
				{	
						
						if(condicaoDeCompra && novaOrdem)
							{
								if(Position.MarketPosition == MarketPosition.Flat)
									{
										Print ("------------------------------------------------");
										SetStopLoss(CalculationMode.Ticks, StopLossTicks);
										SetProfitTarget(CalculationMode.Ticks, ProfitTargetTicks, isMIT);
										//EnterShort();
										EnterLongMIT(Quantidade, Close[1]);
										//EnterLongLimit(Close[1]);
										novaOrdem = false;
										novoTP = true;
										Print ("Posição: Comprado");
									}
							}
						
						if(condicaoDeVenda && novaOrdem)
							{
								if(Position.MarketPosition == MarketPosition.Flat)
									{
										Print ("------------------------------------------------");
										SetStopLoss(CalculationMode.Ticks, StopLossTicks);
										SetProfitTarget(CalculationMode.Ticks, ProfitTargetTicks, isMIT);
										//EnterLong();
										EnterShortMIT(Quantidade, Close[1]);
										//EnterShortLimit(Close[1]);
										novaOrdem = false;
										novoTP = true;
										Print ("Posição: Vendido");
									}
							}
					
					
					if(novoTP && marketDataUpdate.Price != 0)
						{
							if(Position.MarketPosition == MarketPosition.Long && marketDataUpdate.Price <= Open[1])
								{
									Print ("------------------------------------------------");
									//SetStopLoss(CalculationMode.Ticks, StopLossTicks);
									SetProfitTarget(CalculationMode.Ticks, (ProfitTargetTicks + 1));
									novoTP = false;
									Print ("Take Profit Alterado");
								}
							if(Position.MarketPosition == MarketPosition.Short && marketDataUpdate.Price >= Open[1])
								{
									Print ("------------------------------------------------");
									//SetStopLoss(CalculationMode.Ticks, StopLossTicks);
									SetProfitTarget(CalculationMode.Ticks, (ProfitTargetTicks + 1));
									novoTP = false;
									Print ("Take Profit Alterado");
								}
						 }
					
				}
			
			

#region ConnectionHandling
protected override void OnConnectionStatusUpdate(ConnectionStatusEventArgs connectionStatusUpdate)
{
if(connectionStatusUpdate.Status == ConnectionStatus.Connected)
  {
//    Print(Time[0] + " " + Instrument.FullName + " Connected at " + DateTime.Now);
//	  if (atmStrategyId.Length > 0)
//	  {
//		AtmStrategyClose(atmStrategyId);
//		atmStrategyId = string.Empty;
//		Print(Time[0] + " " + Instrument.FullName + " Todas ATMs Fechadas");
//	  }
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
	  }
  }
  
  else if(connectionStatusUpdate.Status == ConnectionStatus.ConnectionLost)
  {
//    Print(Time[0] + " " + Instrument.FullName + " Connection lost at: " + DateTime.Now);
//	if (orderId.Length > 0)
//	{
//	  AtmStrategyCancelEntryOrder(orderId);
//	  orderId = string.Empty;
//	  Print(Time[0] + " " + Instrument.FullName + " Todas Ordens Canceladas");
//	}
  }
}
#endregion

		#region Properties
			
			[NinjaScriptProperty]
			[Range(1, int.MaxValue)]
			[Display(Name="Numero de Contratos", Order=1, GroupName="Parameters")]
			public int Quantidade
			{ get; set; }
			
			[NinjaScriptProperty]
			[Range(1, int.MaxValue)]
			[Display(Name="Caixa Renko", Order=2, GroupName="Parameters")]
			public int RenkoBox
			{ get; set; }
			
			[NinjaScriptProperty]
			[Range(1, int.MaxValue)]
			[Display(Name="Alvo", Order=3, GroupName="Parameters")]
			public int PT
			{ get; set; }
			
			[NinjaScriptProperty]
			[Range(1, int.MaxValue)]
			[Display(Name="Stop", Order=4, GroupName="Parameters")]
			public int SL
			{ get; set; }

		#endregion

}
}
