using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRage.ObjectBuilders;
using VRageMath;

namespace Automatic_Shipyard__SE_
{
    public class Program : MyGridProgram
    {
        public int internal_state = 0;
        public PlatformMotor platformMotor;
        public PlatformTracker platformTracker;
        public WelderController welderController;
        public CargoController cargoController;
        public ProjectionController projectionController;
        public GridProgramRef GridProgram;
        public void Main(string arguments, UpdateType updateSource)
        {
            if(arguments == "i")
            {
                GridProgram = new GridProgramRef(GridTerminalSystem, null, Echo, Me);
                GridProgram.Utils = new UtilsClass(GridProgram);
                BlueprintDefinitions.Initialize(GridProgram);
                Echo("Initialized blueprint definitions.");
                Echo($"{BlueprintDefinitions.blueprints.Count} definitions loaded.");
                projectionController = new ProjectionController(GridProgram, "Projector", ProjectorState.Unpowered);
                projectionController.SetProjectorState(ProjectorState.Powered);
                if(!projectionController.Projector.IsProjecting)
                {
                    Echo("Projector wasn't projecting.");
                    return;
                }
                Dictionary<string, int> components = projectionController.GetRequiredComponents();
                foreach(KeyValuePair<string, int> kvp in components)
                {
                    Echo($"{kvp.Key}: {kvp.Value}");
                }
            }
            if(arguments == "a")
            {
                List<IMyAssembler> assemblers = new List<IMyAssembler>();
                GridTerminalSystem.GetBlocksOfType(assemblers);
                IMyAssembler assembler = assemblers[0];
                IMyTextPanel textpanel = GridTerminalSystem.GetBlockWithName("TXTPNL") as IMyTextPanel;
                MyDefinitionId id = MyDefinitionId.Parse("MyObjectBuilder_BlueprintDefinition/BulletproofGlass");
                assembler.AddQueueItem(id, 1.0);
                return;
            }
            if(arguments == "p")
            {
                IMyTextPanel textpanel = GridTerminalSystem.GetBlockWithName("TXTPNL") as IMyTextPanel;
                textpanel.WriteText(projectionController.Projector.DetailedInfo);
                return;
            }
            if(arguments == "m")
            {
                
                return;
            }
            switch(internal_state)
            {
                case 0:
                    // config
                    try
                    {
                        GridProgram = new GridProgramRef(GridTerminalSystem, null, Echo, Me);
                        GridProgram.Utils = new UtilsClass(GridProgram);
                        platformMotor = new PlatformMotor(GridProgram, "Pistons", new MotorConfig(0.05f, 10f, 0.05f, MotorExtendType.POSITIVE));
                        platformTracker = new PlatformTracker(new TrackerConfig(GridProgram, "Camera X1", "Camera X2", "Camera Y1", "Camera Y2", 370), GridProgram);
                        welderController = new WelderController(GridProgram, new WelderConfig("Welders", WelderState.Unpowered));
                        cargoController = new CargoController(GridProgram, true);
                        projectionController = new ProjectionController(GridProgram, "Projector", ProjectorState.Unpowered);
                        platformTracker.EnableRaycast();
                        internal_state = 1;
                    }
                    catch(Exception ex)
                    {
                        Echo($"Execution halted, safeguard violation raised in {ex.TargetSite}: {ex.Message}");
                    }
                    break;
                case 1:
                    try
                    {
                        platformMotor.SetMotorState(MotorState.Extend);
                        internal_state = 2;
                    }
                    catch (Exception ex)
                    {
                        Echo($"Execution halted, safeguard violation raised in {ex.TargetSite}: {ex.Message}");
                    }
                    break;
                case 2:
                    try
                    {
                        TrackedPlatform tp = platformTracker.Track();
                        platformTracker.IsPlatformSafe(tp);
                    }
                    catch (Exception ex)
                    {
                        Echo($"Execution halted, safeguard violation raised in {ex.TargetSite}: {ex.Message}");
                    }
                    break;
            }
        }
    }
    public enum ProjectorState
    {
        Powered,
        Unpowered
    }
    public enum ProjectionState
    {
        Projecting,
        Finished,
        Idle
    }
    public class ProjectionController
    {
        public GridProgramRef GridProgram;
        public IMyGridTerminalSystem GridTerminalSystem;
        public IMyProjector Projector;
        public string ProjectorName;
        public ProjectorState ProjectorState;
        public ProjectionState ProjectionState;
        public ProjectionController(GridProgramRef gridProgramRef, IMyProjector projector, ProjectorState projectorState)
        {
            if (gridProgramRef.GridTerminalSystem == null) throw new ArgumentNullException("Passed GTS reference was null.");
            if (gridProgramRef.Echo == null) throw new ArgumentNullException("Passed Echo reference was null.");
            if (projector == null) throw new ArgumentNullException("Passed projector reference was null.");
            GridProgram = gridProgramRef;
            Projector = projector;
            ProjectorName = projector.Name;
            ProjectorState = projectorState;
            GridTerminalSystem = GridProgram.GridTerminalSystem;
            Safeguard_Check();
        }
        public ProjectionController(GridProgramRef gridProgramRef, string projectorName, ProjectorState projectorState)
        {
            if (gridProgramRef.GridTerminalSystem == null) throw new ArgumentNullException("Passed GTS reference was null.");
            if (gridProgramRef.Echo == null) throw new ArgumentNullException("Passed Echo reference was null.");
            if (gridProgramRef.Utils == null) throw new ArgumentNullException("Passed UtilsClass reference was null.");
            if (projectorName == null) throw new ArgumentNullException("projectorName was null.");
            if (!gridProgramRef.Utils.BlockWithNameExists(projectorName)) throw new ArgumentNullException("Projector was not found.");
            Projector = gridProgramRef.GridTerminalSystem.GetBlockWithName(projectorName) as IMyProjector;
            GridProgram = gridProgramRef;
            ProjectorName = projectorName;
            ProjectorState = projectorState;
            GridTerminalSystem = GridProgram.GridTerminalSystem;
            Safeguard_Check();
        }
        private void Safeguard_Check()
        {
            if (GridProgram.Echo == null) throw new ArgumentNullException("Passed Echo reference was null.");
            if (GridProgram.GridTerminalSystem == null) throw new ArgumentNullException("Passed GTS reference was null.");
            GridProgram.Echo("Running SafeGuard check..");
            if (Projector == null) throw new NullReferenceException("Projector reference was null.");
            if (!Projector.IsProjecting)
                ProjectionState = ProjectionState.Idle;
            else if (Projector.IsProjecting && Projector.RemainingBlocks == 0)
                ProjectionState = ProjectionState.Finished;
            else if (Projector.IsProjecting && Projector.RemainingBlocks != 0)
                ProjectionState = ProjectionState.Projecting;
            ApplyConfig();
            GridProgram.Echo("OK.");
        }
        public void ApplyConfig()
        {
            if (Projector == null) return;
            GridProgram.Echo($"Applying ProjectorState {ProjectorState} to the projector...");
            switch (ProjectorState)
            {
                case ProjectorState.Powered:
                    Projector.ApplyAction("OnOff_On");
                    break;
                case ProjectorState.Unpowered:
                    Projector.ApplyAction("OnOff_Off");
                    break;
            }
        }
        public void SetProjectorState(ProjectorState projectorState)
        {
            ProjectorState = projectorState;
            GridProgram.Echo($"New ProjectorState set: {ProjectorState}");
            Safeguard_Check();
        }
        public int GetRemainingBlockCount()
        {
            Safeguard_Check();
            return Projector.RemainingBlocks;
        }
        public int GetRemainingArmorBlockCount()
        {
            Safeguard_Check();
            return Projector.RemainingArmorBlocks;
        }
        public Dictionary<string, int> GetRequiredComponents()
        {
            IMyTextPanel panel = GridTerminalSystem.GetBlockWithName("TXTPNL") as IMyTextPanel;
            var blocks = Projector.RemainingBlocksPerType;
            char[] delimiters = new char[] { ',' };
            char[] remove = new char[] { '[', ']' };
            Dictionary<string, int> totalComponents = new Dictionary<string, int>();
            foreach (var item in blocks)
            {
                string[] blockInfo = item.ToString().Trim(remove).Split(delimiters, StringSplitOptions.None);
                try
                {
                    

                    string blockName = blockInfo[0];
                    int amount = Convert.ToInt32(blockInfo[1]);

                    BlueprintDefinitions.AddComponents(totalComponents, BlueprintDefinitions.GetBlockComponents(blockName), amount);
                }
                catch(Exception ex)
                {
                    GridProgram.Echo($"Error in GetRequiredComponent() at {blockInfo[0]}:{blockInfo[1]} => {ex.Message}");
                }
                // blockInfo[0] is blueprint, blockInfo[1] is number of required item
                
            }
            string output = "";
            foreach (KeyValuePair<string, int> component in totalComponents)
                output += component.Key.Replace("MyObjectBuilder_BlueprintDefinition/", "") + " " + component.Value.ToString() + "\n";
            panel.WriteText(output);
            return totalComponents;
        }
    }
    public class CargoController
    {
        public GridProgramRef GridProgramRef;
        public List<IMyCargoContainer> CargoContainers;

