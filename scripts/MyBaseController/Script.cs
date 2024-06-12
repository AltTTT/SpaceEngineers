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

/*
 * Must be unique per each script project.
 * Prevents collisions of multiple `class Program` declarations.
 * Will be used to detect the ingame script region, whose name is the same.
 */
namespace MyBaseController {

    /*
     * Do not change this declaration because this is the game requirement.
     */
    public sealed class Program : MyGridProgram {

        /*
         * Must be same as the namespace. Will be used for automatic script export.
         * The code inside this region is the ingame script.
         */
        #region MyBaseController
        public class BeanProvider {

            private interface IBean {
                public void Init(IMyGridTerminalSystem gridTerminalSystem);
                public void Update();
            }
            private interface IRepository : IBean {

            }
            private interface IService : IBean {

            }
            private interface IController : IBean {

            }

            private List<IRepository> repositories;
            private class BatteryRepository : IRepository {
                private readonly List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();

                private float CurrentStoredPower = 0f;
                private float MaxStoredPower = 1f;
                public float StoredPowerRatio {
                    get {
                        if (MaxStoredPower == 0f) {
                            return -1f;
                        }
                        return CurrentStoredPower / MaxStoredPower;
                    }
                }
                public void Init(IMyGridTerminalSystem gridTerminalSystem) {
                    gridTerminalSystem.GetBlocksOfType(batteries);
                }
                public void Update() {
                    CurrentStoredPower = 0f;
                    MaxStoredPower = 0f;
                    foreach (var battery in batteries) {
                        CurrentStoredPower += battery.CurrentStoredPower;
                        MaxStoredPower += battery.MaxStoredPower;
                    }
                }
            }
        }

        public Program() { }


        public void Save() { }


        public void Main(string argument, UpdateType updateSource) { }

        #endregion // MyBaseController
    }
}