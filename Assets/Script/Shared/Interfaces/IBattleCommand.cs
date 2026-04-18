using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Script.Shared.Interfaces
{
    public interface IBattleCommand
    {
        CommandType Type { get; }
        int PlayerId { get; set; }
        bool IsValid(BattleState state);
        void Execute(BattleState state);
        byte[] Serialize();
        void Deserialize(byte[] data);
    }
}