        public CargoController(GridProgramRef gridProgramRef, bool sameGridOnly)
        {
            if (gridProgramRef.GridTerminalSystem == null) throw new ArgumentNullException("Passed GTS reference was null.");
            if (gridProgramRef.Echo == null) throw new ArgumentNullException("Passed Echo reference was null.");
            GridProgramRef = gridProgramRef;
            CargoContainers = new List<IMyCargoContainer>();
            GridProgramRef.GridTerminalSystem.GetBlocksOfType(CargoContainers, b => b.CubeGrid == GridProgramRef.Me.CubeGrid || !sameGridOnly);
            if (CargoContainers.Count == 0) throw new Exception("No cargo containers were found.");
        }

        public long GetMaxVolume()
        {
            long maxVolume = 0;
            foreach(IMyCargoContainer container in CargoContainers)
                maxVolume += container.GetInventory().MaxVolume.RawValue;

            return maxVolume;
        }

        public long GetCurrentVolume()
        {
            long currentVolume = 0;
            foreach (IMyCargoContainer container in CargoContainers)
                currentVolume += container.GetInventory().CurrentVolume.RawValue;

            return currentVolume;
        }

        public long GetRemainingVolume()
        {
            return GetMaxVolume() - GetCurrentVolume();
        }

        public long GetItemAmount(MyItemType itemType)
        {
            long amount = 0;
            GridProgramRef.Echo($"TypeId: {itemType.TypeId}, SubtypeId: {itemType.SubtypeId}");
            foreach (IMyCargoContainer container in CargoContainers)
            {
                List<MyInventoryItem> _items = new List<MyInventoryItem>();
                container.GetInventory().GetItems(_items, i => i.Type == itemType);
                _items.ForEach(new Action<MyInventoryItem>((b) => { amount += b.Amount.RawValue; }));
            }

            return amount / 1000000;
        }
    }
    public static class BlueprintDefinitions
    {
        public static Dictionary<string, Dictionary<string, int>> blueprints = new Dictionary<string, Dictionary<string, int>>();
        public static GridProgramRef GridProgramRef;
        public static IMyGridTerminalSystem GridTerminalSystem;
        public static UtilsClass Utils;
        public static Action<string> Echo;
        public static IMyProgrammableBlock Me;
        public static void Initialize(GridProgramRef gridProgramRef)
        {
            if (gridProgramRef.GridTerminalSystem == null) throw new ArgumentNullException("Passed GTS reference was null.");
            if (gridProgramRef.Echo == null) throw new ArgumentNullException("Passed Echo reference was null.");
            if (gridProgramRef.Me == null) throw new ArgumentNullException("Passed Me reference was null.");
            GridProgramRef = gridProgramRef;
            GridTerminalSystem = gridProgramRef.GridTerminalSystem;
            Echo = gridProgramRef.Echo;
            Me = gridProgramRef.Me;
            Utils = gridProgramRef.Utils;
            // get data from customdata
            string[] splitted = Me.CustomData.Split(new char[] { '$' });
            string[] componentNames = splitted[0].Split(new char[] { '*' });
            for (var i = 0; i < componentNames.Length; i++)
                componentNames[i] = "MyObjectBuilder_BlueprintDefinition/" + componentNames[i];

            //$SmallMissileLauncher*(null)=0:4,2:2,5:1,7:4,8:1,4:1*LargeMissileLauncher=0:35,2:8,5:30,7:25,8:6,4:4$
            char[] asterisk = new char[] { '*' };
            char[] equalsign = new char[] { '=' };
            char[] comma = new char[] { ',' };
            char[] colon = new char[] { ':' };

            for (var i = 1; i < splitted.Length; i++)
            {
                // splitted[1 to n] are type names and all associated subtypes
                // blocks[0] is the type name, blocks[1 to n] are subtypes and component amounts
                string[] blocks = splitted[i].Split(asterisk);
                string typeName = "MyObjectBuilder_" + blocks[0];

                for (var j = 1; j < blocks.Length; j++)
                {
                    string[] compSplit = blocks[j].Split(equalsign);
                    string blockName = typeName + '/' + compSplit[0];

                    // add a new dict for the block
                    try
                    {
                        blueprints.Add(blockName, new Dictionary<string, int>());
                    }
                    catch (Exception e)
                    {
                        Echo("Error adding block: " + blockName);
                    }
                    var components = compSplit[1].Split(comma);
                    foreach (var component in components)
                    {
                        string[] amounts = component.Split(colon);
                        int idx = Convert.ToInt32(amounts[0]);
                        int amount = Convert.ToInt32(amounts[1]);
                        string compName = componentNames[idx];
                        blueprints[blockName].Add(compName, amount);
                    }
                }
            }
        }
        public static Dictionary<string, int> GetBlockComponents(string definition)
        {
            return blueprints[definition];
        }
        public static void AddComponents(Dictionary<string, int> AddTo, Dictionary<string, int> AddFrom, int Amount = 1)
        {
            foreach(KeyValuePair<string, int> component in AddFrom)
            {
                if (AddTo.ContainsKey(component.Key))
                    AddTo[component.Key] += component.Value * Amount;
                else
                    AddTo[component.Key] = component.Value * Amount;
            }
        }
    }
    public struct GridProgramRef
    {
        public IMyGridTerminalSystem GridTerminalSystem;
        public UtilsClass Utils;
        public Action<string> Echo;
        public IMyProgrammableBlock Me;

