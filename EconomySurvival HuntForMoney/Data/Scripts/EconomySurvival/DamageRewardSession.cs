using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace EconomySurvival
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    class DamageRewardSession : MySessionComponentBase
    {
        List<IMyPlayer> Players = new List<IMyPlayer>();

        public override void LoadData()
        {
            if (!MyAPIGateway.Session.IsServer) return;

            MyVisualScriptLogicProvider.BlockDamaged += OnBlockDamaged;

            MyLog.Default.WriteLine("EconomySurvival.DamageRewardSession: loaded...");
        }

        private void OnBlockDamaged(string entityName, string gridName, string typeId, string subtypeId, float damage, string damageType, long attackerId)
        {
            try
            {
                if (entityName.Length == 0) return;

                var entity = MyAPIGateway.Entities.GetEntityByName(entityName);

                if (entity == null) return;

                var cubeGrid = (entity as IMyCubeBlock).CubeGrid;

                if (cubeGrid == null) return;

                Players.Clear();
                MyAPIGateway.Multiplayer.Players.GetPlayers(Players);

                IMyPlayer player = Players.SingleOrDefault(_player => _player.Character != null && _player.Character.EntityId == attackerId);

                var attackerIdentityId = (player == null) ? GetAttackerIdentityId(attackerId) : player.IdentityId;
                var victimIdentityId = (cubeGrid.BigOwners != null && cubeGrid.BigOwners.Count > 0) ? cubeGrid.BigOwners[0] : 0L;
                var relation = MyIDModule.GetRelationPlayerPlayer(attackerIdentityId, victimIdentityId);

                player = Players.SingleOrDefault(_player => _player?.IdentityId == attackerIdentityId);

                if (player == null || relation != MyRelationsBetweenPlayers.Enemies) return;

                player.RequestChangeBalance((long)damage);
            }
            catch (Exception _exception)
            {
                MyLog.Default.WriteLine($"EconomySurvival.DamageReward: {_exception}");
            }
        }

        private long GetAttackerIdentityId(long attackerId)
        {
            var entity = MyAPIGateway.Entities.GetEntityById(attackerId);
            var myControllableEntity = entity as VRage.Game.ModAPI.Interfaces.IMyControllableEntity;
            var cubeGrid = entity as IMyCubeGrid;

            if (cubeGrid != null) return (cubeGrid.BigOwners.Count > 0) ? cubeGrid.BigOwners[0] : 0;

            if (myControllableEntity != null)
            {
                var controllerInfo = myControllableEntity.ControllerInfo;

                if (controllerInfo != null) return controllerInfo.ControllingIdentityId;
            }
            else
            {
                IMyGunBaseUser myGunBaseUser;
                if ((myGunBaseUser = (entity as IMyGunBaseUser)) != null) return myGunBaseUser.OwnerId;

                IMyHandheldGunObject<MyDeviceBase> myHandheldGunObject;
                if ((myHandheldGunObject = (entity as IMyHandheldGunObject<MyDeviceBase>)) != null) return myHandheldGunObject.OwnerIdentityId;
            }

            return 0L;
        }

        protected override void UnloadData()
        {
            MyVisualScriptLogicProvider.BlockDamaged -= OnBlockDamaged;
        }
    }
}
