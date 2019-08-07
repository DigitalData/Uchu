using RakDotNet;
using RakDotNet.IO;
using Uchu.Core;

namespace Uchu.World
{
    public class SetMissionTypeStateMessage : ServerGameMessage
    {
        public override ushort GameMessageId => 0x353;

        public MissionLockState LockState { get; set; } = MissionLockState.New;
        
        public string SubType { get; set; }
        
        public string Type { get; set; }

        public override void SerializeMessage(BitWriter writer)
        {
            var hasState = LockState != MissionLockState.New;

            writer.WriteBit(hasState);

            if (hasState) writer.Write((int) LockState);

            writer.Write((uint) SubType.Length);
            writer.WriteString(SubType, SubType.Length);
            
            writer.Write((uint) Type.Length);
            writer.WriteString(Type, Type.Length);
        }
    }
}