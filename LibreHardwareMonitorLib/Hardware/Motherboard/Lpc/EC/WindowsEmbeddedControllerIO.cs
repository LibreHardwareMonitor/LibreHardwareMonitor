// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Threading;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc.EC
{
    /// <summary>
    /// An unsafe but universal implementation for the ACPI Embedded Controller IO interface for Windows
    /// </summary>
    /// <remarks>
    /// It is unsafe because of possible race condition between this application and the PC firmware when 
    /// writing to the EC registers. For a safe approach ACPI/WMI methods have to be used, but those are
    /// different for each motherboard model.
    /// </remarks> 
    public class WindowsEmbeddedControllerIO : IEmbeddedControllerIO
    {
        public class BusMutexLockingFailedException : ApplicationException
        {
            public BusMutexLockingFailedException()
                : base("Could not lock ISA bus mutex")
            {

            }
        }

        public WindowsEmbeddedControllerIO()
        {
            if (!Ring0.WaitIsaBusMutex(10))
            {
                throw new BusMutexLockingFailedException();
            }
            _disposedValue = false;
        }

        public byte ReadByte(byte register)
        {
            return ReadLoop<byte>(register, ReadByteOp);
        }

        public ushort ReadWordLE(byte register)
        {
            return ReadLoop<ushort>(register, ReadWordLEOp);
        }
        public ushort ReadWordBE(byte register)
        {
            return ReadLoop<ushort>(register, ReadWordBEOp);
        }

        public void WriteByte(byte register, byte value)
        {
            WriteLoop(register, value, WriteByteOp);
        }
        public void WriteWord(byte register, ushort value)
        {
            WriteLoop(register, value, WriteWordOp);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                Ring0.ReleaseIsaBusMutex();
                _disposedValue = true;
            }
        }

        ~WindowsEmbeddedControllerIO()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private delegate bool ReadOp<Param>(byte register, out Param p);

        private Result ReadLoop<Result>(byte register, ReadOp<Result> op) where Result : new()
        {
            Result result = new();

            for (int i = 0; i < MaxRetries; i++)
            {
                if (op(register, out result))
                {
                    return result;
                }
            }

            return result;
        }

        private delegate bool WriteOp<Param>(byte register, Param p);

        private void WriteLoop<Value>(byte register, Value value, WriteOp<Value> op)
        {
            for (int i = 0; i < MaxRetries; i++)
            {
                if (op(register, value))
                {
                    return;
                }
            }
        }

        #region Read/Write ops
        protected bool ReadByteOp(byte register, out byte value)
        {
            if (WaitWrite())
            {
                WriteIOPort(Port.Command, (byte)Command.Read);

                if (WaitWrite())
                {
                    WriteIOPort(Port.Data, register);

                    if (WaitWrite() && WaitRead())
                    {
                        value = ReadIOPort(Port.Data);
                        return true;
                    }
                }
            }

            value = 0;
            return false;
        }

        protected bool WriteByteOp(byte register, byte value)
        {
            if (WaitWrite())
            {
                WriteIOPort(Port.Command, (byte)Command.Write);
                if (WaitWrite())
                {
                    WriteIOPort(Port.Data, register);
                    if (WaitWrite())
                    {
                        WriteIOPort(Port.Data, value);
                        return true;
                    }
                }
            }

            return false;
        }

        protected bool ReadWordLEOp(byte register, out ushort value)
        {
            return ReadWordOp(register, (byte)(register + 1), out value);
        }

        protected bool ReadWordBEOp(byte register, out ushort value)
        {
            return ReadWordOp((byte)(register + 1), register, out value);
        }

        protected bool ReadWordOp(byte registerLSB, byte registerMSB, out ushort value)
        {
            byte result = 0;
            value = 0;

            if (!ReadByteOp(registerLSB, out result))
            {
                return false;
            }

            value = result;

            if (!ReadByteOp(registerMSB, out result))
            {
                return false;
            }

            value |= (ushort)(result << 8);

            return true;
        }

        protected bool WriteWordOp(byte register, ushort value)
        {
            //Byte order: little endia

            byte msb = (byte)(value >> 8);
            byte lsb = (byte)value;

            return WriteByteOp(register, lsb) && WriteByteOp((byte)(register + 1), msb);
        }
        #endregion

        private bool WaitForStatus(Status status, bool isSet)
        {
            for (int i = 0; i < WaitSpins; i++)
            {
                byte value = ReadIOPort(Port.Command);

                if (((byte)status & (!isSet ? value : (byte)~value)) == 0)
                {
                    return true;
                }
                Thread.Sleep(1);
            }

            return false;
        }

        private bool WaitRead()
        {
            if (waitReadFailures > FailuresBeforeSkip)
            {
                return true;
            }
            else if (WaitForStatus(Status.OutputBufferFull, true))
            {
                waitReadFailures = 0;
                return true;
            }
            else
            {
                waitReadFailures++;
                return false;
            }
        }

        private bool WaitWrite()
        {
            return WaitForStatus(Status.InputBufferFull, false);
        }

        private byte ReadIOPort(Port port)
        {
            return Ring0.ReadIoPort((uint)port);
        }

        private void WriteIOPort(Port port, byte datum)
        {
            Ring0.WriteIoPort((uint)port, datum);
        }

        // see the ACPI specification chapter 12
        enum Port : byte
        {
            Command = 0x66,
            Data = 0x62
        }

        enum Command : byte
        {
            Read = 0x80,            // RD_EC
            Write = 0x81,           // WR_EC
            BurstEnable = 0x82,     // BE_EC
            BurstDisable = 0x83,    // BD_EC
            Query = 0x84            // QR_EC
        }

        enum Status : byte
        {
            OutputBufferFull = 0x01,    // EC_OBF
            InputBufferFull = 0x02,     // EC_IBF
            Command = 0x08,             // CMD
            BurstMode = 0x10,           // BURST
            SCIEventPending = 0x20,     // SCI_EVT
            SMIEventPending = 0x40      // SMI_EVT
        }

        // implementation 
        const int WaitSpins = 50;
        const int FailuresBeforeSkip = 20;
        const int MaxRetries = 5;

        int waitReadFailures = 0;
        bool _disposedValue = true;
    }
}
