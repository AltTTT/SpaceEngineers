using System;

// Space Engineers DLLs
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using VRageMath;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Text;

/*
 * Must be unique per each script project.
 * Prevents collisions of multiple `class Program` declarations.
 * Will be used to detect the ingame script region, whose name is the same.
 */
namespace MyBatteryController {

    /*
     * Do not change this declaration because this is the game requirement.
     */
    public sealed class Program : MyGridProgram {

        /*
         * Must be same as the namespace. Will be used for automatic script export.
         * The code inside this region is the ingame script.
         */
        #region MyBatteryController

        List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
        List<IMyTextPanel> textPanels = new List<IMyTextPanel>();
        float CurrentStoredPower = 0.0f;
        float MaxStoredPower = 1.0f;

        float getChargedRatio() {
            if (MaxStoredPower == 0f) {
                return -1;
            }
            return CurrentStoredPower / MaxStoredPower;
        }

        public void init() {
            GridTerminalSystem.GetBlocksOfType(batteries);
            GridTerminalSystem.GetBlocksOfType(textPanels);
        }
        public void update() {
            CurrentStoredPower = 0f;
            MaxStoredPower = 0f;
            foreach (IMyBatteryBlock battery in batteries) {
                CurrentStoredPower += battery.CurrentStoredPower;
                MaxStoredPower += battery.MaxStoredPower;
            }
            Print();
        }
        public void Print() {
            foreach(var textPanel in textPanels){
                string now=textPanel.GetPublicTitle();
                
            }
        }
        public Program() {
            init();
            Echo(batteries[0].MaxStoredPower.ToString());
        }

        public void Save() { }


        public void Main(string argument, UpdateType updateSource) {
            if ((updateSource & UpdateType.Update100) != 0) {
                update();
            }
        }

        #endregion // MyBatteryController
    }
}