using DarkRift;
using DV.Logic.Job;
using DVMultiplayer.Darkrift;
using UnityEngine;

namespace DVMultiplayer.DTO.Train
{
    public class WorldTrain : IDarkRiftSerializable
    {
        // Positioning and Physics
        public string Guid { get; set; }
        public string Id { get; set; } = "";
        public TrainCarType CarType { get; set; } = TrainCarType.NotSet;
        public bool IsLoco { get; set; }
        public bool IsRemoved { get; set; } = false;

        // Player
        public bool IsPlayerSpawned { get; set; }
        public ushort AuthorityPlayerId { get; set; } = 0;

        // Position and physics
        public Vector3 Position { get; set; }
        public Vector3 Forward { get; set; }
        public Quaternion Rotation { get; set; }
        public bool IsStationary { get; set; }
        public TrainBogie[] Bogies { get; set; }

        // Couplers
        public bool IsFrontCouplerCoupled { get; set; } = false;
        public bool IsRearCouplerCoupled { get; set; } = false;
        public bool IsFrontCouplerCockOpen { get; set; } = false;
        public bool IsRearCouplerCockOpen { get; set; } = false;
        public bool IsFrontCouplerHoseConnected { get; set; } = false;
        public bool IsRearCouplerHoseConnected { get; set; } = false;

        // Damage
        public float CarHealth { get; set; }
        public string CarHealthData { get; set; } = "";

        // Locomotive (Only set if item is locomotive)
        public float Throttle { get; set; } = 0;
        public float Brake { get; set; } = 0;
        public float IndepBrake { get; set; } = 0;
        public float Sander { get; set; } = 0;
        public float Reverser { get; set; } = 0;

        // Specific Train states
        public Shunter Shunter { get; set; } = new Shunter();
        public MultipleUnit MultipleUnit { get; set; } = new MultipleUnit();

        // Cargo based trains
        public float CargoAmount { get; set; }
        public CargoType CargoType { get; set; } = CargoType.None;
        public float CargoHealth { get; set; }

        //Data specific
        public long updatedAt { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            Guid = e.Reader.ReadString();
            Id = e.Reader.ReadString();
            CarType = (TrainCarType)e.Reader.ReadUInt32();
            IsLoco = e.Reader.ReadBoolean();
            IsRemoved = e.Reader.ReadBoolean();

            Position = e.Reader.ReadVector3();
            Forward = e.Reader.ReadVector3();
            Rotation = e.Reader.ReadQuaternion();
            IsStationary = e.Reader.ReadBoolean();
            Bogies = e.Reader.ReadSerializables<TrainBogie>();

            CarHealth = e.Reader.ReadSingle();
            CarHealthData = e.Reader.ReadString();

            IsFrontCouplerCoupled = e.Reader.ReadBoolean();
            IsRearCouplerCoupled = e.Reader.ReadBoolean();
            IsFrontCouplerCockOpen = e.Reader.ReadBoolean();
            IsRearCouplerCockOpen = e.Reader.ReadBoolean();
            IsFrontCouplerHoseConnected = e.Reader.ReadBoolean();
            IsRearCouplerHoseConnected = e.Reader.ReadBoolean();

            IsPlayerSpawned = e.Reader.ReadBoolean();
            AuthorityPlayerId = e.Reader.ReadUInt16();

            if (IsLoco)
            {
                Throttle = e.Reader.ReadSingle();
                Brake = e.Reader.ReadSingle();
                IndepBrake = e.Reader.ReadSingle();
                Sander = e.Reader.ReadSingle();
                Reverser = e.Reader.ReadSingle();
            }
            else
            {
                CargoType = (CargoType)e.Reader.ReadUInt32();
                CargoAmount = e.Reader.ReadSingle();
                CargoHealth = e.Reader.ReadSingle();
            }

            switch (CarType)
            {
                case TrainCarType.LocoShunter:
                    Shunter = e.Reader.ReadSerializable<Shunter>();
                    MultipleUnit = e.Reader.ReadSerializable<MultipleUnit>();
                    break;
            }

            updatedAt = e.Reader.ReadInt64();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Guid);
            e.Writer.Write(Id);
            e.Writer.Write((uint)CarType);
            e.Writer.Write(IsLoco);
            e.Writer.Write(IsRemoved);

            e.Writer.Write(Position);
            e.Writer.Write(Forward);
            e.Writer.Write(Rotation);
            e.Writer.Write(IsStationary);
            e.Writer.Write(Bogies);

            e.Writer.Write(CarHealth);
            e.Writer.Write(CarHealthData);

            e.Writer.Write(IsFrontCouplerCoupled);
            e.Writer.Write(IsRearCouplerCoupled);
            e.Writer.Write(IsFrontCouplerCockOpen);
            e.Writer.Write(IsRearCouplerCockOpen);
            e.Writer.Write(IsFrontCouplerHoseConnected);
            e.Writer.Write(IsRearCouplerHoseConnected);

            e.Writer.Write(IsPlayerSpawned);
            e.Writer.Write(AuthorityPlayerId);

            if (IsLoco)
            {
                e.Writer.Write(Throttle);
                e.Writer.Write(Brake);
                e.Writer.Write(IndepBrake);
                e.Writer.Write(Sander);
                e.Writer.Write(Reverser);
            }
            else
            {
                e.Writer.Write((uint)CargoType);
                e.Writer.Write(CargoAmount);
                e.Writer.Write(CargoHealth);
            }

            switch (CarType)
            {
                case TrainCarType.LocoShunter:
                    e.Writer.Write(Shunter);
                    e.Writer.Write(MultipleUnit);
                    break;
            }
            e.Writer.Write(updatedAt);
        }
    }
}
