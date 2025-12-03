using LiteNetLib.Utils;

namespace RealismModSync.QuestExtended.Packets
{
    /// <summary>
    /// Packet for synchronizing Quest Extended optional condition completions
    /// </summary>
    public struct QuestExtendedSyncPacket : INetSerializable
    {
        public string QuestId { get; set; }
        public string ConditionId { get; set; }
        public EQuestSyncType SyncType { get; set; }
        public int CurrentValue { get; set; }
        public bool IsCompleted { get; set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(QuestId ?? string.Empty);
            writer.Put(ConditionId ?? string.Empty);
            writer.Put((byte)SyncType);
            writer.Put(CurrentValue);
            writer.Put(IsCompleted);
        }

        public void Deserialize(NetDataReader reader)
        {
            QuestId = reader.GetString();
            ConditionId = reader.GetString();
            SyncType = (EQuestSyncType)reader.GetByte();
            CurrentValue = reader.GetInt();
            IsCompleted = reader.GetBool();
        }
    }

    public enum EQuestSyncType : byte
    {
        ConditionProgress = 0,
        ConditionCompleted = 1,
        OptionalChoiceMade = 2,
        MultiChoiceQuestStarted = 3
    }
}
