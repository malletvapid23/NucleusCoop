using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nucleus.Gaming.Forms
{
    public partial class CustomPrompt : Form
    {
        private int index;

        public CustomPrompt(string message, string prevAnswer, int i)
        {
            InitializeComponent();
            lbl_Desc.Text = message;

            index = i;

            TopMost = true;
            TopMost = false;
            TopMost = true;

            WindowScrape.Static.HwndInterface.MakeTopMost(Handle);
        }

        private void btn_Ok_Click(object sender, EventArgs e)
        {
            GenericGameHandler.customValue[index] = txt_UserInput.Text;
            Close();
        }
    }
}