        public GridProgramRef(IMyGridTerminalSystem gridTerminalSystem, UtilsClass utils, Action<string> echo, IMyProgrammableBlock me)
        {
            if (gridTerminalSystem == null) throw new ArgumentNullException("Passed GTS reference was null.");
            if (echo == null) throw new ArgumentNullException("Passed Echo reference was null.");
            if (me == null) throw new ArgumentNullException("Passed Me reference was null.");
            GridTerminalSystem = gridTerminalSystem;
            Utils = utils;
            Echo = echo;
            Me = me;
        }
    }
    public class UtilsClass
    {
        public GridProgramRef GridProgramRef;

        public UtilsClass(GridProgramRef gridProgramRef)
        {
            GridProgramRef = gridProgramRef;
        }

        public void SetGridProgramRef(GridProgramRef gridProgramRef)
        {
            GridProgramRef = gridProgramRef;
        }

        public bool BlockWithNameExists(string Name)
        {
            List<IMyTerminalBlock> __l = new List<IMyTerminalBlock>();
            GridProgramRef.GridTerminalSystem.SearchBlocksOfName(Name, __l);
            return __l.Count > 0;
        }

        public bool BlockWithNameExistsAndIsOfType<T>(string Name)
        {
            List<IMyTerminalBlock> __l = new List<IMyTerminalBlock>();
            GridProgramRef.GridTerminalSystem.SearchBlocksOfName(Name, __l);
            bool a = __l.Count > 0;
            bool b = __l is T;
            GridProgramRef.Echo($"a = {a}");
            GridProgramRef.Echo($"b = {b}");
            return a && b;
        }
    }
    #region WelderController
    public struct WelderConfig
    {
        public string WelderGroupName;
        public WelderState WelderState;

