using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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
        public Dictionary<string,BlockVariant> BlockVariants;

        public BlockDefinition(string typeId)
        {
            TypeId = typeId ?? throw new ArgumentNullException(nameof(typeId));
            BlockVariants = new Dictionary<string,BlockVariant>();
        }

        public BlockDefinition(string typeId, Dictionary<string,BlockVariant> blockVariants)
        {
            TypeId = typeId ?? throw new ArgumentNullException(nameof(typeId));
            BlockVariants = blockVariants ?? throw new ArgumentNullException(nameof(blockVariants));
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            Dictionary<string, BlockDefinition> blockDefinitionCache = new Dictionary<string, BlockDefinition>();
            foreach(string definition_file in Directory.GetFiles("./CubeBlocks"))
            {
                XDocument doc = XDocument.Load(definition_file);
                XElement cubeblocks = doc.Descendants("Definitions").First().Descendants("CubeBlocks").First();
                foreach (XElement block in cubeblocks.Nodes().Where(x => x.NodeType != System.Xml.XmlNodeType.Comment))
                {
                    string typeId = block.Descendants("TypeId").First().Value;
                    string subtypeId = block.Descendants("SubtypeId").First().Value.Replace(" ", "");
                    if (subtypeId == "") subtypeId = "(null)";
                    BlockDefinition blockDefinition;
                    if (blockDefinitionCache.ContainsKey(typeId))
                        blockDefinition = blockDefinitionCache[typeId];
                    else
                        blockDefinition = new BlockDefinition(typeId);

                    if (!blockDefinition.BlockVariants.ContainsKey(subtypeId))
                    {
                        BlockVariant variant = new BlockVariant(subtypeId);
                        foreach (XElement component in block.Descendants("Components").Nodes().Where(x => x.NodeType != System.Xml.XmlNodeType.Comment))
                        {
                            string componentType = component.Attribute("Subtype").Value.Replace(" ", "");
                            int Amount = int.Parse(component.Attribute("Count").Value);
                            variant.AddComponents(componentType, Amount);
                        }
                        variant.ConvertComponentsToBlueprintDefinition();
                        blockDefinition.BlockVariants.Add(subtypeId, variant);
                    }

                    blockDefinitionCache[typeId] = blockDefinition;
                }
            }
            
            Serialize(blockDefinitionCache.Values.ToList());
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
                foreach(KeyValuePair<string,BlockVariant> variant in block.BlockVariants)
                {
                    string blockSubtypeDefinition = $"{variant.Value.SubtypeId}";
                    string blockComponentDefinition = "";
                    foreach (KeyValuePair<string, int> component in variant.Value.Components)
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
                foreach(KeyValuePair<string,BlockVariant> variant in block.BlockVariants)
                {
                    foreach(KeyValuePair<string,int> kvp in variant.Value.Components)
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
