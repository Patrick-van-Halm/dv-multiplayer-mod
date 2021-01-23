using DarkRift;
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
        public uint AmountCars { get; set; }
        public uint LocoInTrainSetIndex { get; set; }
        public Vector3[] CarsPositions { get; set; }
        public Quaternion[] CarsRotation { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            TrainId = e.Reader.ReadString();
            this.Forward = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
            this.Velocity = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
            this.AngularVelocity = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
            this.IsDerailed = e.Reader.ReadBoolean();
            AmountCars = e.Reader.ReadUInt32();
            LocoInTrainSetIndex = e.Reader.ReadUInt32();

            CarsPositions = new Vector3[AmountCars];
            for (int i = 0; i < AmountCars; i++)
            {
                CarsPositions[i] = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
            }
            CarsRotation = new Quaternion[AmountCars];
            for (int i = 0; i < AmountCars; i++)
            {
                CarsRotation[i] = new Quaternion(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
            }
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(TrainId);
            e.Writer.Write(this.Forward.x);
            e.Writer.Write(this.Forward.y);
            e.Writer.Write(this.Forward.z);
            e.Writer.Write(this.Velocity.x);
            e.Writer.Write(this.Velocity.y);
            e.Writer.Write(this.Velocity.z);
            e.Writer.Write(this.AngularVelocity.x);
            e.Writer.Write(this.AngularVelocity.y);
            e.Writer.Write(this.AngularVelocity.z);
            e.Writer.Write(this.IsDerailed);
            e.Writer.Write(AmountCars);
            e.Writer.Write(LocoInTrainSetIndex);
            for(int i = 0; i < AmountCars; i++)
            {
                e.Writer.Write(this.CarsPositions[i].x);
                e.Writer.Write(this.CarsPositions[i].y);
                e.Writer.Write(this.CarsPositions[i].z);
            }
            for (int i = 0; i < AmountCars; i++)
            {
                e.Writer.Write(this.CarsRotation[i].x);
                e.Writer.Write(this.CarsRotation[i].y);
                e.Writer.Write(this.CarsRotation[i].z);
                e.Writer.Write(this.CarsRotation[i].w);
            }
        }
    }
}