        public WelderConfig(string welderGroupName, WelderState welderState) : this()
        {
            if(welderGroupName == null) throw new ArgumentNullException(nameof(welderGroupName));
            WelderGroupName = welderGroupName;
            WelderState = welderState;
        }
    }
    public enum WelderState
    {
        Powered,
        Unpowered
    }
    public class WelderController
    {
        public IMyGridTerminalSystem GridTerminalSystem;
        public Action<string> Echo;
        public List<IMyShipWelder> WelderList;
        public WelderConfig WelderConfig;

        public WelderController(GridProgramRef gridProgramRef, WelderConfig welderConfig)
        {
            // Safe guard: Throw an exception if GridTerminalSystem is null
            if (gridProgramRef.GridTerminalSystem == null) throw new ArgumentNullException("Passed GTS reference was null.");
            if (gridProgramRef.Echo == null) throw new ArgumentNullException("Passed Echo reference was null.");
            // Initialize initial variables
            WelderList = new List<IMyShipWelder>();
            GridTerminalSystem = gridProgramRef.GridTerminalSystem;
            Echo = gridProgramRef.Echo;
            WelderConfig = welderConfig;
            // Safe guard: Throw an exception if group is non existent
            var blocks = GridTerminalSystem.GetBlockGroupWithName(WelderConfig.WelderGroupName);
            if (blocks == null) throw new ArgumentException("Welder block group was not found.");
            blocks.GetBlocksOfType(WelderList);
            ApplyConfig();
        }

