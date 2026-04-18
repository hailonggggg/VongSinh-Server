using Assets.Script.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Script.Shared.Commands
{
    public abstract class BattleCommand : IBattleCommand
    {
        public abstract CommandType Type { get; }
        public int PlayerId { get; set; }

        public abstract void Execute(BattleState state);
        public abstract bool IsValid(BattleState state);

        public byte[] Serialize()
        {
            return Encoding.UTF8.GetBytes(JsonUtility.ToJson(this));
        }
        public void Deserialize(byte[] data)
        {
            JsonUtility.FromJsonOverwrite(Encoding.UTF8.GetString(data), this);
        }
    }
}
