using DarkRift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVMultiplayer.Darkrift
{
    class BufferQueue
    {
        private List<BufferItem> bufferList = new List<BufferItem>();
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
            foreach(BufferItem item in bufferList.ToList())
            {
                try
                {
                    item.RunAction();
                }
                catch(Exception ex)
                {
                    throw ex;
                }
                bufferList.Remove(item);
            }
        }
    }

    class BufferItem
    {
        Action<Message> bufferAction;
        Action<Message, bool> bufferAction2;
        Message message;
        bool var;

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
            catch(Exception ex)
            {
                throw ex;
            }
        }
    }
}