        private void Safeguard_Check()
        {
            Echo("Running SafeGuard check..");
            if (GridTerminalSystem == null) throw new ArgumentNullException("Passed GTS reference was null.");
            // Validate welder config
            var blocks = GridTerminalSystem.GetBlockGroupWithName(WelderConfig.WelderGroupName);
            if (blocks == null) throw new ArgumentException("Welder block group was not found.");
            List<IMyShipWelder> pistons = new List<IMyShipWelder>();
            blocks.GetBlocksOfType(pistons);
            if (pistons.Count != WelderList.Count)
                WelderList = pistons;
            // Validate welder config end

            // Make sure all welders have the same configuration
            ApplyConfig();
            Echo("OK.");
        }

        public void ApplyConfig()
        {
            if (WelderList.Count == 0) return;
            Echo($"Applying WelderState {WelderConfig.WelderState} to all welders...");
            foreach (IMyShipWelder welder in WelderList)
            {
                switch (WelderConfig.WelderState)
                {
                    case WelderState.Powered:
                        welder.ApplyAction("OnOff_On");
                        break;
                    case WelderState.Unpowered:
                        welder.ApplyAction("OnOff_Off");
                        break;
                }
            }
        }

        public void SetWelderState(WelderState welderState)
        {
            WelderConfig.WelderState = welderState;
            Echo($"New MotorState set: {welderState}");
            Safeguard_Check();
        }
    }
    #endregion
    #region PlatformTracker
    public struct TrackerConfig
    {
        public IMyCameraBlock TrackerTopLeft;
        public IMyCameraBlock TrackerTopRight;
        public IMyCameraBlock TrackerBottomLeft;
        public IMyCameraBlock TrackerBottomRight;
        public string TrackerTopLeft_Name;
        public string TrackerTopRight_Name;
        public string TrackerBottomLeft_Name;
        public string TrackerBottomRight_Name;
        public int Shipyard_Length;

        public TrackerConfig(IMyCameraBlock trackerTopLeft, IMyCameraBlock trackerTopRight, IMyCameraBlock trackerBottomLeft, IMyCameraBlock trackerBottomRight, int shipyard_Length) : this()
        {
            if (shipyard_Length <= 0) throw new ArgumentNullException("Shipyard_Length must be a positive value.");
            if(trackerTopLeft == null) throw new ArgumentNullException("trackerTopLeft was null.");
            if (trackerTopRight == null) throw new ArgumentNullException("trackerTopRight was null.");
            if (trackerBottomLeft == null) throw new ArgumentNullException("trackerBottomLeft was null.");
            if (trackerBottomRight == null) throw new ArgumentNullException("trackerBottomRight was null.");
            Shipyard_Length = shipyard_Length;
            TrackerTopLeft = trackerTopLeft;
            TrackerTopRight = trackerTopRight;
            TrackerBottomLeft = trackerBottomLeft;
            TrackerBottomRight = trackerBottomRight;
            TrackerTopLeft_Name = trackerTopLeft.Name;
            TrackerTopRight_Name = trackerTopRight.Name;
            TrackerBottomLeft_Name = trackerBottomLeft.Name;
            TrackerBottomRight_Name = trackerBottomRight.Name;
        }
        
