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

            #region Repository
            private interface IRepository {
                public void Load(IMyGridTerminalSystem gridTerminalSystem);
            }
            //Repositories

            private class BatteryRepository : IRepository {
                private readonly List<IMyBatteryBlock> _batteries = new List<IMyBatteryBlock>();
                public List<IMyBatteryBlock> Batteries { get { return _batteries; } }
                public void Load(IMyGridTerminalSystem gridTerminalSystem) {
                    gridTerminalSystem.GetBlocksOfType(Batteries);
                }
            }
            private class TextPanelRepository : IRepository {
                private readonly List<IMyTextPanel> _textPanels = new List<IMyTextPanel>();

                public List<IMyTextPanel> TextPanels { get { return _textPanels; } }
                public void Load(IMyGridTerminalSystem gridTerminalSystem) {
                    gridTerminalSystem.GetBlocksOfType(_textPanels);
                }

            }
            private List<IRepository> _repositories = new List<IRepository>();

            //Initialize Repositories
            public void initRepositories() {
                _repositories.Add(new BatteryRepository());
                _repositories.Add(new TextPanelRepository());
            }
            #endregion Repository
            #region  Service

            //Service
            private interface IService {
                public void Update();
            }
            private class BatteryService : IService {
                private readonly BatteryRepository _repository;
                private float _currentStoredPower = 0f;
                private float _maxStoredPower = 1f;
                public float StoredPowerRatio {
                    get {
                        if (_maxStoredPower == 0f) {
                            return -1f;
                        }
                        return _currentStoredPower / _maxStoredPower;
                    }
                }
                public BatteryService(BatteryRepository repository) {
                    _repository = repository;
                }
                public void Update() {
                    _currentStoredPower = 0f;
                    _maxStoredPower = 0f;
                    foreach (var battery in _repository.Batteries) {
                        _currentStoredPower += battery.CurrentStoredPower;
                        _maxStoredPower += battery.MaxStoredPower;
                    }
                }

            }
            #endregion Service
            #region  Controller
            private interface IController {
                public void Apply();
            }
            #endregion Controller

        }

        public Program() { }


        public void Save() { }


        public void Main(string argument, UpdateType updateSource) { }

        #endregion // MyBaseController
    }
}