using System;
using System.Drawing;
using System.Windows.Forms;

namespace HotCornerBarrier
{
    public partial class ChangeBarrierHeightForm : Form
    {
        private Program ParentProgram { get; set; }

        public ChangeBarrierHeightForm(Program parent)
        {
            ParentProgram = parent;
            InitializeComponent();
            barrierInput.Text = parent.HotCornerClipLength.ToString();

            AcceptButton = okButton;
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void okButton_Click(object sender, EventArgs args)
        {
            int newLength;
            if (Int32.TryParse(barrierInput.Text, out newLength))
            {
                if (newLength <= 0)
                {
                    barrierInput.BackColor = Color.IndianRed;
                    errorLabel.Text = "Height must be greater than zero";
                    return;
                }
                var minHeight = ParentProgram.MinDimensions.Height;
                if (newLength >= minHeight)
                {
                    barrierInput.BackColor = Color.IndianRed;
                    errorLabel.Text = "Height must be less than " + minHeight;
                    return;
                }

                try
                {
                    ParentProgram.HotCornerClipLength = newLength;
                }
                catch (Exception e)
                {
                    barrierInput.BackColor = Color.IndianRed;
                    errorLabel.Text = e.Message;
                    return;
                }
                Close();
            }
            else
            {
                barrierInput.BackColor = Color.IndianRed;
                errorLabel.Text = "Value must be a positive integer";
            }
        }
    }
}