        public TrackerConfig(GridProgramRef gridref, string trackerTopLeft_Name, string trackerTopRight_Name, string trackerBottomLeft_Name, string trackerBottomRight_Name, int shipyard_Length) : this()
        {
            if (shipyard_Length <= 0) throw new ArgumentNullException("Shipyard_Length must be a positive value.");
            Shipyard_Length = shipyard_Length;
            if (trackerTopLeft_Name == null) throw new ArgumentNullException("trackerTopLeft_Name was null.");
            if (trackerTopRight_Name == null) throw new ArgumentNullException("trackerTopRight_Name was null.");
            if (trackerBottomLeft_Name == null) throw new ArgumentNullException("trackerBottomLeft_Name was null.");
            if (trackerBottomRight_Name == null) throw new ArgumentNullException("trackerBottomRight_Name was null.");
            TrackerTopLeft_Name = trackerTopLeft_Name;
            TrackerTopRight_Name = trackerTopRight_Name;
            TrackerBottomLeft_Name = trackerBottomLeft_Name;
            TrackerBottomRight_Name = trackerBottomRight_Name;

            if (gridref.GridTerminalSystem == null) throw new ArgumentNullException("Passed GTS reference was null.");
            if (!gridref.Utils.BlockWithNameExists(trackerTopLeft_Name)) throw new ArgumentNullException("TrackerTopLeft was not found.");
            if (!gridref.Utils.BlockWithNameExists(trackerTopRight_Name)) throw new ArgumentNullException("TrackerTopRight was not found.");
            if (!gridref.Utils.BlockWithNameExists(trackerBottomLeft_Name)) throw new ArgumentNullException("TrackerBottomLeft was not found.");
            if (!gridref.Utils.BlockWithNameExists(trackerBottomRight_Name)) throw new ArgumentNullException("TrackerBottomRight was not found.");
            TrackerTopLeft = gridref.GridTerminalSystem.GetBlockWithName(trackerTopLeft_Name) as IMyCameraBlock;
            TrackerTopRight = gridref.GridTerminalSystem.GetBlockWithName(trackerTopRight_Name) as IMyCameraBlock;
            TrackerBottomLeft = gridref.GridTerminalSystem.GetBlockWithName(trackerBottomLeft_Name) as IMyCameraBlock;
            TrackerBottomRight = gridref.GridTerminalSystem.GetBlockWithName(trackerBottomRight_Name) as IMyCameraBlock;
        }
    }
    public struct TrackedPlatform
    {
        public double X1;
        public double X2;
        public double Y1;
        public double Y2;

        public TrackedPlatform(double x1, double x2, double y1, double y2)
        {
            X1 = x1;
            X2 = x2;
            Y1 = y1;
            Y2 = y2;
        }
    }
    public class PlatformTracker
    {
        public TrackerConfig TrackerConfig;
        public GridProgramRef gridProgramRef;

        public PlatformTracker(TrackerConfig trackerConfig, GridProgramRef gridprogramref)
        {
            TrackerConfig = trackerConfig;
            gridProgramRef = gridprogramref;
            Safeguard_Check();
        }

        private void Safeguard_Check()
        {
            if (TrackerConfig.TrackerTopLeft == null) throw new ArgumentNullException("TrackerTopLeft was null.");
            if (TrackerConfig.TrackerTopRight == null) throw new ArgumentNullException("TrackerTopRight was null.");
            if (TrackerConfig.TrackerBottomLeft == null) throw new ArgumentNullException("TrackerBottomLeft was null.");
            if (TrackerConfig.TrackerBottomRight == null) throw new ArgumentNullException("TrackerBottomRight was null.");
            if (TrackerConfig.Shipyard_Length <= 0) throw new ArgumentNullException("Shipyard_Length must be a positive value.");
            if (gridProgramRef.GridTerminalSystem == null) throw new ArgumentNullException("Passed GTS reference was null.");
            if (gridProgramRef.Echo == null) throw new ArgumentNullException("Passed Echo reference was null.");
        }

