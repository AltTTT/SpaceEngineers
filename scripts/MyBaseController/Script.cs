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
using Sandbox.Game.Entities.Blocks;
using System.ComponentModel.Design;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.Game.GUI;
using VRage.ObjectBuilders;
using Sandbox.Game.Gui;
using Sandbox.Game;

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

        //util
        private static string[] readlines(string s) {
            return s.Replace("\r\n", "\n").Split(new[] { '\n', '\r' });
        }
        private static string subTypeIdToSubTypeIdForBluePrint(string subTypeId) {
            switch (subTypeId) {
                case "Motor":
                case "Computer":
                case "Construction":
                case "Detector":
                case "Girder":
                case "GravityGenerator":
                case "Medical":
                case "RadioCommunication":
                case "Reactor":
                case "Thrust":
                    subTypeId += "Component";
                    break;
                case "Canvas":
                    subTypeId = "Position0030_Canvas";
                    break;
            }
            return subTypeId;
        }
        //const
        public static readonly string[] ComponentSubtypeIds ={
            "BulletproofGlass",
            "Canvas",
            "Computer",
            "Construction",
            "Detector",
            "Display",
            "Girder",
            "GravityGenerator",
            "InteriorPlate",
            "LargeTube",
            "Medical",
            "MetalGrid",
            "Motor",
            "PowerCell",
            "RadioCommunication",
            "Reactor",
            "SmallTube",
            "SolarCell",
            "SteelPlate",
            "Superconductor",
            "Thrust",
        };
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

            private class CustomDatarRepository : IRepository {
                private IMyProgrammableBlock _me;
                public void Load(Program program) {
                    _me = program.Me;
                }
                public string CustomData {
                    get { return _me.CustomData; }
                    set { _me.CustomData = value; }
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
            private class AssemblerRepository : IRepository {
                List<IMyAssembler> _assemblers = new List<IMyAssembler>();

                public ImmutableList<IMyAssembler> Assemblers {
                    get {
                        return ImmutableList.ToImmutableList(_assemblers);
                    }
                }
                public void Load(Program program) {
                    program.GridTerminalSystem.GetBlocksOfType(
                        _assemblers,
                        assembler => assembler.IsSameConstructAs(program.Me)
                    );
                }

            }
            //Initialize Repositories
            private List<IRepository> _repositories = new List<IRepository>();
            private BatteryRepository _batteryRepository;
            private TextPanelRepository _textPanelRepository;
            private CargoContainerRepository _cargoContainerRepository;
            private CustomDatarRepository _customDataRepository;
            private AssemblerRepository _assemblerRepository;

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
                _customDataRepository = new CustomDatarRepository();
                _repositories.Add(_customDataRepository);
                _assemblerRepository = new AssemblerRepository();
                _repositories.Add(_assemblerRepository);
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
                        string[] lines = readlines(cData);
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
            private class CustomDataService : IService {
                private CustomDatarRepository _repository;
                public CustomDataService(CustomDatarRepository repository) {
                    _repository = repository;
                }
                private Dictionary<string, int> _map = new Dictionary<string, int>();

                public ImmutableDictionary<string, int> ComponentList {
                    get {
                        return ImmutableDictionary.ToImmutableDictionary(_map);
                    }
                }
                public void Update(Program program) {
                    _map.Clear();
                    foreach (var line in readlines(_repository.CustomData)) {
                        var p = line.Split('=');
                        if (p.Count() > 1) {
                            _map.Add(p[0], int.Parse(p[1]));
                        }
                    }
                }
                public void init() {
                    StringBuilder sb = new StringBuilder();
                    foreach (var subtypeId in ComponentSubtypeIds) {
                        sb.AppendLine(subtypeId + "=0");
                    }
                    _repository.CustomData = sb.ToString();
                }
            }
            private class AssemblerService : IService {
                AssemblerRepository _repository;

                public AssemblerService(AssemblerRepository repository) {
                    _repository = repository;
                }
                private IMyAssembler _mainAssembler;
                public IMyAssembler MainAssembler {
                    get { return _mainAssembler; }
                }
                private Dictionary<string, MyFixedPoint> _queuedItems = new Dictionary<string, MyFixedPoint>();
                public ImmutableDictionary<string, MyFixedPoint> QueuedItems {
                    get {
                        return ImmutableDictionary.ToImmutableDictionary(_queuedItems);
                    }
                }
                private List<MyProductionItem> _items = new List<MyProductionItem>();
                public void Update(Program program) {
                    _queuedItems.Clear();
                    foreach (var a in _repository.Assemblers) {
                        if (!a.CooperativeMode) {
                            _mainAssembler = a;
                        }
                        _items.Clear();
                        a.GetQueue(_items);
                        foreach (var item in _items) {
                            if (_queuedItems.ContainsKey(item.BlueprintId.SubtypeId.ToString())) {
                                _queuedItems[item.BlueprintId.SubtypeId.ToString()] += item.Amount;
                            } else {
                                _queuedItems.Add(item.BlueprintId.SubtypeId.ToString(), item.Amount);
                            }
                        }
                    }
                }

            }
            //Initialize Services

            private List<IService> _services = new List<IService>();

            private BatteryService _batteryService;
            private CargoContainerService _cargoContainerService;
            private TextPanelService _textPanelService;
            private ItemListService _itemListService;

            private CustomDataService _customDataService;
            private AssemblerService _assemblerService;


            public void initServices() {
                _batteryService = new BatteryService(_batteryRepository);
                _services.Add(_batteryService);
                _textPanelService = new TextPanelService(_textPanelRepository);
                _services.Add(_textPanelService);
                _itemListService = new ItemListService(_terminalBlockWithInventoryRepository);
                _services.Add(_itemListService);
                _cargoContainerService = new CargoContainerService(_cargoContainerRepository);
                _services.Add(_cargoContainerService);
                _customDataService = new CustomDataService(_customDataRepository);
                _services.Add(_customDataService);
                _assemblerService = new AssemblerService(_assemblerRepository);
                _services.Add(_assemblerService);
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
                private CustomDataService _customDataService;
                public ItemListController(
                    ItemListService service,
                    TextPanelService textPanelService,
                    CustomDataService customDataService) {
                    _service = service;
                    _textPanelService = textPanelService;
                    _customDataService = customDataService;
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
                    foreach (var subtypeId in ComponentSubtypeIds) {
                        int value = 0;
                        if (_service.Components.ContainsKey(subtypeId)) {
                            value = _service.Components[subtypeId].ToIntSafe();
                        }
                        int maxValue = 0;
                        if (_customDataService.ComponentList.ContainsKey(subtypeId)) {
                            maxValue = _customDataService.ComponentList[subtypeId];
                        }
                        sb.AppendLine(subtypeId + ":" + value + "/" + maxValue);
                    }
                    _textPanelService.send("Component", sb.ToString());
                }

            }
            private class AssemblerController : IController {
                AssemblerService _assemblerService;
                ItemListService _itemListService;
                CustomDataService _customDataService;

                public AssemblerController(
                    AssemblerService assemblerService,
                    ItemListService itemListService,
                    CustomDataService customDataService
                ) {
                    _assemblerService = assemblerService;
                    _itemListService = itemListService;
                    _customDataService = customDataService;
                }
                public void Apply(Program program) {
                    foreach (var subtypeId in ComponentSubtypeIds) {
                        if (
                         _customDataService.ComponentList.ContainsKey(subtypeId)
                        ) {

                            MyFixedPoint shortage = _customDataService.ComponentList[subtypeId];
                            if (_itemListService.Components.ContainsKey(subtypeId)) {
                                shortage -= _itemListService.Components[subtypeId];
                            }
                            if (_assemblerService.QueuedItems.ContainsKey(subTypeIdToSubTypeIdForBluePrint(subtypeId))) {
                                shortage -= _assemblerService.QueuedItems[subTypeIdToSubTypeIdForBluePrint(subtypeId)];
                            }

                            if (shortage > 0) {
                                MyDefinitionId id = MyDefinitionId.Parse(
                                    "MyObjectBuilder_BlueprintDefinition/" + subTypeIdToSubTypeIdForBluePrint(subtypeId));
                                _assemblerService.MainAssembler.AddQueueItem(id, shortage);
                            }
                        }
                    }
                }

            }
            //initialize Controllers
            List<IController> _controllers = new List<IController>();
            public void initControllers() {
                _controllers.Add(new BatteryController(_batteryService, _textPanelService));
                _controllers.Add(new ItemListController(_itemListService, _textPanelService, _customDataService));
                _controllers.Add(new CargoContainerController(_cargoContainerService, _textPanelService));
                _controllers.Add(new AssemblerController(_assemblerService, _itemListService, _customDataService));
            }
            //method
            public void ApplyAllController(Program program) {
                foreach (var controller in _controllers) {
                    controller.Apply(program);
                }
            }
            #endregion Controller
            #region QueryExecuter
            private interface QueryExecuter {
                void Exec();
            }

            private class CustomDataInitializer : QueryExecuter {
                private CustomDataService _service;

                public CustomDataInitializer(CustomDataService service) {
                    _service = service;
                }
                public void Exec() {
                    _service.init();
                }

            }
            CustomDataInitializer _customDataInitializer;
            Dictionary<string, QueryExecuter> _queryExecuters = new Dictionary<string, QueryExecuter>();
            public void InitQueryExcecuter() {
                _customDataInitializer = new CustomDataInitializer(_customDataService);
                _queryExecuters.Add("initCustomData", _customDataInitializer);
            }
            public void ExecQuery(Program program, string query) {
                if (_queryExecuters.ContainsKey(query)) {
                    _queryExecuters[query].Exec();
                } else {
                    program.Echo("invalid query");
                }
            }
            #endregion QueryExecuter

            public void InitAll(Program program) {
                initRepositories();
                initServices();
                initControllers();
                InitQueryExcecuter();

                LoadAllRepository(program);
            }

            public void UpdateAll(Program program) {
                UpdateAllService(program);
                ApplyAllController(program);
            }

        }

        public void test() {

        }
        private BeanProvider _beanProvider;
        public Program() {
            _beanProvider = new BeanProvider();
            _beanProvider.InitAll(this);

            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            test();
        }


        public void Save() { }


        public void Main(string argument, UpdateType updateSource) {
            if ((updateSource & UpdateType.Update100) != 0) {
                _beanProvider.UpdateAll(this);
            } else if ((updateSource & UpdateType.Terminal) != 0) {
                _beanProvider.ExecQuery(this, argument);
            }
        }

        #endregion // MyBaseController
    }
}