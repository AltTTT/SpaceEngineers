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
using Sandbox.Game.Screens.ViewModels;
using System.Data;
using System.Collections.Immutable;

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
                void Load(Program program);
            }
            //Repositories

            private class BatteryRepository : IRepository {
                private readonly List<IMyBatteryBlock> _batteries = new List<IMyBatteryBlock>();
                public ImmutableList<IMyBatteryBlock> Batteries {
                    get { return ImmutableList.ToImmutableList(_batteries); }
                }
                public void Load(Program program) {
                    program.GridTerminalSystem.GetBlocksOfType(
                        _batteries,
                        battery => battery.IsSameConstructAs(program.Me)
                    );
                }
            }
            private class TextPanelRepository : IRepository {
                private readonly List<IMyTextPanel> _textPanels = new List<IMyTextPanel>();

                public ImmutableList<IMyTextPanel> TextPanels {
                    get { return ImmutableList.ToImmutableList(_textPanels); }
                }
                public void Load(Program program) {
                    program.GridTerminalSystem.GetBlocksOfType(
                        _textPanels,
                        textpanel => textpanel.IsSameConstructAs(program.Me)
                    );
                }

            }

            private class TerminalBlockWithInventoryRepository : IRepository {
                private readonly List<IMyTerminalBlock> _terminalBlocks = new List<IMyTerminalBlock>();
                public ImmutableList<IMyTerminalBlock> TerminalBlocks {
                    get { return ImmutableList.ToImmutableList(_terminalBlocks); }
                }
                public void Load(Program program) {
                    program.GridTerminalSystem.GetBlocksOfType(
                        _terminalBlocks,
                        b => b.IsSameConstructAs(program.Me) && b.HasInventory);
                }
            }

            private class CargoContainerRepository : IRepository {
                private readonly List<IMyCargoContainer> _cargos = new List<IMyCargoContainer>();
                public ImmutableList<IMyCargoContainer> CargoContainers {
                    get { return ImmutableList.ToImmutableList(_cargos); }
                }
                public void Load(Program program) {
                    program.GridTerminalSystem.GetBlocksOfType(
                        _cargos,
                        cargo => cargo.IsSameConstructAs(program.Me)
                    );
                }

            }
            //Initialize Repositories
            private List<IRepository> _repositories = new List<IRepository>();
            private BatteryRepository _batteryRepository;
            private TextPanelRepository _textPanelRepository;
            private CargoContainerRepository _cargoContainerRepository;

            private TerminalBlockWithInventoryRepository _terminalBlockWithInventoryRepository;
            public void initRepositories() {
                _batteryRepository = new BatteryRepository();
                _repositories.Add(_batteryRepository);
                _textPanelRepository = new TextPanelRepository();
                _repositories.Add(_textPanelRepository);
                _terminalBlockWithInventoryRepository = new TerminalBlockWithInventoryRepository();
                _repositories.Add(_terminalBlockWithInventoryRepository);
                _cargoContainerRepository = new CargoContainerRepository();
                _repositories.Add(_cargoContainerRepository);
            }
            //method
            public void LoadAllRepository(Program program) {
                foreach (var repository in _repositories) {
                    repository.Load(program);
                }
            }
            #endregion Repository
            //Service
            #region  Service
            private interface IService {
                void Update(Program program);
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
                public void Update(Program program) {
                    _currentStoredPower = 0f;
                    _maxStoredPower = 0f;
                    foreach (var battery in _repository.Batteries) {
                        _currentStoredPower += battery.CurrentStoredPower;
                        _maxStoredPower += battery.MaxStoredPower;
                    }
                }
            }
            private class CargoContainerService : IService {
                private readonly CargoContainerRepository _repository;
                public CargoContainerService(CargoContainerRepository repository) {
                    _repository = repository;
                }

                private MyFixedPoint _currentVolume = 0;
                private MyFixedPoint _maxVolume = 1;
                public float VolumeRatio {
                    get {
                        if (_maxVolume == 0) {
                            return -1;
                        }
                        return (float)_currentVolume.RawValue / _maxVolume.RawValue;
                    }
                }
                public void Update(Program program) {
                    _currentVolume = 0;
                    _maxVolume = 0;
                    foreach (var cargo in _repository.CargoContainers) {
                        IMyInventory inventory = cargo.GetInventory();
                        _currentVolume += inventory.CurrentVolume;
                        _maxVolume += inventory.MaxVolume;
                    }
                }

            }
            private class TextPanelService : IService {
                private readonly TextPanelRepository _repository;
                public TextPanelService(TextPanelRepository repository) {
                    _repository = repository;
                    send("", "\n");
                }
                public void Update(Program program) {
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
            private class ItemListService : IService {
                private readonly TerminalBlockWithInventoryRepository _repository;
                public ItemListService(TerminalBlockWithInventoryRepository repository) {
                    _repository = repository;
                }
                private readonly Dictionary<string, MyFixedPoint> _ingots = new Dictionary<string, MyFixedPoint>();
                public ImmutableDictionary<string, MyFixedPoint> Ingots { get { return ImmutableDictionary.ToImmutableDictionary(_ingots); } }
                private readonly Dictionary<string, MyFixedPoint> _ores = new Dictionary<string, MyFixedPoint>();
                public ImmutableDictionary<string, MyFixedPoint> Ores { get { return ImmutableDictionary.ToImmutableDictionary(_ores); } }
                private readonly Dictionary<string, MyFixedPoint> _components = new Dictionary<string, MyFixedPoint>();
                public ImmutableDictionary<string, MyFixedPoint> Components { get { return ImmutableDictionary.ToImmutableDictionary(_components); } }


                private readonly List<MyInventoryItem> _items = new List<MyInventoryItem>();
                public void Update(Program program) {
                    _ingots.Clear();
                    _ores.Clear();
                    _components.Clear();
                    foreach (var block in _repository.TerminalBlocks) {
                        for (int i = 0; i < block.InventoryCount; i++) {
                            IMyInventory inventory = block.GetInventory(i);
                            _items.Clear();
                            inventory.GetItems(_items);
                            foreach (var item in _items) {
                                string typeId = item.Type.TypeId;
                                string subTypeId = item.Type.SubtypeId;
                                switch (typeId) {
                                    case "MyObjectBuilder_Ingot": {
                                            if (!_ingots.ContainsKey(subTypeId)) {
                                                _ingots.Add(subTypeId, item.Amount);
                                            } else {
                                                _ingots[subTypeId] += item.Amount;
                                            }
                                            break;
                                        }
                                    case "MyObjectBuilder_Ore": {
                                            if (!_ores.ContainsKey(subTypeId)) {
                                                _ores.Add(subTypeId, item.Amount);
                                            } else {
                                                _ores[subTypeId] += item.Amount;
                                            }
                                            break;
                                        }
                                    case "MyObjectBuilder_Component": {
                                            if (!_components.ContainsKey(subTypeId)) {
                                                _components.Add(subTypeId, item.Amount);
                                            } else {
                                                _components[subTypeId] += item.Amount;
                                            }
                                            break;
                                        }
                                    default: {
                                            program.Echo("invalid item type : " + item.Type.ToString());
                                            break;
                                        }
                                }
                            }
                        }
                    }
                    /*
                    foreach (var p in _ingots) {
                        program.Echo(p.ToString());
                    }
                    foreach (var p in _ores) {
                        program.Echo(p.ToString());
                    }
                    foreach (var p in _components) {
                        program.Echo(p.ToString());
                    }
                    */
                }
            }
            //Initialize Services
            private List<IService> _services = new List<IService>();

            private BatteryService _batteryService;
            private CargoContainerService _cargoContainerService;
            private TextPanelService _textPanelService;
            private ItemListService _itemListService;


            public void initServices() {
                _batteryService = new BatteryService(_batteryRepository);
                _services.Add(_batteryService);
                _textPanelService = new TextPanelService(_textPanelRepository);
                _services.Add(_textPanelService);
                _itemListService = new ItemListService(_terminalBlockWithInventoryRepository);
                _services.Add(_itemListService);
                _cargoContainerService = new CargoContainerService(_cargoContainerRepository);
                _services.Add(_cargoContainerService);
            }
            //method
            public void UpdateAllService(Program program) {
                foreach (var service in _services) {
                    service.Update(program);
                }
            }
            #endregion Service
            //Controller
            #region  Controller
            private interface IController {
                void Apply(Program program);
            }
            //util
            private static string MakeGauge(string name, float fRatio) {
                StringBuilder sb = new StringBuilder();
                int ratio = (int)Math.Round(fRatio * 100);
                sb.AppendLine(name + " : " + ratio + "%");
                sb.Append("[");
                int i = 0;
                for (; i < ratio / 10; i++) {
                    sb.Append("#");
                }
                for (; i < 10; i++) {
                    sb.Append(" ");
                }
                sb.AppendLine("]");
                return sb.ToString();
            }
            //Controllers
            private class BatteryController : IController {
                private BatteryService _batteryService;
                private TextPanelService _textPanelService;
                public BatteryController(BatteryService batteryService, TextPanelService textPanelService) {
                    _batteryService = batteryService;
                    _textPanelService = textPanelService;
                }
                public void Apply(Program program) {
                    _textPanelService.send("Battery", MakeGauge("Battery", _batteryService.StoredPowerRatio));
                }
            }
            private class CargoContainerController : IController {
                private CargoContainerService _cargoContainerService;
                private TextPanelService _textPanelService;

                public CargoContainerController(CargoContainerService cargoContainerService,
                TextPanelService textPanelService) {
                    _cargoContainerService = cargoContainerService;
                    _textPanelService = textPanelService;
                }
                public void Apply(Program program) {
                    _textPanelService.send("Cargo",
                    MakeGauge("Cargo", _cargoContainerService.VolumeRatio));
                }
            }
            private class ItemListController : IController {
                private ItemListService _service;
                private TextPanelService _textPanelService;
                public ItemListController(ItemListService service, TextPanelService textPanelService) {
                    _service = service;
                    _textPanelService = textPanelService;
                }
                public void Apply(Program program) {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("--Ingots--");
                    foreach (var p in _service.Ingots) {
                        sb.AppendLine(p.Key + ":" + p.Value.ToIntSafe());
                    }
                    _textPanelService.send("Ingot", sb.ToString());
                    sb.Clear();
                    sb.AppendLine("--Ores--");
                    foreach (var p in _service.Ores) {
                        sb.AppendLine(p.Key + ":" + p.Value.ToIntSafe());
                    }
                    _textPanelService.send("Ore", sb.ToString());
                    sb.Clear();
                    sb.AppendLine("--Components--");
                    foreach (var p in _service.Components) {
                        sb.AppendLine(p.Key + ":" + p.Value.ToIntSafe());
                    }
                    _textPanelService.send("Component", sb.ToString());
                }

            }
            //initialize Controllers
            List<IController> _controllers = new List<IController>();
            public void initControllers() {
                _controllers.Add(new BatteryController(_batteryService, _textPanelService));
                _controllers.Add(new ItemListController(_itemListService, _textPanelService));
                _controllers.Add(new CargoContainerController(_cargoContainerService, _textPanelService));
            }
            //method
            public void ApplyAllController(Program program) {
                foreach (var controller in _controllers) {
                    controller.Apply(program);
                }
            }
            #endregion Controller

        }

        public void test() {

        }
        private BeanProvider _beanProvider;
        public Program() {
            _beanProvider = new BeanProvider();

            _beanProvider.initRepositories();
            _beanProvider.initServices();
            _beanProvider.initControllers();

            _beanProvider.LoadAllRepository(this);

            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            test();
        }


        public void Save() { }


        public void Main(string argument, UpdateType updateSource) {
            if ((updateSource & UpdateType.Update100) != 0) {
                _beanProvider.UpdateAllService(this);
                _beanProvider.ApplyAllController(this);
            }
        }

        #endregion // MyBaseController
    }
}