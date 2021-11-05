using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Utils;
using VRageMath;

namespace EconomySurvival.LCDInfo
{
    class cargoItemType
    {
        public VRage.Game.ModAPI.Ingame.MyInventoryItem item;
        public int amount;
    }

    [MyTextSurfaceScript("LCDInfoScreen", "ES Info LCD")]
    public class LCDInfo : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        IMyTextSurface mySurface;
        IMyTerminalBlock myTerminalBlock;

        List<IMyBatteryBlock> batteryBlocks = new List<IMyBatteryBlock>();
        List<IMyPowerProducer> windTurbines = new List<IMyPowerProducer>();
        List<IMyPowerProducer> hydroenEngines = new List<IMyPowerProducer>();
        List<IMySolarPanel> solarPanels = new List<IMySolarPanel>();
        List<IMyReactor> reactors = new List<IMyReactor>();
        List<IMyGasTank> tanks = new List<IMyGasTank>();

        List<IMyInventory> inventorys = new List<IMyInventory>();
        List<VRage.Game.ModAPI.Ingame.MyInventoryItem> inventoryItems = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();

        Dictionary<string, cargoItemType> cargoOres = new Dictionary<string, cargoItemType>();
        Dictionary<string, cargoItemType> cargoIngots = new Dictionary<string, cargoItemType>();
        Dictionary<string, cargoItemType> cargoComponents = new Dictionary<string, cargoItemType>();
        Dictionary<string, cargoItemType> cargoItems = new Dictionary<string, cargoItemType>();

        Vector2 right;
        Vector2 newLine;
        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> myDefinitions;
        MyDefinitionId myDefinitionId;

        bool ConfigCheck = false;

        public LCDInfo(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            mySurface = surface;
            myTerminalBlock = block as IMyTerminalBlock;
        }

        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;

        public override void Dispose()
        {

        }

