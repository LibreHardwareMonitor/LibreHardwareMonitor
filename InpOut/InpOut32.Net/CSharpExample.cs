using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System.Runtime.InteropServices;

namespace InpOut32.Net
{
    public partial class CSharpExample : Form
    {
        
        [DllImport("inpout32.dll")]
        private static extern UInt32 IsInpOutDriverOpen();
        [DllImport("inpout32.dll")]
        private static extern void Out32(short PortAddress, short Data);
        [DllImport("inpout32.dll")]
        private static extern char Inp32(short PortAddress);

        [DllImport("inpout32.dll")]
        private static extern void DlPortWritePortUshort(short PortAddress, ushort Data);
        [DllImport("inpout32.dll")]
        private static extern ushort DlPortReadPortUshort(short PortAddress);

        [DllImport("inpout32.dll")]
        private static extern void DlPortWritePortUlong(int PortAddress, uint Data);
        [DllImport("inpout32.dll")]
        private static extern uint DlPortReadPortUlong(int PortAddress);

        [DllImport("inpoutx64.dll")]
        private static extern bool GetPhysLong(ref int PortAddress, ref uint Data);
        [DllImport("inpoutx64.dll")]
        private static extern bool SetPhysLong(ref int PortAddress, ref uint Data);


        [DllImport("inpoutx64.dll", EntryPoint="IsInpOutDriverOpen")]
        private static extern UInt32 IsInpOutDriverOpen_x64();
        [DllImport("inpoutx64.dll", EntryPoint = "Out32")]
        private static extern void Out32_x64(short PortAddress, short Data);
        [DllImport("inpoutx64.dll", EntryPoint = "Inp32")]
        private static extern char Inp32_x64(short PortAddress);

        [DllImport("inpoutx64.dll", EntryPoint = "DlPortWritePortUshort")]
        private static extern void DlPortWritePortUshort_x64(short PortAddress, ushort Data);
        [DllImport("inpoutx64.dll", EntryPoint = "DlPortReadPortUshort")]
        private static extern ushort DlPortReadPortUshort_x64(short PortAddress);

        [DllImport("inpoutx64.dll", EntryPoint = "DlPortWritePortUlong")]
        private static extern void DlPortWritePortUlong_x64(int PortAddress, uint Data);
        [DllImport("inpoutx64.dll", EntryPoint = "DlPortReadPortUlong")]
        private static extern uint DlPortReadPortUlong_x64(int PortAddress);

        [DllImport("inpoutx64.dll", EntryPoint = "GetPhysLong")]
        private static extern bool GetPhysLong_x64(ref int PortAddress, ref uint Data);
        [DllImport("inpoutx64.dll", EntryPoint = "SetPhysLong")]
        private static extern bool SetPhysLong_x64(ref int PortAddress, ref uint Data);


        bool m_bX64 = false;

        public CSharpExample()
        {
            InitializeComponent();
            try
            {
                uint nResult = 0;
                try
                {
                    nResult = IsInpOutDriverOpen();
                }
                catch (BadImageFormatException)
                {
                    nResult = IsInpOutDriverOpen_x64();
                    if (nResult != 0)
                        m_bX64 = true;

                }

                if (nResult == 0)
                {
                    lblMessage.Text = "Unable to open InpOut32 driver";
                    button1.Enabled = false;
                    button2.Enabled = false;
                    button3.Enabled = false;
                    button4.Enabled = false;
                    button5.Enabled = false;
                    button6.Enabled = false;
                    button7.Enabled = false;
                }
            }
            catch (DllNotFoundException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                lblMessage.Text = "Unable to find InpOut32.dll";
                button1.Enabled = false;
                button2.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = false;
                button5.Enabled = false;
                button6.Enabled = false;
                button7.Enabled = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                short iPort = Convert.ToInt16(textBox1.Text);

                char c;
                if (m_bX64)
                    c = Inp32_x64(iPort);
                else
                    c = Inp32(iPort);
                
                textBox2.Text = Convert.ToInt32(c).ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occured:\n" + ex.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                short iPort = Convert.ToInt16(textBox1.Text);
                short iData = Convert.ToInt16(textBox2.Text);
                textBox2.Text = "";
                if (m_bX64)
                    Out32_x64(iPort, iData);
                else
                    Out32(iPort, iData);

                
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occured:\n" + ex.Message);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                short iPort = Convert.ToInt16(textBox1.Text);
                ushort s;
                if (m_bX64)
                    s = DlPortReadPortUshort_x64(iPort);
                else
                    s = DlPortReadPortUshort(iPort);

                textBox2.Text = Convert.ToUInt16(s).ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occured:\n" + ex.Message);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                int nPort = Convert.ToInt32(textBox1.Text);

                uint l;
                if (m_bX64)
                    l = DlPortReadPortUlong_x64(nPort);
                else
                    l = DlPortReadPortUlong(nPort);

                textBox2.Text = l.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occured:\n" + ex.Message);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                short sPort = Convert.ToInt16(textBox1.Text);
                ushort iData = Convert.ToUInt16(textBox2.Text);
                textBox2.Text = "";

                if (m_bX64)
                    DlPortWritePortUshort_x64(sPort, iData);
                else
                    DlPortWritePortUshort(sPort, iData);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occured:\n" + ex.Message);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                int nPort = Convert.ToInt32(textBox1.Text);
                uint nData = Convert.ToUInt32(textBox2.Text);
                textBox2.Text = "";
                if (m_bX64)
                    DlPortWritePortUlong_x64(nPort, nData);
                else
                    DlPortWritePortUlong(nPort, nData);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occured:\n" + ex.Message);
            }
        }

        private void Beep(uint freq)
        {
            if (m_bX64)
            {
                Out32_x64(0x43, 0xB6);
                Out32_x64(0x42, (byte)(freq & 0xFF));
                Out32_x64(0x42, (byte)(freq >> 9));
                System.Threading.Thread.Sleep(10);
                Out32_x64(0x61, (byte)(Convert.ToByte(Inp32_x64(0x61)) | 0x03));
            }
            else
            {
                Out32(0x43, 0xB6);
                Out32(0x42, (byte)(freq & 0xFF));
                Out32(0x42, (byte)(freq >> 9));
                System.Threading.Thread.Sleep(10);
                Out32(0x61, (byte)(Convert.ToByte(Inp32(0x61)) | 0x03));
               }
        }

        private void StopBeep()
        {
            if (m_bX64)
                Out32_x64(0x61, (byte)(Convert.ToByte(Inp32_x64(0x61)) & 0xFC));
            else
                Out32(0x61, (byte)(Convert.ToByte(Inp32(0x61)) & 0xFC));
        }

        private void CSharpExample_Load(object sender, EventArgs e)
        {
            button7_Click(this, null);
        }


        private void ThreadBeeper()
        {
            for (uint i = 440000; i < 500000; i += 1000)
            {
                uint freq = 1193180000 / i; // 440Hz
                Beep(freq);
            }
            StopBeep();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(ThreadBeeper));
            t.Start();
        }
    }
}