using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Nucleus.Gaming;
using Nucleus.Gaming.Coop;

namespace Nucleus.Coop
{
    public class GameControl : UserControl, IDynamicSized, IRadioControl
    {
        public GenericGameInfo GameInfo { get; private set; }
        public UserGameInfo UserGameInfo { get; private set; }

        private PictureBox picture;
        private PictureBox playerIcon;
        private Label title;
        private Label players;
        private ToolTip numPlayersTt;
        public string TitleText { get; set; }
        public string PlayerText { get; set; }

        public GameControl(GenericGameInfo game, UserGameInfo userGame)
        {
            GameInfo = game;
            UserGameInfo = userGame;

            picture = new PictureBox();
            picture.SizeMode = PictureBoxSizeMode.StretchImage;

            playerIcon = new PictureBox();
            playerIcon.SizeMode = PictureBoxSizeMode.StretchImage;
            playerIcon.Image = Gaming.Properties.Resources.players;

            numPlayersTt = new ToolTip();
            numPlayersTt.SetToolTip(playerIcon, "Number of players");

            title = new Label();
            title.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            players = new Label();
            players.Font = new Font("Segoe UI", 9);
            if (game == null)
            {
                title.Text = "No games";
                players.Text = string.Empty;
                title.Font = new Font("Segoe UI", 11, FontStyle.Regular);
            }
            else
            {
                title.Text = GameInfo.GameName;
                if(GameInfo.MaxPlayers > 2)
                {
                    players.Text = "2-" + GameInfo.MaxPlayers;
                }
                else
                {
                    players.Text = GameInfo.MaxPlayers.ToString();
                }
                //players.Text = "Players: " + GameInfo.MaxPlayers;
            }
            TitleText = title.Text;
            PlayerText = players.Text;

            BackColor = Color.FromArgb(30, 30, 30);
            Size = new Size(200, 52);

            Controls.Add(picture);
            Controls.Add(title);
            Controls.Add(players);
            Controls.Add(playerIcon);

            DPIManager.Register(this);
        }
        ~GameControl()
        {
            DPIManager.Unregister(this);
        }

        public void UpdateSize(float scale)
        {
            if (IsDisposed)
            {
                DPIManager.Unregister(this);
                return;
            }

            SuspendLayout();

            int border = DPIManager.Adjust(4, scale);
            int dborder = border * 2;

            picture.Location = new Point(border, border);
            picture.Size = new Size(DPIManager.Adjust(44, scale), DPIManager.Adjust(44, scale));

            Height = DPIManager.Adjust(52, scale);

            Size labelSize = TextRenderer.MeasureText(TitleText, title.Font);
            Size plabelSize = TextRenderer.MeasureText(PlayerText, players.Font);
            float reservedSpaceLabel = this.Width - picture.Width;

            if (labelSize.Width > reservedSpaceLabel)
            {
                // make text smaller
                int charSize = TextRenderer.MeasureText("g", title.Font).Width;
                int toRemove = (int)((reservedSpaceLabel - labelSize.Width) / (float)charSize);
                toRemove = Math.Max(toRemove + 3, 7);
                title.Text = TitleText.Remove(TitleText.Length - toRemove, toRemove) + "...";
            }
            else
            {
                title.Text = TitleText;
            }
            players.Text = PlayerText;
            title.Size = labelSize;
            players.Size = plabelSize;

            float height = this.Height / 2.0f;
            float lheight = labelSize.Height / 2.0f;

            title.Location = new Point(picture.Width + picture.Left + border, (int)(height - labelSize.Height)/*(int)(height - lheight)*/);
            players.Location = new Point(picture.Width + picture.Left + border + playerIcon.Width + 10, (int)height);

            playerIcon.Location = new Point(picture.Width + picture.Left + border + 10, (int)height);
            playerIcon.Size = new Size(DPIManager.Adjust(players.Size.Height, scale), DPIManager.Adjust(players.Size.Height, scale));

            ResumeLayout();
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);

            Control c = e.Control;
            c.Click += C_Click;
            c.MouseEnter += C_MouseEnter;
            c.MouseLeave += C_MouseLeave;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateSize(DPIManager.Scale);
        }

        private void C_MouseEnter(object sender, EventArgs e)
        {
            OnMouseEnter(e);
        }

        private void C_MouseLeave(object sender, EventArgs e)
        {
            OnMouseLeave(e);
        }

        private void C_Click(object sender, EventArgs e)
        {
            OnClick(e);
        }

        public Image Image
        {
            get { return this.picture.Image; }
            set { this.picture.Image = value; }
        }

        public override string ToString()
        {
            return Text;
        }

        private bool isSelected;
        public void RadioSelected()
        {
            BackColor = Color.FromArgb(80, 80, 80);
            isSelected = true;
        }

        public void RadioUnselected()
        {
            BackColor = Color.FromArgb(30, 30, 30);
            isSelected = false;
        }

        public void UserOver()
        {
            BackColor = Color.FromArgb(60, 60, 60);
        }

        public void UserLeave()
        {
            if (isSelected)
            {
                BackColor = Color.FromArgb(80, 80, 80);
            }
            else
            {
                BackColor = Color.FromArgb(30, 30, 30);
            }
        }
    }
}
