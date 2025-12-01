using LiteNetLib.Utils;

namespace RealismModSync.Health.Packets
{
    /// <summary>
    /// Packet for synchronizing Realism medical item usage across FIKA clients
    /// Handles charges, healing amounts, and custom effects
    /// </summary>
    public struct RealismMedicalSyncPacket : INetSerializable
    {
        public int NetId;
        public EMedicalSyncType SyncType;
        public MedicalSyncData Data;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            SyncType = (EMedicalSyncType)reader.GetByte();

            switch (SyncType)
            {
                case EMedicalSyncType.UseMedItem:
                    Data.UseMedItem = new UseMedItemData
                    {
                        ItemId = reader.GetString(),
                        BodyPart = reader.GetByte(),
                        HpResource = reader.GetFloat(),
                        Amount = reader.GetFloat()
                    };
                    break;

                case EMedicalSyncType.ApplyCustomEffect:
                    Data.ApplyCustomEffect = new ApplyCustomEffectData
                    {
                        EffectType = reader.GetString(),
                        BodyPart = reader.GetByte(),
                        Duration = reader.GetFloat(),
                        Strength = reader.GetFloat(),
                        Delay = reader.GetInt()
                    };
                    break;

                case EMedicalSyncType.RemoveCustomEffect:
                    Data.RemoveCustomEffect = new RemoveCustomEffectData
                    {
                        EffectType = reader.GetString(),
                        BodyPart = reader.GetByte()
                    };
                    break;

                case EMedicalSyncType.UpdateMedCharges:
                    Data.UpdateMedCharges = new UpdateMedChargesData
                    {
                        ItemId = reader.GetString(),
                        NewCharges = reader.GetFloat()
                    };
                    break;

                case EMedicalSyncType.TourniquetApplied:
                    Data.TourniquetApplied = new TourniquetAppliedData
                    {
                        BodyPart = reader.GetByte(),
                        DamageRate = reader.GetFloat(),
                        Delay = reader.GetInt()
                    };
                    break;

                case EMedicalSyncType.SurgeryEffect:
                    Data.SurgeryEffect = new SurgeryEffectData
                    {
                        BodyPart = reader.GetByte(),
                        TickRate = reader.GetFloat(),
                        RegenLimitFactor = reader.GetFloat(),
                        Delay = reader.GetInt()
                    };
                    break;
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.Put((byte)SyncType);

            switch (SyncType)
            {
                case EMedicalSyncType.UseMedItem:
                    writer.Put(Data.UseMedItem.ItemId);
                    writer.Put(Data.UseMedItem.BodyPart);
                    writer.Put(Data.UseMedItem.HpResource);
                    writer.Put(Data.UseMedItem.Amount);
                    break;

                case EMedicalSyncType.ApplyCustomEffect:
                    writer.Put(Data.ApplyCustomEffect.EffectType);
                    writer.Put(Data.ApplyCustomEffect.BodyPart);
                    writer.Put(Data.ApplyCustomEffect.Duration);
                    writer.Put(Data.ApplyCustomEffect.Strength);
                    writer.Put(Data.ApplyCustomEffect.Delay);
                    break;

                case EMedicalSyncType.RemoveCustomEffect:
                    writer.Put(Data.RemoveCustomEffect.EffectType);
                    writer.Put(Data.RemoveCustomEffect.BodyPart);
                    break;

                case EMedicalSyncType.UpdateMedCharges:
                    writer.Put(Data.UpdateMedCharges.ItemId);
                    writer.Put(Data.UpdateMedCharges.NewCharges);
                    break;

                case EMedicalSyncType.TourniquetApplied:
                    writer.Put(Data.TourniquetApplied.BodyPart);
                    writer.Put(Data.TourniquetApplied.DamageRate);
                    writer.Put(Data.TourniquetApplied.Delay);
                    break;

                case EMedicalSyncType.SurgeryEffect:
                    writer.Put(Data.SurgeryEffect.BodyPart);
                    writer.Put(Data.SurgeryEffect.TickRate);
                    writer.Put(Data.SurgeryEffect.RegenLimitFactor);
                    writer.Put(Data.SurgeryEffect.Delay);
                    break;
            }
        }

        public enum EMedicalSyncType : byte
        {
            UseMedItem,
            ApplyCustomEffect,
            RemoveCustomEffect,
            UpdateMedCharges,
            TourniquetApplied,
            SurgeryEffect
        }

        public struct MedicalSyncData
        {
            public UseMedItemData UseMedItem;
            public ApplyCustomEffectData ApplyCustomEffect;
            public RemoveCustomEffectData RemoveCustomEffect;
            public UpdateMedChargesData UpdateMedCharges;
            public TourniquetAppliedData TourniquetApplied;
            public SurgeryEffectData SurgeryEffect;
        }

        public struct UseMedItemData
        {
            public string ItemId;
            public byte BodyPart;
            public float HpResource;
            public float Amount;
        }

        public struct ApplyCustomEffectData
        {
            public string EffectType;
            public byte BodyPart;
            public float Duration;
            public float Strength;
            public int Delay;
        }

        public struct RemoveCustomEffectData
        {
            public string EffectType;
            public byte BodyPart;
        }

        public struct UpdateMedChargesData
        {
            public string ItemId;
            public float NewCharges;
        }

        public struct TourniquetAppliedData
        {
            public byte BodyPart;
            public float DamageRate;
            public int Delay;
        }

        public struct SurgeryEffectData
        {
            public byte BodyPart;
            public float TickRate;
            public float RegenLimitFactor;
            public int Delay;
        }
    }
}
