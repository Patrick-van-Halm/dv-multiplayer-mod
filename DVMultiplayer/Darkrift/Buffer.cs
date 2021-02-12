using DarkRift;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DVMultiplayer.Darkrift
{
    internal class BufferQueue
    {
        private readonly List<BufferItem> bufferList = new List<BufferItem>();
        public bool NotSyncedAddToBuffer(bool synced, Action<Message> action, Message message)
        {
            if (!synced)
                bufferList.Add(new BufferItem(action, message));
            return !synced;
        }

        public bool NotSyncedAddToBuffer(bool synced, Action<Message, bool> action, Message message, bool var)
        {
            if (!synced)
                bufferList.Add(new BufferItem(action, message, var));
            return !synced;
        }

        public void RunBuffer()
        {
            foreach (BufferItem item in bufferList.ToList())
            {
                try
                {
                    item.RunAction();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                bufferList.Remove(item);
            }
        }
    }

    internal class BufferItem
    {
        private readonly Action<Message> bufferAction;
        private readonly Action<Message, bool> bufferAction2;
        private readonly Message message;
        private readonly bool var;

        public BufferItem(Action<Message> bufferAction, Message message)
        {
            this.bufferAction = bufferAction;
            this.message = message;
        }

        public BufferItem(Action<Message, bool> bufferAction, Message message, bool var)
        {
            this.bufferAction2 = bufferAction;
            this.message = message;
            this.var = var;
        }

        public void RunAction()
        {
            try
            {
                bufferAction?.Invoke(message);
                bufferAction2?.Invoke(message, var);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
