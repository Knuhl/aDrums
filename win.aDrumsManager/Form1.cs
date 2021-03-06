﻿using aDrumsLib;
using System;
using System.Linq;
using System.Windows.Forms;

namespace aDrum
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.Text = String.Join(Environment.NewLine, Factory.GetPortNames());

        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (var dm = new DrumManager(Factory.GetPortNames()[0]))
            {
                dm.Triggers.ElementAt(2).Threshold = 100;
                dm.SaveSettings();
                dm.LoadSettings();
                MessageBox.Show(dm.Triggers.ElementAt(2).Threshold.ToString());
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var o = AvrUploader.Upload();
            MessageBox.Show(o);
        }
    }
}
