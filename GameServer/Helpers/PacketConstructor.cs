﻿using System;
using System.Collections.Generic;
namespace GameServer
{
   
    public unsafe class PacketHelper
    {
        public static byte[] StringPayload(params string[] Messages)
        {
            List<byte> Payload = new List<byte>();

            for (int i = 0; i < Messages.Length; i++)
            {
                string Message = Messages[i];

                Payload.Add((byte)Message.Length);
                for (int j = 0; j < Message.Length; j++)
                {
                    Payload.Add((byte)Message[j]);
                }
            }
            return Payload.ToArray();
        }

        public static AttackTargetPacket* AttackPacket(params AttackTarget[] Targets)
        {
            int Size = 28 + (Targets.Length * 12);

            AttackTargetPacket* Packet = (AttackTargetPacket*)Memory.Alloc(Size);
            Packet->Size = (ushort)Size;
            Packet->Type = 0x451;
            Packet->Amount = (uint)Targets.Length;

            fixed (AttackTarget* target = Targets)
            {
                Memory.Copy(target, Packet->Targets, 12 * Targets.Length);
            }
            return Packet;
        }

        public static NpcDialog* NpcPacket(string Input = "")
        {
            int Size = 16 + Input.Length;
            NpcDialog* Packet = (NpcDialog*)Memory.Alloc(Size);
            Packet->Size = (ushort)Size;
            Packet->Type = 0x7F0;
            Packet->Timer = (uint)Environment.TickCount;
            Packet->OptionID = 0xFF;
            Packet->DontDisplay = true;
            if (!string.IsNullOrEmpty(Input))
            {
                byte[] Payload = StringPayload(Input);
                fixed (byte* pPayload = Payload)
                {
                    Memory.Copy(pPayload, Packet->Input, Payload.Length);
                }
            }
            return Packet;
        }

        public static StatusUpdate* UpdatePacket(params StatusUpdateEntry[] entries)
        {
            int Size = 20 + (entries.Length * 8);
            StatusUpdate* Packet = (StatusUpdate*)Memory.Alloc(Size);
            Packet->Size = (ushort)Size;
            Packet->Type = 0x3F9;
            Packet->Amount = (uint)entries.Length;
            fixed (StatusUpdateEntry* entry = entries)
            {
                Memory.Copy(entry, Packet->Data, 8 * entries.Length);
            }
            return Packet;
        }

        public static GeneralData* ReAllocGeneral(void *Block)
        {
            int size = sizeof(GeneralData);
            Block = Memory.ReAlloc(Block, size);

            GeneralData* Packet = (GeneralData*)Block;
            Packet->Size = (ushort)size;
            Packet->Type = 0x3F2;
            Packet->Timer = (uint)Environment.TickCount;
            return Packet;
        }
        public static GeneralData* AllocGeneral()
        {
            int size = sizeof(GeneralData);
            GeneralData* Packet = (GeneralData*)Memory.Alloc(size);
            Packet->Size = (ushort)size;
            Packet->Type = 0x3F2;
            Packet->Timer = (uint)Environment.TickCount;
            return Packet;
        }
        public static EntitySpawn EntitySpawn(Entity Entity)
        {
            byte[] Payload = StringPayload(Entity.Name);

            int Size = 57 + Payload.Length;
            EntitySpawn Packet = new EntitySpawn();
            Packet.Size = (ushort)Size;
            Packet.Type = 0x3F6;
            Packet.UID = Entity.UID;
            Packet.Mesh = Entity.Model;
            Packet.Status = Entity.Status;
            Packet.GuildID = 0;
            Packet.GuildRank = 0;
            Packet.Items = new EntityItems();

            ConquerItem Item;
            if (Entity.Owner.TryGetEquipment(ItemPosition.Headgear, out Item))
                Packet.Items.Helmet = Item.ID;
            if (Entity.Owner.TryGetEquipment(ItemPosition.Armor, out Item))
                Packet.Items.Armor = Item.ID;
            if (Entity.Owner.TryGetEquipment(ItemPosition.Left, out Item))
                Packet.Items.LeftHand = Item.ID;
            if (Entity.Owner.TryGetEquipment(ItemPosition.Right, out Item))
                Packet.Items.RightHand = Item.ID;
           
            Packet.X = Entity.Location.X;
            Packet.Y = Entity.Location.Y;
            Packet.HairStyle = Entity.HairStyle;
            Packet.Angle = Entity.Angle;
            Packet.Action = Entity.Action;
            Packet.ShowNames = true;

            fixed (byte* pPayload = Payload)
            {
                Memory.Copy(pPayload, Packet.Name, Payload.Length);
            }
            return Packet;
        }

        public static CharacterInformation *CreateInformation(GameClient Client)
        {
            
            byte[] Payload = StringPayload(Client.Entity.Name, Client.Entity.Spouse);

            int Size = 66 + Payload.Length;
            CharacterInformation* Packet = (CharacterInformation*)Memory.Alloc(Size);
            
            Packet->Size = (ushort)Size;
            Packet->Type = 0x3EE;

            Packet->ID = Client.Entity.UID;
            Packet->Model = Client.Entity.Model;
            Packet->HairStyle = Client.Entity.HairStyle;
            Packet->Gold = Client.Entity.Money;
            Packet->Experience = Client.Entity.Experience;
            Packet->StatPoints = Client.Entity.StatusPoints.Free;
            Packet->Strength = Client.Entity.StatusPoints.Strength;
            Packet->Dexterity = Client.Entity.StatusPoints.Dexterity;
            Packet->Vitality = Client.Entity.StatusPoints.Vitality;
            Packet->Spirit = Client.Entity.StatusPoints.Spirit;
            Packet->HitPoints = Client.Entity.HitPoints;
            Packet->ManaPoints = Client.Entity.ManaPoints;
            Packet->PKPoints = Client.Entity.PKPoints;
            Packet->Level = Client.Entity.Level;
            Packet->Class = Client.Entity.Class;
            Packet->Reborn = Client.Entity.Reborn;
            Packet->DisplayName = true;
            Packet->NameCount = 2;

            fixed (byte* pPayload = Payload)
            {
                Memory.Copy(pPayload, Packet->Names, Payload.Length);
            }    
            return Packet;
        }

        public static Chat* CreateChat(string From, string To, string Message)
        {
            int Size = 24 + From.Length + To.Length + Message.Length;
            Chat* Packet = (Chat*)Memory.Alloc(Size+1);
            Packet->Size = (ushort)Size;
            Packet->Type = 0x3EC;
            Packet->Count = 4;

            byte[] Payload = StringPayload(From, "", To, Message);

            fixed (byte* pPayload = Payload)
            {
                Memory.Copy(pPayload, Packet->Data, Payload.Length);
            }
            return Packet;
        }
        public static string[] ParseChat(Chat* Packet)
        {
            List<string> Parameters = new List<string>();
            for (int i = 0, Index = 0; i < Packet->Count; i++)
            {
                string Parameter = new string(Packet->Data, Index + 1, Packet->Data[Index]);
                Index = Index + (Parameter.Length + 1);
                Parameters.Add(Parameter);
            }
            return Parameters.ToArray();
        }
    }
}