        public override void Run()
        {
            if (myTerminalBlock.CustomData.Length <= 0)
                CreateConfig();

            LoadConfig();

            if (!ConfigCheck)
                return;

            var myCubeGrid = myTerminalBlock.CubeGrid as MyCubeGrid;
            var myFatBlocks = myCubeGrid.GetFatBlocks().Where(block => block.IsWorking);

            batteryBlocks.Clear();
            windTurbines.Clear();
            hydroenEngines.Clear();
            solarPanels.Clear();
            reactors.Clear();
            inventorys.Clear();
            tanks.Clear();

            foreach (var myBlock in myFatBlocks)
            {
                if (myBlock is IMyBatteryBlock)
                {
                    batteryBlocks.Add((IMyBatteryBlock)myBlock);
                }
                else if (myBlock is IMyPowerProducer)
                {
                    if (myBlock.BlockDefinition.Id.SubtypeName.Contains("Wind"))
                    {
                        windTurbines.Add((IMyPowerProducer)myBlock);
                    }
                    else if (myBlock.BlockDefinition.Id.SubtypeName.Contains("Hydrogen"))
                    {
                        hydroenEngines.Add((IMyPowerProducer)myBlock);
                    }
                    else if (myBlock is IMyReactor)
                    {
                        reactors.Add((IMyReactor)myBlock);
                    }
                    else if (myBlock is IMySolarPanel)
                    {
                        solarPanels.Add((IMySolarPanel)myBlock);
                    }
                }
                else if (myBlock is IMyGasTank)
                {
                    tanks.Add((IMyGasTank)myBlock);
                }

                if (myBlock.HasInventory)
                {
                    for (int i = 0; i < myBlock.InventoryCount; i++)
                    {
                        inventorys.Add(myBlock.GetInventory(i));
                    }
                }
            }

            cargoOres.Clear();
            cargoIngots.Clear();
            cargoComponents.Clear();
            cargoItems.Clear();

            foreach (var inventory in inventorys)
            {
                if (inventory.ItemCount == 0)
                    continue;

                inventoryItems.Clear();
                inventory.GetItems(inventoryItems);

                foreach (var item in inventoryItems.OrderBy(i => i.Type.SubtypeId))
                {
                    var type = item.Type.TypeId.Split('_')[1];
                    var name = item.Type.SubtypeId;
                    var amount = item.Amount.ToIntSafe();

                    var myType = new cargoItemType { item=item, amount=0 };

                    if (type == "Ore")
                    {
                        if (!cargoOres.ContainsKey(name))
                            cargoOres.Add(name, myType);

                        cargoOres[name].amount += amount;
                    }
                    else if (type == "Ingot")
                    {
                        if (!cargoIngots.ContainsKey(name))
                            cargoIngots.Add(name, myType);

                        cargoIngots[name].amount += amount;
                    }
                    else if (type == "Component")
                    {
                        if (!cargoComponents.ContainsKey(name))
                            cargoComponents.Add(name, myType);

                        cargoComponents[name].amount += amount;
                    }
                    else
                    {
                        if (!cargoItems.ContainsKey(name))
                            cargoItems.Add(name, myType);

                        cargoItems[name].amount += amount;
                    }
                }
            }

            var myFrame = mySurface.DrawFrame();
            var myViewport = new RectangleF((mySurface.TextureSize - mySurface.SurfaceSize) / 2f, mySurface.SurfaceSize);
            var myPosition = new Vector2(5, 5) + myViewport.Position;

            right = new Vector2(mySurface.SurfaceSize.X - 10, 0);
            newLine = new Vector2(0, 30);
            myDefinitions = MyDefinitionManager.Static.GetAllDefinitions();

            if (config.Get("Settings", "Battery").ToBoolean())
                DrawBatterySprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get("Settings", "WindTurbine").ToBoolean())
                DrawWindTurbineSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get("Settings", "HydrogenEngine").ToBoolean())
                DrawHydrogenEngineSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get("Settings", "Tanks").ToBoolean())
                DrawTanksSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get("Settings", "Solar").ToBoolean())
                DrawSolarPanelSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get("Settings", "Reactor").ToBoolean())
                DrawReactorSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get("Settings", "Ore").ToBoolean())
                DrawOreSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get("Settings", "Ingot").ToBoolean())
                DrawIngotSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get("Settings", "Component").ToBoolean())
                DrawComponentSprite(ref myFrame, ref myPosition, mySurface);

            if (config.Get("Settings", "Items").ToBoolean())
                DrawItemsSprite(ref myFrame, ref myPosition, mySurface);

            myFrame.Dispose();
        }

        void DrawBatterySprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var current = batteryBlocks.Sum(block => block.CurrentStoredPower);
            var total = batteryBlocks.Sum(block => block.MaxStoredPower);
            var input = batteryBlocks.Sum(block => block.CurrentInput);
            var output = batteryBlocks.Sum(block => block.CurrentOutput);

            WriteTextSprite(ref frame, "Battery", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Stored Power:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, current.ToString("#0.00") + " MWh", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Max Stored Power:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, total.ToString("#0.00") + " MWh", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Input:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, input.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, output.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine + newLine;
        }

        void DrawWindTurbineSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var current = windTurbines.Sum(block => block.CurrentOutput);
            var currentMax = windTurbines.Sum(block => block.MaxOutput);
            var total = windTurbines.Sum(block => block.Components.Get<MyResourceSourceComponent>().DefinedOutput);

            WriteTextSprite(ref frame, "Wind Turbine", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, current.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Max Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, currentMax.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Total Max Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, total.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine + newLine;
        }

        void DrawHydrogenEngineSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var current = hydroenEngines.Sum(block => block.CurrentOutput);
            var total = hydroenEngines.Sum(block => block.MaxOutput);

            WriteTextSprite(ref frame, "Hydrogen Engines", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, current.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Max Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, total.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine + newLine;
        }

        void DrawTanksSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var hydrogenTanks = tanks.Where(block => block.BlockDefinition.SubtypeName.Contains("Hydrogen"));
            var oxygenTanks = tanks.Where(block => !block.BlockDefinition.SubtypeName.Contains("Hydrogen"));

            var currentHydrogen = hydrogenTanks.Count() == 0 ? 0 : hydrogenTanks.Average(block => block.FilledRatio * 100);
            var totalHydrogen = hydrogenTanks.Count() == 0 ? 0 : hydrogenTanks.Sum(block => block.Capacity);

            var currentOxygen = oxygenTanks.Count() == 0 ? 0 : oxygenTanks.Average(block => block.FilledRatio * 100);
            var totalOxygen = oxygenTanks.Count() == 0 ? 0 : oxygenTanks.Sum(block => block.Capacity);

            WriteTextSprite(ref frame, "Hydrogen Tanks", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, currentHydrogen.ToString("#0.00") + " %", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Total:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, KiloFormat((int)totalHydrogen), position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Oxygen Tanks", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, currentOxygen.ToString("#0.00") + " %", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Total:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, KiloFormat((int)totalOxygen), position + right, TextAlignment.RIGHT);

            position += newLine + newLine;
        }

        void DrawSolarPanelSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var current = solarPanels.Sum(block => block.CurrentOutput);
            var currentMax = solarPanels.Sum(block => block.MaxOutput);
            var total = solarPanels.Sum(block => block.Components.Get<MyResourceSourceComponent>().DefinedOutput);

            WriteTextSprite(ref frame, "Solar Panels", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, current.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Max Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, currentMax.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Total Max Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, total.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine + newLine;
        }

        void DrawReactorSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            var current = reactors.Sum(block => block.CurrentOutput);
            var total = reactors.Sum(block => block.MaxOutput);

            WriteTextSprite(ref frame, "Reactors", position, TextAlignment.LEFT);

            position += newLine;

            WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, current.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine;

            WriteTextSprite(ref frame, "Max Output:", position, TextAlignment.LEFT);
            WriteTextSprite(ref frame, total.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

            position += newLine + newLine;
        }

        void DrawOreSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "Ores", position, TextAlignment.LEFT);

            position += newLine;

            foreach (var item in cargoOres)
            {
                MyDefinitionId.TryParse(item.Value.item.Type.TypeId, item.Value.item.Type.SubtypeId, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.Value.amount), position + right, TextAlignment.RIGHT);

                position += newLine;
            }
        }

        void DrawIngotSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "Ingots", position, TextAlignment.LEFT);

            position += newLine;

            foreach (var item in cargoIngots)
            {
                MyDefinitionId.TryParse(item.Value.item.Type.TypeId, item.Value.item.Type.SubtypeId, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.Value.amount), position + right, TextAlignment.RIGHT);

                position += newLine;
            }
        }

        void DrawComponentSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "Components", position, TextAlignment.LEFT);

            position += newLine;

            foreach (var item in cargoComponents)
            {
                MyDefinitionId.TryParse(item.Value.item.Type.TypeId, item.Value.item.Type.SubtypeId, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.Value.amount), position + right, TextAlignment.RIGHT);

                position += newLine;
            }
        }

        void DrawItemsSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextSurface surface)
        {
            WriteTextSprite(ref frame, "Items", position, TextAlignment.LEFT);

            position += newLine;

            foreach (var item in cargoItems)
            {
                MyDefinitionId.TryParse(item.Value.item.Type.TypeId, item.Value.item.Type.SubtypeId, out myDefinitionId);

                WriteTextSprite(ref frame, myDefinitions[myDefinitionId].DisplayNameText, position, TextAlignment.LEFT);
                WriteTextSprite(ref frame, KiloFormat(item.Value.amount), position + right, TextAlignment.RIGHT);

                position += newLine;
            }
        }

        static string KiloFormat(int num)
        {
            if (num >= 100000000)
                return (num / 1000000).ToString("#,0 M");

            if (num >= 10000000)
                return (num / 1000000).ToString("0.#") + " M";

            if (num >= 100000)
                return (num / 1000).ToString("#,0 K");

            if (num >= 10000)
                return (num / 1000).ToString("0.#") + " K";

            return num.ToString("#,0");
        }

        void WriteTextSprite(ref MySpriteDrawFrame frame, string text, Vector2 position, TextAlignment alignment)
        {
            var sprite = new MySprite
            {
                Type = SpriteType.TEXT,
                Data = text,
                Position = position,
                RotationOrScale = 1f,
                Color = mySurface.ScriptForegroundColor,
                Alignment = alignment,
                FontId = "White"
            };

            frame.Add(sprite);
        }

        private void CreateConfig()
        {
            config.AddSection("Settings");

            config.Set("Settings", "Battery", "false");
            config.Set("Settings", "WindTurbine", "false");
            config.Set("Settings", "HydrogenEngine", "false");
            config.Set("Settings", "Tanks", "false");
            config.Set("Settings", "Solar", "false");
            config.Set("Settings", "Reactor", "false");
            config.Set("Settings", "Ore", "false");
            config.Set("Settings", "Ingot", "false");
            config.Set("Settings", "Component", "false");
            config.Set("Settings", "Items", "false");

            config.Invalidate();
            myTerminalBlock.CustomData = config.ToString();
        }

        private void LoadConfig()
        {
            ConfigCheck = false;

            if (config.TryParse(myTerminalBlock.CustomData))
            {
                if (config.ContainsSection("Settings"))
                {
                    ConfigCheck = true;
                }
                else
                {
                    MyLog.Default.WriteLine("EconomySurvival.LCDInfo: Config Value error");
                }
            }
            else
            {
                MyLog.Default.WriteLine("EconomySurvival.LCDInfo: Config Syntax error");
            }
        }
    }
}