        public TrackedPlatform Track()
        {
            Safeguard_Check();
            
            if (!CanScanPlatform()) throw new InvalidOperationException("Trackers are not ready for raycasting.");
            MyDetectedEntityInfo x1_entity = TrackerConfig.TrackerTopLeft.Raycast(TrackerConfig.Shipyard_Length, 0f, 0f);
            MyDetectedEntityInfo x2_entity = TrackerConfig.TrackerTopRight.Raycast(TrackerConfig.Shipyard_Length, 0f, 0f);
            MyDetectedEntityInfo y1_entity = TrackerConfig.TrackerBottomLeft.Raycast(TrackerConfig.Shipyard_Length, 0f, 0f);
            MyDetectedEntityInfo y2_entity = TrackerConfig.TrackerBottomRight.Raycast(TrackerConfig.Shipyard_Length, 0f, 0f);
            double x1 = Vector3.Distance((Vector3)x1_entity.HitPosition, TrackerConfig.TrackerTopLeft.GetPosition());
            double x2 = Vector3.Distance((Vector3)x2_entity.HitPosition, TrackerConfig.TrackerTopRight.GetPosition());
            double y1 = Vector3.Distance((Vector3)y1_entity.HitPosition, TrackerConfig.TrackerBottomLeft.GetPosition());
            double y2 = Vector3.Distance((Vector3)y2_entity.HitPosition, TrackerConfig.TrackerBottomRight.GetPosition());
            return new TrackedPlatform(x1, x2, y1, y2);
        }
        
        public void EnableRaycast()
        {
            Safeguard_Check();
            TrackerConfig.TrackerTopLeft.EnableRaycast = true;
            TrackerConfig.TrackerTopRight.EnableRaycast = true;
            TrackerConfig.TrackerBottomLeft.EnableRaycast = true;
            TrackerConfig.TrackerBottomRight.EnableRaycast = true;
        }

        public bool CanScanPlatform()
        {
            return (TrackerConfig.TrackerTopLeft.CanScan(TrackerConfig.Shipyard_Length) && TrackerConfig.TrackerTopRight.CanScan(TrackerConfig.Shipyard_Length) &&
                TrackerConfig.TrackerBottomLeft.CanScan(TrackerConfig.Shipyard_Length) && TrackerConfig.TrackerBottomRight.CanScan(TrackerConfig.Shipyard_Length));
        }

        public double CalculatePlatformDisplacement(TrackedPlatform platform)
        {
            List<double> distancelist = new List<double>
            {
                platform.X1,
                platform.X2,
                platform.Y1,
                platform.Y2
            };

            distancelist.Sort();
            return distancelist[3] - distancelist[0];
        }

