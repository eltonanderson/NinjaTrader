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
		public class RWSv2 : Strategy
		{
	

			private bool	reset					= true;

			protected override void OnStateChange()
			{
				if (State == State.SetDefaults)
				{
					Description									= @"MediaPVT, UO, impulse";
					Name										= "RWSv2";
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
					BarsRequiredToTrade							= 20;
					ConnectionLossHandling 						= ConnectionLossHandling.KeepRunning;
					IncludeTradeHistoryInBacktest				= false;

					// Disable this property for performance gains in Strategy Analyzer optimizations
					// See the Help Guide for additional information
					IsInstantiatedOnEachOptimizationIteration	= true;
					
					StopLossTicks				= 12;
					ProfitTargetTicks			= 5;
					Quantidade 					= 1;
					RenkoBox					= 4;
					WickSize					= 7;
					
				}

				else if (State == State.Configure)
				{
					SetStopLoss(CalculationMode.Ticks, StopLossTicks);
					SetProfitTarget(CalculationMode.Ticks, ProfitTargetTicks);
				}

				else if (State == State.DataLoaded)
				{				
				    Draw.TextFixed(this,"Robo", "RWSv2", TextPosition.BottomLeft);
				}
			}

			protected override void OnBarUpdate()
			{
			
				if (Position.MarketPosition == MarketPosition.Flat && reset)
					{
						SetStopLoss(CalculationMode.Ticks, StopLossTicks);
						SetProfitTarget(CalculationMode.Ticks, ProfitTargetTicks);
						reset = false;
					}
					
					if ((ToTime(Time[0]) > 150000 && ToTime(Time[0]) < 190000))
							return;
			
		
					if (Position.MarketPosition == MarketPosition.Flat)
					{
						
						if ((Close[0] > Open[0])
							&& (Close[1] > Open[1])
							
							&& (Low[0] <= Open[1] - ((WickSize - RenkoBox) * TickSize))
							)
						{
							EnterLongLimit(Quantidade, Close[0]);
							reset = true;
							
						}
						
						
						if ((Close[0] < Open[0])
							&& (Close[1] < Open[1])
							
							&& (High[0] >= Open[1] + ((WickSize - RenkoBox) * TickSize))
							)
						{
							EnterShortLimit(Quantidade, Close[0]);
							reset = true;
							
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
			[Range(0, int.MaxValue)]
			[NinjaScriptProperty]
			[Display(Name="StopLossTicks", Description="Numbers of ticks away from entry price for the Stop Loss order", Order=1, GroupName="Parameters")]
			public int StopLossTicks
			{ get; set; }

			[Range(0, int.MaxValue)]
			[NinjaScriptProperty]
			[Display(Name="ProfitTargetTicks", Description="Number of ticks away from entry price for the Profit Target order", Order=2, GroupName="Parameters")]
			public int ProfitTargetTicks
			{ get; set; }
			
			[NinjaScriptProperty]
			[Range(1, int.MaxValue)]
			[Display(Name="Numero de Contratos", Order=3, GroupName="Parameters")]
			public int Quantidade
			{ get; set; }
			
			[NinjaScriptProperty]
			[Range(1, int.MaxValue)]
			[Display(Name="Caixa Renko", Order=4, GroupName="Parameters")]
			public int RenkoBox
			{ get; set; }
			
			[NinjaScriptProperty]
			[Range(1, int.MaxValue)]
			[Display(Name="Sombra", Order=5, GroupName="Parameters")]
			public int WickSize
			{ get; set; }

		#endregion

}
}
