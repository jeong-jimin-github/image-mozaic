namespace ImageMosaicEditor
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // Controls
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileMenu;
        private System.Windows.Forms.ToolStripMenuItem menuOpen;
        private System.Windows.Forms.ToolStripMenuItem menuSave;
        private System.Windows.Forms.ToolStripSeparator menuSeparator;
        private System.Windows.Forms.ToolStripMenuItem menuExit;
        private System.Windows.Forms.ToolStripMenuItem editMenu;
        private System.Windows.Forms.ToolStripMenuItem menuUndo;
        private System.Windows.Forms.ToolStripMenuItem menuRedo;
        private System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            // ── MenuStrip ──────────────────────────────────────────────────────
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.menuOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSave = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.menuExit = new System.Windows.Forms.ToolStripMenuItem();
            this.editMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.menuUndo = new System.Windows.Forms.ToolStripMenuItem();
            this.menuRedo = new System.Windows.Forms.ToolStripMenuItem();

            this.menuOpen.Text = "열기(&O)";
            this.menuOpen.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O;
            this.menuOpen.Click += new System.EventHandler(this.MenuOpen_Click);

            this.menuSave.Text = "저장(&S)";
            this.menuSave.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S;
            this.menuSave.Click += new System.EventHandler(this.MenuSave_Click);

            this.menuExit.Text = "종료(&X)";
            this.menuExit.Click += (s, e) => this.Close();

            this.fileMenu.Text = "파일(&F)";
            this.fileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[]
            {
                this.menuOpen,
                this.menuSave,
                this.menuSeparator,
                this.menuExit
            });

            this.menuStrip.Items.Add(this.fileMenu);
            this.menuStrip.Items.Add(this.editMenu);

            // ── Edit Menu ──────────────────────────────────────────────────────
            this.editMenu.Text = "편집(&E)";
            this.menuUndo.Text = "되돌리기(&U)";
            this.menuUndo.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z;
            this.menuUndo.Click += new System.EventHandler(this.MenuUndo_Click);

            this.menuRedo.Text = "다시 실행(&R)";
            this.menuRedo.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Y;
            this.menuRedo.Click += new System.EventHandler(this.MenuRedo_Click);

            this.editMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[]
            {
                this.menuUndo,
                this.menuRedo
            });

            // ── PictureBox ─────────────────────────────────────────────────────
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.pictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox.BackColor = System.Drawing.Color.DimGray;
            this.pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox.Cursor = System.Windows.Forms.Cursors.Cross;

            this.pictureBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PictureBox_MouseDown);
            this.pictureBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PictureBox_MouseMove);
            this.pictureBox.MouseUp   += new System.Windows.Forms.MouseEventHandler(this.PictureBox_MouseUp);
            this.pictureBox.Paint     += new System.Windows.Forms.PaintEventHandler(this.PictureBox_Paint);

            // ── StatusStrip ────────────────────────────────────────────────────
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusLabel.Text = "이미지를 열어 드래그로 모자이크 영역을 선택하세요.";
            this.statusStrip.Items.Add(this.statusLabel);

            // ── Form ───────────────────────────────────────────────────────────
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 650);
            this.MainMenuStrip = this.menuStrip;
            this.Text = "이미지 모자이크 편집기";
            this.MinimumSize = new System.Drawing.Size(400, 300);

            this.Controls.Add(this.pictureBox);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.menuStrip);
        }
    }
}
