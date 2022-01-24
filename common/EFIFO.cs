using System;
using System.Collections.Generic;
using System.Text;


class EFIFO
{
        public Int32 m_WritePos;
        public Int32 m_ReadPos;
        private byte[] FifoBuf;
        private Int32 FifoBufMax;
        public void Fifo(ref byte[] buf, Int32 bufmax)
        {
            m_WritePos = 0;
            m_ReadPos = 0;
            FifoBufMax = bufmax;
            FifoBuf = buf;
        }
        public bool Fifo_PutCh(byte ch)
        {
            Int32 wrpos, rdpos;
            wrpos = m_WritePos;
            rdpos = m_ReadPos;
            if (wrpos + 1 == rdpos) return false;
            FifoBuf[wrpos++] = ch;
            if (wrpos >= FifoBufMax) wrpos = 0;
            m_WritePos = wrpos;
            return true;
        }
        public bool Fifo_GetCh(ref byte ch)
        {
            Int32 wrpos, rdpos;
            wrpos = m_WritePos;
            rdpos = m_ReadPos;
            if (rdpos == wrpos) return false;
            ch = FifoBuf[rdpos++];
            if (rdpos >= FifoBufMax) rdpos = 0;
            m_ReadPos = rdpos;
            return true;
        }
        public Int32 Fifo_GetSize()
        {
            Int32 r;
            Int32 wrpos, rdpos;
            wrpos = m_WritePos;
            rdpos = m_ReadPos;
            if (wrpos >= rdpos) r = wrpos - rdpos;
            else r = FifoBufMax - rdpos + wrpos;
            return r;
        }
        public Int32 Fifo_GetFreeSize()
        {
            Int32 wrpos, rdpos;
            wrpos = m_WritePos;
            rdpos = m_ReadPos;
            if (rdpos >= wrpos) return (rdpos - wrpos);
            else return (FifoBufMax - wrpos + rdpos);
        }

        public Int32 Fifo_Read(ref byte[] outbuf, Int32 maxlen)
        {
            Int32 i, len, wrpos, rdpos, temp;
            wrpos = m_WritePos;
            rdpos = m_ReadPos;
            if (wrpos >= rdpos) len = wrpos - rdpos;
            else len = FifoBufMax - rdpos + wrpos;
            if (len > maxlen) len = maxlen;
            if ((rdpos + len) <= FifoBufMax)
            {
                for (i = 0; i < len; i++) outbuf[i] = FifoBuf[i + rdpos];
                rdpos += len;
                if (rdpos + len == FifoBufMax) rdpos = 0;
            }
            else
            {
                temp = FifoBufMax - rdpos;
                for (i = 0; i < temp; i++) outbuf[i] = FifoBuf[i + rdpos];
                for (i = 0; i < (len - (FifoBufMax - rdpos)); i++) outbuf[i + temp] = FifoBuf[i];
                rdpos = len - (FifoBufMax - rdpos);
            }
            m_ReadPos = rdpos;
            return len;
        }
        public Int32 Fifo_Write(byte[] inbuf, Int32 maxlen)
        {
            Int32 i, wrpos, rdpos, templen;
            wrpos = m_WritePos;
            rdpos = m_ReadPos;
            if (wrpos >= rdpos) templen = FifoBufMax - wrpos + rdpos;
            else templen = rdpos - wrpos;
            if (maxlen < templen) templen = maxlen;
            if ((wrpos + templen) > FifoBufMax)
            {
                for (i = 0; i < (FifoBufMax - wrpos); i++) FifoBuf[wrpos + i] = inbuf[i];
                for (i = 0; i < (templen - (FifoBufMax - wrpos)); i++) FifoBuf[i] = inbuf[i + FifoBufMax - wrpos];
                wrpos = templen - (FifoBufMax - wrpos);
            }
            else if ((wrpos + templen) == FifoBufMax)
            {
                for (i = 0; i < templen; i++) FifoBuf[i + wrpos] = inbuf[i];
                wrpos = 0;
            }
            else
            {
                for (i = 0; i < templen; i++) FifoBuf[i + wrpos] = inbuf[i];
                wrpos += templen;
            }
            m_WritePos = wrpos;
            return templen;
        }
    }

