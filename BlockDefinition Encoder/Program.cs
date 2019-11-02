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
    public class BlockVariant
    {
        public string SubtypeId;
        public Dictionary<string, int> Components;

        public BlockVariant(string subtypeId)
        {
            SubtypeId = subtypeId ?? throw new ArgumentNullException(nameof(subtypeId));
            Components = new Dictionary<string, int>();
        }

        public BlockVariant(string subtypeId, Dictionary<string, int> components) : this(subtypeId)
        {
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
    public class BlockDefinition
    {
        public string TypeId;
        public List<BlockVariant> BlockVariants;

        public BlockDefinition(string typeId)
        {
            TypeId = typeId ?? throw new ArgumentNullException(nameof(typeId));
            BlockVariants = new List<BlockVariant>();
        }

        public BlockDefinition(string typeId, List<BlockVariant> blockVariants)
        {
            TypeId = typeId ?? throw new ArgumentNullException(nameof(typeId));
            BlockVariants = blockVariants ?? throw new ArgumentNullException(nameof(blockVariants));
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            List<BlockVariant> assembler_variants = new List<BlockVariant>();
            BlockVariant LargeAssembler = new BlockVariant("LargeAssembler");
            LargeAssembler.AddComponents("SteelPlate", 140);
            LargeAssembler.AddComponents("Construction", 80);
            LargeAssembler.AddComponents("Motor", 20);
            LargeAssembler.AddComponents("Display", 10);
            LargeAssembler.AddComponents("MetalGrid", 10);
            LargeAssembler.AddComponents("Computer", 160);
            LargeAssembler.AddComponents("SteelPlate", 20);
            LargeAssembler.ConvertComponentsToBlueprintDefinition();
            BlockVariant BasicAssembler = new BlockVariant("BasicAssembler");
            BasicAssembler.AddComponents("SteelPlate", 60);
            BasicAssembler.AddComponents("Construction", 40);
            BasicAssembler.AddComponents("Motor", 10);
            BasicAssembler.AddComponents("Display", 4);
            BasicAssembler.AddComponents("Computer", 80);
            BasicAssembler.AddComponents("SteelPlate", 20);
            BasicAssembler.ConvertComponentsToBlueprintDefinition();
            assembler_variants.Add(LargeAssembler);
            assembler_variants.Add(BasicAssembler);
            BlockDefinition definition = new BlockDefinition("Assembler", assembler_variants);
            Serialize(new List<BlockDefinition>() { definition });
            Console.ReadLine();
        }

        public static void Serialize(List<BlockDefinition> blockDefinitions)
        {
            Dictionary<string, int> ReferenceList = MakeComponentReferenceList(blockDefinitions);
            string TableString = "";
            TableString += ReferenceListToString(ReferenceList);
            foreach(BlockDefinition block in blockDefinitions)
            {
                string blockTypeDefinition = $"{block.TypeId}";
                string blockDefinitionSoFar = "";
                blockDefinitionSoFar += $"${blockTypeDefinition}";
                foreach(BlockVariant variant in block.BlockVariants)
                {
                    string blockSubtypeDefinition = $"{variant.SubtypeId}";
                    string blockComponentDefinition = "";
                    foreach (KeyValuePair<string, int> component in variant.Components)
                    {
                        if (blockComponentDefinition == "")
                            blockComponentDefinition += $"{ReferenceList[component.Key]}:{component.Value}";
                        else
                            blockComponentDefinition += $",{ReferenceList[component.Key]}:{component.Value}";
                    }
                    blockDefinitionSoFar += $"*{blockSubtypeDefinition}={blockComponentDefinition}";
                }
                TableString += blockDefinitionSoFar;
            }
            Console.WriteLine(TableString);
        }

        public static Dictionary<string,int> MakeComponentReferenceList(List<BlockDefinition> blockDefinitions)
        {
            Dictionary<string, int> ReferenceList = new Dictionary<string, int>();
            foreach(BlockDefinition block in blockDefinitions)
            {
                foreach(BlockVariant variant in block.BlockVariants)
                {
                    foreach(KeyValuePair<string,int> kvp in variant.Components)
                    {
                        if (!ReferenceList.ContainsKey(kvp.Key))
                            ReferenceList[kvp.Key] = ReferenceList.Count;
                    }
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
