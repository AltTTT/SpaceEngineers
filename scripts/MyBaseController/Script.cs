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
using System.IO;
using System.Text;

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
            //Repository
            #region Repository

            public interface IRepository {
                void Load(IMyGridTerminalSystem gridTerminalSystem);
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
            //Initialize Repositories
            private List<IRepository> _repositories = new List<IRepository>();
            private BatteryRepository _batteryRepository;
            private TextPanelRepository _textPanelRepository;
            public void initRepositories() {
                _batteryRepository = new BatteryRepository();
                _repositories.Add(_batteryRepository);
                _textPanelRepository = new TextPanelRepository();
                _repositories.Add(_textPanelRepository);
            }
            //method
            public void LoadAllRepository(IMyGridTerminalSystem gridTerminalSystem) {
                foreach (var repository in _repositories) {
                    repository.Load(gridTerminalSystem);
                }
            }
            #endregion Repository
            //Service
            #region  Service
            private interface IService {
                void Update();
            }
            //Services
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
            private class TextPanelService : IService {
                private readonly TextPanelRepository _repository;
                public TextPanelService(TextPanelRepository repository) {
                    _repository = repository;
                }
                public void Update() {
                    foreach (var textPanel in _repository.TextPanels) {
                        var cData = textPanel.CustomData;
                        string[] lines = cData.Replace("\r\n", "\n").Split(new[] { '\n', '\r' });
                        StringBuilder sb = new StringBuilder();
                        foreach (var line in lines) {
                            if (map.ContainsKey(line))
                                sb.Append(map[line]);
                        }
                        textPanel.WriteText(sb.ToString());
                    }
                }
                Dictionary<string, string> map = new Dictionary<string, string>();
                public void send(string key, string value) {
                    map[key] = value;
                }

            }
            //Initialize Services
            private List<IService> _services = new List<IService>();

            private BatteryService _batteryService;
            private TextPanelService _textPanelService;

            public void initServices() {
                _batteryService = new BatteryService(_batteryRepository);
                _services.Add(_batteryService);
                _textPanelService = new TextPanelService(_textPanelRepository);
                _services.Add(_textPanelService);
            }
            //method
            public void UpdateAllService() {
                foreach (var service in _services) {
                    service.Update();
                }
            }
            #endregion Service
            //Controller
            #region  Controller
            private interface IController {
                void Apply();
            }
            //Controllers
            private class BatteryController : IController {
                private BatteryService _batteryService;
                private TextPanelService _textPanelService;
                public BatteryController(BatteryService batteryService, TextPanelService textPanelService) {
                    _batteryService = batteryService;
                    _textPanelService = textPanelService;
                }
                public void Apply() {
                    int ratio = (int)Math.Round(_batteryService.StoredPowerRatio * 100);
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Battery : " + ratio + "%");
                    sb.Append("[");
                    int i = 0;
                    for (; i < ratio / 10; i++) {
                        sb.Append("#");
                    }
                    for (; i < 10; i++) {
                        sb.Append(" ");
                    }
                    sb.AppendLine("]");
                    _textPanelService.send("Battery", sb.ToString());
                }
            }
            //initialize Controllers
            List<IController> _controllers = new List<IController>();
            public void initControllers() {
                _controllers.Add(new BatteryController(_batteryService, _textPanelService));
            }
            //method
            public void ApplyAllController() {
                foreach (var controller in _controllers) {
                    controller.Apply();
                }
            }
            #endregion Controller

        }

        private BeanProvider _beanProvider;
        public Program() {
            _beanProvider = new BeanProvider();

            _beanProvider.initRepositories();
            _beanProvider.initServices();
            _beanProvider.initControllers();

            _beanProvider.LoadAllRepository(GridTerminalSystem);

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }


        public void Save() { }


        public void Main(string argument, UpdateType updateSource) {
            if ((updateSource & UpdateType.Update100) != 0) {
                _beanProvider.UpdateAllService();
                _beanProvider.ApplyAllController();
            }
        }

        #endregion // MyBaseController
    }
}