using RakDotNet;
using Uchu.Core;

namespace Uchu.World
{
    public class RestoreToPostLoadStatsMessage : ServerGameMessage
    {
        public override ushort GameMessageId => 0x05bc;

        public override void Deserialize(BitStream stream)
        {
            
        }
    }
}