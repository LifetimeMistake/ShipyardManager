using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockDefinition_Encoder
{
    public static class ComponentDefinition
    {
        public static Dictionary<string, string> Definition = new Dictionary<string, string>()
        {
            { "SteelPlate", "SteelPlate" },
            { "Construction", "ConstructionComponent" },
            { "PowerCell", "PowerCell" },
            { "Computer", "ComputerComponent" },
            { "LargeTube", "LargeTube" },
            { "Motor", "MotorComponent" },
            { "Display", "Display" },
            { "MetalGrid", "MetalGrid" },
            { "InteriorPlate", "InteriorPlate" },
            { "SmallTube" , "SmallTube" },
            { "RadioCommunication", "RadioCommunicationComponent" },
            { "BulletproofGlass" , "BulletproofGlass" },
            { "Girder", "GirderComponent" },
            { "Explosives", "ExplosivesComponent" },
            { "Detector", "DetectorComponent" },
            { "Medical", "MedicalComponent" },
            { "GravityGenerator", "GravityGeneratorComponent" },
            { "Superconductor", "Superconductor" },
            { "Thrust", "ThrustComponent" },
            { "Reactor", "ReactorComponent" },
            { "SolarCell", "SolarCell" }
        };
    }
    public class BlockDefinition
    {
        public string TypeId;
        public string SubtypeId;
        public Dictionary<string, int> Components;

        public BlockDefinition(string typeId, string subtypeId)
        {
            TypeId = typeId ?? throw new ArgumentNullException(nameof(typeId));
            SubtypeId = subtypeId ?? throw new ArgumentNullException(nameof(subtypeId));
            Components = new Dictionary<string, int>();
        }

        public BlockDefinition(string typeId, string subtypeId, Dictionary<string, int> components) : this(typeId, subtypeId)
        {
            TypeId = typeId ?? throw new ArgumentNullException(nameof(typeId));
            SubtypeId = subtypeId ?? throw new ArgumentNullException(nameof(subtypeId));
            Components = components ?? throw new ArgumentNullException(nameof(components));
            SimplifyDefinition();
        }

        public void AddComponents(string componentType, int amount)
        {
            // If component type is already registered, stack the component amount.
            if (Components.ContainsKey(componentType))
                Components[componentType] += amount;
            else
                Components[componentType] = amount;
        }

        public void SimplifyDefinition()
        {
            Dictionary<string, int> SortedComponents = new Dictionary<string, int>();
            foreach (KeyValuePair<string, int> component in Components)
            {
                if (SortedComponents.ContainsKey(component.Key))
                    SortedComponents[component.Key] += component.Value;
                else
                    SortedComponents[component.Key] = component.Value;
            }
            Components = SortedComponents;
        }

        public void ConvertComponentsToBlueprintDefinition()
        {
            Dictionary<string, int> ConvertedComponents = new Dictionary<string, int>();
            foreach (KeyValuePair<string, int> component in Components)
            {
                if (ComponentDefinition.Definition.ContainsKey(component.Key)) // convert component
                    ConvertedComponents[ComponentDefinition.Definition[component.Key]] = component.Value;
                else // the component has either been converted or comes from a mod
                    ConvertedComponents[component.Key] = component.Value;
            }
            Components = ConvertedComponents;
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            List<BlockDefinition> blocks = new List<BlockDefinition>();
            BlockDefinition assembler = new BlockDefinition("Assembler", "LargeAssembler");
            assembler.AddComponents("SteelPlate", 120);
            assembler.AddComponents("Construction", 80);
            assembler.AddComponents("Motor", 20);
            assembler.AddComponents("MetalGrid", 10);
            assembler.AddComponents("Computer", 160);
            assembler.AddComponents("SteelPlate", 20);
            assembler.ConvertComponentsToBlueprintDefinition();
            blocks.Add(assembler);
            Serialize(blocks);
            Console.ReadLine();
        }

        public static void Serialize(List<BlockDefinition> blockDefinitions)
        {
            Dictionary<string, int> ReferenceList = MakeComponentReferenceList(blockDefinitions);
            string TableString = "";
            TableString += ReferenceListToString(ReferenceList);
            foreach(BlockDefinition block in blockDefinitions)
            {
                string blockTypeDefinition = $"{block.TypeId}*{block.SubtypeId}";
                string blockComponentDefinition = "";
                foreach(KeyValuePair<string,int> component in block.Components)
                {
                    if (blockComponentDefinition == "")
                        blockComponentDefinition += $"{ReferenceList[component.Key]}:{component.Value}";
                    else
                        blockComponentDefinition += $",{ReferenceList[component.Key]}:{component.Value}";
                }
                TableString += $"${blockTypeDefinition}={blockComponentDefinition}";
            }
            Console.WriteLine(TableString);
        }

        public static Dictionary<string,int> MakeComponentReferenceList(List<BlockDefinition> blockDefinitions)
        {
            Dictionary<string, int> ReferenceList = new Dictionary<string, int>();
            foreach(BlockDefinition block in blockDefinitions)
            {
                foreach(KeyValuePair<string,int> kvp in block.Components)
                {
                    if (!ReferenceList.ContainsKey(kvp.Key))
                        ReferenceList[kvp.Key] = ReferenceList.Count;
                }
            }
            ReferenceList.OrderBy(reference => reference.Value);
            return ReferenceList;
        }

        public static string ReferenceListToString(Dictionary<string,int> ReferenceList)
        {
            ReferenceList.OrderBy(reference => reference.Value);
            return string.Join("*", ReferenceList.Keys);
        }
    }
}
