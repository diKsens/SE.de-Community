/*
 * R e a d m e
 * -----------
 * 
 * copy one or more type names inside your cargos customData to sort items there
 * 
 * to ignore a container, write "Ignore" in the customData
 * use "Stop" on your connector to ignore grids on the other side
 * 
 * Example groups:
 * 
 * Component
 * Ore
 * Ingot
 * AmmoMagazine
 * PhysicalGunObject
 * GasContainerObject
 * OxygenContainerObject
 * PhysicalObject
 * ConsumableItem
 * 
 * You can sort special ores and components with:
 * 
 * Manual
 * Iron
 * IronIngot
 * SteelPlate
 * Ice
 * ...
 */

// CONFIG

const int waitTicks = 3;

// CONFIG END

List<IMyTerminalBlock> terminalBlocksFilter = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> terminalBlocks = new List<IMyTerminalBlock>();
List<IMyCargoContainer> cargoContainers = new List<IMyCargoContainer>();
List<MyInventoryItem> inventoryItems = new List<MyInventoryItem>();
List<IMyCubeGrid> ignoreGrids = new List<IMyCubeGrid>();

IEnumerator<bool> stateMachine;

IMyInventory blockInventory;
IMyInventory cargoInventory;

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

    terminalBlocksFilter.Clear();
    GridTerminalSystem.GetBlocks(terminalBlocksFilter);

    for (int i = 0; i < waitTicks; ++i)
        yield return true;

    terminalBlocks.Clear();
    terminalBlocks.AddRange(terminalBlocksFilter.Where(block => block is IMyCargoContainer || block is IMyShipConnector || block is IMyAssembler || block is IMyRefinery));

    ignoreGrids.Clear();
    ignoreGrids.AddRange(terminalBlocks.Where(block => block is IMyShipConnector && block.CustomData.Contains("Stop")
        && (block as IMyShipConnector).Status == MyShipConnectorStatus.Connected).Select(block => (block as IMyShipConnector).OtherConnector.CubeGrid).Where(cubeGrid => cubeGrid != Me.CubeGrid));

    for (int i = 0; i < waitTicks; ++i)
        yield return true;

    cargoContainers.Clear();
    GridTerminalSystem.GetBlocksOfType(cargoContainers);

    for (int i = 0; i < waitTicks; ++i)
        yield return true;

    foreach (IMyTerminalBlock block in terminalBlocks)
    {
        if (block is IMyProductionBlock)
        {
            if (block is IMyAssembler && ((IMyAssembler)block).Mode == MyAssemblerMode.Disassembly)
            {
                blockInventory = ((IMyProductionBlock)block).InputInventory;
            }
            else
            {
                blockInventory = ((IMyProductionBlock)block).OutputInventory;
            }
        }
        else
        {
            if (block == null)
                continue;

            blockInventory = block.GetInventory();
        }

        for (int i = 0; i < waitTicks; ++i)
            yield return true;

        if (blockInventory.ItemCount < 1)
            continue;

        inventoryItems.Clear();
        blockInventory.GetItems(inventoryItems);

        for (int i = 0; i < waitTicks; ++i)
            yield return true;

        foreach (MyInventoryItem item in inventoryItems)
        {
            string type = item.Type.TypeId.Split('_')[1];

            foreach (IMyCargoContainer cargo in cargoContainers)
            {
                if (cargo == null || block == null || !cargo.IsWorking || !block.IsWorking)
                    continue;

                if (cargo.CustomData.Contains("Ignore") || block.CustomData.Contains("Ignore"))
                    continue;

                if (ignoreGrids.Contains(cargo.CubeGrid) || ignoreGrids.Contains(block.CubeGrid))
                    continue;

                if (cargo.CustomData.Contains("Manual") && cargo.CustomData.Contains(item.Type.SubtypeId)
                    || block.CustomData.Contains("Manual") && block.CustomData.Contains(item.Type.SubtypeId))
                {
                    if (!cargo.CustomData.Contains(item.Type.SubtypeId) || block.CustomData.Contains(item.Type.SubtypeId) || cargo.Equals(block))
                        continue;
                }
                else
                {
                    if (!cargo.CustomData.Contains(type) || block.CustomData.Contains(type) || cargo.Equals(block))
                        continue;
                }

                cargoInventory = cargo.GetInventory();

                for (int i = 0; i < waitTicks; ++i)
                    yield return true;

                if (cargoInventory == null || blockInventory == null || item == null || cargoInventory.IsFull)
                    continue;

                MyFixedPoint calcAmount = MyFixedPoint.MultiplySafe(cargoInventory.MaxVolume - cargoInventory.CurrentVolume, (1 / item.Type.GetItemInfo().Volume));
                MyFixedPoint amount = MyFixedPoint.Min(calcAmount.ToIntSafe(), item.Amount);

                if (amount == 0 || !CheckValidTransfer(blockInventory, cargoInventory, item, amount))
                    continue;

                for (int i = 0; i < waitTicks; ++i)
                    yield return true;

                if (cargoInventory == null || blockInventory == null || item == null)
                    continue;

                blockInventory.TransferItemTo(cargoInventory, item, amount);

                for (int i = 0; i < waitTicks; ++i)
                    yield return true;
            }
        }
    }

    Echo($"Total Runtime: {(DateTime.Now - startTime).TotalMilliseconds.ToString("#0.00")} ms");
}

bool CheckValidTransfer(IMyInventory from, IMyInventory to, MyInventoryItem item, MyFixedPoint amount)
{
    return from.IsConnectedTo(to) && from.CanTransferItemTo(to, item.Type) && to.CanItemsBeAdded(amount, item.Type);
}