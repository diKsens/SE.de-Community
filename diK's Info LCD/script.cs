/*
 * R e a d m e
 * -----------
 * 
 * Change LCD Content Type to "Script"
 * To display battery, solar panels and other power suppliers the following tags can be written to the custom data.
 * 
 * Battery
 * WindTurbine
 * SolarPanel
 * HydrogenEngine
 * Reactor
 * 
 * to display ores, ingots or components
 * 
 * Ore
 * Ingot
 * Component
 * 
 * to split the display to several use the Ore, Ingot, Component tag several times on different LCD's
 * use LCD Panel A, LCD Panel B or <LCD Ore/Ingot/Component> A, <LCD Ore/Ingot/Component> B to specify your LCD arrangement
 */

// CONFIG

const int waitTicks = 3;
const string fontId = "White";

// CONFIG END

List<IMyTextPanel> textPanels = new List<IMyTextPanel>();
List<IMyBatteryBlock> batteryBlocks = new List<IMyBatteryBlock>();
List<IMyPowerProducer> windTurbines = new List<IMyPowerProducer>();
List<IMyPowerProducer> hydroenEngines = new List<IMyPowerProducer>();
List<IMySolarPanel> solarPanels = new List<IMySolarPanel>();
List<IMyReactor> reactors = new List<IMyReactor>();

List<IMyCargoContainer> cargoContainers = new List<IMyCargoContainer>();
List<MyInventoryItem> inventoryItems = new List<MyInventoryItem>();

Dictionary<string, int> cargoOres = new Dictionary<string, int>();
Dictionary<string, int> cargoIngots = new Dictionary<string, int>();
Dictionary<string, int> cargoComponents = new Dictionary<string, int>();

RectangleF viewport;
MySpriteDrawFrame frame;

Vector2 right;
Vector2 newLine;

Color fgColor;

IEnumerator<bool> stateMachine;

int skipOres;
int skipIngots;
int skipComponents;

public Program()
{
    Runtime.UpdateFrequency |= UpdateFrequency.Once;
}

public void Save()
{

}

public void Main(string argument, UpdateType updateType)
{
    if ((updateType & UpdateType.Once) == UpdateType.Once)
    {
        if (stateMachine == null)
            stateMachine = MainScript();

        if (stateMachine.MoveNext() == false)
        {
            stateMachine.Dispose();
            stateMachine = MainScript();
        }

        Runtime.UpdateFrequency |= UpdateFrequency.Once;
    }
}

