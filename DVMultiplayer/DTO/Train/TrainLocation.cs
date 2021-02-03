using DarkRift;
using DVMultiplayer.Darkrift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DVMultiplayer.DTO.Train
{
    public class TrainLocation : IDarkRiftSerializable
    {
        public string TrainId { get; set; }
        public Vector3 Forward { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 AngularVelocity { get; set; }
        public bool IsDerailed { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        //public uint AmountCars { get; set; }
        //public uint LocoInTrainSetIndex { get; set; }
        //public Vector3[] CarsPositions { get; set; }
        //public Quaternion[] CarsRotation { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            TrainId = e.Reader.ReadString();
            Forward = e.Reader.ReadVector3();
            Velocity = e.Reader.ReadVector3();
            AngularVelocity = e.Reader.ReadVector3();
            IsDerailed = e.Reader.ReadBoolean();
            Position = e.Reader.ReadVector3();
            Rotation = e.Reader.ReadQuaternion();
            //AmountCars = e.Reader.ReadUInt32();
            //LocoInTrainSetIndex = e.Reader.ReadUInt32();

            //CarsPositions = new Vector3[AmountCars];
            //for (int i = 0; i < AmountCars; i++)
            //{
            //    CarsPositions[i] = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
            //}
            //CarsRotation = new Quaternion[AmountCars];
            //for (int i = 0; i < AmountCars; i++)
            //{
            //    CarsRotation[i] = new Quaternion(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
            //}
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(TrainId);
            e.Writer.Write(Forward);
            e.Writer.Write(Velocity);
            e.Writer.Write(AngularVelocity);
            e.Writer.Write(IsDerailed);

            e.Writer.Write(Position);

            e.Writer.Write(Rotation);
            //e.Writer.Write(AmountCars);
            //e.Writer.Write(LocoInTrainSetIndex);
            //for(int i = 0; i < AmountCars; i++)
            //{
            //    e.Writer.Write(this.CarsPositions[i].x);
            //    e.Writer.Write(this.CarsPositions[i].y);
            //    e.Writer.Write(this.CarsPositions[i].z);
            //}
            //for (int i = 0; i < AmountCars; i++)
            //{
            //    e.Writer.Write(this.CarsRotation[i].x);
            //    e.Writer.Write(this.CarsRotation[i].y);
            //    e.Writer.Write(this.CarsRotation[i].z);
            //    e.Writer.Write(this.CarsRotation[i].w);
            //}
        }
    }
}
