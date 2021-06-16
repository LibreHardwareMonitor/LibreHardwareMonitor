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
        private const int FailuresBeforeSkip = 20;
        private const int MaxRetries = 5;

        // implementation 
        private const int WaitSpins = 50;
        private bool _disposed;

        private int _waitReadFailures;

        public WindowsEmbeddedControllerIO()
        {
            if (!Ring0.WaitIsaBusMutex(10))
            {
                throw new BusMutexLockingFailedException();
            }
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

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Ring0.ReleaseIsaBusMutex();
            }
        }

        private TResult ReadLoop<TResult>(byte register, ReadOp<TResult> op) where TResult : new()
        {
            TResult result = new();

            for (int i = 0; i < MaxRetries; i++)
            {
                if (op(register, out result))
                {
                    return result;
                }
            }

            return result;
        }

        private void WriteLoop<TValue>(byte register, TValue value, WriteOp<TValue> op)
        {
            for (int i = 0; i < MaxRetries; i++)
            {
                if (op(register, value))
                {
                    return;
                }
            }
        }

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
            if (_waitReadFailures > FailuresBeforeSkip)
            {
                return true;
            }

            if (WaitForStatus(Status.OutputBufferFull, true))
            {
                _waitReadFailures = 0;
                return true;
            }

            _waitReadFailures++;
            return false;
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

        public class BusMutexLockingFailedException : ApplicationException
        {
            public BusMutexLockingFailedException()
                : base("Could not lock ISA bus mutex")
            { }
        }

        private delegate bool ReadOp<TParam>(byte register, out TParam p);

        private delegate bool WriteOp<in TParam>(byte register, TParam p);

        // see the ACPI specification chapter 12
        private enum Port : byte
        {
            Command = 0x66,
            Data = 0x62
        }

        private enum Command : byte
        {
            Read = 0x80, // RD_EC
            Write = 0x81, // WR_EC
            BurstEnable = 0x82, // BE_EC
            BurstDisable = 0x83, // BD_EC
            Query = 0x84 // QR_EC
        }

        private enum Status : byte
        {
            OutputBufferFull = 0x01, // EC_OBF
            InputBufferFull = 0x02, // EC_IBF
            Command = 0x08, // CMD
            BurstMode = 0x10, // BURST
            SciEventPending = 0x20, // SCI_EVT
            SmiEventPending = 0x40 // SMI_EVT
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

        protected bool ReadWordOp(byte registerLsb, byte registerMsb, out ushort value)
        {
            value = 0;

            if (!ReadByteOp(registerLsb, out byte result))
            {
                return false;
            }

            value = result;

            if (!ReadByteOp(registerMsb, out result))
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
    }
}