IEnumerator<bool> MainScript()
{
    var startTime = DateTime.Now;

    textPanels.Clear();
    GridTerminalSystem.GetBlocksOfType(textPanels, b => b.IsWorking && !b.CustomData.Contains("Ignore"));
    for (int i = 0; i < waitTicks; ++i) yield return true;

    batteryBlocks.Clear();
    GridTerminalSystem.GetBlocksOfType(batteryBlocks, b => b.IsWorking);
    for (int i = 0; i < waitTicks; ++i) yield return true;

    windTurbines.Clear();
    GridTerminalSystem.GetBlocksOfType(windTurbines, b => b.IsWorking && b.BlockDefinition.SubtypeId.Contains("Wind"));
    for (int i = 0; i < waitTicks; ++i) yield return true;

    hydroenEngines.Clear();
    GridTerminalSystem.GetBlocksOfType(hydroenEngines, b => b.IsWorking && b.BlockDefinition.SubtypeId.Contains("Hydrogen"));
    for (int i = 0; i < waitTicks; ++i) yield return true;

    solarPanels.Clear();
    GridTerminalSystem.GetBlocksOfType(solarPanels, b => b.IsWorking);
    for (int i = 0; i < waitTicks; ++i) yield return true;

    reactors.Clear();
    GridTerminalSystem.GetBlocksOfType(reactors, b => b.IsWorking);
    for (int i = 0; i < waitTicks; ++i) yield return true;

    cargoContainers.Clear();
    GridTerminalSystem.GetBlocksOfType(cargoContainers);
    for (int i = 0; i < waitTicks; ++i) yield return true;

    cargoOres.Clear();
    cargoIngots.Clear();
    cargoComponents.Clear();

    foreach (var cargo in cargoContainers)
    {
        var inventory = cargo.GetInventory();

        if (inventory.ItemCount == 0) continue;

        inventoryItems.Clear();
        inventory.GetItems(inventoryItems);

        for (int i = 0; i < waitTicks; ++i) yield return true;

        foreach (var item in inventoryItems.OrderBy(i => i.Type.SubtypeId))
        {
            var type = item.Type.TypeId.Split('_')[1];
            var name = item.Type.SubtypeId;
            var amount = item.Amount.ToIntSafe();

            if (type == "Ore")
            {
                if (!cargoOres.ContainsKey(name)) cargoOres.Add(name, 0);

                cargoOres[name] += amount;
            }
            else if (type == "Ingot")
            {
                if (!cargoIngots.ContainsKey(name)) cargoIngots.Add(name, 0);

                cargoIngots[name] += amount;
            }
            else if (type == "Component")
            {
                if (!cargoComponents.ContainsKey(name)) cargoComponents.Add(name, 0);

                cargoComponents[name] += amount;
            }
        }

        for (int i = 0; i < waitTicks; ++i) yield return true;
    }

    skipOres = 0;
    skipIngots = 0;
    skipComponents = 0;

    foreach (var textPanel in textPanels.OrderBy(b => b.DisplayNameText))
    {
        fgColor = textPanel.ScriptForegroundColor;

        viewport = new RectangleF((textPanel.TextureSize - textPanel.SurfaceSize) / 2f, textPanel.SurfaceSize);
        frame = textPanel.DrawFrame();

        // All sprites must be added to the frame here
        var position = new Vector2(5, 5) + viewport.Position;

        right = new Vector2(textPanel.SurfaceSize.X - 10, 0);
        newLine = new Vector2(0, 30);

        WriteTextSprite(ref frame, startTime.ToString("H:mm:ss"), position + right, TextAlignment.RIGHT);

        if (textPanel.CustomData.Contains("Battery")) DrawBatterySprite(ref frame, ref position, textPanel);
        for (int i = 0; i < waitTicks; ++i) yield return true;

        if (textPanel.CustomData.Contains("WindTurbine")) DrawWindTurbineSprite(ref frame, ref position, textPanel);
        for (int i = 0; i < waitTicks; ++i) yield return true;

        if (textPanel.CustomData.Contains("HydrogenEngine")) DrawHydrogenEngineSprite(ref frame, ref position, textPanel);
        for (int i = 0; i < waitTicks; ++i) yield return true;

        if (textPanel.CustomData.Contains("SolarPanel")) DrawSolarPanelSprite(ref frame, ref position, textPanel);
        for (int i = 0; i < waitTicks; ++i) yield return true;

        if (textPanel.CustomData.Contains("Reactor")) DrawReactorSprite(ref frame, ref position, textPanel);
        for (int i = 0; i < waitTicks; ++i) yield return true;

        if (textPanel.CustomData.Contains("Ore")) DrawOreSprite(ref frame, ref position, textPanel);
        for (int i = 0; i < waitTicks; ++i) yield return true;

        if (textPanel.CustomData.Contains("Ingot")) DrawIngotSprite(ref frame, ref position, textPanel);
        for (int i = 0; i < waitTicks; ++i) yield return true;

        if (textPanel.CustomData.Contains("Component")) DrawComponentSprite(ref frame, ref position, textPanel);
        for (int i = 0; i < waitTicks; ++i) yield return true;

        frame.Dispose();
    }

    var endTime = DateTime.Now;
    Echo($"Total Runtime: {(endTime - startTime).TotalMilliseconds.ToString("#0.00")} ms");
}

void DrawBatterySprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextPanel textPanel)
{
    var current = batteryBlocks.Sum(b => b.CurrentStoredPower);
    var total = batteryBlocks.Sum(b => b.MaxStoredPower);
    var input = batteryBlocks.Sum(b => b.CurrentInput);
    var output = batteryBlocks.Sum(b => b.CurrentOutput);

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

void DrawWindTurbineSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextPanel textPanel)
{
    var current = windTurbines.Sum(b => b.CurrentOutput);
    var total = windTurbines.Sum(b => b.Components.Get<MyResourceSourceComponent>().DefinedOutput);

    WriteTextSprite(ref frame, "Wind Turbine", position, TextAlignment.LEFT);

    position += newLine;

    WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
    WriteTextSprite(ref frame, current.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

    position += newLine;

    WriteTextSprite(ref frame, "Max Output:", position, TextAlignment.LEFT);
    WriteTextSprite(ref frame, total.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

    position += newLine + newLine;
}

void DrawHydrogenEngineSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextPanel textPanel)
{
    var current = hydroenEngines.Sum(b => b.CurrentOutput);
    var total = hydroenEngines.Sum(b => b.MaxOutput);

    WriteTextSprite(ref frame, "Hydrogen Engines", position, TextAlignment.LEFT);

    position += newLine;

    WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
    WriteTextSprite(ref frame, current.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

    position += newLine;

    WriteTextSprite(ref frame, "Max Output:", position, TextAlignment.LEFT);
    WriteTextSprite(ref frame, total.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

    position += newLine + newLine;
}

void DrawSolarPanelSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextPanel textPanel)
{
    var current = solarPanels.Sum(b => b.CurrentOutput);
    var total = solarPanels.Sum(b => b.Components.Get<MyResourceSourceComponent>().DefinedOutput);

    WriteTextSprite(ref frame, "Solar Panels", position, TextAlignment.LEFT);

    position += newLine;

    WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
    WriteTextSprite(ref frame, current.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

    position += newLine;

    WriteTextSprite(ref frame, "Max Output:", position, TextAlignment.LEFT);
    WriteTextSprite(ref frame, total.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

    position += newLine + newLine;
}

void DrawReactorSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextPanel textPanel)
{
    var current = reactors.Sum(b => b.CurrentOutput);
    var total = reactors.Sum(b => b.MaxOutput);

    WriteTextSprite(ref frame, "Reactors", position, TextAlignment.LEFT);

    position += newLine;

    WriteTextSprite(ref frame, "Current Output:", position, TextAlignment.LEFT);
    WriteTextSprite(ref frame, current.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

    position += newLine;

    WriteTextSprite(ref frame, "Max Output:", position, TextAlignment.LEFT);
    WriteTextSprite(ref frame, total.ToString("#0.00") + " MW", position + right, TextAlignment.RIGHT);

    position += newLine + newLine;
}

void DrawOreSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextPanel textPanel)
{
    WriteTextSprite(ref frame, "Ores", position, TextAlignment.LEFT);

    position += newLine;

    foreach (var item in cargoOres.Skip(skipOres))
    {
        WriteTextSprite(ref frame, item.Key, position, TextAlignment.LEFT);
        WriteTextSprite(ref frame, KiloFormat(item.Value), position + right, TextAlignment.RIGHT);

        position += newLine;

        skipOres++;

        if (position.Y + newLine.Y >= textPanel.SurfaceSize.Y) break;
    }

    if (skipOres >= cargoOres.Count()) skipOres = 0;
}

void DrawIngotSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextPanel textPanel)
{
    WriteTextSprite(ref frame, "Ingots", position, TextAlignment.LEFT);

    position += newLine;

    foreach (var item in cargoIngots.Skip(skipIngots))
    {
        WriteTextSprite(ref frame, item.Key, position, TextAlignment.LEFT);
        WriteTextSprite(ref frame, KiloFormat(item.Value), position + right, TextAlignment.RIGHT);

        position += newLine;

        skipIngots++;

        if (position.Y + newLine.Y >= textPanel.SurfaceSize.Y) break;
    }

    if (skipIngots >= cargoIngots.Count()) skipIngots = 0;
}

void DrawComponentSprite(ref MySpriteDrawFrame frame, ref Vector2 position, IMyTextPanel textPanel)
{
    WriteTextSprite(ref frame, "Components", position, TextAlignment.LEFT);

    position += newLine;

    foreach (var item in cargoComponents.Skip(skipComponents))
    {
        WriteTextSprite(ref frame, item.Key, position, TextAlignment.LEFT);
        WriteTextSprite(ref frame, KiloFormat(item.Value), position + right, TextAlignment.RIGHT);

        position += newLine;

        skipComponents++;

        if (position.Y + newLine.Y >= textPanel.SurfaceSize.Y) break;
    }

    if (skipComponents >= cargoComponents.Count()) skipComponents = 0;
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
        Color = fgColor,
        Alignment = alignment,
        FontId = fontId
    };

    frame.Add(sprite);
}