        public bool IsPlatformSafe(TrackedPlatform platform)
        {
            double displacement = CalculatePlatformDisplacement(platform);
            gridProgramRef.Echo("Platform displacement = {displacement}");
            gridProgramRef.Echo("X1: {platform.X1} X2: {platform.X2}");
            gridProgramRef.Echo("Y1: {platform.Y1} Y2: {platform.Y2}");
            return false;
        }
    }
    #endregion
    #region PlatformMotor
    public struct MotorConfig
    {
        public float MotorSpeed;
        public float MotorMaxElevation;
        public float MotorMinElevation;
        public MotorExtendType MOTOR_EXTEND_TYPE;
        public MotorConfig(float motorSpeed = 0.025f, float motorMaxElevation = 10f, float motorMinElevation = 0.05f, MotorExtendType motorExtendType = MotorExtendType.POSITIVE)
        {
            MotorSpeed = motorSpeed;
            MotorMaxElevation = motorMaxElevation;
            MotorMinElevation = motorMinElevation;
            MOTOR_EXTEND_TYPE = motorExtendType;
        }
    }
    public enum MotorExtendType
    {
        POSITIVE,
        NEGATIVE
    }
    public enum MotorState
    {
        Extend = 1,
        Retract = 2,
        Halt = 4,
        Idle = 8
    }
    public class PlatformMotor
    {
        public IMyGridTerminalSystem GridTerminalSystem;
        public Action<string> Echo;
        public string PistonGroupName;
        public List<IMyPistonBase> PistonList;
        public MotorState MotorState;
        public MotorConfig MotorConfig;
        public PlatformMotor(GridProgramRef gridProgramRef, string pistonGroupName, MotorConfig motorConfig)
        {
            // Safe guard: Throw an exception if GridTerminalSystem is null
            if (gridProgramRef.GridTerminalSystem == null) throw new ArgumentNullException("Passed GTS reference was null.");
            if (gridProgramRef.Echo == null) throw new ArgumentNullException("Passed Echo reference was null.");
            // Initialize initial variables
            PistonGroupName = pistonGroupName;
            PistonList = new List<IMyPistonBase>();
            MotorState = MotorState.Idle;
            GridTerminalSystem = gridProgramRef.GridTerminalSystem;
            Echo = gridProgramRef.Echo;
            MotorConfig = motorConfig;
            // Validate motor config
            if (MotorConfig.MotorSpeed < 0) throw new ArgumentException("MotorConfig: Velocity must be a signed number.");
            if (MotorConfig.MotorMaxElevation < 0) throw new ArgumentException("MotorConfig: Max elevation must be a signed number.");
            if (MotorConfig.MotorMinElevation < 0) throw new ArgumentException("MotorConfig: Min elevation must be a signed number.");
            // Validate motor config end
            // Safe guard: Throw an exception if group is non existent
            var blocks = GridTerminalSystem.GetBlockGroupWithName(PistonGroupName);
            if (blocks == null) throw new ArgumentException("Piston block group was not found.");
            blocks.GetBlocksOfType(PistonList);
            ApplyConfig();
        }
        private void Safeguard_Check()
        {
            Echo("Running SafeGuard check..");
            if (GridTerminalSystem == null) throw new ArgumentNullException("Passed GTS reference was null.");
            // Validate piston config
            var blocks = GridTerminalSystem.GetBlockGroupWithName(PistonGroupName);
            if (blocks == null) throw new ArgumentException("Piston block group was not found.");
            List<IMyPistonBase> pistons = new List<IMyPistonBase>();
            blocks.GetBlocksOfType(pistons);
            if (pistons.Count != PistonList.Count)
                PistonList = pistons;
            // Validate piston config end

            // Validate motor config
            if (MotorConfig.MotorSpeed < 0) throw new ArgumentException("MotorConfig: Velocity must be a signed number.");
            if (MotorConfig.MotorMaxElevation < 0) throw new ArgumentException("MotorConfig: Max elevation must be a signed number.");
            if (MotorConfig.MotorMinElevation < 0) throw new ArgumentException("MotorConfig: Min elevation must be a signed number.");
            // Validate motor config end

            // Make sure all pistons have the same configuration
            ApplyConfig();
            Echo("OK.");
        }
        public void ApplyConfig()
        {
            if (PistonList.Count == 0) return;
            Echo($"Applying MotorState {MotorState} to all pistons...");
            Echo($"DEBUG: MotorSpeed = {MotorConfig.MotorSpeed}");
            foreach (IMyPistonBase piston in PistonList)
            {
                switch (MotorState)
                {
                    case MotorState.Extend:
                        if (MotorConfig.MOTOR_EXTEND_TYPE == MotorExtendType.POSITIVE)
                            piston.Velocity = MotorConfig.MotorSpeed;
                        else
                            piston.Velocity = MotorConfig.MotorSpeed * -1; 
                        break;
                    case MotorState.Retract:
                        if (MotorConfig.MOTOR_EXTEND_TYPE == MotorExtendType.POSITIVE)
                            piston.Velocity = MotorConfig.MotorSpeed * -1;
                        else
                            piston.Velocity = MotorConfig.MotorSpeed;
                        break;
                    case MotorState.Idle:
                        piston.Velocity = 0;
                        break;
                    case MotorState.Halt:
                        piston.Velocity = 0;
                        break;
                }
            }
        }
        public void SetMotorState(MotorState motorState)
        {
            MotorState = motorState;
            Echo($"New MotorState set: {MotorState}");
            Safeguard_Check();
        }

        public void SetMinElevation(float MinElevation)
        {
            if(MinElevation < 0) throw new ArgumentException("MotorConfig: Min elevation must be a signed number.");
            MotorConfig.MotorMinElevation = MinElevation;
            Echo($"New MinElevation set: {MotorConfig.MotorMinElevation}");
            Safeguard_Check();
        }

        public void SetMaxElevation(float MaxElevation)
        {
            if (MaxElevation < 0) throw new ArgumentException("MotorConfig: Max elevation must be a signed number.");
            MotorConfig.MotorMaxElevation = MaxElevation;
            Echo($"New MaxElevation set: {MotorConfig.MotorMaxElevation}");
            Safeguard_Check();
        }
    }
    #endregion
}
